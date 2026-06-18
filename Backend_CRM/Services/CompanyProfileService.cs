using CRM.DATA;
using CRM.DTO;
using CRM.Helpers;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Services
{
    public interface ICompanyProfileService
    {
        Task<CompanyProfileDto> GetAsync(CancellationToken ct = default);
        Task<CompanyProfileBrandingDto> GetBrandingAsync(CancellationToken ct = default);
        Task<CompanyProfileDto> UpdateAsync(CompanyProfileUpsertDto dto, CancellationToken ct = default);
    }

    public sealed class CompanyProfileService : ICompanyProfileService
    {
        private const int MaxLogoBase64Length = 2_800_000;

        private readonly TaskDbcontext _db;

        public CompanyProfileService(TaskDbcontext db)
        {
            _db = db;
        }

        public async Task<CompanyProfileDto> GetAsync(CancellationToken ct = default)
        {
            var row = await EnsureRowAsync(ct);
            return CompanyProfileMappingHelper.ToDto(row);
        }

        public async Task<CompanyProfileBrandingDto> GetBrandingAsync(CancellationToken ct = default)
        {
            var row = await EnsureRowAsync(ct);
            return CompanyProfileMappingHelper.ToBrandingDto(row);
        }

        public async Task<CompanyProfileDto> UpdateAsync(CompanyProfileUpsertDto dto, CancellationToken ct = default)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (!dto.RemoveLogo && !string.IsNullOrWhiteSpace(dto.LogoBase64) &&
                dto.LogoBase64.Length > MaxLogoBase64Length)
            {
                throw new InvalidOperationException("Logo file is too large.");
            }

            var row = await EnsureRowAsync(ct);
            CompanyProfileMappingHelper.Apply(row, dto);
            await _db.SaveChangesAsync(ct);
            return CompanyProfileMappingHelper.ToDto(row);
        }

        private async Task<CompanyProfile> EnsureRowAsync(CancellationToken ct)
        {
            var row = await _db.CompanyProfiles.FirstOrDefaultAsync(p => p.Id == 1, ct);
            if (row != null)
            {
                return row;
            }

            row = CompanyProfileMappingHelper.CreateDefaultRow();
            _db.CompanyProfiles.Add(row);
            await _db.SaveChangesAsync(ct);
            return row;
        }
    }
}
