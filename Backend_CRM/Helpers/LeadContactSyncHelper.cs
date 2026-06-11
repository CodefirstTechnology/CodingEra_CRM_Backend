using CRM.DATA;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>Creates a CRM contact from lead personal fields when a lead is added.</summary>
    public static class LeadContactSyncHelper
    {
        public static async Task TryAddContactFromLeadAsync(
            TaskDbcontext context,
            Lead lead,
            ISet<string>? batchEmails = null,
            ISet<string>? batchMobiles = null,
            CancellationToken cancellationToken = default)
        {
            var firstName = lead.FirstName?.Trim() ?? string.Empty;
            var lastName = lead.LastName?.Trim() ?? string.Empty;
            var email = lead.Email?.Trim() ?? string.Empty;
            var phone = lead.Mobile?.Trim() ?? string.Empty;
            var phoneDigits = NormalizeMobile(phone);

            if (firstName.Length == 0 && lastName.Length == 0 && email.Length == 0 && phoneDigits.Length == 0)
            {
                return;
            }

            if (email.Length > 0)
            {
                if (batchEmails?.Contains(email) == true)
                {
                    return;
                }

                if (await context.Contacts.AsNoTracking()
                        .AnyAsync(c => c.Email.ToLower() == email.ToLower(), cancellationToken))
                {
                    return;
                }
            }

            if (phoneDigits.Length > 0)
            {
                if (batchMobiles?.Contains(phoneDigits) == true)
                {
                    return;
                }

                var existingPhones = await context.Contacts.AsNoTracking()
                    .Where(c => c.Phone != null && c.Phone != string.Empty)
                    .Select(c => c.Phone)
                    .ToListAsync(cancellationToken);

                if (existingPhones.Any(p => NormalizeMobile(p) == phoneDigits))
                {
                    return;
                }
            }

            var salutation = string.Empty;
            if (lead.SalutationId is int sid && sid > 0)
            {
                salutation = await context.Salutations.AsNoTracking()
                    .Where(s => s.Id == sid)
                    .Select(s => s.Name)
                    .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
            }

            await context.Contacts.AddAsync(new Contact
            {
                Id = 0,
                Salutation = salutation,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = phone,
                Gender = lead.Gender?.Trim() ?? string.Empty,
                OrganizationId = lead.OrganizationId is > 0 ? lead.OrganizationId : null,
                Designation = string.Empty,
                Address = lead.Location?.Trim() ?? string.Empty,
            }, cancellationToken);

            if (email.Length > 0)
            {
                batchEmails?.Add(email);
            }

            if (phoneDigits.Length > 0)
            {
                batchMobiles?.Add(phoneDigits);
            }
        }

        private static string NormalizeMobile(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value.Where(char.IsDigit).ToArray());
        }
    }
}
