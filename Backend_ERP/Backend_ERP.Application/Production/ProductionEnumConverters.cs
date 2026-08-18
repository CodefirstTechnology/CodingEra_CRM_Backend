using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ERP.Domain.Production;

namespace ERP.Application.Production
{
    public class WorkOrderStatusConverter : JsonConverter<WorkOrderStatus>
    {
        public override WorkOrderStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            if (val == "In Progress") return WorkOrderStatus.InProgress;
            return Enum.TryParse<WorkOrderStatus>(val?.Replace(" ", ""), true, out var result) ? result : default;
        }

        public override void Write(Utf8JsonWriter writer, WorkOrderStatus value, JsonSerializerOptions options)
        {
            if (value == WorkOrderStatus.InProgress) writer.WriteStringValue("In Progress");
            else writer.WriteStringValue(value.ToString());
        }
    }
}
