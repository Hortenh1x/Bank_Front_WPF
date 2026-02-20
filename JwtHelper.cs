using System;
using System.Text;
using System.Text.Json;

namespace BankFrontEnd
{
    public static class JwtHelper
    {
        public static bool TryGetExpiryUtc(string token, out DateTime expiryUtc)
        {
            expiryUtc = default;

            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            string[] parts = token.Split('.');
            if (parts.Length != 3)
            {
                return false;
            }

            try
            {
                byte[] payloadBytes = Base64UrlDecode(parts[1]);
                using JsonDocument doc = JsonDocument.Parse(payloadBytes);

                if (!doc.RootElement.TryGetProperty("exp", out JsonElement expElement))
                {
                    return false;
                }

                long expSeconds = expElement.ValueKind switch
                {
                    JsonValueKind.Number => expElement.GetInt64(),
                    JsonValueKind.String when long.TryParse(expElement.GetString(), out long parsed) => parsed,
                    _ => 0
                };

                if (expSeconds <= 0)
                {
                    return false;
                }

                expiryUtc = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static byte[] Base64UrlDecode(string base64Url)
        {
            string padded = base64Url.Replace('-', '+').Replace('_', '/');
            int padding = 4 - (padded.Length % 4);
            if (padding is > 0 and < 4)
            {
                padded = padded.PadRight(padded.Length + padding, '=');
            }

            return Convert.FromBase64String(padded);
        }
    }
}
