using System;
using System.Collections.Generic;

namespace BankFrontEnd
{
    public sealed class PaymentDeepLink
    {
        private PaymentDeepLink(string externalPaymentId, string? clientToken)
        {
            ExternalPaymentId = externalPaymentId;
            ClientToken = clientToken;
        }

        public string ExternalPaymentId { get; }
        public string? ClientToken { get; }

        public static PaymentDeepLink? TryParse(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || !Uri.TryCreate(raw, UriKind.Absolute, out Uri? uri))
            {
                return null;
            }

            if (!string.Equals(uri.Scheme, "bankfront", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            bool isPaymentRoute =
                string.Equals(uri.Host, "pay", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(uri.AbsolutePath.Trim('/'), "pay", StringComparison.OrdinalIgnoreCase);
            if (!isPaymentRoute)
            {
                return null;
            }

            Dictionary<string, string> query = ParseQuery(uri.Query);
            if (!query.TryGetValue("externalPaymentId", out string? externalPaymentId) ||
                string.IsNullOrWhiteSpace(externalPaymentId))
            {
                return null;
            }

            query.TryGetValue("clientToken", out string? clientToken);
            return new PaymentDeepLink(externalPaymentId, clientToken);
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string trimmed = query.TrimStart('?');
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return values;
            }

            foreach (string pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = pair.Split('=', 2);
                string key = Uri.UnescapeDataString(parts[0]);
                string value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
                values[key] = value;
            }

            return values;
        }
    }
}
