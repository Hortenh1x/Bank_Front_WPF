using System;
using System.Globalization;
using System.Windows.Data;

namespace BankFrontEnd
{
    public static class EuroFormatter
    {
        private static readonly CultureInfo EuroCulture = CreateEuroCulture();

        public static string Format(double amount)
        {
            return amount.ToString("C2", EuroCulture);
        }

        private static CultureInfo CreateEuroCulture()
        {
            var culture = (CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
            culture.NumberFormat.CurrencySymbol = "\u20AC";
            return culture;
        }
    }

    public sealed class EuroCurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double amount)
            {
                return EuroFormatter.Format(amount);
            }

            if (value is float amountFloat)
            {
                return EuroFormatter.Format(amountFloat);
            }

            if (value is decimal amountDecimal)
            {
                return EuroFormatter.Format((double)amountDecimal);
            }

            return EuroFormatter.Format(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
