using System.Text.Json;
using CRM.DTO;

namespace CRM.Helpers
{
    public static class QuotationTemplatePayloadHelper
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };

        public static TechnicalProposalPayloadDto ParseTechnicalProposal(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return DefaultTechnicalProposal();
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<TechnicalProposalPayloadDto>(json, JsonOpts);
                return NormalizeTechnicalProposal(parsed ?? DefaultTechnicalProposal());
            }
            catch
            {
                return DefaultTechnicalProposal();
            }
        }

        public static string SerializeTechnicalProposal(TechnicalProposalPayloadDto? payload)
        {
            var normalized = NormalizeTechnicalProposal(payload ?? DefaultTechnicalProposal());
            return JsonSerializer.Serialize(normalized, JsonOpts);
        }

        public static TechnicalProposalPayloadDto DefaultTechnicalProposal() =>
            new() { CurrencyCode = "INR" };

        private static TechnicalProposalPayloadDto NormalizeTechnicalProposal(TechnicalProposalPayloadDto dto)
        {
            dto.ProjectName = (dto.ProjectName ?? string.Empty).Trim();
            dto.KindAttnDesignation = (dto.KindAttnDesignation ?? string.Empty).Trim();
            dto.CommercialTerms = (dto.CommercialTerms ?? string.Empty).Trim();
            dto.TaxLabel = (dto.TaxLabel ?? string.Empty).Trim();
            dto.PaymentTerms = (dto.PaymentTerms ?? string.Empty).Trim();
            dto.HsnCode = (dto.HsnCode ?? string.Empty).Trim();
            dto.Incoterms = (dto.Incoterms ?? string.Empty).Trim();
            dto.DispatchLeadTime = (dto.DispatchLeadTime ?? string.Empty).Trim();
            dto.ProposalIntro = (dto.ProposalIntro ?? string.Empty).Trim();
            dto.CurrencyCode = NormalizeCurrency(dto.CurrencyCode);
            dto.TechnicalSections = NormalizeTerms(dto.TechnicalSections);
            dto.CommercialSections = NormalizeTerms(dto.CommercialSections);
            return dto;
        }

        private static string NormalizeCurrency(string? code)
        {
            var c = (code ?? string.Empty).Trim().ToUpperInvariant();
            return string.IsNullOrWhiteSpace(c) ? "INR" : c;
        }

        private static List<CompanyProfileTermDto> NormalizeTerms(IEnumerable<CompanyProfileTermDto>? terms) =>
            (terms ?? Enumerable.Empty<CompanyProfileTermDto>())
                .Select(t => new CompanyProfileTermDto
                {
                    Title = (t.Title ?? string.Empty).Trim(),
                    Body = (t.Body ?? string.Empty).Trim(),
                })
                .Where(t => !string.IsNullOrWhiteSpace(t.Title) || !string.IsNullOrWhiteSpace(t.Body))
                .ToList();
    }
}
