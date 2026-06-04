using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Helpers
{
    /// <summary>
    /// When the client sends a JWT Bearer token, ensures the <c>userId</c> query matches the token subject.
    /// Opaque session tokens skip this check (session user id is supplied via query from login).
    /// </summary>
    public static class ApiActingUserValidation
    {
        public static IActionResult? EnsureQueryUserMatchesBearer(HttpRequest request, int userId)
        {
            var bearerUserId = TryReadUserIdFromAuthorization(request.Headers.Authorization.ToString());
            if (bearerUserId == null)
            {
                return null;
            }

            if (bearerUserId.Value != userId)
            {
                return new UnauthorizedObjectResult(new
                {
                    message = "The userId query parameter does not match the authenticated user.",
                });
            }

            return null;
        }

        public static int? TryReadUserIdFromAuthorization(string? authorization)
        {
            if (string.IsNullOrWhiteSpace(authorization)
                || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var token = authorization["Bearer ".Length..].Trim();
            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                return null;
            }

            try
            {
                var json = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                foreach (var key in new[] { "sub", "userId", "user_id", "UserId", "id", "nameid" })
                {
                    if (!root.TryGetProperty(key, out var el))
                    {
                        continue;
                    }

                    if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n) && n > 0)
                    {
                        return n;
                    }

                    if (el.ValueKind == JsonValueKind.String
                        && int.TryParse(el.GetString(), out var parsed)
                        && parsed > 0)
                    {
                        return parsed;
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private static byte[] Base64UrlDecode(string segment)
        {
            var s = segment.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }

            return Convert.FromBase64String(s);
        }
    }
}
