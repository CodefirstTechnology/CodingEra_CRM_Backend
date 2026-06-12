using System.Globalization;
using System.Text;
using System.Text.Json;
using CRM.Configuration;
using CRM.DTO;
using CRM.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CRM.Services
{
    public interface IMorningBriefingAiService
    {
        Task<(string Message, string Source)> GenerateBriefingAsync(
            DailyBriefingMetricsDto metrics,
            CancellationToken cancellationToken = default);
    }

    public class MorningBriefingAiService : IMorningBriefingAiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MorningBriefingAiOptions _options;
        private readonly ILogger<MorningBriefingAiService> _logger;

        public MorningBriefingAiService(
            IHttpClientFactory httpClientFactory,
            IOptions<MorningBriefingAiOptions> options,
            ILogger<MorningBriefingAiService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<(string Message, string Source)> GenerateBriefingAsync(
            DailyBriefingMetricsDto metrics,
            CancellationToken cancellationToken = default)
        {
            if (_options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                try
                {
                    var aiMessage = await TryGenerateWithGeminiAsync(metrics, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(aiMessage))
                    {
                        return (SanitizeMessage(aiMessage), "ai");
                    }

                    _logger.LogWarning("Gemini returned empty content; using template fallback.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Gemini briefing request failed; using template fallback.");
                }
            }
            else
            {
                _logger.LogInformation("Morning briefing AI disabled or missing API key; using template fallback.");
            }

            return (AdminBriefingFallbackBuilder.Build(metrics), "fallback");
        }

        private async Task<string?> TryGenerateWithGeminiAsync(
            DailyBriefingMetricsDto metrics,
            CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient("MorningBriefingAi");
            var apiUrl = BuildGeminiUrl();
            var statsJson = JsonSerializer.Serialize(BuildStatsPayload(metrics));

            var prompt =
                "Write an admin CRM daily executive briefing using ONLY the JSON statistics below.\n"
                + "Follow this structure and tone (adapt numbers from JSON; never invent data):\n"
                + "\"Hi Admin Good morning. The CRM currently contains 1,250 leads. 15 new leads were added today. "
                + "38 follow-ups remain pending, including 4 overdue activities. There are 21 active deals, "
                + "with 2 expected to close today. Three meetings are scheduled. "
                + "15 deals and 6 leads have been inactive for over 24 hours. "
                + "Immediate attention is recommended for overdue follow-ups and high-priority opportunities.\"\n"
                + "Rules:\n"
                + "- Use adminName from JSON in greeting (default Admin).\n"
                + "- Include total leads, new leads today, pending/overdue follow-ups, active deals, "
                + "deals closing today, meetings today, stuck deals/leads when > 0.\n"
                + "- Do NOT mention monthly revenue, pipeline value, conversion rates, or all-time totals.\n"
                + "- Use 70-100 words, professional executive tone, full sentences only.\n"
                + "Return only the briefing paragraph.\n\n"
                + $"Statistics:\n{statsJson}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt },
                        },
                    },
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    maxOutputTokens = 512,
                    // Gemini 2.5 uses internal thinking tokens that otherwise consume maxOutputTokens.
                    thinkingConfig = new { thinkingBudget = 0 },
                },
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            };

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Gemini API {StatusCode}: {ErrorBody}",
                    (int)response.StatusCode,
                    errorBody.Length > 500 ? errorBody[..500] : errorBody);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            return ExtractGeminiText(doc.RootElement);
        }

        private string? ExtractGeminiText(JsonElement root)
        {
            if (!root.TryGetProperty("candidates", out var candidates)
                || candidates.GetArrayLength() == 0)
            {
                return null;
            }

            var candidate = candidates[0];
            if (!candidate.TryGetProperty("content", out var contentEl)
                || !contentEl.TryGetProperty("parts", out var parts)
                || parts.GetArrayLength() == 0)
            {
                return null;
            }

            var textParts = new List<string>();
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textEl))
                {
                    var piece = textEl.GetString();
                    if (!string.IsNullOrWhiteSpace(piece))
                    {
                        textParts.Add(piece.Trim());
                    }
                }
            }

            if (textParts.Count == 0)
            {
                return null;
            }

            var message = string.Join(" ", textParts).Trim();
            var finishReason = candidate.TryGetProperty("finishReason", out var reasonEl)
                ? reasonEl.GetString()
                : null;

            if (string.Equals(finishReason, "MAX_TOKENS", StringComparison.OrdinalIgnoreCase)
                && message.Length < 80)
            {
                _logger.LogWarning(
                    "Gemini briefing truncated ({CharCount} chars, finishReason={FinishReason}).",
                    message.Length,
                    finishReason);
                return null;
            }

            return message;
        }

        private string BuildGeminiUrl()
        {
            var baseUrl = string.IsNullOrWhiteSpace(_options.ApiUrl)
                ? $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent"
                : _options.ApiUrl.Trim();

            var separator = baseUrl.Contains('?') ? '&' : '?';
            return $"{baseUrl}{separator}key={Uri.EscapeDataString(_options.ApiKey.Trim())}";
        }

        private static Dictionary<string, object> BuildStatsPayload(DailyBriefingMetricsDto m)
        {
            var stats = new Dictionary<string, object>
            {
                ["adminName"] = string.IsNullOrWhiteSpace(m.AdminName) ? "Admin" : m.AdminName.Trim(),
            };

            if (m.TotalLeads > 0) stats["totalLeads"] = m.TotalLeads;
            if (m.ActiveDeals > 0) stats["activeDeals"] = m.ActiveDeals;
            if (m.NewLeadsToday > 0) stats["newLeadsToday"] = m.NewLeadsToday;
            if (m.NewDealsToday > 0) stats["newDealsToday"] = m.NewDealsToday;
            if (m.PendingFollowUps > 0) stats["pendingFollowUps"] = m.PendingFollowUps;
            if (m.FollowUpsToday > 0) stats["followUpsToday"] = m.FollowUpsToday;
            if (m.OverdueFollowUps > 0) stats["overdueFollowUps"] = m.OverdueFollowUps;
            if (m.DealsPendingClosure > 0) stats["dealsPendingClosure"] = m.DealsPendingClosure;
            if (m.DealsWonToday > 0) stats["dealsWonToday"] = m.DealsWonToday;
            if (m.DealsLostToday > 0) stats["dealsLostToday"] = m.DealsLostToday;
            if (m.MeetingsToday > 0) stats["meetingsToday"] = m.MeetingsToday;
            if (m.TasksDueToday > 0) stats["tasksDueToday"] = m.TasksDueToday;
            if (m.HighPriorityLeads > 0) stats["highPriorityLeads"] = m.HighPriorityLeads;
            if (m.StuckDealsCount > 0) stats["stuckDealsCount"] = m.StuckDealsCount;
            if (m.StuckLeadsCount > 0) stats["stuckLeadsCount"] = m.StuckLeadsCount;
            if (m.RevenueToday is > 0)
            {
                stats["revenueTodayInr"] = AdminDashboardBriefingMetrics.ToInr(m.RevenueToday.Value);
            }

            return stats;
        }

        private static string SanitizeMessage(string message)
        {
            var trimmed = message.Trim().Trim('"');
            if (trimmed.Length > 520)
            {
                trimmed = trimmed[..520];
            }

            return trimmed;
        }
    }

    internal static class AdminBriefingFallbackBuilder
    {
        private static readonly string[] SmallNumbers =
        {
            "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
        };

        public static string Build(DailyBriefingMetricsDto m)
        {
            var adminName = string.IsNullOrWhiteSpace(m.AdminName) ? "Admin" : m.AdminName.Trim();
            var hour = GetLocalHour();
            var greeting = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";
            var parts = new List<string> { $"Hi {adminName} {greeting}." };

            if (m.TotalLeads > 0)
            {
                parts.Add($"The CRM currently contains {FormatCount(m.TotalLeads)} leads.");
            }

            if (m.NewLeadsToday > 0)
            {
                var leadVerb = m.NewLeadsToday == 1 ? "was" : "were";
                parts.Add($"{FormatCount(m.NewLeadsToday)} new lead{(m.NewLeadsToday == 1 ? "" : "s")} {leadVerb} added today.");
            }

            if (m.PendingFollowUps > 0)
            {
                var line = $"{FormatCount(m.PendingFollowUps)} follow-up{(m.PendingFollowUps == 1 ? "" : "s")} remain pending";
                if (m.OverdueFollowUps > 0)
                {
                    line += $", including {FormatCount(m.OverdueFollowUps)} overdue activit{(m.OverdueFollowUps == 1 ? "y" : "ies")}";
                }

                parts.Add($"{line}.");
            }
            else if (m.OverdueFollowUps > 0)
            {
                parts.Add($"{FormatCount(m.OverdueFollowUps)} overdue activit{(m.OverdueFollowUps == 1 ? "y" : "ies")} need attention.");
            }

            if (m.ActiveDeals > 0)
            {
                var line = $"There are {FormatCount(m.ActiveDeals)} active deal{(m.ActiveDeals == 1 ? "" : "s")}";
                if (m.DealsPendingClosure > 0)
                {
                    line += $", with {FormatCount(m.DealsPendingClosure)} expected to close today";
                }

                parts.Add($"{line}.");
            }

            var meetingLine = MeetingPhrase(m.MeetingsToday);
            if (!string.IsNullOrEmpty(meetingLine))
            {
                parts.Add($"{meetingLine}.");
            }

            if (m.StuckDealsCount > 0 && m.StuckLeadsCount > 0)
            {
                parts.Add(
                    $"{FormatCount(m.StuckDealsCount)} deal{(m.StuckDealsCount == 1 ? "" : "s")} and "
                    + $"{FormatCount(m.StuckLeadsCount)} lead{(m.StuckLeadsCount == 1 ? "" : "s")} "
                    + "have been inactive for over 24 hours.");
            }
            else if (m.StuckDealsCount > 0)
            {
                var verb = m.StuckDealsCount == 1 ? "has" : "have";
                parts.Add(
                    $"{FormatCount(m.StuckDealsCount)} deal{(m.StuckDealsCount == 1 ? "" : "s")} "
                    + $"{verb} been inactive for over 24 hours.");
            }
            else if (m.StuckLeadsCount > 0)
            {
                var verb = m.StuckLeadsCount == 1 ? "has" : "have";
                parts.Add(
                    $"{FormatCount(m.StuckLeadsCount)} lead{(m.StuckLeadsCount == 1 ? "" : "s")} "
                    + $"{verb} been inactive for over 24 hours.");
            }

            if (m.OverdueFollowUps > 0
                || m.HighPriorityLeads > 0
                || m.StuckDealsCount > 0
                || m.StuckLeadsCount > 0)
            {
                parts.Add(
                    "Immediate attention is recommended for overdue follow-ups and high-priority opportunities.");
            }

            return string.Join(" ", parts);
        }

        private static string FormatCount(int n) => n.ToString("N0", CultureInfo.InvariantCulture);

        private static string MeetingPhrase(int count)
        {
            if (count <= 0)
            {
                return string.Empty;
            }

            if (count <= 10)
            {
                var word = char.ToUpper(SmallNumbers[count][0]) + SmallNumbers[count][1..];
                return $"{word} meeting{(count == 1 ? "" : "s")} are scheduled";
            }

            return $"{FormatCount(count)} meetings are scheduled";
        }

        private static int GetLocalHour()
        {
            try
            {
                var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist).Hour;
            }
            catch
            {
                return DateTime.Now.Hour;
            }
        }
    }
}
