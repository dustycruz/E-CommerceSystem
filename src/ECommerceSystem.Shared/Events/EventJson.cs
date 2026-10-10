using System.Text.Json;
using System.Text.Json.Serialization;

namespace ECommerceSystem.Shared.Events;

/// <summary>
/// The ONLY JSON settings used for message bodies (publishers and consumers must agree).
/// camelCase, enums as strings, and strict reading: a missing constructor parameter or a null in a
/// non-nullable property throws JsonException, so a malformed message becomes a poison message (DLQ).
/// </summary>
public static class EventJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            RespectRequiredConstructorParameters = true,
            RespectNullableAnnotations = true,
            WriteIndented = false
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}