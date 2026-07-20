using System.Globalization;
using System.Text.RegularExpressions;
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
                var orgName = row.Organization?.Trim() ?? string.Empty;
                var organizationId = 0;

                if (orgName.Length > 0)
                {
                    var orgKey = orgName.ToLowerInvariant();

                    if (!orgByName.TryGetValue(orgKey, out organizationId))
                    {
                        if (!pendingNewOrgs.TryGetValue(orgKey, out var pendingOrg))
                        {
                            pendingOrg = BuildOrganization(row, orgName, masters);
                            pendingNewOrgs[orgKey] = pendingOrg;
                        }

                        organizationId = 0;
                    }

                    var leadWithOrg = BuildLead(row, organizationId, masters, userLookups, userId, now);
                    leadsToInsert.Add(leadWithOrg);

                    if (organizationId == 0 && pendingNewOrgs.TryGetValue(orgKey, out var linkedOrg))
                    {
                        leadWithOrg.Organization = linkedOrg;
                    }
                }
                else
                {
                    leadsToInsert.Add(BuildLead(row, 0, masters, userLookups, userId, now));
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

                var batchContactEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var batchContactMobiles = new HashSet<string>(StringComparer.Ordinal);
                foreach (var lead in leadsToInsert)
                {
                    await LeadContactSyncHelper.TryAddContactFromLeadAsync(
                        _context,
                        lead,
                        batchContactEmails,
                        batchContactMobiles,
                        cancellationToken);
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
                var errors = ValidateRowFields(row, masters, userLookups);
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
                Gst = row.Gst?.Trim() ?? string.Empty,
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
                LastName = row.LastName?.Trim() ?? string.Empty,
                Mobile = row.Mobile?.Trim() ?? string.Empty,
                Email = row.Email?.Trim() ?? string.Empty,
                Gender = ResolveLeadGender(row.Gender),
                Location = row.Location?.Trim() ?? string.Empty,
                Notes = ComposeNotes(row.Requirement, row.AdditionalDetails),
                LeadSource = ExcelLeadSource,
                LeadOwnerId = ResolveLeadOwnerId(row.LeadOwner, importingUserId, users),
                SalutationId = ResolveOptionalMasterId(masters.Salutations, row.Salutation),
                LeadStatusId = ResolveLeadStatusId(row.Status, masters),
                RequestTypeId = ResolveOptionalMasterId(masters.RequestTypes, row.RequestType),
                OrganizationId = organizationId > 0 ? organizationId : null,
                IsActive = true,
                CreatedAt = now,
                LeadDate = ParseLeadDate(row.LeadDate) ?? now.Date,
            };

            return lead;
        }

        private static int? ResolveLeadStatusId(string? status, MasterLookups masters)
        {
            if (!string.IsNullOrWhiteSpace(status))
            {
                var id = ResolveMasterId(masters.LeadStatuses, status);
                if (id is > 0) return id;
                if (TryResolveConversionStatusAlias(status, masters.LeadStatuses, out var aliasId))
                {
                    return aliasId;
                }
            }

            if (masters.LeadStatuses.TryGetValue(NormalizeKey(DefaultLeadStatusName), out var newStatusId))
            {
                return newStatusId;
            }

            return masters.LeadStatuses.Values.FirstOrDefault() is int fallback && fallback > 0
                ? fallback
                : null;
        }

        /// <summary>Accepts legacy conversion status aliases when resolving import status.</summary>
        private static bool TryResolveConversionStatusAlias(
            string status,
            Dictionary<string, int> leadStatuses,
            out int id)
        {
            id = 0;
            if (!LeadStatusMovedToDealSeed.IsConversionStatusName(status)) return false;
            foreach (var candidate in LeadStatusMovedToDealSeed.ConversionStatusLookupNames(status))
            {
                if (leadStatuses.TryGetValue(NormalizeKey(candidate), out id) && id > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static int ResolveLeadOwnerId(string? leadOwnerText, int importingUserId, UserLookups users)
        {
            if (string.IsNullOrWhiteSpace(leadOwnerText))
            {
                return importingUserId;
            }

            return TryResolveLeadOwnerId(leadOwnerText.Trim(), users, out var ownerId)
                ? ownerId
                : importingUserId;
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

        private static List<string> ValidateRowFields(LeadImportRowDto row, MasterLookups masters, UserLookups users)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(row.FirstName))
            {
                errors.Add("Full Name is required");
            }

            if (string.IsNullOrWhiteSpace(row.Organization))
            {
                errors.Add("Organization is required");
            }

            if (string.IsNullOrWhiteSpace(row.Industry))
            {
                errors.Add("Industry is required");
            }
            else if (!masters.Industries.ContainsKey(NormalizeKey(row.Industry)))
            {
                errors.Add("Invalid Industry");
            }

            if (string.IsNullOrWhiteSpace(row.Status))
            {
                errors.Add("Status is required");
            }
            else if (!masters.LeadStatuses.ContainsKey(NormalizeKey(row.Status)) &&
                     !TryResolveConversionStatusAlias(row.Status, masters.LeadStatuses, out _))
            {
                errors.Add("Invalid Status");
            }

            errors.AddRange(ValidateLeadOwnerField(row.LeadOwner, users));

            if (string.IsNullOrWhiteSpace(row.Requirement))
            {
                errors.Add("Requirement is required");
            }

            errors.AddRange(ValidateMobileField(row.Mobile));
            errors.AddRange(ValidateEmailField(row.Email));
            errors.AddRange(ValidateGenderField(row.Gender));

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

        private static List<string> ValidateMobileField(string? mobile)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(mobile))
            {
                return errors;
            }

            var digits = NormalizeMobile(mobile);
            if (digits.Length < 8 || digits.Length > 15)
            {
                errors.Add("Invalid Mobile");
            }

            return errors;
        }

        private static List<string> ValidateEmailField(string? email)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(email))
            {
                return errors;
            }

            var trimmed = email.Trim();
            if (!Regex.IsMatch(trimmed, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
            {
                errors.Add("Invalid Email");
            }

            return errors;
        }

        private static List<string> ValidateGenderField(string? gender)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(gender))
            {
                return errors;
            }

            var normalized = gender.Trim().ToLowerInvariant();
            if (normalized is not ("male" or "female" or "other" or "prefer not to say"))
            {
                errors.Add("Invalid Gender");
            }

            return errors;
        }

        private static List<string> ValidateLeadOwnerField(string? leadOwner, UserLookups users)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(leadOwner))
            {
                errors.Add("Lead Owner is required");
                return errors;
            }

            if (!TryResolveLeadOwnerId(leadOwner.Trim(), users, out _))
            {
                errors.Add("Invalid Lead Owner");
            }

            return errors;
        }

        private static string ResolveLeadGender(string? gender)
        {
            if (string.IsNullOrWhiteSpace(gender))
            {
                return DefaultLeadGender;
            }

            var trimmed = gender.Trim();
            return trimmed.ToLowerInvariant() switch
            {
                "male" => "Male",
                "female" => "Female",
                "other" => "Other",
                "prefer not to say" => "Prefer not to say",
                _ => DefaultLeadGender,
            };
        }

        private static DateTime? ParseLeadDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateTime.TryParse(
                    value.Trim(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var parsed))
            {
                return parsed.Date;
            }

            return null;
        }

        private static bool TryResolveLeadOwnerId(string leadOwnerText, UserLookups users, out int ownerId)
        {
            ownerId = 0;

            if (int.TryParse(leadOwnerText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedId)
                && users.ById.ContainsKey(parsedId))
            {
                ownerId = parsedId;
                return true;
            }

            var key = leadOwnerText.ToLowerInvariant();

            if (users.AssignableByEmail.TryGetValue(key, out var assignableByEmail))
            {
                ownerId = assignableByEmail;
                return true;
            }

            if (users.AssignableByName.TryGetValue(key, out var assignableByName))
            {
                ownerId = assignableByName;
                return true;
            }

            if (users.ByEmail.TryGetValue(key, out var byEmail))
            {
                ownerId = byEmail;
                return true;
            }

            if (users.ByName.TryGetValue(key, out var byName))
            {
                ownerId = byName;
                return true;
            }

            return false;
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
