namespace ECommerceSystem.Shared.Correlation;

/// <summary>
/// Creates and validates correlation ids.
/// </summary>
public static class CorrelationIdGenerator
{
    /// <summary>Creates a new id: 32 lowercase hex characters (a Guid without dashes).</summary>
    public static string NewId() => Guid.NewGuid().ToString("N");

    /// <summary>
    /// Accepts only short ids made of letters, digits, '-', '_' and '.', so a malicious header can never
    /// inject text into logs or downstream headers. Anything else is replaced by a new id.
    /// </summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > CorrelationConstants.MaxLength)
        {
            return false;
        }

        foreach (var c in value)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_' and not '.')
            {
                return false;
            }
        }

        return true;
    }
}