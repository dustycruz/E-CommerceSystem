using ECommerceSystem.CatalogService.Contracts;
using ECommerceSystem.CatalogService.Data;
using ECommerceSystem.CatalogService.Data.Entities;
using ECommerceSystem.Shared.Problems;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ECommerceSystem.CatalogService.Services;

public sealed class ProductService(CatalogDbContext db, ILogger<ProductService> logger) : IProductService
{
    public async Task<IReadOnlyList<ProductResponse>> ListAsync(string? category, bool includeInactive, CancellationToken cancellationToken)
    {
        var query = db.Products.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var trimmed = category.Trim();
            // SQL Server's default collation is case-insensitive, so "audio" matches "Audio".
            query = query.Where(p => p.Category == trimmed);
        }

        var products = await query
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "Listed {Count} products (category {Category}, includeInactive {IncludeInactive}).",
            products.Count,
            string.IsNullOrWhiteSpace(category) ? "(all)" : category,
            includeInactive);

        return products.Select(p => p.ToResponse()).ToList();
    }

    public async Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var product = await db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Product {id} was not found.");

        logger.LogInformation(
            "Returned product {ProductId} ({Sku}) price {Price} {Currency}, active {IsActive}.",
            product.Id,
            product.Sku,
            product.Price,
            ProductMappings.Currency,
            product.IsActive);

        return product.ToResponse();
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (await db.Products.AnyAsync(p => p.Sku == request.Sku, cancellationToken))
        {
            throw new ConflictException($"A product with SKU '{request.Sku}' already exists.");
        }

        var now = DateTime.UtcNow;
        var product = new Product
        {
            Sku = request.Sku,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Category = request.Category.Trim(),
            Price = decimal.Round(request.Price, 2),
            ImageUrl = request.ImageUrl ?? string.Empty,
            IsActive = request.IsActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Products.Add(product);
        await SaveChangesAsync(request.Sku, cancellationToken);

        logger.LogInformation("Created product {ProductId} ({Sku}).", product.Id, product.Sku);
        return product.ToResponse();
    }

    public async Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Product {id} was not found.");

        if (await db.Products.AnyAsync(p => p.Sku == request.Sku && p.Id != id, cancellationToken))
        {
            throw new ConflictException($"Another product already uses SKU '{request.Sku}'.");
        }

        product.Sku = request.Sku;
        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim() ?? string.Empty;
        product.Category = request.Category.Trim();
        product.Price = decimal.Round(request.Price, 2);
        product.ImageUrl = request.ImageUrl ?? string.Empty;
        product.IsActive = request.IsActive;
        product.UpdatedAtUtc = DateTime.UtcNow;

        await SaveChangesAsync(request.Sku, cancellationToken);

        logger.LogInformation("Updated product {ProductId} ({Sku}), active {IsActive}.", product.Id, product.Sku, product.IsActive);
        return product.ToResponse();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Product {id} was not found.");

        db.Products.Remove(product);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted product {ProductId} ({Sku}).", product.Id, product.Sku);
    }

    /// <summary>Turns a unique-index race (two requests with the same SKU) into a clean 409.</summary>
    private async Task SaveChangesAsync(string sku, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException($"A product with SKU '{sku}' already exists.", ex);
        }
    }
}