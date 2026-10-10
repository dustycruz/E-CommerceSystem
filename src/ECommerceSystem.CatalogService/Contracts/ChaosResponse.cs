namespace ECommerceSystem.CatalogService.Contracts;

/// <summary>Response of POST /catalog/v1/chaos.</summary>
public sealed record ChaosResponse(int FailNextRequests, string Message);