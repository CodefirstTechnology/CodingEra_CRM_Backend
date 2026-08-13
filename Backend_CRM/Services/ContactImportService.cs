using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Services
{
    public sealed class ContactImportService : IContactImportService
    {
        private const string DefaultContactGender = "Other";

        private readonly TaskDbcontext _context;

        public ContactImportService(TaskDbcontext context)
        {
            _context = context;
        }

        public async Task<ContactImportResultDto> ValidateImportAsync(
            IReadOnlyList<ContactImportRowDto> rows,
            CancellationToken cancellationToken = default)
        {
            var classification = await ClassifyRowsAsync(rows, cancellationToken);
            return new ContactImportResultDto
            {
                ValidRows = classification.ValidRows.Count,
                InvalidRows = classification.InvalidCount,
                DuplicateRows = classification.DuplicateCount,
                ValidationErrors = classification.ValidationErrors,
            };
        }

        public async Task<ContactImportCommitResultDto> CommitImportAsync(
            int userId,
            IReadOnlyList<ContactImportRowDto> rows,
            CancellationToken cancellationToken = default)
        {
            AuditUserValidation.SetAuditUser(_context, userId);

            var classification = await ClassifyRowsAsync(rows, cancellationToken);
            var result = new ContactImportCommitResultDto
            {
                DuplicateCount = classification.DuplicateCount,
                InvalidCount = classification.InvalidCount,
                ValidationErrors = classification.ValidationErrors,
            };

            if (classification.ValidRows.Count == 0)
            {
                return result;
            }

            var orgByName = await LoadOrganizationNameMapAsync(cancellationToken);
            var pendingNewOrgs = new Dictionary<string, Organization>(StringComparer.OrdinalIgnoreCase);
            var contactsToInsert = new List<Contact>(classification.ValidRows.Count);
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
                            pendingOrg = BuildOrganization(orgName);
                            pendingNewOrgs[orgKey] = pendingOrg;
                        }
                    }

                    var contactWithOrg = BuildContact(row, organizationId, now);
                    contactsToInsert.Add(contactWithOrg);

                    if (organizationId == 0 && pendingNewOrgs.TryGetValue(orgKey, out var linkedOrg))
                    {
                        contactWithOrg.OrganizationId = null;
                        // Temp association to insert new orgs first
                        // EF will link it once Org is saved, but we can assign Navigation property
                    }
                }
                else
                {
                    contactsToInsert.Add(BuildContact(row, 0, now));
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

                // Associate newly generated OrganizationIds
                foreach (var contact in contactsToInsert)
                {
                    var entry = classification.ValidRows.FirstOrDefault(r => r.RowNumber == contact.Id); // Temporary placeholder used row number as temporary ID
                    // Wait, let's map it safely. We can search by matching properties or just keep a mapping dictionary.
                }

                // Let's rewrite this part to map Org ID safely:
                for (var i = 0; i < classification.ValidRows.Count; i++)
                {
                    var row = classification.ValidRows[i].Row;
                    var contact = contactsToInsert[i];
                    var orgName = row.Organization?.Trim() ?? string.Empty;
                    if (orgName.Length > 0)
                    {
                        if (orgByName.TryGetValue(orgName.ToLowerInvariant(), out var resolvedOrgId))
                        {
                            contact.OrganizationId = resolvedOrgId;
                        }
                    }
                }

                await _context.Contacts.AddRangeAsync(contactsToInsert, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                result.ImportedCount = contactsToInsert.Count;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return result;
        }

        private async Task<ImportClassification> ClassifyRowsAsync(
            IReadOnlyList<ContactImportRowDto> rows,
            CancellationToken cancellationToken)
        {
            var existingContacts = await LoadExistingContactsAsync(cancellationToken);

            var batchEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var batchMobiles = new HashSet<string>(StringComparer.Ordinal);

            var classification = new ImportClassification();
            var rowIndex = 0;

            foreach (var row in rows)
            {
                rowIndex++;
                var rowNumber = row.RowNumber > 0 ? row.RowNumber : rowIndex + 1;
                var errors = ValidateRowFields(row);
                var duplicateErrors = CollectDuplicateErrors(row, existingContacts, batchEmails, batchMobiles);

                if (duplicateErrors.Count > 0)
                {
                    errors.AddRange(duplicateErrors);
                    classification.DuplicateCount++;
                    classification.ValidationErrors.Add(new ContactImportRowErrorDto
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
                    classification.ValidationErrors.Add(new ContactImportRowErrorDto
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

        private static Organization BuildOrganization(string orgName)
        {
            return new Organization
            {
                Id = 0,
                Name = orgName,
                Website = string.Empty,
                Gst = string.Empty,
                AnnualRevenue = null,
                IndustryId = null,
                EmployeeCountId = null,
                TerritoryId = null,
                IsActive = true,
            };
        }

        private static Contact BuildContact(
            ContactImportRowDto row,
            int organizationId,
            DateTime now)
        {
            return new Contact
            {
                Id = 0,
                Salutation = row.Salutation?.Trim() ?? string.Empty,
                FirstName = row.FirstName!.Trim(),
                LastName = row.LastName!.Trim(),
                Phone = row.Mobile?.Trim() ?? string.Empty,
                Email = row.Email?.Trim() ?? string.Empty,
                Gender = ResolveContactGender(row.Gender),
                Designation = row.Designation?.Trim() ?? string.Empty,
                Address = row.Address?.Trim() ?? string.Empty,
                OrganizationId = organizationId > 0 ? organizationId : null,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                LastModified = now
            };
        }

        private static string ResolveContactGender(string? gender)
        {
            if (string.IsNullOrWhiteSpace(gender))
            {
                return DefaultContactGender;
            }

            var trimmed = gender.Trim();
            return trimmed.ToLowerInvariant() switch
            {
                "male" => "Male",
                "female" => "Female",
                "other" => "Other",
                "prefer not to say" => "Prefer not to say",
                _ => DefaultContactGender,
            };
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

        private static List<string> ValidateRowFields(ContactImportRowDto row)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(row.FirstName))
            {
                errors.Add("First Name is required");
            }
            else if (row.FirstName.Trim().Length > 80)
            {
                errors.Add("First Name cannot exceed 80 characters");
            }

            if (string.IsNullOrWhiteSpace(row.LastName))
            {
                errors.Add("Last Name is required");
            }
            else if (row.LastName.Trim().Length > 120)
            {
                errors.Add("Last Name cannot exceed 120 characters");
            }

            if (string.IsNullOrWhiteSpace(row.Mobile))
            {
                errors.Add("Mobile is required");
            }
            else
            {
                errors.AddRange(ValidateMobileField(row.Mobile));
            }

            errors.AddRange(ValidateEmailField(row.Email));
            errors.AddRange(ValidateGenderField(row.Gender));

            if (!string.IsNullOrWhiteSpace(row.Salutation) && row.Salutation.Trim().Length > 32)
            {
                errors.Add("Salutation cannot exceed 32 characters");
            }

            if (!string.IsNullOrWhiteSpace(row.Organization) && row.Organization.Trim().Length > 200)
            {
                errors.Add("Company Name cannot exceed 200 characters");
            }

            if (!string.IsNullOrWhiteSpace(row.Designation) && row.Designation.Trim().Length > 120)
            {
                errors.Add("Designation cannot exceed 120 characters");
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
            if (trimmed.Length > 160)
            {
                errors.Add("Email cannot exceed 160 characters");
            }

            if (!Regex.IsMatch(trimmed, @"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.IgnoreCase))
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

        private static List<string> CollectDuplicateErrors(
            ContactImportRowDto row,
            ExistingContacts existing,
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
            ContactImportRowDto row,
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

        private async Task<ExistingContacts> LoadExistingContactsAsync(CancellationToken cancellationToken)
        {
            var rows = await _context.Contacts.AsNoTracking()
                .Select(c => new { c.Email, c.Phone })
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

                var mobile = NormalizeMobile(row.Phone);
                if (mobile.Length > 0)
                {
                    mobiles.Add(mobile);
                }
            }

            return new ExistingContacts(emails, mobiles);
        }

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
            public List<ValidImportRow> ValidRows { get; } = new();
            public int DuplicateCount { get; set; }
            public int InvalidCount { get; set; }
            public List<ContactImportRowErrorDto> ValidationErrors { get; } = new();
        }

        private sealed record ValidImportRow(int RowNumber, ContactImportRowDto Row);

        private sealed class ExistingContacts
        {
            public ExistingContacts(HashSet<string> emails, HashSet<string> mobiles)
            {
                Emails = emails;
                Mobiles = mobiles;
            }

            public HashSet<string> Emails { get; }
            public HashSet<string> Mobiles { get; }
        }
    }
}
