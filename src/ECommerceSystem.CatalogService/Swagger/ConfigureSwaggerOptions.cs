using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECommerceSystem.CatalogService.Swagger;

/// <summary>
/// Creates one Swagger document per API version discovered by Asp.Versioning
/// (v1 now; v2 is added in the bonus part without touching this class).
/// Uses Microsoft.OpenApi 2.x types (namespace Microsoft.OpenApi, JsonSchemaType).
/// </summary>
public sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "ECommerceSystem Catalog API",
                Version = description.ApiVersion.ToString(),
                Description = description.IsDeprecated
                    ? "Catalog Service. This API version is DEPRECATED - see the Deprecation and Sunset response headers."
                    : "Catalog Service. Contract-first: generated from controllers built to match contracts catalog.yaml.",
                Contact = new OpenApiContact { Name = "Laurence Mirandilla (LaurenceMirandilla)" }
            });
        }

        // Same money representation as the contract (NSwag then generates C# decimal, not double).
        options.MapType<decimal>(() => new OpenApiSchema { Type = JsonSchemaType.Number, Format = "decimal" });

        options.SupportNonNullableReferenceTypes();
    }
}