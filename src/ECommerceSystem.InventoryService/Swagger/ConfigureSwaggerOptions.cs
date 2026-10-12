using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECommerceSystem.InventoryService.Swagger;

/// <summary>One Swagger document per API version (Microsoft.OpenApi 2.x types).</summary>
public sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "ECommerceSystem Inventory API",
                Version = description.ApiVersion.ToString(),
                Description = "Inventory Service. Stock CRUD plus a read-only view of saga reservations. Contract: contracts inventory.yaml.",
                Contact = new OpenApiContact { Name = "Laurence Mirandilla (LaurenceMirandilla)" }
            });
        }

        options.MapType<decimal>(() => new OpenApiSchema { Type = JsonSchemaType.Number, Format = "decimal" });
        options.SupportNonNullableReferenceTypes();
    }
}