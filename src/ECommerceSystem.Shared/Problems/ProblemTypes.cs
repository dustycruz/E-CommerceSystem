namespace ECommerceSystem.Shared.Problems;

/// <summary>
/// "type" URIs for Problem Details (RFC 9457), pointing at the HTTP status definitions in RFC 9110.
/// </summary>
public static class ProblemTypes
{
    public const string BadRequest = "https://tools.ietf.org/html/rfc9110#section-15.5.1";
    public const string NotFound = "https://tools.ietf.org/html/rfc9110#section-15.5.5";
    public const string Conflict = "https://tools.ietf.org/html/rfc9110#section-15.5.10";
    public const string UnprocessableEntity = "https://tools.ietf.org/html/rfc9110#section-15.5.21";
    public const string InternalServerError = "https://tools.ietf.org/html/rfc9110#section-15.6.1";
    public const string ServiceUnavailable = "https://tools.ietf.org/html/rfc9110#section-15.6.4";

    public static string? ForStatus(int? statusCode) => statusCode switch
    {
        400 => BadRequest,
        404 => NotFound,
        409 => Conflict,
        422 => UnprocessableEntity,
        500 => InternalServerError,
        503 => ServiceUnavailable,
        _ => null
    };
}