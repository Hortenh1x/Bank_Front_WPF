using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace BankFrontEnd
{
    public partial class UserOverviewPage : Page
    {
        public UserOverviewPage()
        {
            InitializeComponent();
            Loaded += UserOverviewPage_Loaded;
        }

        private async void UserOverviewPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Session.IsAuthenticated)
            {
                if (Session.IsExpired)
                {
                    Session.Clear();
                    StatusText.Text = "Your session expired. Please log in again.";
                }
                else
                {
                    StatusText.Text = "Please log in to view your accounts.";
                }
                return;
            }

            try
            {
                StatusText.Text = "Loading your profile...";
                UserMeResponse? me = await GetUserProfile();

                if (me == null)
                {
                    StatusText.Text = "Unable to load your profile.";
                    Notifier.Error("Unable to load your profile.");
                    return;
                }

                UserNameText.Text = $"{me.First_name} {me.Last_name}";

                StatusText.Text = "Loading your accounts...";
                AccountDto[] accounts = await GetAccounts();

                AccountsListView.ItemsSource = accounts;
                StatusText.Text = accounts.Length == 0 ? "No accounts found." : string.Empty;
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                Notifier.Error($"Error: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task<UserMeResponse?> GetUserProfile()
        {
            HttpResponseMessage response = await ApiClient.HttpClient.GetAsync("api/User/me");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HandleUnauthorized();
                return null;
            }
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserMeResponse>(body, ApiClient.JsonOptions);
        }

        private async System.Threading.Tasks.Task<AccountDto[]> GetAccounts()
        {
            HttpResponseMessage response = await ApiClient.HttpClient.GetAsync("api/User/show-my-accounts");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
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

        private void OpenAccount_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not AccountDto account)
            {
                return;
            }

            NavigationService?.Navigate(new AccountDetailsPage(account.Id));
        }

        private void AddAccount_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new AddAccountPage());
        }

        private void HandleUnauthorized()
        {
            Session.Clear();
            StatusText.Text = "Your session expired. Please log in again.";
            Notifier.Error("Your session expired. Please log in again.");
            NavigationService?.Navigate(new LoginPage());
        }

        private sealed class UserMeResponse
        {
            public int Id { get; set; }
            public string First_name { get; set; } = string.Empty;
            public string Last_name { get; set; } = string.Empty;
        }

        private sealed class AccountDto
        {
            public int Id { get; set; }
            public decimal Deposit { get; set; }
            public int User_id { get; set; }
        }
    }
}
