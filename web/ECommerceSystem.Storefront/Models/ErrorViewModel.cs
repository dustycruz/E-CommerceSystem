namespace ECommerceSystem.Storefront.Models;

/// <summary>Shown by Views\Shared\Error.cshtml; the correlation id lets support find the request in the logs.</summary>
public sealed class ErrorViewModel
{
    public string? RequestId { get; init; }

    public string? CorrelationId { get; init; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}