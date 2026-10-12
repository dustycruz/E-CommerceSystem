using Asp.Versioning;
using ECommerceSystem.InventoryService.Contracts;
using ECommerceSystem.InventoryService.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceSystem.InventoryService.Controllers;

/// <summary>/inventory/v1/stock-items - implements contracts\inventory.yaml (tag "StockItems").</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("inventory/v{version:apiVersion}/stock-items")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json")]
public sealed class StockItemsController(IStockItemService stockItemService) : ControllerBase
{
    /// <summary>List stock items (operationId listStockItems).</summary>
    [HttpGet(Name = "listStockItems")]
    [ProducesResponseType<IReadOnlyList<StockItemResponse>>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<IReadOnlyList<StockItemResponse>>> ListStockItems(CancellationToken cancellationToken)
    {
        return Ok(await stockItemService.ListAsync(cancellationToken));
    }

    /// <summary>Get one stock item (operationId getStockItemById).</summary>
    [HttpGet("{id:int}", Name = "getStockItemById")]
    [ProducesResponseType<StockItemResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<StockItemResponse>> GetStockItemById(int id, CancellationToken cancellationToken)
    {
        return Ok(await stockItemService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>Get the stock item of a catalog product (operationId getStockItemByProductId).</summary>
    [HttpGet("by-product/{productId:int}", Name = "getStockItemByProductId")]
    [ProducesResponseType<StockItemResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<StockItemResponse>> GetStockItemByProductId(int productId, CancellationToken cancellationToken)
    {
        return Ok(await stockItemService.GetByProductIdAsync(productId, cancellationToken));
    }

    /// <summary>Create a stock item (operationId createStockItem).</summary>
    [HttpPost(Name = "createStockItem")]
    [Consumes("application/json")]
    [ProducesResponseType<StockItemResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<StockItemResponse>> CreateStockItem(
        [FromBody] CreateStockItemRequest request,
        CancellationToken cancellationToken)
    {
        var created = await stockItemService.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("getStockItemById", new { id = created.Id, version = "1" }, created);
    }

    /// <summary>Replace the stock level (operationId updateStockItem).</summary>
    [HttpPut("{id:int}", Name = "updateStockItem")]
    [Consumes("application/json")]
    [ProducesResponseType<StockItemResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<StockItemResponse>> UpdateStockItem(
        int id,
        [FromBody] UpdateStockItemRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await stockItemService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Delete a stock item (operationId deleteStockItem).</summary>
    [HttpDelete("{id:int}", Name = "deleteStockItem")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> DeleteStockItem(int id, CancellationToken cancellationToken)
    {
        await stockItemService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}