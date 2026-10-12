using ECommerceSystem.InventoryService.Contracts;
using ECommerceSystem.InventoryService.Data;
using ECommerceSystem.InventoryService.Data.Entities;
using ECommerceSystem.Shared.Problems;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ECommerceSystem.InventoryService.Services;

public sealed class StockItemService(InventoryDbContext db, ILogger<StockItemService> logger) : IStockItemService
{
    public async Task<IReadOnlyList<StockItemResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var items = await db.StockItems
            .AsNoTracking()
            .OrderBy(s => s.ProductId)
            .ToListAsync(cancellationToken);

        return items.Select(s => s.ToResponse()).ToList();
    }

    public async Task<StockItemResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var item = await db.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Stock item {id} was not found.");

        return item.ToResponse();
    }

    public async Task<StockItemResponse> GetByProductIdAsync(int productId, CancellationToken cancellationToken)
    {
        var item = await db.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.ProductId == productId, cancellationToken)
            ?? throw new NotFoundException($"No stock item exists for product {productId}.");

        return item.ToResponse();
    }

    public async Task<StockItemResponse> CreateAsync(CreateStockItemRequest request, CancellationToken cancellationToken)
    {
        if (await db.StockItems.AnyAsync(s => s.ProductId == request.ProductId, cancellationToken))
        {
            throw new ConflictException($"A stock item for product {request.ProductId} already exists.");
        }

        if (await db.StockItems.AnyAsync(s => s.Sku == request.Sku, cancellationToken))
        {
            throw new ConflictException($"A stock item with SKU '{request.Sku}' already exists.");
        }

        var item = new StockItem
        {
            ProductId = request.ProductId,
            Sku = request.Sku,
            QuantityOnHand = request.QuantityOnHand,
            QuantityReserved = 0,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.StockItems.Add(item);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException($"A stock item for product {request.ProductId} or SKU '{request.Sku}' already exists.", ex);
        }

        logger.LogInformation("Created stock item {StockItemId} for product {ProductId} ({Sku}) with {Quantity} unit(s).",
            item.Id, item.ProductId, item.Sku, item.QuantityOnHand);

        return item.ToResponse();
    }

    public async Task<StockItemResponse> UpdateAsync(int id, UpdateStockItemRequest request, CancellationToken cancellationToken)
    {
        var item = await db.StockItems.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Stock item {id} was not found.");

        if (request.RowVersion is { Length: > 0 })
        {
            // EF puts the ORIGINAL rowversion in the UPDATE's WHERE clause: a stale value updates 0 rows -> concurrency exception.
            db.Entry(item).Property(s => s.RowVersion).OriginalValue = request.RowVersion;
        }

        if (await db.StockItems.AnyAsync(s => s.Sku == request.Sku && s.Id != id, cancellationToken))
        {
            throw new ConflictException($"Another stock item already uses SKU '{request.Sku}'.");
        }

        if (request.QuantityOnHand < item.QuantityReserved)
        {
            throw new ConflictException(
                $"QuantityOnHand cannot be {request.QuantityOnHand}: {item.QuantityReserved} unit(s) are reserved by in-flight orders.");
        }

        item.Sku = request.Sku;
        item.QuantityOnHand = request.QuantityOnHand;
        item.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException(
                $"Stock item {id} was changed by someone else (stale rowVersion). Reload it and try again.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException($"Another stock item already uses SKU '{request.Sku}'.", ex);
        }

        logger.LogInformation("Updated stock item {StockItemId} ({Sku}): on hand {OnHand}, reserved {Reserved}.",
            item.Id, item.Sku, item.QuantityOnHand, item.QuantityReserved);

        return item.ToResponse();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var item = await db.StockItems.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Stock item {id} was not found.");

        var hasActiveReservations = await db.StockReservations
            .AnyAsync(r => r.ProductId == item.ProductId && r.Status == ReservationStatus.Reserved, cancellationToken);

        if (hasActiveReservations)
        {
            throw new ConflictException(
                $"Stock item {id} cannot be deleted while product {item.ProductId} has active reservations.");
        }

        db.StockItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted stock item {StockItemId} for product {ProductId}.", item.Id, item.ProductId);
    }

    public async Task<IReadOnlyList<ReservationResponse>> ListReservationsAsync(Guid? orderId, CancellationToken cancellationToken)
    {
        var query = db.StockReservations.AsNoTracking();

        if (orderId is not null)
        {
            query = query.Where(r => r.OrderId == orderId.Value);
        }

        var reservations = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .ThenBy(r => r.Id)
            .Take(200)
            .ToListAsync(cancellationToken);

        return reservations.Select(r => r.ToResponse()).ToList();
    }
}