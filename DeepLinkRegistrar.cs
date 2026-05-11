using System;
using Microsoft.Win32;

namespace BankFrontEnd
{
    public static class DeepLinkRegistrar
    {
        public static void RegisterBankFrontProtocol()
        {
            string? executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                return;
            }

            using RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\bankfront");
            key.SetValue("", "URL:Bank Front Payment Protocol");
            key.SetValue("URL Protocol", "");

            using RegistryKey commandKey = key.CreateSubKey(@"shell\open\command");
            commandKey.SetValue("", $"\"{executablePath}\" \"%1\"");
        }
    }
}
