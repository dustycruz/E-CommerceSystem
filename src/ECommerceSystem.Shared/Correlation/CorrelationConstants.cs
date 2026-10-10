namespace ECommerceSystem.Shared.Correlation;

/// <summary>
/// Names used for the correlation id everywhere (HTTP header, HttpContext item, log scope, RabbitMQ header).
/// </summary>
public static class CorrelationConstants
{
    /// <summary>HTTP header and RabbitMQ header name.</summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>Key used to store the id in HttpContext.Items.</summary>
    public const string HttpContextItemKey = "CorrelationId";

    /// <summary>
    /// Log scope template. With Console IncludeScopes = true every log line contains "CorrelationId:&lt;id&gt;",
    /// which makes "findstr CorrelationId:&lt;id&gt;" work across all service log files.
    /// </summary>
    public const string LogScopeFormat = "CorrelationId:{CorrelationId}";

    /// <summary>Maximum accepted length of an incoming correlation id.</summary>
    public const int MaxLength = 100;
}