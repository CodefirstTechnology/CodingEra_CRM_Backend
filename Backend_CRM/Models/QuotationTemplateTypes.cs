namespace CRM.models
{
    public static class QuotationTemplateTypes
    {
        public const string Standard = "Standard";
        public const string TechnicalProposal = "TechnicalProposal";

        public static readonly string[] All = { Standard, TechnicalProposal };

        public static string Normalize(string? value)
        {
            var v = (value ?? string.Empty).Trim();
            if (string.Equals(v, TechnicalProposal, StringComparison.OrdinalIgnoreCase))
            {
                return TechnicalProposal;
            }

            return Standard;
        }
    }
}
