using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BankFrontEnd
{
    public partial class TransferPage : Page
    {
        public TransferPage()
        {
            InitializeComponent();
            Loaded += TransferPage_Loaded;
        }

        private async void TransferPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!EnsureAuthenticatedOrRedirect())
            {
                return;
            }

            await LoadAccountsAsync();
        }

        private async void Transfer_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureAuthenticatedOrRedirect())
            {
                return;
            }

            if (FromAccountComboBox.SelectedItem is not AccountDto fromAccount)
            {
                string message = "Please select a source account.";
                StatusText.Text = message;
                Notifier.Error(message);
                return;
            }

            if (!int.TryParse(ToAccountTextBox.Text.Trim(), out int toAccountId) || toAccountId <= 0)
            {
                string message = "Please enter a valid target account ID.";
                StatusText.Text = message;
                Notifier.Error(message);
                return;
            }

            if (toAccountId == fromAccount.Id)
            {
                string message = "Target account must be different from source account.";
                StatusText.Text = message;
                Notifier.Error(message);
                return;
            }

            if (!TryReadAmount(out double amount, out string validationMessage))
            {
                string message = validationMessage;
                StatusText.Text = message;
                Notifier.Error(message);
                return;
            }

            try
            {
                StatusText.Text = "Processing transfer...";

                var payload = new TransferRequest
                {
                    To_id = toAccountId,
                    From_id = fromAccount.Id,
                    Amount = amount
                };

                string json = JsonSerializer.Serialize(payload, ApiClient.JsonOptions);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await ApiClient.HttpClient.PostAsync("api/Transaction/transfer-money", content);
                string body = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized();
                    return;
                }

                if (!response.IsSuccessStatusCode)
                {
                    string message = TryGetErrorMessage(body) ?? "Transfer failed.";
                    StatusText.Text = message;
                    Notifier.Error(message);
                    return;
                }

                TransferResponse? result = JsonSerializer.Deserialize<TransferResponse>(body, ApiClient.JsonOptions);
                StatusText.Text = $"Transfer completed. Transaction ID: {result?.TransactionId ?? 0}";
                Notifier.Success("Transfer successful.");
                AmountTextBox.Clear();
                await LoadAccountsAsync();
            }
            catch (Exception ex)
            {
                string message = $"Network error: {ex.Message}";
                StatusText.Text = message;
                Notifier.Error(message);
            }
        }

        private void ViewAccounts_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureAuthenticatedOrRedirect())
            {
                return;
            }

            NavigationService?.Navigate(new UserOverviewPage());
        }

        private async Task LoadAccountsAsync()
        {
            StatusText.Text = "Loading accounts...";
            AccountDto[] accounts = await GetAccountsAsync();
            FromAccountComboBox.ItemsSource = accounts;

            if (accounts.Length == 0)
            {
                StatusText.Text = "No accounts available.";
                return;
            }

            FromAccountComboBox.SelectedIndex = 0;
            StatusText.Text = string.Empty;
        }

        private async Task<AccountDto[]> GetAccountsAsync()
        {
            HttpResponseMessage response = await ApiClient.HttpClient.GetAsync("api/User/show-my-accounts");
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                HandleUnauthorized();
                return Array.Empty<AccountDto>();
            }

            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<AccountDto>();
            }

            string body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AccountDto[]>(body, ApiClient.JsonOptions) ?? Array.Empty<AccountDto>();
        }

        private bool EnsureAuthenticatedOrRedirect()
        {
            if (Session.IsAuthenticated)
            {
                return true;
            }

            if (Session.IsExpired)
            {
                Session.Clear();
            }

            NavigationService?.Navigate(new LoginPage());
            Notifier.Info("Please log in first.");
            return false;
        }

        private void HandleUnauthorized()
        {
            Session.Clear();
            StatusText.Text = "Your session expired. Please log in again.";
            Notifier.Error("Your session expired. Please log in again.");
            NavigationService?.Navigate(new LoginPage());
        }

        private bool TryReadAmount(out double amount, out string validationMessage)
        {
            return AmountInput.TryParse(AmountTextBox.Text, out amount, out validationMessage);
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
                // Ignore malformed JSON and return fallback message.
            }

            return null;
        }

        private sealed class TransferRequest
        {
            public int To_id { get; set; }
            public double Amount { get; set; }
            public int From_id { get; set; }
        }

        private sealed class TransferResponse
        {
            public int TransactionId { get; set; }
        }

        private sealed class AccountDto
        {
            public int Id { get; set; }
            public double Deposit { get; set; }
            public int User_id { get; set; }
            [JsonIgnore]
            public string DisplayText => $"{Id} ({EuroFormatter.Format(Deposit)})";
        }
    }
}
