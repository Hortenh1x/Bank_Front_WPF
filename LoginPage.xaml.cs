using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace BankFrontEnd
{
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();

            int? prefillUserId = Session.ConsumeLoginPrefillUserId();
            if (prefillUserId.HasValue)
            {
                UserIdTextBox.Text = prefillUserId.Value.ToString();
                UserPasswordBox.Focus();
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // 1. Clear any previous error messages
            ErrorMessageText.Visibility = Visibility.Collapsed;

            // 2. Grab the inputs
            string userIdInput = UserIdTextBox.Text;
            string password = UserPasswordBox.Password;

            // 3. Basic Local Validation
            if (string.IsNullOrWhiteSpace(userIdInput) || string.IsNullOrWhiteSpace(password))
            {
                string message = "Please enter both User ID and Password.";
                ShowError(message);
                Notifier.Error(message);
                return;
            }

            if (!int.TryParse(userIdInput, out int userId))
            {
                string message = "User ID must be a valid number.";
                ShowError(message);
                Notifier.Error(message);
                return;
            }

            var payload = new LoginRequest
            {
                Id = userId,
                Password_hash = password
            };

            try
            {
                string json = JsonSerializer.Serialize(payload, ApiClient.JsonOptions);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await ApiClient.HttpClient.PostAsync("api/Auth/login", content);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string message = TryGetErrorMessage(body) ?? "Invalid User ID or Password.";
                    ShowError(message);
                    Notifier.Error(message);
                    return;
                }

                string? token = TryGetToken(body);
                if (string.IsNullOrWhiteSpace(token))
                {
                    string message = "Login failed: token was not returned.";
                    ShowError(message);
                    Notifier.Error(message);
                    return;
                }

                Session.SetToken(token);
                Notifier.Success("Login successful.");
                NavigationService?.Navigate(new UserOverviewPage());
            }
            catch (Exception ex)
            {
                string message = $"Network error: {ex.Message}";
                ShowError(message);
                Notifier.Error(message);
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new RegisterPage());
        }

        private static string? TryGetToken(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("token", out JsonElement tokenElement) &&
                    tokenElement.ValueKind == JsonValueKind.String)
                {
                    return tokenElement.GetString();
                }
            }
            catch
            {
                // Ignore malformed JSON and fall back to error handling.
            }

            return null;
        }

        private static string? TryGetErrorMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out JsonElement messageElement) &&
                    messageElement.ValueKind == JsonValueKind.String)
                {
                    return messageElement.GetString();
                }
            }
            catch
            {
                // Ignore malformed JSON and fall back to generic error.
            }

            return null;
        }

        // Helper method to keep the code clean
        private void ShowError(string message)
        {
            ErrorMessageText.Text = message;
            ErrorMessageText.Visibility = Visibility.Visible;
        }

        private sealed class LoginRequest
        {
            public required int Id { get; set; }
            public required string Password_hash { get; set; }
        }
    }
}
