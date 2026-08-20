using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public class DispatchPlanStatusConverter : JsonConverter<DispatchPlanStatus>
    {
        public override DispatchPlanStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            if (string.Equals(val, "Ready For Dispatch", StringComparison.OrdinalIgnoreCase))
                return DispatchPlanStatus.ReadyForDispatch;

            return Enum.TryParse<DispatchPlanStatus>(val?.Replace(" ", ""), true, out var result) ? result : default;
        }

        public override void Write(Utf8JsonWriter writer, DispatchPlanStatus value, JsonSerializerOptions options)
        {
            if (value == DispatchPlanStatus.ReadyForDispatch)
                writer.WriteStringValue("Ready For Dispatch");
            else
                writer.WriteStringValue(value.ToString());
        }
    }

    public class DispatchPriorityConverter : JsonConverter<DispatchPriority>
    {
        public override DispatchPriority Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            return Enum.TryParse<DispatchPriority>(val, true, out var result) ? result : DispatchPriority.Normal;
        }

        public override void Write(Utf8JsonWriter writer, DispatchPriority value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
