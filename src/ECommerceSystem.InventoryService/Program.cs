using ECommerceSystem.InventoryService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var inventoryConnectionString = builder.Configuration.GetConnectionString("InventoryDb")
    ?? throw new InvalidOperationException("Connection string 'InventoryDb' is missing from appsettings.json.");

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(inventoryConnectionString));

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Creates ECommerceInventoryDb and applies all migrations (including seed stock) on a clean machine.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    db.Database.Migrate();
}

app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    service = "ECommerceSystem.InventoryService",
    status = "running"
}));

app.Run();