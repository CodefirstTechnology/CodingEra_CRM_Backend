using System.Text.Json;
using CRM.DTO;
using CRM.models;

namespace CRM.Helpers
{
    public static class CompanyProfileMappingHelper
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        public static CompanyProfileDto ToDto(CompanyProfile row, bool includeLogoPayload = true)
        {
            return new CompanyProfileDto
            {
                BrandName = row.BrandName ?? string.Empty,
                CompanyName = row.CompanyName ?? string.Empty,
                Tagline = row.Tagline ?? string.Empty,
                BusinessLine = row.BusinessLine ?? string.Empty,
                LogoContentType = row.LogoContentType ?? string.Empty,
                LogoBase64 = includeLogoPayload && !string.IsNullOrWhiteSpace(row.LogoBase64)
                    ? row.LogoBase64
                    : null,
                FaviconContentType = row.FaviconContentType ?? string.Empty,
                FaviconBase64 = includeLogoPayload && !string.IsNullOrWhiteSpace(row.FaviconBase64)
                    ? row.FaviconBase64
                    : null,
                Gstin = row.Gstin ?? string.Empty,
                CinNumber = row.CinNumber ?? string.Empty,
                Address = row.Address ?? string.Empty,
                ContactNumber = row.ContactNumber ?? string.Empty,
                Email = row.Email ?? string.Empty,
                Website = row.Website ?? string.Empty,
                BankName = row.BankName ?? string.Empty,
                AccountNumber = row.AccountNumber ?? string.Empty,
                IfscCode = row.IfscCode ?? string.Empty,
                BranchName = row.BranchName ?? string.Empty,
                SignatoryName = row.SignatoryName ?? string.Empty,
                SignatoryMobile = row.SignatoryMobile ?? string.Empty,
                Terms = ParseTerms(row.TermsConditionsJson),
                IntroText = row.IntroText ?? string.Empty,
                TransportationLabel = row.TransportationLabel ?? string.Empty,
                Jurisdiction = row.Jurisdiction ?? string.Empty,
                DefaultGstPercent = row.DefaultGstPercent,
                UpdatedAt = row.UpdatedAt,
            };
        }

        public static CompanyProfileBrandingDto ToBrandingDto(CompanyProfile row)
        {
            return new CompanyProfileBrandingDto
            {
                BrandName = row.BrandName ?? string.Empty,
                CompanyName = row.CompanyName ?? string.Empty,
                Tagline = row.Tagline ?? string.Empty,
                LogoContentType = row.LogoContentType ?? string.Empty,
                LogoBase64 = string.IsNullOrWhiteSpace(row.LogoBase64) ? null : row.LogoBase64,
                FaviconContentType = row.FaviconContentType ?? string.Empty,
                FaviconBase64 = string.IsNullOrWhiteSpace(row.FaviconBase64) ? null : row.FaviconBase64,
            };
        }

        public static void Apply(CompanyProfile row, CompanyProfileUpsertDto dto)
        {
            row.BrandName = (dto.BrandName ?? string.Empty).Trim();
            row.CompanyName = (dto.CompanyName ?? string.Empty).Trim();
            row.Tagline = (dto.Tagline ?? string.Empty).Trim();
            row.BusinessLine = (dto.BusinessLine ?? string.Empty).Trim();
            row.Gstin = (dto.Gstin ?? string.Empty).Trim();
            row.CinNumber = (dto.CinNumber ?? string.Empty).Trim();
            row.Address = (dto.Address ?? string.Empty).Trim();
            row.ContactNumber = (dto.ContactNumber ?? string.Empty).Trim();
            row.Email = (dto.Email ?? string.Empty).Trim();
            row.Website = (dto.Website ?? string.Empty).Trim();
            row.BankName = (dto.BankName ?? string.Empty).Trim();
            row.AccountNumber = (dto.AccountNumber ?? string.Empty).Trim();
            row.IfscCode = (dto.IfscCode ?? string.Empty).Trim();
            row.BranchName = (dto.BranchName ?? string.Empty).Trim();
            row.SignatoryName = (dto.SignatoryName ?? string.Empty).Trim();
            row.SignatoryMobile = (dto.SignatoryMobile ?? string.Empty).Trim();
            row.IntroText = (dto.IntroText ?? string.Empty).Trim();
            row.TransportationLabel = (dto.TransportationLabel ?? string.Empty).Trim();
            row.Jurisdiction = (dto.Jurisdiction ?? string.Empty).Trim();
            row.DefaultGstPercent = dto.DefaultGstPercent > 0 ? dto.DefaultGstPercent : 18m;
            row.TermsConditionsJson = SerializeTerms(dto.Terms);

            if (dto.RemoveLogo)
            {
                row.LogoBase64 = string.Empty;
                row.LogoContentType = string.Empty;
            }
            else if (!string.IsNullOrWhiteSpace(dto.LogoBase64))
            {
                row.LogoBase64 = dto.LogoBase64.Trim();
                row.LogoContentType = (dto.LogoContentType ?? string.Empty).Trim();
            }
            else if (!string.IsNullOrWhiteSpace(dto.LogoContentType))
            {
                row.LogoContentType = dto.LogoContentType.Trim();
            }

            if (dto.RemoveFavicon)
            {
                row.FaviconBase64 = string.Empty;
                row.FaviconContentType = string.Empty;
            }
            else if (!string.IsNullOrWhiteSpace(dto.FaviconBase64))
            {
                row.FaviconBase64 = dto.FaviconBase64.Trim();
                row.FaviconContentType = (dto.FaviconContentType ?? string.Empty).Trim();
            }
            else if (!string.IsNullOrWhiteSpace(dto.FaviconContentType))
            {
                row.FaviconContentType = dto.FaviconContentType.Trim();
            }

            row.UpdatedAt = DateTime.UtcNow;
        }

        public static List<CompanyProfileTermDto> ParseTerms(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<CompanyProfileTermDto>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<CompanyProfileTermDto>>(json, JsonOpts)
                    ?? new List<CompanyProfileTermDto>();
            }
            catch
            {
                return new List<CompanyProfileTermDto>();
            }
        }

        public static string SerializeTerms(IEnumerable<CompanyProfileTermDto>? terms)
        {
            var list = terms?
                .Where(t => !string.IsNullOrWhiteSpace(t.Title) || !string.IsNullOrWhiteSpace(t.Body))
                .Select(t => new CompanyProfileTermDto
                {
                    Title = (t.Title ?? string.Empty).Trim(),
                    Body = (t.Body ?? string.Empty).Trim(),
                })
                .ToList() ?? new List<CompanyProfileTermDto>();

            return list.Count == 0 ? string.Empty : JsonSerializer.Serialize(list, JsonOpts);
        }

        /// <summary>Empty singleton row; company details are entered manually in Admin settings.</summary>
        public static CompanyProfile CreateDefaultRow()
        {
            return new CompanyProfile
            {
                Id = 1,
                DefaultGstPercent = 18m,
                UpdatedAt = DateTime.UtcNow,
            };
        }
    }
}
