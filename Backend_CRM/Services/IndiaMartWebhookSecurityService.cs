using System.Net;
using System.Security.Cryptography;
using CRM.Configuration;
using Microsoft.Extensions.Options;

namespace CRM.Services
{
    public enum IndiaMartWebhookSecurityStatus
    {
        Allowed,
        Disabled,
        InvalidApiKey,
        IpNotAllowed,
        PayloadTooLarge,
        Malformed
    }

    public sealed class IndiaMartWebhookSecurityResult
    {
        public IndiaMartWebhookSecurityStatus Status { get; init; }
        public string Message { get; init; } = string.Empty;

        public static IndiaMartWebhookSecurityResult Ok() =>
            new() { Status = IndiaMartWebhookSecurityStatus.Allowed };

        public static IndiaMartWebhookSecurityResult Fail(IndiaMartWebhookSecurityStatus status, string message) =>
            new() { Status = status, Message = message };
    }

    public interface IIndiaMartWebhookSecurityService
    {
        IndiaMartWebhookSecurityResult Evaluate(HttpRequest request);
    }

    public sealed class IndiaMartWebhookSecurityService : IIndiaMartWebhookSecurityService
    {
        private readonly IndiaMartWebhookOptions _options;

        public IndiaMartWebhookSecurityService(IOptions<IndiaMartWebhookOptions> options)
        {
            _options = options.Value;
        }

        public IndiaMartWebhookSecurityResult Evaluate(HttpRequest request)
        {
            if (!_options.Enabled)
            {
                return IndiaMartWebhookSecurityResult.Fail(
                    IndiaMartWebhookSecurityStatus.Disabled,
                    "IndiaMART webhook is disabled.");
            }

            if (_options.RequireIpWhitelist)
            {
                var remoteIp = ResolveRemoteIp(request);
                if (!IsIpAllowed(remoteIp, _options.AllowedIpAddresses))
                {
                    return IndiaMartWebhookSecurityResult.Fail(
                        IndiaMartWebhookSecurityStatus.IpNotAllowed,
                        "Remote IP is not allowed.");
                }
            }

            if (_options.RequireApiKey)
            {
                if (string.IsNullOrWhiteSpace(_options.ApiKey))
                {
                    return IndiaMartWebhookSecurityResult.Fail(
                        IndiaMartWebhookSecurityStatus.InvalidApiKey,
                        "Webhook API key is not configured.");
                }

                var presented = ResolvePresentedApiKey(request);
                if (string.IsNullOrEmpty(presented) || !FixedTimeEquals(presented, _options.ApiKey))
                {
                    return IndiaMartWebhookSecurityResult.Fail(
                        IndiaMartWebhookSecurityStatus.InvalidApiKey,
                        "Invalid API key.");
                }
            }

            if (HttpMethods.IsPost(request.Method)
                && request.ContentLength is long length
                && _options.MaxRequestBodyBytes > 0
                && length > _options.MaxRequestBodyBytes)
            {
                return IndiaMartWebhookSecurityResult.Fail(
                    IndiaMartWebhookSecurityStatus.PayloadTooLarge,
                    "Request body exceeds configured size limit.");
            }

            return IndiaMartWebhookSecurityResult.Ok();
        }

        private string? ResolvePresentedApiKey(HttpRequest request)
        {
            var headerName = string.IsNullOrWhiteSpace(_options.ApiKeyHeaderName)
                ? "X-IndiaMart-Webhook-Key"
                : _options.ApiKeyHeaderName;

            if (request.Headers.TryGetValue(headerName, out var headerValues))
            {
                var headerKey = headerValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(headerKey))
                {
                    return headerKey.Trim();
                }
            }

            if (request.Headers.TryGetValue("X-Api-Key", out var altHeaderValues))
            {
                var altKey = altHeaderValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(altKey))
                {
                    return altKey.Trim();
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

                if (request.Query.TryGetValue("glusr_crm_key", out var glusrValues))
                {
                    var glusrKey = glusrValues.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(glusrKey))
                    {
                        return glusrKey.Trim();
                    }
                }
            }

            return null;
        }

        private static string? ResolveRemoteIp(HttpRequest request)
        {
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

            if (a.Length != b.Length)
            {
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

            return CryptographicOperations.FixedTimeEquals(a, b);
        }
    }
}
