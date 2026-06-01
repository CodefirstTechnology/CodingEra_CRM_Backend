using CRM.DATA;
using CRM.DTO;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Services
{
    public interface ILeadImportService
    {
        Task<LeadImportResultDto> ValidateImportAsync(
            IReadOnlyList<LeadImportRowDto> rows,
            CancellationToken cancellationToken = default);
    }

    public sealed class LeadImportService : ILeadImportService
    {
        private readonly TaskDbcontext _context;

        public LeadImportService(TaskDbcontext context)
        {
            _context = context;
        }

        public async Task<LeadImportResultDto> ValidateImportAsync(
            IReadOnlyList<LeadImportRowDto> rows,
            CancellationToken cancellationToken = default)
        {
            var masters = await LoadMasterLookupsAsync(cancellationToken);
            var existingContacts = await LoadExistingLeadContactsAsync(cancellationToken);

            var batchEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var batchMobiles = new HashSet<string>(StringComparer.Ordinal);

            var result = new LeadImportResultDto();
            var rowIndex = 0;

            foreach (var row in rows)
            {
                rowIndex++;
                var rowNumber = row.RowNumber > 0 ? row.RowNumber : rowIndex + 1;
                var errors = ValidateRowFields(row, masters);
                var isDuplicate = IsDuplicateRow(row, existingContacts, batchEmails, batchMobiles);

                if (isDuplicate)
                {
                    errors.Add("Duplicate lead: email or mobile already exists in CRM or this upload.");
                    result.DuplicateRows++;
                    result.ValidationErrors.Add(new LeadImportRowErrorDto
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
                    result.InvalidRows++;
                    result.ValidationErrors.Add(new LeadImportRowErrorDto
                    {
                        RowNumber = rowNumber,
                        IsDuplicate = false,
                        Errors = errors,
                    });
                }
                else
                {
                    result.ValidRows++;
                }
            }

            return result;
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
                errors.Add($"Industry '{row.Industry.Trim()}' does not exist or is inactive.");
            }

            if (!string.IsNullOrWhiteSpace(row.Salutation) &&
                !masters.Salutations.ContainsKey(NormalizeKey(row.Salutation)))
            {
                errors.Add($"Salutation '{row.Salutation.Trim()}' does not exist or is inactive.");
            }

            if (!string.IsNullOrWhiteSpace(row.Territory) &&
                !masters.Territories.ContainsKey(NormalizeKey(row.Territory)))
            {
                errors.Add($"Territory '{row.Territory.Trim()}' does not exist or is inactive.");
            }

            if (!string.IsNullOrWhiteSpace(row.Status) &&
                !masters.LeadStatuses.ContainsKey(NormalizeKey(row.Status)))
            {
                errors.Add($"Status '{row.Status.Trim()}' does not exist or is inactive.");
            }

            if (!string.IsNullOrWhiteSpace(row.RequestType) &&
                !masters.RequestTypes.ContainsKey(NormalizeKey(row.RequestType)))
            {
                errors.Add($"Request Type '{row.RequestType.Trim()}' does not exist or is inactive.");
            }

            if (!string.IsNullOrWhiteSpace(row.NoOfEmployees) &&
                !masters.EmployeeCounts.ContainsKey(NormalizeKey(row.NoOfEmployees)))
            {
                errors.Add($"No Of Employees '{row.NoOfEmployees.Trim()}' does not exist or is inactive.");
            }

            return errors;
        }

        private static bool IsDuplicateRow(
            LeadImportRowDto row,
            ExistingLeadContacts existing,
            HashSet<string> batchEmails,
            HashSet<string> batchMobiles)
        {
            var email = row.Email?.Trim() ?? string.Empty;
            var mobile = NormalizeMobile(row.Mobile);

            if (email.Length == 0 && mobile.Length == 0)
            {
                return false;
            }

            if (email.Length > 0)
            {
                if (existing.Emails.Contains(email) || batchEmails.Contains(email))
                {
                    return true;
                }
            }

            if (mobile.Length > 0)
            {
                if (existing.Mobiles.Contains(mobile) || batchMobiles.Contains(mobile))
                {
                    return true;
                }
            }

            return false;
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

        private sealed class MasterLookups
        {
            public Dictionary<string, int> Salutations { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> LeadStatuses { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> RequestTypes { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> Industries { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> EmployeeCounts { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> Territories { get; init; } = new(StringComparer.OrdinalIgnoreCase);
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
