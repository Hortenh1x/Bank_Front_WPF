using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BankFrontEnd
{
    public static class PaymentApiClient
    {
        public static async Task<ApiResult<PaymentCheckoutResponse>> GetCheckoutAsync(string externalPaymentId)
        {
            HttpResponseMessage response = await ApiClient.HttpClient.GetAsync(
                $"api/payments/{Uri.EscapeDataString(externalPaymentId)}/checkout");
            return await ReadResult<PaymentCheckoutResponse>(response);
        }

        public static async Task<ApiResult<ProviderPaymentStatusResponse>> ConfirmAsync(string externalPaymentId, int fromAccountId)
        {
            var request = new ConfirmPaymentRequest
            {
                FromAccountId = fromAccountId
            };
            string json = JsonSerializer.Serialize(request, ApiClient.JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await ApiClient.HttpClient.PostAsync(
                $"api/payments/{Uri.EscapeDataString(externalPaymentId)}/confirm",
                content);
            return await ReadResult<ProviderPaymentStatusResponse>(response);
        }

        private static async Task<ApiResult<T>> ReadResult<T>(HttpResponseMessage response)
        {
            string body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                T? value = JsonSerializer.Deserialize<T>(body, ApiClient.JsonOptions);
                return ApiResult<T>.Success(response.StatusCode, value);
            }

            return ApiResult<T>.Failure(response.StatusCode, TryGetErrorMessage(body) ?? response.ReasonPhrase ?? "Request failed.");
        }

        private static string? TryGetErrorMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out JsonElement messageElement) &&
                    messageElement.ValueKind == JsonValueKind.String)
                {
                    return messageElement.GetString();
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private sealed class ConfirmPaymentRequest
        {
            public int FromAccountId { get; set; }
        }
    }

    public sealed class ApiResult<T>
    {
        private ApiResult(bool isSuccess, HttpStatusCode statusCode, T? value, string? errorMessage)
        {
            IsSuccess = isSuccess;
            StatusCode = statusCode;
            Value = value;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }
        public HttpStatusCode StatusCode { get; }
        public T? Value { get; }
        public string? ErrorMessage { get; }

        public static ApiResult<T> Success(HttpStatusCode statusCode, T? value)
        {
            return new ApiResult<T>(true, statusCode, value, null);
        }

        public static ApiResult<T> Failure(HttpStatusCode statusCode, string errorMessage)
        {
            return new ApiResult<T>(false, statusCode, default, errorMessage);
        }
    }

    public sealed class PaymentCheckoutResponse
    {
        public string ExternalPaymentId { get; set; } = string.Empty;
        public string MerchantName { get; set; } = string.Empty;
        public long ShopOrderId { get; set; }
        public long ShopPaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public sealed class ProviderPaymentStatusResponse
    {
        public string ExternalPaymentId { get; set; } = string.Empty;
        public long ShopOrderId { get; set; }
        public long ShopPaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? PayerAccountId { get; set; }
        public int? BankTransactionId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
