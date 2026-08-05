using ERP.Application.Sales;
using ERP.Application.Sales.Dtos;
using ERP.Domain.Sales;

namespace ERP.Infrastructure.Sales
{
    public class SalesTargetService : ISalesTargetService
    {
        private readonly ISalesTargetRepository _repo;
        private readonly ISalesTargetNumberingService _numbering;

        public SalesTargetService(
            ISalesTargetRepository repo,
            ISalesTargetNumberingService numbering)
        {
            _repo = repo;
            _numbering = numbering;
        }

        public async Task<IReadOnlyList<SalesTargetListItemDto>> GetAllAsync(
            SalesTargetListQueryDto? query,
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetAllAsync(query, cancellationToken);
            return rows.Select(ApplyExpiry).Select(SalesTargetMapper.ToListItem).ToList();
        }

        public async Task<SalesTargetDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, false, cancellationToken);
            return entity is null ? null : SalesTargetMapper.ToDto(ApplyExpiry(entity));
        }

        public async Task<SalesTargetDto> CreateAsync(
            SalesTargetCreateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var (targetType, category, assignmentType, start, end, status) =
                ValidateAndNormalizeCreate(request);

            await EnsureNoOverlapAsync(
                request.SalesPersonUserId,
                category,
                start,
                end,
                null,
                status,
                cancellationToken);

            var number = string.IsNullOrWhiteSpace(request.TargetNumber)
                ? await _numbering.GenerateNextTargetNumberAsync(cancellationToken)
                : request.TargetNumber.Trim();

            if (await _repo.TargetNumberExistsAsync(number, null, cancellationToken))
            {
                throw new InvalidOperationException($"Target number '{number}' already exists.");
            }

            var targetValue = SalesTargetCalculator.Round2(request.TargetValue);
            var achieved = SalesTargetCalculator.Round2(Math.Max(0, request.AchievedValue ?? 0m));
            if (SalesTargetCalculator.WouldExceedTarget(targetValue, achieved))
            {
                throw new InvalidOperationException("Achieved value cannot exceed target value.");
            }

            var pct = SalesTargetCalculator.CalcAchievementPercentage(targetValue, achieved);
            if (pct >= 100m && status == SalesTargetStatuses.Active)
            {
                status = SalesTargetStatuses.Completed;
            }

            var now = DateTimeOffset.UtcNow;
            var entity = new SalesTarget
            {
                TargetNumber = number,
                TargetName = request.TargetName.Trim(),
                TargetType = targetType,
                TargetCategory = category,
                AssignmentType = assignmentType,
                SalesPersonUserId = request.SalesPersonUserId,
                SalesTeam = request.SalesTeam?.Trim() ?? string.Empty,
                Branch = request.Branch?.Trim() ?? string.Empty,
                RegionalManager = request.RegionalManager?.Trim() ?? string.Empty,
                FinancialYear = request.FinancialYear,
                StartDate = start,
                EndDate = end,
                TargetValue = targetValue,
                AchievedValue = achieved,
                RemainingValue = SalesTargetCalculator.CalcRemaining(targetValue, achieved),
                AchievementPercentage = pct,
                Currency = request.Currency.Trim().ToUpperInvariant(),
                Status = status,
                Remarks = request.Remarks?.Trim() ?? string.Empty,
                CreatedBy = actingUser,
                CreatedDate = now,
                UpdatedBy = actingUser,
                UpdatedDate = now,
                Assignments = BuildAssignments(request.Assignments, status, actingUser, now),
                StatusHistory =
                [
                    NewStatusHistory(null, status, "Sales target created", actingUser, now)
                ],
                ProgressHistory = achieved > 0
                    ?
                    [
                        NewProgress(0m, achieved, pct, SalesTargetProgressActions.ManualUpdate,
                            "Initial achieved value", actingUser, now)
                    ]
                    : []
            };

            await _repo.CreateAsync(entity, cancellationToken);
            return SalesTargetMapper.ToDto(
                await _repo.GetByIdAsync(entity.Id, true, false, cancellationToken) ?? entity);
        }

        public async Task<SalesTargetDto?> UpdateAsync(
            int id,
            SalesTargetUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            if (entity.Status is SalesTargetStatuses.Completed
                or SalesTargetStatuses.Cancelled
                or SalesTargetStatuses.Expired)
            {
                throw new InvalidOperationException(
                    $"Cannot update a sales target in '{entity.Status}' status.");
            }

            ValidateCoreFields(
                request.TargetName,
                request.TargetValue,
                request.StartDate,
                request.EndDate,
                request.FinancialYear,
                request.TargetType,
                request.TargetCategory,
                request.AssignmentType,
                request.SalesPersonUserId,
                request.SalesTeam,
                request.Branch,
                request.RegionalManager,
                request.Currency);

            var targetType = SalesTargetTypeRules.Normalize(request.TargetType)!;
            var category = SalesTargetCategoryRules.Normalize(request.TargetCategory)!;
            var assignmentType = SalesTargetAssignmentTypeRules.Normalize(request.AssignmentType)!;
            var start = SalesTargetMapper.ParseDate(request.StartDate, entity.StartDate);
            var end = SalesTargetMapper.ParseDate(request.EndDate, entity.EndDate);
            if (end < start)
            {
                throw new InvalidOperationException("End date must be on or after start date.");
            }

            await EnsureNoOverlapAsync(
                request.SalesPersonUserId,
                category,
                start,
                end,
                id,
                entity.Status,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.TargetNumber)
                && !string.Equals(request.TargetNumber.Trim(), entity.TargetNumber, StringComparison.Ordinal))
            {
                var newNumber = request.TargetNumber.Trim();
                if (await _repo.TargetNumberExistsAsync(newNumber, id, cancellationToken))
                {
                    throw new InvalidOperationException($"Target number '{newNumber}' already exists.");
                }

                entity.TargetNumber = newNumber;
            }

            var targetValue = SalesTargetCalculator.Round2(request.TargetValue);
            if (SalesTargetCalculator.WouldExceedTarget(targetValue, entity.AchievedValue))
            {
                throw new InvalidOperationException(
                    "Target value cannot be less than already achieved value.");
            }

            var now = DateTimeOffset.UtcNow;
            entity.TargetName = request.TargetName.Trim();
            entity.TargetType = targetType;
            entity.TargetCategory = category;
            entity.AssignmentType = assignmentType;
            entity.SalesPersonUserId = request.SalesPersonUserId;
            entity.SalesTeam = request.SalesTeam?.Trim() ?? string.Empty;
            entity.Branch = request.Branch?.Trim() ?? string.Empty;
            entity.RegionalManager = request.RegionalManager?.Trim() ?? string.Empty;
            entity.FinancialYear = request.FinancialYear;
            entity.StartDate = start;
            entity.EndDate = end;
            entity.TargetValue = targetValue;
            entity.RemainingValue = SalesTargetCalculator.CalcRemaining(targetValue, entity.AchievedValue);
            entity.AchievementPercentage = SalesTargetCalculator.CalcAchievementPercentage(
                targetValue, entity.AchievedValue);
            entity.Currency = request.Currency.Trim().ToUpperInvariant();
            entity.Remarks = request.Remarks?.Trim() ?? string.Empty;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;

            if (request.Assignments is not null)
            {
                entity.Assignments.Clear();
                foreach (var assignment in BuildAssignments(
                             request.Assignments, entity.Status, actingUser, now))
                {
                    entity.Assignments.Add(assignment);
                }
            }

            await _repo.UpdateAsync(entity, cancellationToken);
            return SalesTargetMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        public async Task<bool> DeleteAsync(
            int id,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, false, true, cancellationToken);
            if (entity is null)
            {
                return false;
            }

            if (entity.Status != SalesTargetStatuses.Draft)
            {
                throw new InvalidOperationException("Only Draft sales targets can be deleted.");
            }

            entity.IsDeleted = true;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = DateTimeOffset.UtcNow;
            return await _repo.DeleteAsync(entity, cancellationToken);
        }

        public async Task<SalesTargetDto?> DuplicateAsync(
            int id,
            SalesTargetDuplicateRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var source = await _repo.GetByIdAsync(id, true, false, cancellationToken);
            if (source is null)
            {
                return null;
            }

            var create = new SalesTargetCreateRequestDto
            {
                TargetName = string.IsNullOrWhiteSpace(request?.TargetName)
                    ? $"{source.TargetName} (Copy)"
                    : request!.TargetName.Trim(),
                TargetType = source.TargetType,
                TargetCategory = source.TargetCategory,
                AssignmentType = source.AssignmentType,
                SalesPersonUserId = source.SalesPersonUserId,
                SalesTeam = source.SalesTeam,
                Branch = source.Branch,
                RegionalManager = source.RegionalManager,
                FinancialYear = request?.FinancialYear is > 0
                    ? request.FinancialYear.Value
                    : source.FinancialYear,
                StartDate = string.IsNullOrWhiteSpace(request?.StartDate)
                    ? SalesTargetMapper.FormatDate(source.StartDate)
                    : request!.StartDate,
                EndDate = string.IsNullOrWhiteSpace(request?.EndDate)
                    ? SalesTargetMapper.FormatDate(source.EndDate)
                    : request!.EndDate,
                TargetValue = request?.TargetValue is > 0 ? request.TargetValue.Value : source.TargetValue,
                AchievedValue = 0m,
                Currency = source.Currency,
                Status = SalesTargetStatuses.Draft,
                Remarks = $"Duplicated from {source.TargetNumber}",
                Assignments = source.Assignments.Select(a => new SalesTargetAssignmentRequestDto
                {
                    SalesPersonUserId = a.SalesPersonUserId,
                    AssignedTarget = a.AssignedTarget,
                    AchievedValue = 0m,
                    Remarks = a.Remarks
                }).ToList()
            };

            return await CreateAsync(create, actingUser, cancellationToken);
        }

        public async Task<SalesTargetDto> CopyPreviousAsync(
            SalesTargetCopyPreviousRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            if (request.TargetFinancialYear <= 0)
            {
                throw new InvalidOperationException("Target financial year is required.");
            }

            if (string.IsNullOrWhiteSpace(request.StartDate) || string.IsNullOrWhiteSpace(request.EndDate))
            {
                throw new InvalidOperationException("Start date and end date are required.");
            }

            var previous = await _repo.FindPreviousTargetAsync(
                request.SourceSalesPersonUserId,
                request.SourceFinancialYear,
                request.TargetCategory,
                cancellationToken)
                ?? throw new InvalidOperationException("No previous sales target found to copy.");

            return await DuplicateAsync(
                previous.Id,
                new SalesTargetDuplicateRequestDto
                {
                    TargetName = string.IsNullOrWhiteSpace(request.TargetName)
                        ? $"{previous.TargetName} FY{request.TargetFinancialYear}"
                        : request.TargetName,
                    FinancialYear = request.TargetFinancialYear,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    TargetValue = previous.TargetValue
                },
                actingUser,
                cancellationToken)
                ?? throw new InvalidOperationException("Failed to copy previous sales target.");
        }

        public async Task<SalesTargetDto?> ActivateAsync(
            int id,
            SalesTargetRemarksRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            EnsureTransition(entity.Status, SalesTargetStatuses.Active);
            await EnsureNoOverlapAsync(
                entity.SalesPersonUserId,
                entity.TargetCategory,
                entity.StartDate,
                entity.EndDate,
                id,
                SalesTargetStatuses.Active,
                cancellationToken);

            var now = DateTimeOffset.UtcNow;
            var old = entity.Status;
            entity.Status = SalesTargetStatuses.Active;
            entity.Remarks = request?.Remarks?.Trim() ?? entity.Remarks;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.StatusHistory.Add(NewStatusHistory(
                old, SalesTargetStatuses.Active,
                request?.Remarks?.Trim() ?? "Target activated",
                actingUser, now));

            if (entity.AchievementPercentage >= 100m)
            {
                entity.Status = SalesTargetStatuses.Completed;
                entity.StatusHistory.Add(NewStatusHistory(
                    SalesTargetStatuses.Active, SalesTargetStatuses.Completed,
                    "Auto-completed on activate (100% achieved)",
                    actingUser, now));
            }

            await _repo.ActivateAsync(entity, cancellationToken);
            return SalesTargetMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        public async Task<SalesTargetDto?> DeactivateAsync(
            int id,
            SalesTargetRemarksRequestDto? request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            EnsureTransition(entity.Status, SalesTargetStatuses.Cancelled);

            var now = DateTimeOffset.UtcNow;
            var old = entity.Status;
            entity.Status = SalesTargetStatuses.Cancelled;
            entity.Remarks = request?.Remarks?.Trim() ?? entity.Remarks;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.StatusHistory.Add(NewStatusHistory(
                old, SalesTargetStatuses.Cancelled,
                request?.Remarks?.Trim() ?? "Target deactivated/cancelled",
                actingUser, now));

            await _repo.DeactivateAsync(entity, cancellationToken);
            return SalesTargetMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        public async Task<SalesTargetDto?> UpdateStatusAsync(
            int id,
            SalesTargetStatusUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            var target = SalesTargetStatusRules.Normalize(request.Status)
                ?? throw new InvalidOperationException($"Unknown status '{request.Status}'.");

            EnsureTransition(entity.Status, target);

            if (target == SalesTargetStatuses.Active)
            {
                await EnsureNoOverlapAsync(
                    entity.SalesPersonUserId,
                    entity.TargetCategory,
                    entity.StartDate,
                    entity.EndDate,
                    id,
                    target,
                    cancellationToken);
            }

            if (entity.Status.Equals(target, StringComparison.Ordinal))
            {
                return SalesTargetMapper.ToDto(entity);
            }

            var now = DateTimeOffset.UtcNow;
            var old = entity.Status;
            entity.Status = target;
            entity.Remarks = request.Remarks?.Trim() ?? entity.Remarks;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.StatusHistory.Add(NewStatusHistory(
                old, target,
                request.Remarks?.Trim() ?? $"{old} → {target}",
                actingUser, now));

            await _repo.UpdateAsync(entity, cancellationToken);
            return SalesTargetMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        public async Task<SalesTargetDto?> UpdateProgressAsync(
            int id,
            SalesTargetProgressUpdateRequestDto request,
            string actingUser,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, true, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            if (entity.Status is not (SalesTargetStatuses.Active or SalesTargetStatuses.Draft))
            {
                throw new InvalidOperationException(
                    $"Cannot update progress for status '{entity.Status}'.");
            }

            var achieved = SalesTargetCalculator.Round2(request.AchievedValue);
            if (achieved < 0)
            {
                throw new InvalidOperationException("Achieved value cannot be negative.");
            }

            if (SalesTargetCalculator.WouldExceedTarget(entity.TargetValue, achieved))
            {
                throw new InvalidOperationException("Achieved value cannot exceed target value.");
            }

            var now = DateTimeOffset.UtcNow;
            var oldAchieved = entity.AchievedValue;
            var pct = SalesTargetCalculator.CalcAchievementPercentage(entity.TargetValue, achieved);
            entity.AchievedValue = achieved;
            entity.RemainingValue = SalesTargetCalculator.CalcRemaining(entity.TargetValue, achieved);
            entity.AchievementPercentage = pct;
            entity.UpdatedBy = actingUser;
            entity.UpdatedDate = now;
            entity.ProgressHistory.Add(NewProgress(
                oldAchieved, achieved, pct, SalesTargetProgressActions.ManualUpdate,
                request.Remarks?.Trim() ?? "Progress updated",
                actingUser, now));

            var newStatus = SalesTargetCalculator.ResolveStatusAfterProgress(entity.Status, pct);
            if (newStatus != entity.Status)
            {
                var oldStatus = entity.Status;
                entity.Status = newStatus;
                entity.StatusHistory.Add(NewStatusHistory(
                    oldStatus, newStatus,
                    "Auto-completed when achievement reached 100%",
                    actingUser, now));
            }

            await _repo.UpdateProgressAsync(entity, cancellationToken);
            return SalesTargetMapper.ToDto(
                await _repo.GetByIdAsync(id, true, false, cancellationToken) ?? entity);
        }

        public async Task<IReadOnlyList<SalesTargetProgressDto>?> GetProgressAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, false, cancellationToken);
            return entity?.ProgressHistory.OrderBy(p => p.UpdatedOn)
                .Select(SalesTargetMapper.ToProgressDto).ToList();
        }

        public async Task<IReadOnlyList<SalesTargetHistoryDto>?> GetHistoryAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var entity = await _repo.GetByIdAsync(id, true, false, cancellationToken);
            return entity?.StatusHistory.OrderBy(h => h.ChangedOn)
                .Select(SalesTargetMapper.ToHistoryDto).ToList();
        }

        public async Task<SalesTargetDashboardDto> GetDashboardAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = (await _repo.GetDashboardAsync(cancellationToken))
                .Select(ApplyExpiry).ToList();

            return new SalesTargetDashboardDto
            {
                TotalCount = rows.Count,
                DraftCount = rows.Count(x => x.Status == SalesTargetStatuses.Draft),
                ActiveCount = rows.Count(x => x.Status == SalesTargetStatuses.Active),
                CompletedCount = rows.Count(x => x.Status == SalesTargetStatuses.Completed),
                ExpiredCount = rows.Count(x => x.Status == SalesTargetStatuses.Expired),
                CancelledCount = rows.Count(x => x.Status == SalesTargetStatuses.Cancelled),
                TotalTargetValue = rows.Sum(x => x.TargetValue),
                TotalAchievedValue = rows.Sum(x => x.AchievedValue),
                AverageAchievementPercentage = rows.Count == 0
                    ? 0m
                    : SalesTargetCalculator.Round2(rows.Average(x => x.AchievementPercentage)),
                Recent = rows.OrderByDescending(x => x.UpdatedDate).Take(10)
                    .Select(SalesTargetMapper.ToListItem).ToList()
            };
        }

        public async Task<SalesTargetReportDto> GetReportsAsync(
            SalesTargetListQueryDto? query,
            CancellationToken cancellationToken = default)
        {
            var rows = await GetAllAsync(query, cancellationToken);
            return new SalesTargetReportDto
            {
                GeneratedOn = DateTimeOffset.UtcNow.UtcDateTime.ToString("O"),
                RowCount = rows.Count,
                TotalTargetValue = rows.Sum(x => x.TargetValue),
                TotalAchievedValue = rows.Sum(x => x.AchievedValue),
                Rows = rows.ToList()
            };
        }

        public async Task<SalesTargetExportMetadataDto> ExportReportsAsync(
            SalesTargetExportRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var report = await GetReportsAsync(new SalesTargetListQueryDto
            {
                Status = request.Status,
                TargetCategory = request.TargetCategory,
                FinancialYear = request.FinancialYear
            }, cancellationToken);

            return new SalesTargetExportMetadataDto
            {
                FileName = $"sales-target-report-{DateTime.UtcNow:yyyyMMddHHmmss}.{(request.Format?.ToLowerInvariant() == "xlsx" ? "xlsx" : "csv")}",
                ContentType = request.Format?.ToLowerInvariant() == "xlsx"
                    ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    : "text/csv",
                Message = $"Export prepared for {report.RowCount} row(s) (placeholder).",
                GeneratedOn = DateTimeOffset.UtcNow.UtcDateTime.ToString("O")
            };
        }

        public Task<IReadOnlyList<string>> GetPermissionsAsync() =>
            Task.FromResult<IReadOnlyList<string>>(
            [
                "sales-targets.view",
                "sales-targets.create",
                "sales-targets.edit",
                "sales-targets.delete",
                "sales-targets.activate",
                "sales-targets.deactivate",
                "sales-targets.progress.update",
                "sales-targets.dashboard.view",
                "sales-targets.report.view"
            ]);

        public async Task<IReadOnlyList<SalesTargetLookupDto>> LookupSalespersonsAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetAllAsync(null, cancellationToken);
            var fromTargets = rows
                .Where(x => x.SalesPersonUserId is > 0)
                .Select(x => x.SalesPersonUserId!.Value);

            return fromTargets.Concat([1, 2, 3])
                .Distinct()
                .OrderBy(x => x)
                .Select(id => new SalesTargetLookupDto
                {
                    Id = id.ToString(),
                    Name = $"Salesperson {id}"
                })
                .ToList();
        }

        public async Task<IReadOnlyList<SalesTargetLookupDto>> LookupTeamsAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetAllAsync(null, cancellationToken);
            var teams = rows
                .Select(x => x.SalesTeam)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            if (teams.Count == 0)
            {
                teams = ["West Team", "North Team", "South Team"];
            }

            return teams.Select(t => new SalesTargetLookupDto { Id = t, Name = t }).ToList();
        }

        public async Task<IReadOnlyList<SalesTargetLookupDto>> LookupBranchesAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetAllAsync(null, cancellationToken);
            var branches = rows
                .Select(x => x.Branch)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            if (branches.Count == 0)
            {
                branches = ["Pune", "Mumbai", "Bengaluru", "Delhi"];
            }

            return branches.Select(b => new SalesTargetLookupDto { Id = b, Name = b }).ToList();
        }

        public async Task<IReadOnlyList<SalesTargetLookupDto>> LookupRegionalManagersAsync(
            CancellationToken cancellationToken = default)
        {
            var rows = await _repo.GetAllAsync(null, cancellationToken);
            var managers = rows
                .Select(x => x.RegionalManager)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            if (managers.Count == 0)
            {
                managers = ["RM West", "RM North", "RM South"];
            }

            return managers.Select(m => new SalesTargetLookupDto { Id = m, Name = m }).ToList();
        }

        private async Task EnsureNoOverlapAsync(
            int? salesPersonUserId,
            string category,
            DateOnly start,
            DateOnly end,
            int? excludeId,
            string status,
            CancellationToken cancellationToken)
        {
            if (status != SalesTargetStatuses.Active && status != SalesTargetStatuses.Draft)
            {
                // Only enforce overlap when activating / creating as Active.
                // Draft creates are also checked when activating.
            }

            if (status != SalesTargetStatuses.Active)
            {
                return;
            }

            if (await _repo.HasOverlappingActiveTargetAsync(
                    salesPersonUserId, category, start, end, excludeId, cancellationToken))
            {
                throw new InvalidOperationException(
                    "An overlapping active target already exists for the same salesperson, category, and period.");
            }
        }

        private static SalesTarget ApplyExpiry(SalesTarget entity)
        {
            // Service-based expiry signal for reads (non-persisting).
            if (entity.Status == SalesTargetStatuses.Active
                && entity.EndDate < DateOnly.FromDateTime(DateTime.UtcNow))
            {
                entity.Status = SalesTargetStatuses.Expired;
            }

            return entity;
        }

        private static (string TargetType, string Category, string AssignmentType, DateOnly Start, DateOnly End, string Status)
            ValidateAndNormalizeCreate(SalesTargetCreateRequestDto request)
        {
            ValidateCoreFields(
                request.TargetName,
                request.TargetValue,
                request.StartDate,
                request.EndDate,
                request.FinancialYear,
                request.TargetType,
                request.TargetCategory,
                request.AssignmentType,
                request.SalesPersonUserId,
                request.SalesTeam,
                request.Branch,
                request.RegionalManager,
                request.Currency);

            var targetType = SalesTargetTypeRules.Normalize(request.TargetType)
                ?? throw new InvalidOperationException($"Unknown target type '{request.TargetType}'.");
            var category = SalesTargetCategoryRules.Normalize(request.TargetCategory)
                ?? throw new InvalidOperationException($"Unknown target category '{request.TargetCategory}'.");
            var assignmentType = SalesTargetAssignmentTypeRules.Normalize(request.AssignmentType)
                ?? throw new InvalidOperationException($"Unknown assignment type '{request.AssignmentType}'.");

            var start = SalesTargetMapper.ParseDate(request.StartDate, DateOnly.FromDateTime(DateTime.UtcNow));
            var end = SalesTargetMapper.ParseDate(request.EndDate, start);
            if (end < start)
            {
                throw new InvalidOperationException("End date must be on or after start date.");
            }

            var status = SalesTargetStatusRules.Normalize(request.Status ?? SalesTargetStatuses.Draft)
                ?? SalesTargetStatuses.Draft;
            if (status is not (SalesTargetStatuses.Draft or SalesTargetStatuses.Active))
            {
                throw new InvalidOperationException("New sales targets may only start as Draft or Active.");
            }

            return (targetType, category, assignmentType, start, end, status);
        }

        private static void ValidateCoreFields(
            string targetName,
            decimal targetValue,
            string startDate,
            string endDate,
            int financialYear,
            string targetType,
            string targetCategory,
            string assignmentType,
            int? salesPersonUserId,
            string? salesTeam,
            string? branch,
            string? regionalManager,
            string currency)
        {
            if (string.IsNullOrWhiteSpace(targetName))
            {
                throw new InvalidOperationException("Target name is required.");
            }

            if (targetValue <= 0)
            {
                throw new InvalidOperationException("Target value must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(startDate))
            {
                throw new InvalidOperationException("Start date is required.");
            }

            if (string.IsNullOrWhiteSpace(endDate))
            {
                throw new InvalidOperationException("End date is required.");
            }

            if (financialYear <= 0)
            {
                throw new InvalidOperationException("Financial year is required.");
            }

            if (string.IsNullOrWhiteSpace(targetType))
            {
                throw new InvalidOperationException("Target type is required.");
            }

            if (string.IsNullOrWhiteSpace(targetCategory))
            {
                throw new InvalidOperationException("Target category is required.");
            }

            if (string.IsNullOrWhiteSpace(assignmentType))
            {
                throw new InvalidOperationException("Assignment type is required.");
            }

            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new InvalidOperationException("Currency is required.");
            }

            var normalizedAssignment = SalesTargetAssignmentTypeRules.Normalize(assignmentType);
            if (normalizedAssignment == SalesTargetAssignmentTypes.IndividualSalesperson
                && salesPersonUserId is null or <= 0)
            {
                throw new InvalidOperationException("Salesperson is required for individual assignment.");
            }

            if (normalizedAssignment == SalesTargetAssignmentTypes.SalesTeam
                && string.IsNullOrWhiteSpace(salesTeam))
            {
                throw new InvalidOperationException("Sales team is required for team assignment.");
            }

            if (normalizedAssignment == SalesTargetAssignmentTypes.Branch
                && string.IsNullOrWhiteSpace(branch))
            {
                throw new InvalidOperationException("Branch is required for branch assignment.");
            }

            if (normalizedAssignment == SalesTargetAssignmentTypes.RegionalManager
                && string.IsNullOrWhiteSpace(regionalManager))
            {
                throw new InvalidOperationException("Regional manager is required for regional assignment.");
            }
        }

        private static void EnsureTransition(string from, string to)
        {
            if (!SalesTargetStatusRules.CanTransition(from, to))
            {
                throw new InvalidOperationException(
                    $"Cannot transition sales target from '{from}' to '{to}'.");
            }
        }

        private static List<SalesTargetAssignment> BuildAssignments(
            IEnumerable<SalesTargetAssignmentRequestDto>? requests,
            string status,
            string actingUser,
            DateTimeOffset now)
        {
            if (requests is null)
            {
                return [];
            }

            return requests.Select(r =>
            {
                if (r.SalesPersonUserId <= 0)
                {
                    throw new InvalidOperationException("Assignment salesperson is required.");
                }

                if (r.AssignedTarget <= 0)
                {
                    throw new InvalidOperationException("Assigned target must be greater than zero.");
                }

                var assigned = SalesTargetCalculator.Round2(r.AssignedTarget);
                var achieved = SalesTargetCalculator.Round2(Math.Max(0, r.AchievedValue ?? 0m));
                if (SalesTargetCalculator.WouldExceedTarget(assigned, achieved))
                {
                    throw new InvalidOperationException(
                        "Assignment achieved value cannot exceed assigned target.");
                }

                return new SalesTargetAssignment
                {
                    SalesPersonUserId = r.SalesPersonUserId,
                    AssignedTarget = assigned,
                    AchievedValue = achieved,
                    AchievementPercentage = SalesTargetCalculator.CalcAchievementPercentage(assigned, achieved),
                    Status = status,
                    Remarks = r.Remarks?.Trim() ?? string.Empty,
                    AssignedBy = actingUser,
                    AssignedDate = now
                };
            }).ToList();
        }

        private static SalesTargetStatusHistory NewStatusHistory(
            string? oldStatus,
            string newStatus,
            string remarks,
            string changedBy,
            DateTimeOffset changedOn) => new()
        {
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Remarks = remarks,
            ChangedBy = changedBy,
            ChangedOn = changedOn
        };

        private static SalesTargetProgressHistory NewProgress(
            decimal oldAchieved,
            decimal newAchieved,
            decimal percentage,
            string action,
            string remarks,
            string updatedBy,
            DateTimeOffset updatedOn) => new()
        {
            OldAchievedValue = oldAchieved,
            NewAchievedValue = newAchieved,
            AchievementPercentage = percentage,
            Action = action,
            Remarks = remarks,
            UpdatedBy = updatedBy,
            UpdatedOn = updatedOn
        };
    }
}
