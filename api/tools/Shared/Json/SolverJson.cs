using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Solver.Model;

namespace Tools.Shared.Json;

/// <summary>System.Text.Json settings for tool files: camelCase, enums as strings, polymorphic rule parameters.</summary>
public static class SolverJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, Options) ?? throw new JsonException($"Empty JSON for {typeof(T).Name}");

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new RuleParametersJsonConverter());
        options.Converters.Add(new ReadOnlyStringSetJsonConverter());
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}

/// <summary>
/// Polymorphic converter for the solver's <see cref="RuleParameters"/> (the Solver model carries no JSON
/// attributes). The discriminator property and values match Domain/DTO rule parameters: <c>"type": "&lt;RuleType&gt;"</c>.
/// </summary>
public sealed class RuleParametersJsonConverter : JsonConverter<RuleParameters>
{
    public const string TypeDiscriminator = "type";

    private static readonly IReadOnlyDictionary<RuleType, Type> TypesByRule = new Dictionary<RuleType, Type>
    {
        [RuleType.SeparateByAttribute] = typeof(SeparateByAttributeParameters),
        [RuleType.NoAdjacentByAttribute] = typeof(NoAdjacentByAttributeParameters),
        [RuleType.AccessibleSeating] = typeof(AccessibleSeatingParameters),
        [RuleType.MinRoomFill] = typeof(MinRoomFillParameters),
        [RuleType.KeepTogetherInCenter] = typeof(KeepTogetherInCenterParameters),
        [RuleType.SpreadInRoom] = typeof(SpreadInRoomParameters),
        [RuleType.FillFromFront] = typeof(FillFromFrontParameters),
    };

    private static readonly IReadOnlyDictionary<Type, RuleType> RulesByType = TypesByRule.ToDictionary(x => x.Value, x => x.Key);

    public override RuleParameters Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var discriminator = root.EnumerateObject()
            .FirstOrDefault(x => string.Equals(x.Name, TypeDiscriminator, StringComparison.OrdinalIgnoreCase))
            .Value;

        if (discriminator.ValueKind != JsonValueKind.String
            || !Enum.TryParse<RuleType>(discriminator.GetString(), ignoreCase: true, out var ruleType)
            || !TypesByRule.TryGetValue(ruleType, out var concrete))
        {
            throw new JsonException($"Rule parameters need a valid '{TypeDiscriminator}' discriminator");
        }

        return (RuleParameters)(root.Deserialize(concrete, options) ?? throw new JsonException($"Empty parameters for {ruleType}"));
    }

    public override void Write(Utf8JsonWriter writer, RuleParameters value, JsonSerializerOptions options)
    {
        var concrete = value.GetType();
        if (!RulesByType.TryGetValue(concrete, out var ruleType))
        {
            throw new JsonException($"Unsupported rule parameters {concrete.Name}");
        }

        var element = JsonSerializer.SerializeToElement(value, concrete, options);
        writer.WriteStartObject();
        writer.WriteString(TypeDiscriminator, ruleType.ToString());
        foreach (var property in element.EnumerateObject())
        {
            property.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}

/// <summary>System.Text.Json cannot instantiate <see cref="IReadOnlySet{T}"/>; read into an ordinal <see cref="HashSet{T}"/>, write sorted.</summary>
public sealed class ReadOnlyStringSetJsonConverter : JsonConverter<IReadOnlySet<string>>
{
    public override IReadOnlySet<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var items = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? [];
        return new HashSet<string>(items, StringComparer.Ordinal);
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlySet<string> value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value.Order(StringComparer.Ordinal).ToList(), options);
}