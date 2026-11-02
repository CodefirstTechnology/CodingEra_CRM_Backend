using System;
using System.Security.Cryptography;
using System.Text;
using ERP.Application.Procurement;

namespace ERP.Infrastructure.Procurement
{
    public class PurchaseRequisitionApprovalEngine : IPurchaseRequisitionApprovalEngine
    {
        private const string SecretKey = "Buildrich_Enterprise_PR_HMAC_Secret_Key_2026";

        public int DetermineRequiredTier(decimal totalEstimatedAmount)
        {
            if (totalEstimatedAmount <= 50_000m)
            {
                return 1;
            }
            if (totalEstimatedAmount <= 500_000m)
            {
                return 2;
            }
            if (totalEstimatedAmount <= 2_500_000m)
            {
                return 3;
            }
            return 4;
        }

        public string GenerateApprovalToken(string prNumber, int tier, string approverUserId, DateTime timestamp)
        {
            string payload = $"{prNumber}:{tier}:{approverUserId}:{timestamp:yyyyMMddHHmmss}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }

        public bool ValidateApprovalToken(string prNumber, int tier, string approverUserId, DateTime timestamp, string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            string expected = GenerateApprovalToken(prNumber, tier, approverUserId, timestamp);
            return string.Equals(expected, token, StringComparison.Ordinal);
        }

        public bool CanUserApproveTier(string userRole, int tier)
        {
            if (string.IsNullOrWhiteSpace(userRole)) return false;
            var role = userRole.Trim().ToLowerInvariant();

            if (role is "admin" or "cfo" or "director" or "board") return true;

            return tier switch
            {
                1 => role is "departmenthead" or "manager" or "plantmanager" or "procurementhead",
                2 => role is "plantmanager" or "operationshead" or "procurementhead",
                3 => role is "procurementhead" or "vpsupplychain",
                4 => role is "cfo" or "director" or "board",
                _ => false
            };
        }
    }
}

