using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace BankFrontEnd
{
    public partial class RegisterPage : Page
    {
        public RegisterPage()
        {
            InitializeComponent();
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessageText.Visibility = Visibility.Collapsed;

            string firstName = FirstNameTextBox.Text.Trim();
            string lastName = LastNameTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string repeatPassword = RepeatPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(repeatPassword))
            {
                string message = "Please fill all fields.";
                ShowError(message);
                Notifier.Error(message);
                return;
            }

            if (password != repeatPassword)
            {
                string message = "Passwords do not match.";
                ShowError(message);
                Notifier.Error(message);
                return;
            }

            var payload = new RegisterRequest
            {
                First_name = firstName,
                Last_name = lastName,
                Password_hash = password,
                Password_hash_repeat = repeatPassword
            };

            try
            {
                string json = JsonSerializer.Serialize(payload, ApiClient.JsonOptions);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await ApiClient.HttpClient.PostAsync("api/Auth/register", content);
                string body = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    int? registeredUserId = TryGetRegisteredUserId(body);
                    if (registeredUserId.HasValue)
                    {
                        Session.SetLoginPrefillUserId(registeredUserId.Value);
                        Notifier.Success($"Registration successful. Your User ID is {registeredUserId.Value}. You can now login.");
                    }
                    else
                    {
                        Notifier.Success("Registration successful. You can now login.");
                    }

                    NavigationService?.Navigate(new LoginPage());
                    return;
                }

                string message = TryGetErrorMessage(body) ?? $"Registration failed ({(int)response.StatusCode}).";
                ShowError(message);
                Notifier.Error(message);
            }
            catch (Exception ex)
            {
                string message = $"Network error: {ex.Message}";
                ShowError(message);
                Notifier.Error(message);
            }
        }

        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
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

        private static int? TryGetRegisteredUserId(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("id", out JsonElement idElement) &&
                    idElement.ValueKind == JsonValueKind.Number &&
                    idElement.TryGetInt32(out int id))
                {
                    return id;
                }
            }
            catch
            {
                // Ignore malformed JSON and fall back.
            }

            return null;
        }

        private void ShowError(string message)
        {
            ErrorMessageText.Text = message;
            ErrorMessageText.Visibility = Visibility.Visible;
        }

        private sealed class RegisterRequest
        {
            public required string First_name { get; set; }
            public required string Last_name { get; set; }
            public required string Password_hash { get; set; }
            public required string Password_hash_repeat { get; set; }
        }
    }
}
