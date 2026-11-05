using System.Reflection;
using System.Text.Json.Serialization;

namespace Api.Modules.Swagger;

/// <summary>Maps System.Text.Json [JsonPolymorphic]/[JsonDerivedType] metadata to OpenAPI oneOf + discriminator.</summary>
internal static class SwaggerPolymorphism
{
    public static IEnumerable<Type> SubTypes(Type baseType)
        => baseType.GetCustomAttributes<JsonDerivedTypeAttribute>(inherit: false).Select(x => x.DerivedType);

    public static string? DiscriminatorName(Type baseType)
        => baseType.GetCustomAttribute<JsonPolymorphicAttribute>(inherit: false)?.TypeDiscriminatorPropertyName;

    public static string? DiscriminatorValue(Type subType)
    {
        var baseType = subType.BaseType;
        while (baseType is not null)
        {
            var attribute = baseType.GetCustomAttributes<JsonDerivedTypeAttribute>(inherit: false).FirstOrDefault(x => x.DerivedType == subType);
            if (attribute is not null)
            {
                return attribute.TypeDiscriminator?.ToString();
            }

            baseType = baseType.BaseType;
        }

        return null;
    }
}