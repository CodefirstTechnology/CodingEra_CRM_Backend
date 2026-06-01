using System.Globalization;
using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Services
{
    public interface ILeadImportService
    {
        Task<LeadImportResultDto> ValidateImportAsync(
            IReadOnlyList<LeadImportRowDto> rows,
            CancellationToken cancellationToken = default);

        Task<LeadImportCommitResultDto> CommitImportAsync(
            int userId,
            IReadOnlyList<LeadImportRowDto> rows,
            CancellationToken cancellationToken = default);
    }

    public sealed class LeadImportService : ILeadImportService
    {
        private const int AdminRoleId = 2;
        private const string ExcelLeadSource = "Excel";
        private const string DefaultLeadStatusName = "New";
        private const string DefaultLeadGender = "Other";

        private readonly TaskDbcontext _context;

        public LeadImportService(TaskDbcontext context)
        {
            _context = context;
        }

        public async Task<LeadImportResultDto> ValidateImportAsync(
            IReadOnlyList<LeadImportRowDto> rows,
            CancellationToken cancellationToken = default)
        {
            var classification = await ClassifyRowsAsync(rows, cancellationToken);
            return new LeadImportResultDto
            {
                ValidRows = classification.ValidRows.Count,
                InvalidRows = classification.InvalidCount,
                DuplicateRows = classification.DuplicateCount,
                ValidationErrors = classification.ValidationErrors,
            };
        }

        public async Task<LeadImportCommitResultDto> CommitImportAsync(
            int userId,
            IReadOnlyList<LeadImportRowDto> rows,
            CancellationToken cancellationToken = default)
        {
            AuditUserValidation.SetAuditUser(_context, userId);

            var classification = await ClassifyRowsAsync(rows, cancellationToken);
            var result = new LeadImportCommitResultDto
            {
                DuplicateCount = classification.DuplicateCount,
                InvalidCount = classification.InvalidCount,
                ValidationErrors = classification.ValidationErrors,
            };

            if (classification.ValidRows.Count == 0)
            {
                return result;
            }

            var masters = classification.Masters;
            var orgByName = await LoadOrganizationNameMapAsync(cancellationToken);
            var userLookups = classification.UserLookups;
            var pendingNewOrgs = new Dictionary<string, Organization>(StringComparer.OrdinalIgnoreCase);
            var leadsToInsert = new List<Lead>(classification.ValidRows.Count);
            var now = DateTime.UtcNow;

            foreach (var entry in classification.ValidRows)
            {
                var row = entry.Row;
                var orgName = row.Organization!.Trim();
                var orgKey = orgName.ToLowerInvariant();

                if (!orgByName.TryGetValue(orgKey, out var organizationId))
                {
                    if (!pendingNewOrgs.TryGetValue(orgKey, out var pendingOrg))
                    {
                        pendingOrg = BuildOrganization(row, orgName, masters);
                        pendingNewOrgs[orgKey] = pendingOrg;
                    }

                    organizationId = 0;
                }

                var lead = BuildLead(row, organizationId, masters, userLookups, userId, now);
                leadsToInsert.Add(lead);

                if (organizationId == 0 && pendingNewOrgs.TryGetValue(orgKey, out var linkedOrg))
                {
                    lead.Organization = linkedOrg;
                }
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (pendingNewOrgs.Count > 0)
                {
                    var newOrgs = pendingNewOrgs.Values.ToList();
                    await _context.Organizations.AddRangeAsync(newOrgs, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);

                    foreach (var org in newOrgs)
                    {
                        orgByName[org.Name.ToLowerInvariant()] = org.Id;
                    }
                }

                foreach (var lead in leadsToInsert)
                {
                    if (lead.OrganizationId is null or 0 && lead.Organization != null)
                    {
                        lead.OrganizationId = lead.Organization.Id;
                        lead.Organization = null;
                    }
                }

                await _context.Leads.AddRangeAsync(leadsToInsert, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                result.ImportedCount = leadsToInsert.Count;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return result;
        }

        private async Task<ImportClassification> ClassifyRowsAsync(
            IReadOnlyList<LeadImportRowDto> rows,
            CancellationToken cancellationToken)
        {
            var masters = await LoadMasterLookupsAsync(cancellationToken);
            var userLookups = await LoadUserLookupsAsync(cancellationToken);
            var existingContacts = await LoadExistingLeadContactsAsync(cancellationToken);

            var batchEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var batchMobiles = new HashSet<string>(StringComparer.Ordinal);

            var classification = new ImportClassification(masters, userLookups);
            var rowIndex = 0;

            foreach (var row in rows)
            {
                rowIndex++;
                var rowNumber = row.RowNumber > 0 ? row.RowNumber : rowIndex + 1;
                var errors = ValidateRowFields(row, masters);
                var duplicateErrors = CollectDuplicateErrors(row, existingContacts, batchEmails, batchMobiles);

                if (duplicateErrors.Count > 0)
                {
                    errors.AddRange(duplicateErrors);
                    classification.DuplicateCount++;
                    classification.ValidationErrors.Add(new LeadImportRowErrorDto
                    {
                        RowNumber = rowNumber,
                        IsDuplicate = true,
                        Errors = errors,
                    });
                    continue;
                }

                RegisterBatchContacts(row, batchEmails, batchMobiles);

                if (errors.Count > 0)
                {
                    classification.InvalidCount++;
                    classification.ValidationErrors.Add(new LeadImportRowErrorDto
                    {
                        RowNumber = rowNumber,
                        IsDuplicate = false,
                        Errors = errors,
                    });
                }
                else
                {
                    classification.ValidRows.Add(new ValidImportRow(rowNumber, row));
                }
            }

            return classification;
        }

        private static Organization BuildOrganization(LeadImportRowDto row, string orgName, MasterLookups masters)
        {
            return new Organization
            {
                Id = 0,
                Name = orgName,
                Website = row.Website?.Trim() ?? string.Empty,
                AnnualRevenue = ParseAnnualRevenue(row.AnnualRevenue),
                IndustryId = ResolveMasterId(masters.Industries, row.Industry),
                EmployeeCountId = ResolveOptionalMasterId(masters.EmployeeCounts, row.NoOfEmployees),
                TerritoryId = ResolveOptionalMasterId(masters.Territories, row.Territory),
                IsActive = true,
            };
        }

        private static Lead BuildLead(
            LeadImportRowDto row,
            int organizationId,
            MasterLookups masters,
            UserLookups users,
            int importingUserId,
            DateTime now)
        {
            var lead = new Lead
            {
                Id = 0,
                FirstName = row.FirstName!.Trim(),
                LastName = row.LastName!.Trim(),
                Mobile = row.Mobile?.Trim() ?? string.Empty,
                Email = row.Email?.Trim() ?? string.Empty,
                Gender = DefaultLeadGender,
                Notes = ComposeNotes(row.Requirement, row.AdditionalDetails),
                LeadSource = ExcelLeadSource,
                LeadOwnerId = ResolveLeadOwnerId(row.LeadOwner, importingUserId, users),
                SalutationId = ResolveOptionalMasterId(masters.Salutations, row.Salutation),
                LeadStatusId = ResolveLeadStatusId(row.Status, masters),
                RequestTypeId = ResolveOptionalMasterId(masters.RequestTypes, row.RequestType),
                OrganizationId = organizationId > 0 ? organizationId : null,
                IsActive = true,
                CreatedAt = now,
            };

            return lead;
        }

        private static int? ResolveLeadStatusId(string? status, MasterLookups masters)
        {
            if (!string.IsNullOrWhiteSpace(status))
            {
                return ResolveMasterId(masters.LeadStatuses, status);
            }

            if (masters.LeadStatuses.TryGetValue(NormalizeKey(DefaultLeadStatusName), out var newStatusId))
            {
                return newStatusId;
            }

            return masters.LeadStatuses.Values.FirstOrDefault() is int fallback && fallback > 0
                ? fallback
                : null;
        }

        private static int ResolveLeadOwnerId(string? leadOwnerText, int importingUserId, UserLookups users)
        {
            if (string.IsNullOrWhiteSpace(leadOwnerText))
            {
                return importingUserId;
            }

            var trimmed = leadOwnerText.Trim();

            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedId)
                && users.ById.ContainsKey(parsedId))
            {
                return parsedId;
            }

            var key = trimmed.ToLowerInvariant();

            if (users.AssignableByEmail.TryGetValue(key, out var assignableByEmail))
            {
                return assignableByEmail;
            }

            if (users.AssignableByName.TryGetValue(key, out var assignableByName))
            {
                return assignableByName;
            }

            if (users.ByEmail.TryGetValue(key, out var byEmail))
            {
                return byEmail;
            }

            if (users.ByName.TryGetValue(key, out var byName))
            {
                return byName;
            }

            return importingUserId;
        }

        private static int? ResolveOptionalMasterId(Dictionary<string, int> map, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return map.TryGetValue(NormalizeKey(value), out var id) ? id : null;
        }

        private static int? ResolveMasterId(Dictionary<string, int> map, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return map.TryGetValue(NormalizeKey(value), out var id) ? id : null;
        }

        private static decimal? ParseAnnualRevenue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var cleaned = new string(value.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
            if (cleaned.Length == 0)
            {
                return null;
            }

            return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }

        private static string ComposeNotes(string? requirement, string? additionalDetails)
        {
            var req = requirement?.Trim() ?? string.Empty;
            var add = additionalDetails?.Trim() ?? string.Empty;

            if (req.Length == 0)
            {
                return add;
            }

            if (add.Length == 0 || string.Equals(add, req, StringComparison.Ordinal))
            {
                return req;
            }

            if (add.Contains(req, StringComparison.Ordinal))
            {
                return add;
            }

            return $"{req}\n\n{add}";
        }

        private async Task<Dictionary<string, int>> LoadOrganizationNameMapAsync(CancellationToken cancellationToken)
        {
            var rows = await _context.Organizations.AsNoTracking()
                .Select(o => new { o.Id, o.Name })
                .ToListAsync(cancellationToken);

            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                var key = row.Name.Trim().ToLowerInvariant();
                if (key.Length == 0 || map.ContainsKey(key))
                {
                    continue;
                }

                map[key] = row.Id;
            }

            return map;
        }

        private async Task<UserLookups> LoadUserLookupsAsync(CancellationToken cancellationToken)
        {
            var rows = await _context.Users.AsNoTracking()
                .Where(u => u.IsActive)
                .Select(u => new { u.Id, u.FullName, u.Email, u.RoleId })
                .ToListAsync(cancellationToken);

            var byId = new Dictionary<int, int>();
            var byName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var byEmail = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var assignableByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var assignableByEmail = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                byId[row.Id] = row.Id;

                var emailKey = row.Email.Trim().ToLowerInvariant();
                if (emailKey.Length > 0 && !byEmail.ContainsKey(emailKey))
                {
                    byEmail[emailKey] = row.Id;
                }

                var nameKey = row.FullName.Trim().ToLowerInvariant();
                if (nameKey.Length > 0 && !byName.ContainsKey(nameKey))
                {
                    byName[nameKey] = row.Id;
                }

                var isAssignable = row.RoleId != AdminRoleId;
                if (!isAssignable)
                {
                    continue;
                }

                if (emailKey.Length > 0 && !assignableByEmail.ContainsKey(emailKey))
                {
                    assignableByEmail[emailKey] = row.Id;
                }

                if (nameKey.Length > 0 && !assignableByName.ContainsKey(nameKey))
                {
                    assignableByName[nameKey] = row.Id;
                }
            }

            return new UserLookups(byId, byName, byEmail, assignableByName, assignableByEmail);
        }

        private static List<string> ValidateRowFields(LeadImportRowDto row, MasterLookups masters)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(row.FirstName))
            {
                errors.Add("First Name is required.");
            }

            if (string.IsNullOrWhiteSpace(row.LastName))
            {
                errors.Add("Last Name is required.");
            }

            if (string.IsNullOrWhiteSpace(row.Requirement))
            {
                errors.Add("Requirement is required.");
            }

            if (string.IsNullOrWhiteSpace(row.Organization))
            {
                errors.Add("Organization is required.");
            }

            if (string.IsNullOrWhiteSpace(row.Industry))
            {
                errors.Add("Industry is required.");
            }
            else if (!masters.Industries.ContainsKey(NormalizeKey(row.Industry)))
            {
                errors.Add("Industry not found");
            }

            if (!string.IsNullOrWhiteSpace(row.Salutation) &&
                !masters.Salutations.ContainsKey(NormalizeKey(row.Salutation)))
            {
                errors.Add("Invalid Salutation");
            }

            if (!string.IsNullOrWhiteSpace(row.Territory) &&
                !masters.Territories.ContainsKey(NormalizeKey(row.Territory)))
            {
                errors.Add("Invalid Territory");
            }

            if (!string.IsNullOrWhiteSpace(row.Status) &&
                !masters.LeadStatuses.ContainsKey(NormalizeKey(row.Status)))
            {
                errors.Add("Invalid Status");
            }

            if (!string.IsNullOrWhiteSpace(row.RequestType) &&
                !masters.RequestTypes.ContainsKey(NormalizeKey(row.RequestType)))
            {
                errors.Add("Invalid Request Type");
            }

            if (!string.IsNullOrWhiteSpace(row.NoOfEmployees) &&
                !masters.EmployeeCounts.ContainsKey(NormalizeKey(row.NoOfEmployees)))
            {
                errors.Add("Invalid Employee Count");
            }

            return errors;
        }

        private static List<string> CollectDuplicateErrors(
            LeadImportRowDto row,
            ExistingLeadContacts existing,
            HashSet<string> batchEmails,
            HashSet<string> batchMobiles)
        {
            var errors = new List<string>();
            var email = row.Email?.Trim() ?? string.Empty;
            var mobile = NormalizeMobile(row.Mobile);

            if (email.Length == 0 && mobile.Length == 0)
            {
                return errors;
            }

            if (email.Length > 0 &&
                (existing.Emails.Contains(email) || batchEmails.Contains(email)))
            {
                errors.Add("Duplicate Email");
            }

            if (mobile.Length > 0 &&
                (existing.Mobiles.Contains(mobile) || batchMobiles.Contains(mobile)))
            {
                errors.Add("Duplicate Mobile");
            }

            return errors;
        }

        private static void RegisterBatchContacts(
            LeadImportRowDto row,
            HashSet<string> batchEmails,
            HashSet<string> batchMobiles)
        {
            var email = row.Email?.Trim() ?? string.Empty;
            if (email.Length > 0)
            {
                batchEmails.Add(email);
            }

            var mobile = NormalizeMobile(row.Mobile);
            if (mobile.Length > 0)
            {
                batchMobiles.Add(mobile);
            }
        }

        private async Task<MasterLookups> LoadMasterLookupsAsync(CancellationToken cancellationToken)
        {
            return new MasterLookups
            {
                Salutations = await LoadActiveMasterNamesAsync(_context.Salutations, cancellationToken),
                LeadStatuses = await LoadActiveMasterNamesAsync(_context.LeadStatuses, cancellationToken),
                RequestTypes = await LoadActiveMasterNamesAsync(_context.RequestTypes, cancellationToken),
                Industries = await LoadActiveMasterNamesAsync(_context.Industries, cancellationToken),
                EmployeeCounts = await LoadActiveMasterNamesAsync(_context.EmployeeCounts, cancellationToken),
                Territories = await LoadActiveMasterNamesAsync(_context.Territories, cancellationToken),
            };
        }

        private static async Task<Dictionary<string, int>> LoadActiveMasterNamesAsync<TEntity>(
            DbSet<TEntity> set,
            CancellationToken cancellationToken)
            where TEntity : class, IMasterDataRow
        {
            var rows = await set.AsNoTracking()
                .Where(e => e.IsActive)
                .Select(e => new { e.Id, e.Name })
                .ToListAsync(cancellationToken);

            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                var key = NormalizeKey(row.Name);
                if (key.Length == 0 || map.ContainsKey(key))
                {
                    continue;
                }

                map[key] = row.Id;
            }

            return map;
        }

        private async Task<ExistingLeadContacts> LoadExistingLeadContactsAsync(CancellationToken cancellationToken)
        {
            var rows = await _context.Leads.AsNoTracking()
                .Select(l => new { l.Email, l.Mobile })
                .ToListAsync(cancellationToken);

            var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var mobiles = new HashSet<string>(StringComparer.Ordinal);

            foreach (var row in rows)
            {
                var email = row.Email?.Trim() ?? string.Empty;
                if (email.Length > 0)
                {
                    emails.Add(email);
                }

                var mobile = NormalizeMobile(row.Mobile);
                if (mobile.Length > 0)
                {
                    mobiles.Add(mobile);
                }
            }

            return new ExistingLeadContacts(emails, mobiles);
        }

        private static string NormalizeKey(string? value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        private static string NormalizeMobile(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value.Where(char.IsDigit).ToArray());
        }

        private sealed class ImportClassification
        {
            public ImportClassification(MasterLookups masters, UserLookups userLookups)
            {
                Masters = masters;
                UserLookups = userLookups;
            }

            public MasterLookups Masters { get; }
            public UserLookups UserLookups { get; }
            public List<ValidImportRow> ValidRows { get; } = new();
            public int DuplicateCount { get; set; }
            public int InvalidCount { get; set; }
            public List<LeadImportRowErrorDto> ValidationErrors { get; } = new();
        }

        private sealed record ValidImportRow(int RowNumber, LeadImportRowDto Row);

        private sealed class MasterLookups
        {
            public Dictionary<string, int> Salutations { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> LeadStatuses { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> RequestTypes { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> Industries { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> EmployeeCounts { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> Territories { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class UserLookups
        {
            public UserLookups(
                Dictionary<int, int> byId,
                Dictionary<string, int> byName,
                Dictionary<string, int> byEmail,
                Dictionary<string, int> assignableByName,
                Dictionary<string, int> assignableByEmail)
            {
                ById = byId;
                ByName = byName;
                ByEmail = byEmail;
                AssignableByName = assignableByName;
                AssignableByEmail = assignableByEmail;
            }

            public Dictionary<int, int> ById { get; }
            public Dictionary<string, int> ByName { get; }
            public Dictionary<string, int> ByEmail { get; }
            public Dictionary<string, int> AssignableByName { get; }
            public Dictionary<string, int> AssignableByEmail { get; }
        }

        private sealed class ExistingLeadContacts
        {
            public ExistingLeadContacts(HashSet<string> emails, HashSet<string> mobiles)
            {
                Emails = emails;
                Mobiles = mobiles;
            }

            public HashSet<string> Emails { get; }
            public HashSet<string> Mobiles { get; }
        }
    }
}
