using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace BankFrontEnd
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer toastTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };

        public MainWindow()
        {
            InitializeComponent();
            toastTimer.Tick += ToastTimer_Tick;
            Notifier.MessageRequested += Notifier_MessageRequested;
            Closed += MainWindow_Closed;

            // This loads your main "Home" view the second the app starts
            MainFrame.Navigate(new HomePage());
        }

        // Redirects to the Home Page
        private void Home_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new HomePage());
        }

        // Redirects to the Login Page
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new LoginPage());
        }

        private void ATM_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureAuthenticatedOrRedirect())
            {
                return;
            }

            MainFrame.Navigate(new ATMPage());
        }

        private void Transfer_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureAuthenticatedOrRedirect())
            {
                return;
            }

            MainFrame.Navigate(new TransferPage());
        }

        // Redirects to the Accounts Page
        private void Accounts_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureAuthenticatedOrRedirect())
            {
                return;
            }

            MainFrame.Navigate(new UserOverviewPage());
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

            MainFrame.Navigate(new LoginPage());
            Notifier.Info("Please log in first.");
            return false;
        }

        private void Notifier_MessageRequested(NotificationMessage message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => Notifier_MessageRequested(message));
                return;
            }

            ToastText.Text = message.Message;
            ToastBorder.Background = new SolidColorBrush(GetToastColor(message.Type));
            ToastBorder.Visibility = Visibility.Visible;

            toastTimer.Stop();
            toastTimer.Start();
        }

        private void ToastTimer_Tick(object? sender, EventArgs e)
        {
            toastTimer.Stop();
            ToastBorder.Visibility = Visibility.Collapsed;
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            Notifier.MessageRequested -= Notifier_MessageRequested;
            toastTimer.Tick -= ToastTimer_Tick;
            Closed -= MainWindow_Closed;
        }

        private static Color GetToastColor(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => Color.FromRgb(46, 204, 113),
                NotificationType.Error => Color.FromRgb(231, 76, 60),
                _ => Color.FromRgb(52, 152, 219)
            };
        }
    }
}
