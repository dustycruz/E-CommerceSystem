using ECommerceSystem.OrderService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var orderConnectionString = builder.Configuration.GetConnectionString("OrderDb")
    ?? throw new InvalidOperationException("Connection string 'OrderDb' is missing from appsettings.json.");

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(orderConnectionString));

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Creates ECommerceOrderDb and applies all migrations on a clean machine.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
}

app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    service = "ECommerceSystem.OrderService",
    status = "running"
}));

app.Run();