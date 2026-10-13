using ECommerceSystem.Shared.Correlation;
using ECommerceSystem.Shared.Resilience;
using ECommerceSystem.Storefront.Clients.Catalog;
using ECommerceSystem.Storefront.Clients.Catalog.Generated;
using ECommerceSystem.Storefront.Clients.Inventory;
using ECommerceSystem.Storefront.Clients.Orders;
using ECommerceSystem.Storefront.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Http.Resilience;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuration ----------
string RequiredSetting(string key) =>
    builder.Configuration[key] ?? throw new InvalidOperationException($"{key} is missing from appsettings.json.");

var catalogBaseUrl = RequiredSetting("Services:CatalogBaseUrl");
var orderBaseUrl = RequiredSetting("Services:OrderBaseUrl");
var inventoryBaseUrl = RequiredSetting("Services:InventoryBaseUrl");
RequiredSetting("Services:PaymentBaseUrl");

// ---------- Correlation: the Storefront is the edge, it creates X-Correlation-ID ----------
builder.Services.AddECommerceCorrelation();

// ---------- MVC (anti-forgery on every POST) + server-side session cart ----------
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".ECommerceSystem.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(2);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<CartSummaryBuilder>();

// ---------- Catalog: NSwag-generated client (contracts\catalog.yaml) via IHttpClientFactory ----------
builder.Services
    .AddHttpClient<ICatalogApiClient, CatalogApiClient>(client => client.BaseAddress = new Uri(catalogBaseUrl))
    .AddECommerceHttpPipeline();
builder.Services.AddScoped<ICatalogStore, CatalogStore>();

// ---------- Inventory: stock hints on product cards ----------
builder.Services
    .AddHttpClient<IInventoryClient, InventoryClient>(client => client.BaseAddress = new Uri(inventoryBaseUrl))
    .AddECommerceHttpPipeline();

// ---------- Orders: own pipeline - placing an order is NOT idempotent, so it is never retried ----------
builder.Services
    .AddHttpClient<IOrderApiClient, OrderApiClient>(client => client.BaseAddress = new Uri(orderBaseUrl))
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
    .AddStandardResilienceHandler(options =>
    {
        // Retry GETs only: retrying POST /orders after a slow-but-successful attempt would place a second order.
        options.Retry.DisableForUnsafeHttpMethods();
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;
        options.Retry.Delay = TimeSpan.FromMilliseconds(500);
        options.Retry.ShouldRetryAfterHeader = false;

        // The Order Service may spend ~10 s retrying the Catalog before it answers 503 itself.
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    });

// ---------- Service status page: one quick attempt per service, no retries ----------
builder.Services
    .AddHttpClient(ServiceStatusChecker.ClientName, client => client.Timeout = TimeSpan.FromSeconds(2))
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
builder.Services.AddScoped<ServiceStatusChecker>();

var app = builder.Build();

// ---------- HTTP pipeline ----------
app.UseECommerceCorrelation();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();