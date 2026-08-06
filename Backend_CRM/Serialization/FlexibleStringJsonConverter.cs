using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CRM.Serialization
{
    /// <summary>
    /// Accepts JSON string or number tokens into a <see cref="string"/> property.
    /// Used for Justdial fields that are documented as int but historically also arrive as strings
    /// (dncmobile, dncphone, pincode, branchpin).
    /// </summary>
    public sealed class FlexibleStringJsonConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return reader.GetString();

                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out var longValue))
                    {
                        return longValue.ToString(CultureInfo.InvariantCulture);
                    }

                    if (reader.TryGetDecimal(out var decimalValue))
                    {
                        return decimalValue.ToString(CultureInfo.InvariantCulture);
                    }

                    throw new JsonException("Cannot convert JSON number to String.");

                case JsonTokenType.Null:
                    return null;

                case JsonTokenType.True:
                    return bool.TrueString;

                case JsonTokenType.False:
                    return bool.FalseString;

                default:
                    throw new JsonException(
                        $"Cannot convert JSON token {reader.TokenType} to String.");
            }
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value);
            }
        }
    }
}
