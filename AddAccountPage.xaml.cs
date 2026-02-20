using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace BankFrontEnd
{
    public partial class AddAccountPage : Page
    {
        public AddAccountPage()
        {
            InitializeComponent();
            Loaded += AddAccountPage_Loaded;
        }

        private void AddAccountPage_Loaded(object sender, RoutedEventArgs e)
        {
            EnsureAuthenticatedOrRedirect();
        }

        private async void CreateAccount_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureAuthenticatedOrRedirect())
            {
                return;
            }

            try
            {
                StatusText.Text = "Creating account...";
                using var content = new StringContent("{}", Encoding.UTF8, "application/json");
                HttpResponseMessage response = await ApiClient.HttpClient.PostAsync("api/User/add-account", content);
                string body = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized();
                    return;
                }

                if (!response.IsSuccessStatusCode)
                {
                    string message = TryGetErrorMessage(body) ?? "Unable to create account.";
                    StatusText.Text = message;
                    Notifier.Error(message);
                    return;
                }

                AddAccountResponse? createdAccount = JsonSerializer.Deserialize<AddAccountResponse>(body, ApiClient.JsonOptions);
                int accountId = createdAccount?.Id ?? 0;
                StatusText.Text = $"Account {accountId} created successfully.";
                Notifier.Success($"Account {accountId} created successfully.");
                NavigationService?.Navigate(new UserOverviewPage());
            }
            catch (Exception ex)
            {
                string message = $"Network error: {ex.Message}";
                StatusText.Text = message;
                Notifier.Error(message);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new UserOverviewPage());
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

        private sealed class AddAccountResponse
        {
            public int Id { get; set; }
            public double Deposit { get; set; }
            public int Belongs_to { get; set; }
        }
    }
}
