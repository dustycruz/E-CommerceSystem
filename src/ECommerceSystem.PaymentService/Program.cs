using System.Text.Json.Serialization;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using ECommerceSystem.PaymentService.Data;
using ECommerceSystem.PaymentService.Services;
using ECommerceSystem.PaymentService.Swagger;
using ECommerceSystem.Shared.Correlation;
using ECommerceSystem.Shared.Problems;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database (ECommercePaymentDb) ----------
var paymentConnectionString = builder.Configuration.GetConnectionString("PaymentDb")
    ?? throw new InvalidOperationException("Connection string 'PaymentDb' is missing from appsettings.json.");

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(paymentConnectionString));

// ---------- Cross-cutting (Shared) ----------
builder.Services.AddECommerceCorrelation();
builder.Services.AddECommerceProblemDetails();

// ---------- Application services ----------
builder.Services.AddScoped<IPaymentService, PaymentService>();

// ---------- MVC + JSON ----------
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---------- API versioning: /payments/v{version} ----------
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
    // Creates ECommercePaymentDb and applies all migrations on a clean machine.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
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
            $"Payments API {description.GroupName.ToUpperInvariant()}");
    }

    options.DocumentTitle = "ECommerceSystem Payments API";
});

app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    service = "ECommerceSystem.PaymentService",
    status = "running",
    swagger = "/swagger"
}))
.ExcludeFromDescription();

app.Run();