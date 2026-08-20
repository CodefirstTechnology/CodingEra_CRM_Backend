using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ERP.Domain.Sales;

namespace ERP.Application.Sales
{
    public class FreightPaymentTypeConverter : JsonConverter<FreightPaymentType>
    {
        public override FreightPaymentType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            if (string.Equals(val, "To Pay", StringComparison.OrdinalIgnoreCase))
                return FreightPaymentType.ToPay;
            if (string.Equals(val, "To Be Billed", StringComparison.OrdinalIgnoreCase))
                return FreightPaymentType.ToBeBilled;
            if (string.Equals(val, "Paid", StringComparison.OrdinalIgnoreCase))
                return FreightPaymentType.Paid;

            return Enum.TryParse<FreightPaymentType>(val?.Replace(" ", ""), true, out var result) ? result : FreightPaymentType.ToBeBilled;
        }

        public override void Write(Utf8JsonWriter writer, FreightPaymentType value, JsonSerializerOptions options)
        {
            var str = value switch
            {
                FreightPaymentType.ToPay => "To Pay",
                FreightPaymentType.ToBeBilled => "To Be Billed",
                _ => "Paid"
            };
            writer.WriteStringValue(str);
        }
    }

    public class LrStatusConverter : JsonConverter<LrStatus>
    {
        public override LrStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            return Enum.TryParse<LrStatus>(val, true, out var result) ? result : default;
        }

        public override void Write(Utf8JsonWriter writer, LrStatus value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class EwayStatusConverter : JsonConverter<EwayStatus>
    {
        public override EwayStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            return Enum.TryParse<EwayStatus>(val, true, out var result) ? result : default;
        }

        public override void Write(Utf8JsonWriter writer, EwayStatus value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
