using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using ECommerceSystem.CatalogService.Chaos;
using ECommerceSystem.CatalogService.Contracts;
using ECommerceSystem.CatalogService.Services;
using ECommerceSystem.Shared.Problems;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSystem.CatalogService.Controllers;

/// <summary>
/// /catalog/v1/products - implements contracts\catalog.yaml (tag "Products").
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("catalog/v{version:apiVersion}/products")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public sealed class ProductsController(
    IProductService productService,
    ChaosState chaosState,
    ILogger<ProductsController> logger) : ControllerBase
{
    /// <summary>List products (operationId listProducts).</summary>
    [HttpGet(Name = "listProducts")]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> ListProducts(
        [FromQuery][StringLength(60)] string? category,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var products = await productService.ListAsync(category, includeInactive, cancellationToken);
        return Ok(products);
    }

    /// <summary>Get one product (operationId getProductById). Used by the Order Service through NSwag.</summary>
    [HttpGet("{id:int}", Name = "getProductById")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<ActionResult<ProductResponse>> GetProductById(int id, CancellationToken cancellationToken)
    {
        if (chaosState.TryConsumeFailure())
        {
            logger.LogWarning(
                "CHAOS: simulated outage for product {ProductId}, returning 503 ({Remaining} simulated failure(s) left).",
                id,
                chaosState.RemainingFailures);

            throw new ServiceUnavailableException("Chaos mode: the Catalog Service is simulating a transient outage. Retry shortly.");
        }

        var product = await productService.GetByIdAsync(id, cancellationToken);
        return Ok(product);
    }

    /// <summary>Create a product (operationId createProduct).</summary>
    [HttpPost(Name = "createProduct")]
    [Consumes("application/json")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<ProductResponse>> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var created = await productService.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("getProductById", new { id = created.Id, version = "1" }, created);
    }

    /// <summary>Replace a product (operationId updateProduct).</summary>
    [HttpPut("{id:int}", Name = "updateProduct")]
    [Consumes("application/json")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(
        int id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await productService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Delete a product (operationId deleteProduct).</summary>
    [HttpDelete("{id:int}", Name = "deleteProduct")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken)
    {
        await productService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}