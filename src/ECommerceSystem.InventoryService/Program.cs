using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using ECommerceSystem.InventoryService.Consumers;
using ECommerceSystem.InventoryService.Data;
using ECommerceSystem.InventoryService.Services;
using ECommerceSystem.InventoryService.Swagger;
using ECommerceSystem.Shared.Correlation;
using ECommerceSystem.Shared.Messaging;
using ECommerceSystem.Shared.Problems;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database (ECommerceInventoryDb) ----------
var inventoryConnectionString = builder.Configuration.GetConnectionString("InventoryDb")
    ?? throw new InvalidOperationException("Connection string 'InventoryDb' is missing from appsettings.json.");

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(inventoryConnectionString));

// ---------- Cross-cutting (Shared) ----------
builder.Services.AddECommerceCorrelation();
builder.Services.AddECommerceProblemDetails();
builder.Services.AddRabbitMqMessaging(builder.Configuration);

// ---------- Application services ----------
builder.Services.AddScoped<IStockItemService, StockItemService>();
builder.Services.AddScoped<InventoryReservationService>();

// ---------- RabbitMQ consumers (hosted background services) ----------
builder.Services.AddHostedService<OrderPlacedConsumer>();
builder.Services.AddHostedService<ReleaseStockConsumer>();

// ---------- MVC + JSON ----------
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---------- API versioning: /inventory/v{version} ----------
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

// ---------- Swagger (Swashbuckle 10) ----------
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Creates ECommerceInventoryDb and applies all migrations (including seed stock) on a clean machine.
    // Runs before the hosted consumers start, so they always see an up-to-date schema.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    db.Database.Migrate();
}

// ---------- HTTP pipeline ----------
app.UseECommerceCorrelation();
app.UseECommerceProblemDetails();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerEndpoint(
            $"/swagger/{description.GroupName}/swagger.json",
            $"Inventory API {description.GroupName.ToUpperInvariant()}");
    }

    options.DocumentTitle = "ECommerceSystem Inventory API";
});

app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    service = "ECommerceSystem.InventoryService",
    status = "running",
    swagger = "/swagger"
}))
.ExcludeFromDescription();

app.Run();