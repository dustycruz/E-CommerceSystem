using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using ECommerceSystem.OrderService.Clients.Catalog;
using ECommerceSystem.OrderService.Clients.Catalog.Generated;
using ECommerceSystem.OrderService.Clients.Payments;
using ECommerceSystem.OrderService.Consumers;
using ECommerceSystem.OrderService.Data;
using ECommerceSystem.OrderService.Outbox;
using ECommerceSystem.OrderService.Saga;
using ECommerceSystem.OrderService.Services;
using ECommerceSystem.OrderService.Swagger;
using ECommerceSystem.Shared.Correlation;
using ECommerceSystem.Shared.Messaging;
using ECommerceSystem.Shared.Problems;
using ECommerceSystem.Shared.Resilience;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuration ----------
var orderConnectionString = builder.Configuration.GetConnectionString("OrderDb")
    ?? throw new InvalidOperationException("Connection string 'OrderDb' is missing from appsettings.json.");
var catalogBaseUrl = builder.Configuration["Services:CatalogBaseUrl"]
    ?? throw new InvalidOperationException("Services:CatalogBaseUrl is missing from appsettings.json.");
var paymentBaseUrl = builder.Configuration["Services:PaymentBaseUrl"]
    ?? throw new InvalidOperationException("Services:PaymentBaseUrl is missing from appsettings.json.");

// ---------- Database (ECommerceOrderDb) ----------
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(orderConnectionString));

// ---------- Cross-cutting (Shared) ----------
builder.Services.AddECommerceCorrelation();
builder.Services.AddECommerceProblemDetails();
builder.Services.AddRabbitMqMessaging(builder.Configuration);

// ---------- Typed HttpClients via IHttpClientFactory (never "new HttpClient()") ----------
builder.Services
    .AddHttpClient<ICatalogApiClient, CatalogApiClient>(client => client.BaseAddress = new Uri(catalogBaseUrl))
    .AddECommerceHttpPipeline();
builder.Services.AddScoped<ICatalogGateway, CatalogGateway>();

builder.Services
    .AddHttpClient<IPaymentClient, PaymentClient>(client => client.BaseAddress = new Uri(paymentBaseUrl))
    .AddECommerceHttpPipeline();

// ---------- Application services ----------
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<OrderSagaOrchestrator>();

// ---------- Background services: transactional outbox publisher + saga consumer ----------
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddHostedService<InventoryResultsConsumer>();

// ---------- MVC + JSON (enums as strings everywhere) ----------
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---------- API versioning: /orders/v{version} ----------
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
    // Creates ECommerceOrderDb and applies all migrations on a clean machine (before the background services start).
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
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
            $"Orders API {description.GroupName.ToUpperInvariant()}");
    }

    options.DocumentTitle = "ECommerceSystem Orders API";
});

app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    service = "ECommerceSystem.OrderService",
    status = "running",
    swagger = "/swagger"
}))
.ExcludeFromDescription();

if (app.Environment.IsDevelopment())
{
    // Development-only diagnostics for the outbound clients (hidden from Swagger, absent in Production).
    app.MapGet("/_diag/catalog/{productId:int}", async (int productId, ICatalogGateway catalogGateway, CancellationToken cancellationToken) =>
            Results.Ok(await catalogGateway.GetProductAsync(productId, cancellationToken)))
        .ExcludeFromDescription();

    app.MapPost("/_diag/payment/charge", async (PaymentChargeRequest request, IPaymentClient paymentClient, CancellationToken cancellationToken) =>
            Results.Ok(await paymentClient.ChargeAsync(request, cancellationToken)))
        .ExcludeFromDescription();
}

app.Run();