using System.Net;
using CRM.Configuration;
using Microsoft.Extensions.Options;

namespace CRM.Services
{
    public enum JustdialWebhookSecurityStatus
    {
        Allowed,
        Disabled,
        InvalidApiKey,
        IpNotAllowed,
        PayloadTooLarge,
        Malformed
    }

    public sealed class JustdialWebhookSecurityResult
    {
        public JustdialWebhookSecurityStatus Status { get; init; }
        public string Message { get; init; } = string.Empty;

        public static JustdialWebhookSecurityResult Ok() =>
            new() { Status = JustdialWebhookSecurityStatus.Allowed };

        public static JustdialWebhookSecurityResult Fail(JustdialWebhookSecurityStatus status, string message) =>
            new() { Status = status, Message = message };
    }

    public interface IJustdialWebhookSecurityService
    {
        JustdialWebhookSecurityResult Evaluate(HttpRequest request);
    }

    public sealed class JustdialWebhookSecurityService : IJustdialWebhookSecurityService
    {
        private readonly JustdialWebhookOptions _options;

        public JustdialWebhookSecurityService(IOptions<JustdialWebhookOptions> options)
        {
            _options = options.Value;
        }

        public JustdialWebhookSecurityResult Evaluate(HttpRequest request)
        {
            if (!_options.Enabled)
            {
                return JustdialWebhookSecurityResult.Fail(
                    JustdialWebhookSecurityStatus.Disabled,
                    "Justdial webhook is disabled.");
            }

            if (_options.RequireIpWhitelist)
            {
                var remoteIp = ResolveRemoteIp(request);
                if (!IsIpAllowed(remoteIp, _options.AllowedIpAddresses))
                {
                    return JustdialWebhookSecurityResult.Fail(
                        JustdialWebhookSecurityStatus.IpNotAllowed,
                        "Remote IP is not allowed.");
                }
            }

            if (_options.RequireApiKey)
            {
                if (string.IsNullOrWhiteSpace(_options.ApiKey))
                {
                    return JustdialWebhookSecurityResult.Fail(
                        JustdialWebhookSecurityStatus.InvalidApiKey,
                        "Webhook API key is not configured.");
                }

                var presented = ResolvePresentedApiKey(request);
                if (!FixedTimeEquals(presented, _options.ApiKey))
                {
                    return JustdialWebhookSecurityResult.Fail(
                        JustdialWebhookSecurityStatus.InvalidApiKey,
                        "Invalid API key.");
                }
            }

            if (HttpMethods.IsPost(request.Method)
                && request.ContentLength is long length
                && _options.MaxRequestBodyBytes > 0
                && length > _options.MaxRequestBodyBytes)
            {
                return JustdialWebhookSecurityResult.Fail(
                    JustdialWebhookSecurityStatus.PayloadTooLarge,
                    "Request body exceeds configured size limit.");
            }

            return JustdialWebhookSecurityResult.Ok();
        }

        private string? ResolvePresentedApiKey(HttpRequest request)
        {
            var headerName = string.IsNullOrWhiteSpace(_options.ApiKeyHeaderName)
                ? "X-Api-Key"
                : _options.ApiKeyHeaderName;

            if (request.Headers.TryGetValue(headerName, out var headerValues))
            {
                var headerKey = headerValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(headerKey))
                {
                    return headerKey.Trim();
                }
            }

            if (request.Headers.TryGetValue("Authorization", out var authValues))
            {
                var auth = authValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(auth)
                    && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return auth["Bearer ".Length..].Trim();
                }
            }

            if (_options.AllowApiKeyQueryParameter)
            {
                var queryName = string.IsNullOrWhiteSpace(_options.ApiKeyQueryParameterName)
                    ? "api_key"
                    : _options.ApiKeyQueryParameterName;
                if (request.Query.TryGetValue(queryName, out var queryValues))
                {
                    var queryKey = queryValues.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(queryKey))
                    {
                        return queryKey.Trim();
                    }
                }
            }

            return null;
        }

        private static string? ResolveRemoteIp(HttpRequest request)
        {
            // Prefer direct connection IP; X-Forwarded-For only when reverse proxy is trusted upstream.
            var ip = request.HttpContext.Connection.RemoteIpAddress;
            if (ip == null)
            {
                return null;
            }

            if (ip.IsIPv4MappedToIPv6)
            {
                ip = ip.MapToIPv4();
            }

            return ip.ToString();
        }

        private static bool IsIpAllowed(string? remoteIp, IReadOnlyList<string> allowed)
        {
            if (string.IsNullOrWhiteSpace(remoteIp) || allowed.Count == 0)
            {
                return false;
            }

            foreach (var entry in allowed)
            {
                if (string.IsNullOrWhiteSpace(entry))
                {
                    continue;
                }

                if (string.Equals(entry.Trim(), remoteIp, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (IPAddress.TryParse(entry.Trim(), out var allowedIp)
                    && IPAddress.TryParse(remoteIp, out var remote)
                    && allowedIp.Equals(remote))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool FixedTimeEquals(string? presented, string expected)
        {
            if (string.IsNullOrEmpty(presented))
            {
                return false;
            }

            var a = System.Text.Encoding.UTF8.GetBytes(presented);
            var b = System.Text.Encoding.UTF8.GetBytes(expected);
            return CryptographicEquals(a, b);
        }

        private static bool CryptographicEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                // Still compare to reduce trivial timing differences on length.
                var max = Math.Max(a.Length, b.Length);
                var diff = a.Length ^ b.Length;
                for (var i = 0; i < max; i++)
                {
                    var left = i < a.Length ? a[i] : (byte)0;
                    var right = i < b.Length ? b[i] : (byte)0;
                    diff |= left ^ right;
                }

                return false;
            }

            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(a, b);
        }
    }
}
