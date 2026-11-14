using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Application.Common.Errors;

namespace Api.Tests.Integration.Sessions;

/// <summary>Helpers shared by the WS-C controller tests (route building, System.Text.Json bodies, error codes).</summary>
public static partial class ApiTestHelpers
{
    public const string SessionParam = "sessionId";
    public const string CenterParam = "centerId";
    public const string RoomParam = "roomId";
    public const string ParticipantParam = "participantId";
    public const string RuleParam = "ruleId";

    /// <summary>Same conventions as the API: camelCase, enums as strings, polymorphic rule parameters.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Fills route template placeholders such as <c>{sessionId:guid}</c>.</summary>
    public static string Route(string template, params (string Name, object Value)[] values)
    {
        var route = template;
        foreach (var (name, value) in values)
        {
            route = Regex.Replace(route, "\\{" + name + "(:[a-z]+)?\\}", value.ToString()!);
        }

        if (PlaceholderRegex().IsMatch(route))
        {
            throw new ArgumentException($"Route '{route}' still has placeholders", nameof(values));
        }

        return "/" + route.TrimStart('/');
    }

    public static Task<HttpResponseMessage> PostJson<T>(this HttpClient client, string url, T body)
        => client.PostAsJsonAsync(url, body, Json);

    public static Task<HttpResponseMessage> PutJson<T>(this HttpClient client, string url, T body)
        => client.PutAsJsonAsync(url, body, Json);

    public static async Task<T> ReadJson<T>(this HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<T>(Json)
           ?? throw new ArgumentException("Response content cannot be null.");

    /// <summary>Reads <c>extensions.code</c> of a ProblemDetails body.</summary>
    public static async Task<ErrorCode> ReadErrorCode(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var code = document.RootElement.GetProperty(ErrorExtensionKeys.Code);

        // ValidationExceptionHandler currently writes the code as a number (see docs/change-requests.md).
        return code.ValueKind == JsonValueKind.Number
            ? (ErrorCode)code.GetInt32()
            : Enum.Parse<ErrorCode>(code.GetString()!);
    }

    /// <summary>Reads <c>extensions.data</c> of a ProblemDetails body.</summary>
    public static async Task<T> ReadErrorData<T>(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty(ErrorExtensionKeys.Data).Deserialize<T>(Json)!;
    }

    [GeneratedRegex("\\{[a-zA-Z]+(:[a-z]+)?\\}")]
    private static partial Regex PlaceholderRegex();
}