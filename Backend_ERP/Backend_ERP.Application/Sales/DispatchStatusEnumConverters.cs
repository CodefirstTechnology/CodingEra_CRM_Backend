using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public class DispatchTrackStatusConverter : JsonConverter<DispatchTrackStatus>
    {
        public override DispatchTrackStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            if (string.Equals(val, "In Transit", StringComparison.OrdinalIgnoreCase))
                return DispatchTrackStatus.InTransit;

            return Enum.TryParse<DispatchTrackStatus>(val?.Replace(" ", ""), true, out var result) ? result : DispatchTrackStatus.Ready;
        }

        public override void Write(Utf8JsonWriter writer, DispatchTrackStatus value, JsonSerializerOptions options)
        {
            if (value == DispatchTrackStatus.InTransit)
            {
                writer.WriteStringValue("In Transit");
            }
            else
            {
                writer.WriteStringValue(value.ToString());
            }
        }
    }

    public class TransportTrackStatusConverter : JsonConverter<TransportTrackStatus>
    {
        public override TransportTrackStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            if (string.Equals(val, "In Transit", StringComparison.OrdinalIgnoreCase))
                return TransportTrackStatus.InTransit;

            return Enum.TryParse<TransportTrackStatus>(val?.Replace(" ", ""), true, out var result) ? result : TransportTrackStatus.Pending;
        }

        public override void Write(Utf8JsonWriter writer, TransportTrackStatus value, JsonSerializerOptions options)
        {
            if (value == TransportTrackStatus.InTransit)
            {
                writer.WriteStringValue("In Transit");
            }
            else
            {
                writer.WriteStringValue(value.ToString());
            }
        }
    }
}
