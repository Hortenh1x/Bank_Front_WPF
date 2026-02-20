using System;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace BankFrontEnd
{
    public partial class AccountDetailsPage : Page
    {
        private readonly int accountId;

        public AccountDetailsPage(int accountId)
        {
            InitializeComponent();
            this.accountId = accountId;
            Loaded += AccountDetailsPage_Loaded;
        }

        private async void AccountDetailsPage_Loaded(object sender, RoutedEventArgs e)
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
                    StatusText.Text = "Please log in to view account details.";
                }
                return;
            }

            AccountIdText.Text = $"Account ID: {accountId}";

            try
            {
                StatusText.Text = "Loading account balance...";
                BalanceResponse? balance = await GetBalance(accountId);
                if (balance != null)
                {
                    DepositText.Text = $"Balance: {EuroFormatter.Format(balance.CurrentBalance)}";
                }

                StatusText.Text = "Loading transactions...";
                TransactionItem[] transactions = await GetTransactions(accountId);
                TransactionsListView.ItemsSource = transactions;
                StatusText.Text = transactions.Length == 0 ? "No transactions found." : string.Empty;
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                Notifier.Error($"Error: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task<BalanceResponse?> GetBalance(int id)
        {
            var response = await ApiClient.HttpClient.GetAsync($"api/Account/show-balance/{id}");
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
            return JsonSerializer.Deserialize<BalanceResponse>(body, ApiClient.JsonOptions);
        }

        private async System.Threading.Tasks.Task<TransactionItem[]> GetTransactions(int id)
        {
            var response = await ApiClient.HttpClient.GetAsync($"api/Account/show-accounts-transactions/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                HandleUnauthorized();
                return Array.Empty<TransactionItem>();
            }
            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<TransactionItem>();
            }

            string body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TransactionItem[]>(body, ApiClient.JsonOptions) ?? Array.Empty<TransactionItem>();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
            {
                NavigationService.GoBack();
                return;
            }

            NavigationService?.Navigate(new UserOverviewPage());
        }

        private void HandleUnauthorized()
        {
            Session.Clear();
            StatusText.Text = "Your session expired. Please log in again.";
            Notifier.Error("Your session expired. Please log in again.");
            NavigationService?.Navigate(new LoginPage());
        }

        private void TransactionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TransactionsListView.SelectedItem is not TransactionItem transaction)
            {
                return;
            }

            TransactionsListView.SelectedItem = null;
            NavigationService?.Navigate(new TransactionDetailsPage(accountId, transaction));
        }

        private sealed class BalanceResponse
        {
            public double CurrentBalance { get; set; }
        }
    }
}
