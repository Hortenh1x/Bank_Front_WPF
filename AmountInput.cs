using System.Globalization;
using System.Text.RegularExpressions;

namespace BankFrontEnd
{
    public static class AmountInput
    {
        // Accept digits with optional decimal separator (dot or comma) and up to 2 decimals.
        private static readonly Regex Pattern = new Regex(@"^\d+([.,]\d{1,2})?$", RegexOptions.Compiled);

        public static bool TryParse(string? rawInput, out double amount, out string validationMessage)
        {
            amount = 0;

            string input = rawInput?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(input))
            {
                validationMessage = "Please enter an amount.";
                return false;
            }

            if (!Pattern.IsMatch(input))
            {
                validationMessage = "Use a valid amount with up to 2 digits after comma or dot.";
                return false;
            }

            string normalized = input.Replace(',', '.');
            if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal parsed))
            {
                validationMessage = "Please enter a valid numeric amount.";
                return false;
            }

            if (parsed <= 0)
            {
                validationMessage = "Amount must be greater than 0.";
                return false;
            }

            amount = (double)parsed;
            validationMessage = string.Empty;
            return true;
        }
    }
}
