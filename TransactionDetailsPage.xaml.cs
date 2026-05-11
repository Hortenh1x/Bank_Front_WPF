using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BankFrontEnd
{
    public partial class TransactionDetailsPage : Page
    {
        private readonly int accountId;
        private readonly TransactionItem transaction;

        public TransactionDetailsPage(int accountId, TransactionItem transaction)
        {
            InitializeComponent();
            this.accountId = accountId;
            this.transaction = transaction;
            Loaded += TransactionDetailsPage_Loaded;
        }

        private void TransactionDetailsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Session.IsAuthenticated)
            {
                if (Session.IsExpired)
                {
                    Session.Clear();
                }

                NavigationService?.Navigate(new LoginPage());
                Notifier.Info("Please log in first.");
                return;
            }

            TransactionIdText.Text = transaction.Id.ToString();
            DateText.Text = transaction.DateDisplay;
            AmountText.Text = transaction.AmountDisplay;
            FromText.Text = transaction.From_id.ToString();
            FromOwnerText.Text = string.IsNullOrWhiteSpace(transaction.FromOwnerDisplay) ? "-" : transaction.FromOwnerDisplay;
            ToText.Text = transaction.To_id.ToString();
            ToOwnerText.Text = string.IsNullOrWhiteSpace(transaction.ToOwnerDisplay) ? "-" : transaction.ToOwnerDisplay;
            TypeText.Text = transaction.Type.ToString();
            TypeValueText.Text = transaction.Type.ToString();

            ApplyTypeBadge(transaction.Type);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
            {
                NavigationService.GoBack();
                return;
            }

            NavigationService?.Navigate(new AccountDetailsPage(accountId));
        }

        private void ApplyTypeBadge(TransactionKind kind)
        {
            switch (kind)
            {
                case TransactionKind.Deposit:
                    TypeBadge.Background = new SolidColorBrush(Color.FromRgb(232, 248, 238));
                    TypeText.Foreground = new SolidColorBrush(Color.FromRgb(15, 138, 74));
                    break;
                case TransactionKind.Withdrawal:
                    TypeBadge.Background = new SolidColorBrush(Color.FromRgb(254, 241, 232));
                    TypeText.Foreground = new SolidColorBrush(Color.FromRgb(214, 91, 11));
                    break;
                default:
                    TypeBadge.Background = new SolidColorBrush(Color.FromRgb(234, 243, 255));
                    TypeText.Foreground = new SolidColorBrush(Color.FromRgb(38, 101, 208));
                    break;
            }
        }
    }
}
