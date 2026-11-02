using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERP.Application.Procurement;
using ERP.Application.Procurement.Dtos;
using ERP.Domain.Procurement;
using ERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.API.Controllers
{
    [Route("api/procurement-audit")]
    [ApiController]
    public class ProcurementAuditController : ControllerBase
    {
        private readonly ERPDbContext _dbContext;
        private readonly IForensicAuditChainingEngine _auditChainingEngine;

        public ProcurementAuditController(
            ERPDbContext dbContext,
            IForensicAuditChainingEngine auditChainingEngine)
        {
            _dbContext = dbContext;
            _auditChainingEngine = auditChainingEngine;
        }

        [HttpGet("events")]
        public async Task<ActionResult<List<ProcurementAuditLogDto>>> GetAuditEvents(
            [FromQuery] string? entityName,
            [FromQuery] string? entityId,
            [FromQuery] string? actionType,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.ProcurementAuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(entityName))
                query = query.Where(a => a.EntityName == entityName);
            if (!string.IsNullOrWhiteSpace(entityId))
                query = query.Where(a => a.EntityId == entityId);
            if (!string.IsNullOrWhiteSpace(actionType))
                query = query.Where(a => a.ActionType == actionType);

            var items = await query
                .OrderByDescending(a => a.BlockIndex)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ProcurementAuditLogDto
                {
                    Id = a.Id,
                    BlockIndex = a.BlockIndex,
                    EntityName = a.EntityName,
                    EntityId = a.EntityId,
                    ActionType = a.ActionType,
                    ChangedByUser = a.ChangedByUser,
                    UserIpAddress = a.UserIpAddress,
                    OldValuesJson = a.OldValuesJson,
                    NewValuesJson = a.NewValuesJson,
                    PreviousLogHash = a.PreviousLogHash,
                    CurrentLogHash = a.CurrentLogHash,
                    Timestamp = a.Timestamp
                })
                .ToListAsync(cancellationToken);

            return Ok(items);
        }

        [HttpPost("verify-chain")]
        public async Task<ActionResult<AuditChainVerificationResultDto>> VerifyAuditChain(
            [FromQuery] long startBlock = 1,
            [FromQuery] long? endBlock = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _auditChainingEngine.VerifyChainIntegrityAsync(startBlock, endBlock, cancellationToken);
            return Ok(result);
        }
    }
}

