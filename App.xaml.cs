using System;
using System.Windows;

namespace BankFrontEnd
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                DeepLinkRegistrar.RegisterBankFrontProtocol();
            }
            catch
            {
                // Protocol registration is optional for environments that block registry writes.
            }

            PaymentDeepLink? deepLink = e.Args.Length > 0 ? PaymentDeepLink.TryParse(e.Args[0]) : null;

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();

            if (deepLink != null)
            {
                mainWindow.OpenPaymentPage(deepLink.ExternalPaymentId, deepLink.ClientToken);
            }
        }
    }
}

