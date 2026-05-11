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
    public partial class ATMPage : Page
    {
        public ATMPage()
        {
            InitializeComponent();
            Loaded += ATMPage_Loaded;
        }

        private async void ATMPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!EnsureAuthenticatedOrRedirect())
            {
                return;
            }

            await LoadAccountsAsync();
        }

        private async void Deposit_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteOperationAsync(isDeposit: true);
        }

        private async void Withdraw_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteOperationAsync(isDeposit: false);
        }

        private void ViewAccounts_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureAuthenticatedOrRedirect())
            {
                return;
            }

            NavigationService?.Navigate(new UserOverviewPage());
        }

        private async Task ExecuteOperationAsync(bool isDeposit)
        {
            if (!EnsureAuthenticatedOrRedirect())
            {
                return;
            }

            if (AccountComboBox.SelectedItem is not AccountDto account)
            {
                string message = "Please select an account first.";
                StatusText.Text = message;
                Notifier.Error(message);
                return;
            }

            if (!TryReadAmount(out decimal amount, out string validationMessage))
            {
                string message = validationMessage;
                StatusText.Text = message;
                Notifier.Error(message);
                return;
            }

            try
            {
                StatusText.Text = isDeposit ? "Processing deposit..." : "Processing withdrawal...";
                string endpoint = isDeposit ? "api/Account/deposit-money" : "api/Account/withdraw-money";
                object payload = isDeposit
                    ? new DepositRequest { To_id = account.Id, Amount = amount }
                    : new WithdrawalRequest { From_id = account.Id, Amount = amount };

                string json = JsonSerializer.Serialize(payload, ApiClient.JsonOptions);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await ApiClient.HttpClient.PostAsync(endpoint, content);
                string body = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized();
                    return;
                }

                if (!response.IsSuccessStatusCode)
                {
                    string message = TryGetErrorMessage(body) ?? "Operation failed.";
                    StatusText.Text = message;
                    Notifier.Error(message);
                    return;
                }

                OperationResponse? result = JsonSerializer.Deserialize<OperationResponse>(body, ApiClient.JsonOptions);
                StatusText.Text = $"Success. New balance: {EuroFormatter.Format(result?.CurrentBalance ?? 0m)}";
                Notifier.Success(isDeposit ? "Deposit successful." : "Withdrawal successful.");
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

        private async Task LoadAccountsAsync()
        {
            StatusText.Text = "Loading accounts...";
            AccountDto[] accounts = await GetAccountsAsync();
            AccountComboBox.ItemsSource = accounts;

            if (accounts.Length == 0)
            {
                StatusText.Text = "No accounts available.";
                return;
            }

            AccountComboBox.SelectedIndex = 0;
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

        private bool TryReadAmount(out decimal amount, out string validationMessage)
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

        private sealed class DepositRequest
        {
            public int To_id { get; set; }
            public decimal Amount { get; set; }
        }

        private sealed class WithdrawalRequest
        {
            public int From_id { get; set; }
            public decimal Amount { get; set; }
        }

        private sealed class OperationResponse
        {
            public int Id { get; set; }
            public decimal CurrentBalance { get; set; }
        }

        private sealed class AccountDto
        {
            public int Id { get; set; }
            public decimal Deposit { get; set; }
            public int User_id { get; set; }
            [JsonIgnore]
            public string DisplayText => $"{Id} ({EuroFormatter.Format(Deposit)})";
        }
    }
}
