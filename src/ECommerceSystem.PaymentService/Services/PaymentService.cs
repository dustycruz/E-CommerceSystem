using ECommerceSystem.PaymentService.Contracts;
using ECommerceSystem.PaymentService.Data;
using ECommerceSystem.PaymentService.Data.Entities;
using ECommerceSystem.Shared.Correlation;
using ECommerceSystem.Shared.Problems;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ECommerceSystem.PaymentService.Services;

public sealed class PaymentService(
    PaymentDbContext db,
    ICorrelationIdAccessor correlationIdAccessor,
    ILogger<PaymentService> logger) : IPaymentService
{
    private const int MaxReasonLength = 500;

    // ------------------------------------------------------------------ queries

    public async Task<IReadOnlyList<PaymentResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var payments = await db.Payments
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        return payments.Select(p => p.ToResponse()).ToList();
    }

    public async Task<PaymentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Payment {id} was not found.");

        return payment.ToResponse();
    }

    public async Task<PaymentResponse> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken)
            ?? throw new NotFoundException($"No payment exists for order {orderId}.");

        return payment.ToResponse();
    }

    // ------------------------------------------------------------------ charge (idempotent by OrderId)

    public async Task<ChargeResult> ChargeAsync(ChargePaymentRequest request, CancellationToken cancellationToken)
    {
        var amount = decimal.Round(request.Amount, 2);
        var cardLast4 = request.CardNumber[^4..];

        logger.LogInformation(
            "Charge requested for order {OrderId}: {Amount} {Currency} on card ****{CardLast4}.",
            request.OrderId,
            amount,
            request.Currency,
            cardLast4);

        var existing = await db.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.OrderId == request.OrderId, cancellationToken);

        if (existing is not null)
        {
            return Replay(existing, amount, request.Currency);
        }

        var (status, failureReason) = TestCardGateway.Authorize(request.CardNumber);
        var now = DateTime.UtcNow;

        var payment = new Payment
        {
            Id = Guid.CreateVersion7(),
            OrderId = request.OrderId,
            Amount = amount,
            Currency = request.Currency,
            CardLast4 = cardLast4,
            Status = status,
            FailureReason = failureReason,
            CorrelationId = correlationIdAccessor.CorrelationId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Payments.Add(payment);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            // Two charges for the same order raced; the unique OrderId index let only one win.
            db.ChangeTracker.Clear();
            var winner = await db.Payments.AsNoTracking().FirstAsync(p => p.OrderId == request.OrderId, cancellationToken);
            logger.LogInformation("Concurrent charge for order {OrderId} detected; returning the payment that won.", request.OrderId);
            return Replay(winner, amount, request.Currency);
        }

        if (payment.Status == PaymentStatus.Charged)
        {
            logger.LogInformation(
                "Payment {PaymentId} CHARGED for order {OrderId}: {Amount} {Currency} (card ****{CardLast4}).",
                payment.Id,
                payment.OrderId,
                payment.Amount,
                payment.Currency,
                payment.CardLast4);
        }
        else
        {
            logger.LogWarning(
                "Payment {PaymentId} DECLINED for order {OrderId}: {Reason}",
                payment.Id,
                payment.OrderId,
                payment.FailureReason);
        }

        return new ChargeResult(payment.ToResponse(), Created: true);
    }

    private ChargeResult Replay(Payment existing, decimal amount, string currency)
    {
        if (existing.Amount != amount || !string.Equals(existing.Currency, currency, StringComparison.Ordinal))
        {
            throw new ConflictException(
                $"Order {existing.OrderId} already has payment {existing.Id} for {existing.Amount} {existing.Currency}; " +
                $"a charge for {amount} {currency} is rejected (idempotency key reused with a different amount).");
        }

        logger.LogInformation(
            "Idempotent replay: order {OrderId} already has payment {PaymentId} ({Status}). No second charge.",
            existing.OrderId,
            existing.Id,
            existing.Status);

        return new ChargeResult(existing.ToResponse(), Created: false);
    }

    // ------------------------------------------------------------------ refund (idempotent)

    public async Task<PaymentResponse> RefundAsync(Guid id, RefundPaymentRequest request, CancellationToken cancellationToken)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Payment {id} was not found.");

        switch (payment.Status)
        {
            case PaymentStatus.Refunded:
                logger.LogInformation("Payment {PaymentId} is already refunded; refund is idempotent, nothing changed.", payment.Id);
                return payment.ToResponse();

            case PaymentStatus.Declined:
                throw new ConflictException($"Payment {id} was declined, so no money was taken and it cannot be refunded.");
        }

        var now = DateTime.UtcNow;
        payment.Status = PaymentStatus.Refunded;
        payment.RefundedAtUtc = now;
        payment.UpdatedAtUtc = now;
        payment.FailureReason = Truncate(
            string.IsNullOrWhiteSpace(request.Reason) ? "Refunded." : $"Refunded: {request.Reason.Trim()}");

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another refund changed the row first (rowversion). If it ended Refunded, the result is the same.
            db.ChangeTracker.Clear();
            var current = await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
                ?? throw new NotFoundException($"Payment {id} was not found.");

            if (current.Status == PaymentStatus.Refunded)
            {
                return current.ToResponse();
            }

            throw new ConflictException($"Payment {id} was changed concurrently. Retry the refund.");
        }

        logger.LogWarning(
            "Payment {PaymentId} REFUNDED for order {OrderId}: {Amount} {Currency}. Reason: {Reason}",
            payment.Id,
            payment.OrderId,
            payment.Amount,
            payment.Currency,
            payment.FailureReason);

        return payment.ToResponse();
    }

    // ------------------------------------------------------------------ admin CRUD

    public async Task<PaymentResponse> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        if (await db.Payments.AnyAsync(p => p.OrderId == request.OrderId, cancellationToken))
        {
            throw new ConflictException($"Order {request.OrderId} already has a payment (OrderId is the idempotency key).");
        }

        var status = request.Status!.Value;
        var now = DateTime.UtcNow;

        var payment = new Payment
        {
            Id = Guid.CreateVersion7(),
            OrderId = request.OrderId,
            Amount = decimal.Round(request.Amount, 2),
            Currency = request.Currency,
            CardLast4 = request.CardLast4,
            Status = status,
            FailureReason = request.FailureReason is null ? null : Truncate(request.FailureReason.Trim()),
            CorrelationId = correlationIdAccessor.CorrelationId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            RefundedAtUtc = status == PaymentStatus.Refunded ? now : null
        };

        db.Payments.Add(payment);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            throw new ConflictException($"Order {request.OrderId} already has a payment.", ex);
        }

        logger.LogInformation("Created payment record {PaymentId} for order {OrderId} with status {Status} (admin).",
            payment.Id, payment.OrderId, payment.Status);

        return payment.ToResponse();
    }

    public async Task<PaymentResponse> UpdateAsync(Guid id, UpdatePaymentRequest request, CancellationToken cancellationToken)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Payment {id} was not found.");

        if (payment.Status != PaymentStatus.Declined)
        {
            throw new ConflictException(
                $"Payment {id} is {payment.Status}. Only Declined payments can be corrected; charged and refunded payments are immutable.");
        }

        payment.Amount = decimal.Round(request.Amount, 2);
        payment.CardLast4 = request.CardLast4;
        payment.FailureReason = request.FailureReason is null ? null : Truncate(request.FailureReason.Trim());
        payment.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException($"Payment {id} was changed by someone else. Reload it and try again.", ex);
        }

        logger.LogInformation("Corrected declined payment {PaymentId} (admin).", payment.Id);
        return payment.ToResponse();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Payment {id} was not found.");

        if (payment.Status == PaymentStatus.Charged)
        {
            throw new ConflictException($"Payment {id} is Charged. Refund it before deleting the record.");
        }

        db.Payments.Remove(payment);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted payment record {PaymentId} ({Status}) for order {OrderId}.",
            payment.Id, payment.Status, payment.OrderId);
    }

    // ------------------------------------------------------------------ helpers

    private static string Truncate(string value) =>
        value.Length <= MaxReasonLength ? value : value[..MaxReasonLength];

    private static bool IsDuplicateKey(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}