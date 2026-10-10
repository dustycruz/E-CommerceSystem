using ECommerceSystem.PaymentService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerceSystem.PaymentService.Data;

/// <summary>
/// EF Core context for ECommercePaymentDb. Only the Payment Service uses this database.
/// </summary>
public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);
    }
}