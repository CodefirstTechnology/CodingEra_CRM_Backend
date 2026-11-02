using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ERP.Infrastructure.Procurement
{
    public class ForensicAuditChainingEngine : IForensicAuditChainingEngine
    {
        private readonly ERPDbContext _dbContext;
        private readonly ILogger<ForensicAuditChainingEngine> _logger;
        private readonly byte[] _hmacSecretKey;

        private const string GENESIS_HASH = "0000000000000000000000000000000000000000000000000000000000000000";

        public ForensicAuditChainingEngine(
            ERPDbContext dbContext,
            IConfiguration configuration,
            ILogger<ForensicAuditChainingEngine> logger)
        {
            _dbContext = dbContext;
            _logger = logger;

            string secret = configuration["Security:AuditHmacSecret"]
                ?? "AntigravityERP_EnterpriseForensicAuditChainingSecretKey2026";
            _hmacSecretKey = Encoding.UTF8.GetBytes(secret);
        }

        public async Task<ProcurementAuditLog> AppendAuditLogAsync(
            string entityName,
            string entityId,
            string actionType,
            string changedByUser,
            string? userIp,
            object? oldValues,
            object? newValues,
            CancellationToken ct = default)
        {
            const string tenantId = "TENANT-DEFAULT";

            var latestBlock = await _dbContext.ProcurementAuditLogs
                .Where(b => b.TenantId == tenantId)
                .OrderByDescending(b => b.BlockIndex)
                .FirstOrDefaultAsync(ct);

            long nextBlockIndex = (latestBlock?.BlockIndex ?? 0) + 1;
            string prevHash = latestBlock?.CurrentLogHash ?? GENESIS_HASH;

            string oldJson = oldValues != null ? JsonSerializer.Serialize(oldValues) : "{}";
            string newJson = newValues != null ? JsonSerializer.Serialize(newValues) : "{}";

            string payloadCombined = $"{oldJson}||{newJson}";
            string payloadSha256 = ComputeSha256Hex(payloadCombined);

            var now = DateTime.UtcNow;

            string payloadToSign = $"{prevHash}|{tenantId}|{entityName}|{entityId}|{actionType}|{now:O}|{payloadSha256}";
            string currentHash = ComputeHmacSha256Hex(payloadToSign);

            var entry = new ProcurementAuditLog
            {
                TenantId = tenantId,
                BlockIndex = nextBlockIndex,
                EntityName = entityName,
                EntityId = entityId,
                ActionType = actionType,
                ChangedByUser = changedByUser,
                UserIpAddress = userIp,
                OldValuesJson = oldJson,
                NewValuesJson = newJson,
                PreviousLogHash = prevHash,
                CurrentLogHash = currentHash,
                Timestamp = now
            };

            _dbContext.ProcurementAuditLogs.Add(entry);
            await _dbContext.SaveChangesAsync(ct);

            return entry;
        }

        public async Task<AuditChainVerificationResultDto> VerifyChainIntegrityAsync(
            long startBlock = 1,
            long? endBlock = null,
            CancellationToken ct = default)
        {
            const string tenantId = "TENANT-DEFAULT";

            var query = _dbContext.ProcurementAuditLogs
                .Where(b => b.TenantId == tenantId && b.BlockIndex >= startBlock);

            if (endBlock.HasValue)
                query = query.Where(b => b.BlockIndex <= endBlock.Value);

            var blocks = await query.OrderBy(b => b.BlockIndex).ToListAsync(ct);

            var result = new AuditChainVerificationResultDto
            {
                VerifiedAt = DateTime.UtcNow,
                TotalBlocksVerified = blocks.Count
            };

            if (blocks.Count == 0)
            {
                result.IsChainValid = true;
                result.VerificationSummary = "No audit blocks found in the specified verification range.";
                return result;
            }

            string expectedPrevHash = startBlock == 1 ? GENESIS_HASH : blocks.First().PreviousLogHash;

            foreach (var block in blocks)
            {
                // Verify link to previous block
                if (block.PreviousLogHash != expectedPrevHash)
                {
                    result.IsChainValid = false;
                    result.TamperedBlockIndex = block.BlockIndex;
                    result.TamperedEntityName = block.EntityName;
                    result.TamperedEntityId = block.EntityId;
                    result.ExpectedHash = expectedPrevHash;
                    result.ActualHash = block.PreviousLogHash;
                    result.VerificationSummary = $"Tampering detected at Block #{block.BlockIndex}: PreviousLogHash mismatch. Out-of-band mutation or block deletion identified.";
                    return result;
                }

                // Verify block's own cryptographic signature
                string oldJson = block.OldValuesJson ?? "{}";
                string newJson = block.NewValuesJson ?? "{}";
                string payloadCombined = $"{oldJson}||{newJson}";
                string payloadSha256 = ComputeSha256Hex(payloadCombined);

                string payloadToSign = $"{block.PreviousLogHash}|{tenantId}|{block.EntityName}|{block.EntityId}|{block.ActionType}|{block.Timestamp:O}|{payloadSha256}";
                string expectedHash = ComputeHmacSha256Hex(payloadToSign);

                if (block.CurrentLogHash != expectedHash)
                {
                    result.IsChainValid = false;
                    result.TamperedBlockIndex = block.BlockIndex;
                    result.TamperedEntityName = block.EntityName;
                    result.TamperedEntityId = block.EntityId;
                    result.ExpectedHash = expectedHash;
                    result.ActualHash = block.CurrentLogHash;
                    result.VerificationSummary = $"Tampering detected at Block #{block.BlockIndex}: Cryptographic HMAC signature corrupted. Payload values were modified directly in PostgreSQL.";
                    return result;
                }

                expectedPrevHash = block.CurrentLogHash;
            }

            result.IsChainValid = true;
            result.VerificationSummary = $"All {blocks.Count} blocks verified successfully. Cryptographic chain integrity intact from Block #{startBlock} to Block #{blocks.Last().BlockIndex}.";
            return result;
        }

        private string ComputeHmacSha256Hex(string input)
        {
            using var hmac = new HMACSHA256(_hmacSecretKey);
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private static string ComputeSha256Hex(string input)
        {
            using var sha = SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}

