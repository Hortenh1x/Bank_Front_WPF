using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BankFrontEnd
{
    public static class ApiClient
    {
        public static readonly string BaseUrl = ResolveBaseUrl();

        private static readonly HttpClient Client = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };

        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        static ApiClient()
        {
            JsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true));
        }

        public static HttpClient HttpClient => Client;

        public static void SetBearerToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                Client.DefaultRequestHeaders.Authorization = null;
                return;
            }

            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private static string ResolveBaseUrl()
        {
            string? configured = Environment.GetEnvironmentVariable("BANK_API_BASE_URL");
            if (string.IsNullOrWhiteSpace(configured))
            {
                return "http://localhost:5231/";
            }

            configured = configured.Trim();
            if (!configured.EndsWith("/", StringComparison.Ordinal))
            {
                configured += "/";
            }

            if (!Uri.TryCreate(configured, UriKind.Absolute, out _))
            {
                return "http://localhost:5231/";
            }

            return configured;
        }
    }
}
