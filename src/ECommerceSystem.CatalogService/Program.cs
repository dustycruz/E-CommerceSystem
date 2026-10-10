using ECommerceSystem.CatalogService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var catalogConnectionString = builder.Configuration.GetConnectionString("CatalogDb")
    ?? throw new InvalidOperationException("Connection string 'CatalogDb' is missing from appsettings.json.");

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlServer(catalogConnectionString));

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Creates ECommerceCatalogDb and applies all migrations (including seed data) on a clean machine.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    db.Database.Migrate();
}

app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    service = "ECommerceSystem.CatalogService",
    status = "running"
}));

app.Run();