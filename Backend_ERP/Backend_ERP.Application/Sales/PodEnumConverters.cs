using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public class PodStatusConverter : JsonConverter<PodStatus>
    {
        public override PodStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            return Enum.TryParse<PodStatus>(val, true, out var result) ? result : PodStatus.Pending;
        }

        public override void Write(Utf8JsonWriter writer, PodStatus value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
