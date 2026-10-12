using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECommerceSystem.OrderService.Swagger;

/// <summary>One Swagger document per API version (Microsoft.OpenApi 2.x types).</summary>
public sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "ECommerceSystem Orders API",
                Version = description.ApiVersion.ToString(),
                Description = "Order Service and saga orchestrator. Placing an order answers 202 Accepted; follow the saga with the get-by-id operation. Contract: contracts orders.yaml.",
                Contact = new OpenApiContact { Name = "Dusty Cruz (dustycruz)" }
            });
        }

        options.MapType<decimal>(() => new OpenApiSchema { Type = JsonSchemaType.Number, Format = "decimal" });
        options.SupportNonNullableReferenceTypes();
    }
}