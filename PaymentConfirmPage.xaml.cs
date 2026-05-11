using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BankFrontEnd
{
    public partial class PaymentConfirmPage : Page
    {
        private PaymentCheckoutResponse? currentPayment;

        public PaymentConfirmPage()
            : this(null, null)
        {
        }

        public PaymentConfirmPage(string? externalPaymentId, string? clientToken)
        {
            InitializeComponent();

            if (!string.IsNullOrWhiteSpace(externalPaymentId))
            {
                ExternalPaymentIdTextBox.Text = externalPaymentId;
            }

            Loaded += PaymentConfirmPage_Loaded;
        }

        private async void PaymentConfirmPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!EnsureAuthenticatedOrRedirect())
            {
                return;
            }

            await LoadAccountsAsync();

            if (!string.IsNullOrWhiteSpace(ExternalPaymentIdTextBox.Text))
            {
                await LoadPaymentAsync();
            }
        }

        private async void LoadPayment_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureAuthenticatedOrRedirect())
            {
                return;
            }

            await LoadPaymentAsync();
        }

        private async void ConfirmPayment_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureAuthenticatedOrRedirect())
            {
                return;
            }

            if (currentPayment == null)
            {
                ShowStatus("Load a payment before confirming it.", true);
                return;
            }

            if (AccountsComboBox.SelectedItem is not AccountDto account)
            {
                ShowStatus("Select a source account.", true);
                return;
            }

            try
            {
                ConfirmPaymentButton.IsEnabled = false;
                ShowStatus("Confirming payment...");

                ApiResult<ProviderPaymentStatusResponse> result =
                    await PaymentApiClient.ConfirmAsync(currentPayment.ExternalPaymentId, account.Id);

                if (result.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized();
                    return;
                }

                if (!result.IsSuccess || result.Value == null)
                {
                    ShowStatus(result.ErrorMessage ?? "Payment confirmation failed.", true);
                    UpdateConfirmButtonState();
                    return;
                }

                ShowStatus($"Payment succeeded. Transaction ID: {result.Value.BankTransactionId ?? 0}");
                Notifier.Success("Payment confirmed.");
                await LoadAccountsAsync();
                await LoadPaymentAsync();
            }
            catch (Exception ex)
            {
                ShowStatus($"Network error: {ex.Message}", true);
                UpdateConfirmButtonState();
            }
        }

        private void AccountsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateConfirmButtonState();
        }

        private async Task LoadPaymentAsync()
        {
            string externalPaymentId = ExternalPaymentIdTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(externalPaymentId))
            {
                ShowStatus("Enter an external payment ID.", true);
                return;
            }

            try
            {
                ShowStatus("Loading payment...");
                ApiResult<PaymentCheckoutResponse> result = await PaymentApiClient.GetCheckoutAsync(externalPaymentId);

                if (result.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Session.SetPendingPayment(externalPaymentId, null);
                    HandleUnauthorized();
                    return;
                }

                if (!result.IsSuccess || result.Value == null)
                {
                    currentPayment = null;
                    PaymentDetailsPanel.Visibility = Visibility.Collapsed;
                    ShowStatus(result.ErrorMessage ?? "Payment was not found.", true);
                    UpdateConfirmButtonState();
                    return;
                }

                currentPayment = result.Value;
                RenderPayment(result.Value);
                ShowStatus(string.Empty);
                UpdateConfirmButtonState();
            }
            catch (Exception ex)
            {
                ShowStatus($"Network error: {ex.Message}", true);
                UpdateConfirmButtonState();
            }
        }

        private async Task LoadAccountsAsync()
        {
            HttpResponseMessage response = await ApiClient.HttpClient.GetAsync("api/User/show-my-accounts");
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                HandleUnauthorized();
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                AccountsComboBox.ItemsSource = Array.Empty<AccountDto>();
                ShowStatus("Unable to load bank accounts.", true);
                return;
            }

            string body = await response.Content.ReadAsStringAsync();
            AccountDto[] accounts = JsonSerializer.Deserialize<AccountDto[]>(body, ApiClient.JsonOptions) ?? Array.Empty<AccountDto>();
            AccountsComboBox.ItemsSource = accounts;
            if (accounts.Length > 0)
            {
                AccountsComboBox.SelectedIndex = 0;
            }
        }

        private void RenderPayment(PaymentCheckoutResponse payment)
        {
            PaymentDetailsPanel.Visibility = Visibility.Visible;
            MerchantText.Text = payment.MerchantName;
            AmountText.Text = $"{payment.Amount:0.00} {payment.Currency}";
            PaymentStatusText.Text = payment.Status;
            ExpiresAtText.Text = payment.ExpiresAt.ToLocalTime().ToString("g");
            ShopOrderText.Text = $"Order #{payment.ShopOrderId}, payment #{payment.ShopPaymentId}";
        }

        private void UpdateConfirmButtonState()
        {
            bool canConfirm =
                currentPayment != null &&
                string.Equals(currentPayment.Status, "PENDING", StringComparison.OrdinalIgnoreCase) &&
                currentPayment.ExpiresAt > DateTime.UtcNow &&
                AccountsComboBox.SelectedItem is AccountDto;

            ConfirmPaymentButton.IsEnabled = canConfirm;
        }

        private bool EnsureAuthenticatedOrRedirect()
        {
            if (Session.IsAuthenticated)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(ExternalPaymentIdTextBox.Text))
            {
                Session.SetPendingPayment(ExternalPaymentIdTextBox.Text.Trim(), null);
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
            ShowStatus("Your session expired. Please log in again.", true);
            Notifier.Error("Your session expired. Please log in again.");
            NavigationService?.Navigate(new LoginPage());
        }

        private void ShowStatus(string message, bool isError = false)
        {
            StatusText.Text = message;
            StatusText.Foreground = isError
                ? System.Windows.Media.Brushes.Firebrick
                : (System.Windows.Media.Brush)Application.Current.Resources["TextMutedBrush"];
        }

        private sealed class AccountDto
        {
            public int Id { get; set; }
            public decimal Deposit { get; set; }
            public int User_id { get; set; }

            [JsonIgnore]
            public string DisplayText => $"Account #{Id} - {EuroFormatter.Format(Deposit)}";
        }
    }
}
