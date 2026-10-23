using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ERP.Application.Sales.Approvals;
using ERP.Domain.Sales;
using Microsoft.Extensions.Configuration;

namespace ERP.Infrastructure.Sales
{
    public class CommercialComplianceService : ICommercialComplianceService
    {
        private readonly byte[] _hmacKey;

        public CommercialComplianceService(IConfiguration? configuration = null)
        {
            var keyString = configuration?["Security:CommercialHmacKey"]
                ?? "EnterpriseERP-QuotationsCompliance-HMAC-Key-2026-Secure-Production-Grade-Key-64B";
            _hmacKey = Encoding.UTF8.GetBytes(keyString);
        }

        public CommercialComplianceResult EvaluateQuotation(Quotation quotation)
        {
            if (quotation == null || quotation.Items == null)
            {
                return CommercialComplianceResult.Compliant();
            }

            var result = new CommercialComplianceResult();
            decimal maxDiscountBreach = 0m;
            decimal maxFloorPriceBreach = 0m;

            foreach (var item in quotation.Items)
            {
                bool lineBreached = false;

                // 1. Absolute Floor Price Breach Check
                if (item.FloorPriceSnapshot > 0 && item.UnitPrice < item.FloorPriceSnapshot)
                {
                    lineBreached = true;
                    var breachAmount = item.FloorPriceSnapshot - item.UnitPrice;
                    if (breachAmount > maxFloorPriceBreach)
                    {
                        maxFloorPriceBreach = breachAmount;
                    }
                    var reason = $"Item '{item.ItemCode}' quoted rate ({item.UnitPrice:N2}) breaches absolute floor price ({item.FloorPriceSnapshot:N2}).";
                    result.BreachReasons.Add(reason);
                    result.DeviationReason = reason;
                    result.ViolationAmount += breachAmount * item.Quantity;
                }

                // 2. Maximum Discount Ceiling Breach Check
                if (item.MaxDiscountPctSnapshot > 0 && item.DiscountPercent > item.MaxDiscountPctSnapshot)
                {
                    lineBreached = true;
                    var breachPct = item.DiscountPercent - item.MaxDiscountPctSnapshot;
                    if (breachPct > maxDiscountBreach)
                    {
                        maxDiscountBreach = breachPct;
                    }
                    var reason = $"Item '{item.ItemCode}' discount ({item.DiscountPercent:N1}%) exceeds allowed ceiling ({item.MaxDiscountPctSnapshot:N1}%).";
                    result.BreachReasons.Add(reason);
                    if (string.IsNullOrWhiteSpace(result.DeviationReason))
                    {
                        result.DeviationReason = reason;
                    }
                }

                if (lineBreached)
                {
                    result.BreachedItemsCount++;
                }
            }

            result.MaxDiscountBreachPercentage = maxDiscountBreach;
            result.MaxFloorPriceBreachAmount = maxFloorPriceBreach;

            if (result.BreachedItemsCount > 0)
            {
                result.IsBlocked = true;

                if (maxFloorPriceBreach > 0 || maxDiscountBreach > 15m || quotation.GrandTotal > 1_000_000m)
                {
                    result.Severity = CommercialDeviationSeverity.ExecutiveApproval;
                    result.RequiredApprovalLevel = "Director";
                }
                else
                {
                    result.Severity = CommercialDeviationSeverity.ManagerialApproval;
                    result.RequiredApprovalLevel = "Manager";
                }
            }
            else
            {
                result.IsBlocked = false;
                result.Severity = CommercialDeviationSeverity.Compliant;
                result.RequiredApprovalLevel = quotation.GrandTotal > 500_000m ? "Manager" : "None";
            }

            return result;
        }

        public string GenerateApprovalToken(Quotation quotation)
        {
            ArgumentNullException.ThrowIfNull(quotation);

            var payload = BuildCommercialPayload(quotation);
            using var hmac = new HMACSHA256(_hmacKey);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash);
        }

        public bool VerifyApprovalToken(Quotation quotation, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            ArgumentNullException.ThrowIfNull(quotation);

            try
            {
                var expectedPayload = BuildCommercialPayload(quotation);
                using var hmac = new HMACSHA256(_hmacKey);
                var expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(expectedPayload));
                var actualHash = Convert.FromHexString(token);

                return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
            }
            catch
            {
                return false;
            }
        }

        private static string BuildCommercialPayload(Quotation quotation)
        {
            var sb = new StringBuilder();
            sb.Append(quotation.TenantId).Append('|')
              .Append(quotation.QuotationNumber).Append('|')
              .Append(quotation.RevisionNumber).Append('|')
              .Append(quotation.GrandTotal.ToString("F2")).Append('|')
              .Append(quotation.Subtotal.ToString("F2")).Append('|')
              .Append(quotation.DiscountTotal.ToString("F2")).Append('|')
              .Append(quotation.TaxTotal.ToString("F2")).Append('|')
              .Append(quotation.CustomerId).Append('|');

            foreach (var item in quotation.Items.OrderBy(i => i.LineNumber))
            {
                sb.Append(item.ItemCode).Append(':')
                  .Append(item.Quantity.ToString("F4")).Append(':')
                  .Append(item.UnitPrice.ToString("F2")).Append(':')
                  .Append(item.DiscountPercent.ToString("F2")).Append(':')
                  .Append(item.FloorPriceSnapshot.ToString("F2")).Append(';');
            }

            return sb.ToString();
        }
    }
}
