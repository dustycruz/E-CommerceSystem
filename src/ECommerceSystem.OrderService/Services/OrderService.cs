using ECommerceSystem.OrderService.Clients.Catalog;
using ECommerceSystem.OrderService.Contracts;
using ECommerceSystem.OrderService.Data;
using ECommerceSystem.OrderService.Data.Entities;
using ECommerceSystem.OrderService.Outbox;
using ECommerceSystem.Shared.Correlation;
using ECommerceSystem.Shared.Events;
using ECommerceSystem.Shared.Problems;
using Microsoft.EntityFrameworkCore;

namespace ECommerceSystem.OrderService.Services;

public sealed class OrderService(
    OrderDbContext db,
    ICatalogGateway catalogGateway,
    ICorrelationIdAccessor correlationIdAccessor,
    ILogger<OrderService> logger) : IOrderService
{
    private const int MaxQuantityPerProduct = 100;
    private const string Currency = "PHP";

    // ------------------------------------------------------------------ place order (saga step 1-3)

    public async Task<PlaceOrderResult> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var correlationId = correlationIdAccessor.CorrelationId ?? CorrelationIdGenerator.NewId();

        // Merge duplicate lines of the same product; remember the first request index for error keys.
        var firstIndexByProduct = new Dictionary<int, int>();
        var quantities = new Dictionary<int, int>();
        for (var index = 0; index < request.Items.Count; index++)
        {
            var item = request.Items[index];
            firstIndexByProduct.TryAdd(item.ProductId, index);
            quantities[item.ProductId] = quantities.GetValueOrDefault(item.ProductId) + item.Quantity;
        }

        var errors = new Dictionary<string, string[]>();
        foreach (var (productId, quantity) in quantities)
        {
            if (quantity > MaxQuantityPerProduct)
            {
                errors[$"Items[{firstIndexByProduct[productId]}].Quantity"] =
                    [$"At most {MaxQuantityPerProduct} units of product {productId} can be ordered at once (requested {quantity})."];
            }
        }

        if (errors.Count > 0)
        {
            return PlaceOrderResult.Invalid(errors);
        }

        logger.LogInformation(
            "Placing order for {CustomerEmail}: {ProductCount} product(s). Validating every line with the Catalog Service.",
            request.CustomerEmail,
            quantities.Count);

        // Synchronous validation through the NSwag-generated client (wrapped by CatalogGateway).
        var products = new Dictionary<int, CatalogProduct>();
        var inactive = new List<string>();

        foreach (var productId in quantities.Keys)
        {
            var lookup = await catalogGateway.GetProductAsync(productId, cancellationToken);

            switch (lookup.Status)
            {
                case CatalogLookupStatus.Found:
                    products[productId] = lookup.Product!;
                    break;

                case CatalogLookupStatus.NotFound:
                    errors[$"Items[{firstIndexByProduct[productId]}].ProductId"] = [lookup.Error ?? $"Product {productId} does not exist."];
                    break;

                case CatalogLookupStatus.Inactive:
                    inactive.Add(lookup.Error ?? $"Product {productId} is not available for sale.");
                    break;

                case CatalogLookupStatus.Unavailable:
                    // Nothing has been saved yet: no partial state. Clear 503 Problem Details for the caller.
                    throw new ServiceUnavailableException(
                        $"The order was not placed: {lookup.Error} Please try again in a moment.");
            }
        }

        if (errors.Count > 0)
        {
            logger.LogWarning("Order rejected: {ErrorCount} unknown product(s).", errors.Count);
            return PlaceOrderResult.Invalid(errors);
        }

        if (inactive.Count > 0)
        {
            throw new UnprocessableEntityException(string.Join(" ", inactive));
        }

        // Build the order with Catalog prices (never browser prices).
        var now = DateTime.UtcNow;
        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            CustomerName = request.CustomerName.Trim(),
            CustomerEmail = request.CustomerEmail.Trim(),
            Status = OrderStatus.Pending,
            Currency = Currency,
            CardLast4 = request.CardNumber[^4..],
            PaymentToken = request.CardNumber,
            CorrelationId = correlationId,
            SimulateFailureAfterPayment = request.SimulateFailureAfterPayment,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        foreach (var (productId, quantity) in quantities)
        {
            var product = products[productId];
            order.Items.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = productId,
                Sku = product.Sku,
                ProductName = product.Name.Length <= 120 ? product.Name : product.Name[..120],
                UnitPrice = product.Price,
                Quantity = quantity,
                LineTotal = decimal.Round(product.Price * quantity, 2)
            });
        }

        order.TotalAmount = order.Items.Sum(i => i.LineTotal);

        var placedDescription =
            $"Order placed: {order.Items.Count} line(s), total {order.TotalAmount:0.00} {Currency}. Prices confirmed by the Catalog Service. Waiting for stock reservation.";
        if (order.SimulateFailureAfterPayment)
        {
            placedDescription += " (Demo: failure after payment will be simulated.)";
        }

        order.AddHistory(OrderStatus.Pending, placedDescription, now);

        var orderPlaced = new OrderPlaced(
            order.Id,
            order.CustomerEmail,
            order.TotalAmount,
            order.Currency,
            order.Items.Select(i => new OrderLine(i.ProductId, i.Sku, i.Quantity)).ToList())
        {
            CorrelationId = correlationId,
            OccurredAtUtc = now
        };

        // ONE local transaction: Order + OrderItems + OrderStatusHistory + OutboxMessage(OrderPlaced).
        db.Orders.Add(order);
        var outboxMessage = OutboxWriter.Add(db, orderPlaced, EventRoutingKeys.OrderPlaced);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Order {OrderId} accepted as Pending: total {TotalAmount} {Currency}. OrderPlaced {MessageId} saved to the outbox in the same transaction.",
            order.Id,
            order.TotalAmount,
            order.Currency,
            outboxMessage.Id);

        return PlaceOrderResult.Accepted(order.ToResponse());
    }

    // ------------------------------------------------------------------ queries

    public async Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Order {id} was not found.");

        return order.ToResponse();
    }

    public async Task<IReadOnlyList<OrderSummaryResponse>> ListAsync(string? customerEmail, CancellationToken cancellationToken)
    {
        var query = db.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(customerEmail))
        {
            var email = customerEmail.Trim();
            // SQL Server's default collation is case-insensitive.
            query = query.Where(o => o.CustomerEmail == email);
        }

        var rows = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(100)
            .Select(o => new
            {
                o.Id,
                o.CustomerName,
                o.CustomerEmail,
                o.Status,
                o.TotalAmount,
                o.Currency,
                ItemCount = o.Items.Count,
                o.FailureReason,
                o.CreatedAtUtc,
                o.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new OrderSummaryResponse(
                r.Id,
                r.CustomerName,
                r.CustomerEmail,
                r.Status,
                r.TotalAmount,
                r.Currency,
                r.ItemCount,
                r.FailureReason,
                OrderMappings.Utc(r.CreatedAtUtc),
                OrderMappings.Utc(r.UpdatedAtUtc)))
            .ToList();
    }

    // ------------------------------------------------------------------ update / delete

    public async Task<OrderResponse> UpdateAsync(Guid id, UpdateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Order {id} was not found.");

        if (order.Status != OrderStatus.Pending)
        {
            throw new ConflictException(
                $"Order {id} is {order.Status}. Customer details can only be changed while the order is Pending.");
        }

        var now = DateTime.UtcNow;
        order.CustomerName = request.CustomerName.Trim();
        order.CustomerEmail = request.CustomerEmail.Trim();
        order.UpdatedAtUtc = now;
        order.AddHistory(OrderStatus.Pending, "Customer details updated.", now);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException($"Order {id} was changed by the saga at the same time. Reload it and try again.", ex);
        }

        logger.LogInformation("Order {OrderId} customer details updated.", order.Id);
        return order.ToResponse();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Order {id} was not found.");

        if (order.Status is not (OrderStatus.Confirmed or OrderStatus.Cancelled))
        {
            throw new ConflictException(
                $"Order {id} is {order.Status}; its saga is still running. Only Confirmed or Cancelled orders can be deleted.");
        }

        // Items and history are removed by the database (ON DELETE CASCADE).
        db.Orders.Remove(order);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException($"Order {id} was changed at the same time. Reload it and try again.", ex);
        }

        logger.LogInformation("Deleted order {OrderId} ({Status}).", order.Id, order.Status);
    }
}