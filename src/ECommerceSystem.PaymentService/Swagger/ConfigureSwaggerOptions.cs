using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECommerceSystem.PaymentService.Swagger;

/// <summary>One Swagger document per API version (Microsoft.OpenApi 2.x types).</summary>
public sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "ECommerceSystem Payments API",
                Version = description.ApiVersion.ToString(),
                Description = "Payment Service. Idempotent charge (OrderId = idempotency key) and refund. Contract: contracts payments.yaml.",
                Contact = new OpenApiContact { Name = "CJ Bello (CJ-Bello)" }
            });
        }

        options.MapType<decimal>(() => new OpenApiSchema { Type = JsonSchemaType.Number, Format = "decimal" });
        options.SupportNonNullableReferenceTypes();
    }
}