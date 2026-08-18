using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ERP.Domain.Procurement;

namespace ERP.Application.Procurement
{
    public class VerificationStatusConverter : JsonConverter<VerificationStatus>
    {
        public override VerificationStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            if (val == "In Progress") return VerificationStatus.InProgress;
            return Enum.TryParse<VerificationStatus>(val?.Replace(" ", ""), true, out var result) ? result : default;
        }

        public override void Write(Utf8JsonWriter writer, VerificationStatus value, JsonSerializerOptions options)
        {
            if (value == VerificationStatus.InProgress) writer.WriteStringValue("In Progress");
            else writer.WriteStringValue(value.ToString());
        }
    }

    public class StockTxnTypeConverter : JsonConverter<StockTxnType>
    {
        public override StockTxnType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            if (val == "Stock In") return StockTxnType.StockIn;
            if (val == "Stock Out") return StockTxnType.StockOut;
            if (val == "Purchase Receipt") return StockTxnType.PurchaseReceipt;
            if (val == "Production Consumption") return StockTxnType.ProductionConsumption;
            if (val == "Transfer In") return StockTxnType.TransferIn;
            if (val == "Transfer Out") return StockTxnType.TransferOut;
            return Enum.TryParse<StockTxnType>(val?.Replace(" ", ""), true, out var result) ? result : default;
        }

        public override void Write(Utf8JsonWriter writer, StockTxnType value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case StockTxnType.StockIn: writer.WriteStringValue("Stock In"); break;
                case StockTxnType.StockOut: writer.WriteStringValue("Stock Out"); break;
                case StockTxnType.PurchaseReceipt: writer.WriteStringValue("Purchase Receipt"); break;
                case StockTxnType.ProductionConsumption: writer.WriteStringValue("Production Consumption"); break;
                case StockTxnType.TransferIn: writer.WriteStringValue("Transfer In"); break;
                case StockTxnType.TransferOut: writer.WriteStringValue("Transfer Out"); break;
                default: writer.WriteStringValue(value.ToString()); break;
            }
        }
    }

    public class BatchStatusConverter : JsonConverter<BatchStatus>
    {
        public override BatchStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            if (val == "Expiring Soon") return BatchStatus.ExpiringSoon;
            return Enum.TryParse<BatchStatus>(val?.Replace(" ", ""), true, out var result) ? result : default;
        }

        public override void Write(Utf8JsonWriter writer, BatchStatus value, JsonSerializerOptions options)
        {
            if (value == BatchStatus.ExpiringSoon) writer.WriteStringValue("Expiring Soon");
            else writer.WriteStringValue(value.ToString());
        }
    }

    public class FgDispatchStatusConverter : JsonConverter<FgDispatchStatus>
    {
        public override FgDispatchStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            if (val == "Not Ready") return FgDispatchStatus.NotReady;
            if (val == "Ready to Dispatch") return FgDispatchStatus.ReadyToDispatch;
            return Enum.TryParse<FgDispatchStatus>(val?.Replace(" ", ""), true, out var result) ? result : default;
        }

        public override void Write(Utf8JsonWriter writer, FgDispatchStatus value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case FgDispatchStatus.NotReady: writer.WriteStringValue("Not Ready"); break;
                case FgDispatchStatus.ReadyToDispatch: writer.WriteStringValue("Ready to Dispatch"); break;
                default: writer.WriteStringValue(value.ToString()); break;
            }
        }
    }

    public class StockAgeBandConverter : JsonConverter<StockAgeBand>
    {
        public override StockAgeBand Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var val = reader.GetString();
            if (val == "0-30") return StockAgeBand.Band0To30;
            if (val == "31-60") return StockAgeBand.Band31To60;
            if (val == "61-90") return StockAgeBand.Band61To90;
            if (val == "90+") return StockAgeBand.Band90Plus;
            return Enum.TryParse<StockAgeBand>(val, true, out var result) ? result : default;
        }

        public override void Write(Utf8JsonWriter writer, StockAgeBand value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case StockAgeBand.Band0To30: writer.WriteStringValue("0-30"); break;
                case StockAgeBand.Band31To60: writer.WriteStringValue("31-60"); break;
                case StockAgeBand.Band61To90: writer.WriteStringValue("61-90"); break;
                case StockAgeBand.Band90Plus: writer.WriteStringValue("90+"); break;
                default: writer.WriteStringValue(value.ToString()); break;
            }
        }
    }
}
