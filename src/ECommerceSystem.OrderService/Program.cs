using System.Text.Json.Serialization;
using ECommerceSystem.OrderService.Clients.Catalog;
using ECommerceSystem.OrderService.Clients.Catalog.Generated;
using ECommerceSystem.OrderService.Clients.Payments;
using ECommerceSystem.OrderService.Data;
using ECommerceSystem.Shared.Correlation;
using ECommerceSystem.Shared.Problems;
using ECommerceSystem.Shared.Resilience;
using Microsoft.EntityFrameworkCore;

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

// ---------- Typed HttpClients via IHttpClientFactory (never "new HttpClient()") ----------
// NSwag-generated Catalog client: base URL from configuration, correlation + resilience pipeline.
builder.Services
    .AddHttpClient<ICatalogApiClient, CatalogApiClient>(client => client.BaseAddress = new Uri(catalogBaseUrl))
    .AddECommerceHttpPipeline();
builder.Services.AddScoped<ICatalogGateway, CatalogGateway>();

// Hand-written Payment client: same pipeline.
builder.Services
    .AddHttpClient<IPaymentClient, PaymentClient>(client => client.BaseAddress = new Uri(paymentBaseUrl))
    .AddECommerceHttpPipeline();

// ---------- MVC + JSON (enums as strings everywhere) ----------
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Creates ECommerceOrderDb and applies all migrations on a clean machine.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
}

// ---------- HTTP pipeline ----------
app.UseECommerceCorrelation();
app.UseECommerceProblemDetails();

app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    service = "ECommerceSystem.OrderService",
    status = "running"
}))
.ExcludeFromDescription();

if (app.Environment.IsDevelopment())
{
    // Development-only diagnostics to test the outbound clients (hidden from Swagger, absent in Production).
    app.MapGet("/_diag/catalog/{productId:int}", async (int productId, ICatalogGateway catalogGateway, CancellationToken cancellationToken) =>
            Results.Ok(await catalogGateway.GetProductAsync(productId, cancellationToken)))
        .ExcludeFromDescription();

    app.MapPost("/_diag/payment/charge", async (PaymentChargeRequest request, IPaymentClient paymentClient, CancellationToken cancellationToken) =>
            Results.Ok(await paymentClient.ChargeAsync(request, cancellationToken)))
        .ExcludeFromDescription();
}

app.Run();