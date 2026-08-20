using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public class VehicleAssignmentStatusConverter : JsonConverter<VehicleAssignmentStatus>
    {
        public override VehicleAssignmentStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            return Enum.TryParse<VehicleAssignmentStatus>(val?.Replace(" ", ""), true, out var result) ? result : default;
        }

        public override void Write(Utf8JsonWriter writer, VehicleAssignmentStatus value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class VehicleTypeConverter : JsonConverter<VehicleType>
    {
        public override VehicleType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            return Enum.TryParse<VehicleType>(val, true, out var result) ? result : VehicleType.Truck;
        }

        public override void Write(Utf8JsonWriter writer, VehicleType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class TransportStatusConverter : JsonConverter<TransportStatus>
    {
        public override TransportStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            if (string.Equals(val, "In Transit", StringComparison.OrdinalIgnoreCase))
                return TransportStatus.InTransit;

            return Enum.TryParse<TransportStatus>(val?.Replace(" ", ""), true, out var result) ? result : default;
        }

        public override void Write(Utf8JsonWriter writer, TransportStatus value, JsonSerializerOptions options)
        {
            if (value == TransportStatus.InTransit)
                writer.WriteStringValue("In Transit");
            else
                writer.WriteStringValue(value.ToString());
        }
    }

    public class TransportModeConverter : JsonConverter<TransportMode>
    {
        public override TransportMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            return Enum.TryParse<TransportMode>(val, true, out var result) ? result : TransportMode.Road;
        }

        public override void Write(Utf8JsonWriter writer, TransportMode value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
