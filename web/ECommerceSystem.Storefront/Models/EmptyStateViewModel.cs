namespace ECommerceSystem.Storefront.Models;

/// <summary>
/// Shared empty state (same pattern as the DGCS Enrollment System): plain grey line icon, heading,
/// one sentence, and an optional btn-brand action.
/// </summary>
public sealed record EmptyStateViewModel(
    string Icon,
    string Title,
    string Text,
    string? ActionText = null,
    string? ActionUrl = null);