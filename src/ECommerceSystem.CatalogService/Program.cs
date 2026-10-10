using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using ECommerceSystem.CatalogService.Chaos;
using ECommerceSystem.CatalogService.Data;
using ECommerceSystem.CatalogService.Services;
using ECommerceSystem.CatalogService.Swagger;
using ECommerceSystem.Shared.Correlation;
using ECommerceSystem.Shared.Problems;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database (ECommerceCatalogDb) ----------
var catalogConnectionString = builder.Configuration.GetConnectionString("CatalogDb")
    ?? throw new InvalidOperationException("Connection string 'CatalogDb' is missing from appsettings.json.");

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlServer(catalogConnectionString));

// ---------- Cross-cutting (Shared) ----------
builder.Services.AddECommerceCorrelation();
builder.Services.AddECommerceProblemDetails();

// ---------- Application services ----------
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddSingleton<ChaosState>();

// ---------- MVC + JSON ----------
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---------- API versioning: /catalog/v{version} ----------
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
    // Creates ECommerceCatalogDb and applies all migrations (including seed data) on a clean machine.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    db.Database.Migrate();
}

// ---------- HTTP pipeline ----------
app.UseECommerceCorrelation();      // 1. X-Correlation-ID + log scope (first!)
app.UseECommerceProblemDetails();   // 2. exceptions and empty error responses -> Problem Details

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerEndpoint(
            $"/swagger/{description.GroupName}/swagger.json",
            $"Catalog API {description.GroupName.ToUpperInvariant()}");
    }

    options.DocumentTitle = "ECommerceSystem Catalog API";
});

app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    service = "ECommerceSystem.CatalogService",
    status = "running",
    swagger = "/swagger"
}))
.ExcludeFromDescription();

app.Run();