using BankFrontEnd;

namespace BankFrontWpf.Tests;

internal static class Program
{
    private static int failed;

    private static int Main()
    {
        Run("Deep link parses bankfront host route", ParsesHostRoute);
        Run("Deep link parses bankfront path route", ParsesPathRoute);
        Run("Deep link rejects wrong scheme", RejectsWrongScheme);
        Run("Deep link rejects missing payment id", RejectsMissingPaymentId);

        return failed == 0 ? 0 : 1;
    }

    private static void ParsesHostRoute()
    {
        PaymentDeepLink? link = PaymentDeepLink.TryParse("bankfront://pay?externalPaymentId=bank-pay-123&clientToken=client-token");

        Assert.NotNull(link, "payment link should parse");
        Assert.Equal("bank-pay-123", link!.ExternalPaymentId);
        Assert.Equal("client-token", link.ClientToken);
    }

    private static void ParsesPathRoute()
    {
        PaymentDeepLink? link = PaymentDeepLink.TryParse("bankfront:/pay?externalPaymentId=bank-pay-456");

        Assert.NotNull(link, "payment path route should parse");
        Assert.Equal("bank-pay-456", link!.ExternalPaymentId);
        Assert.Equal(null, link.ClientToken);
    }

    private static void RejectsWrongScheme()
    {
        PaymentDeepLink? link = PaymentDeepLink.TryParse("https://pay?externalPaymentId=bank-pay-123");

        Assert.Equal(null, link);
    }

    private static void RejectsMissingPaymentId()
    {
        PaymentDeepLink? link = PaymentDeepLink.TryParse("bankfront://pay?clientToken=client-token");

        Assert.Equal(null, link);
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            failed++;
            Console.WriteLine($"FAIL {name}");
            Console.WriteLine(ex);
        }
    }
}

internal static class Assert
{
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected {expected}, got {actual}.");
        }
    }

    public static void NotNull(object? value, string message)
    {
        if (value == null)
        {
            throw new InvalidOperationException(message);
        }
    }
}
