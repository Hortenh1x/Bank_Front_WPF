namespace BankFrontEnd
{
    public static class Session
    {
        public static string? Token { get; private set; }
        public static DateTime? ExpiresAtUtc { get; private set; }
        public static int? LoginPrefillUserId { get; private set; }

        public static bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token) && !IsExpired;

        public static bool IsExpired
        {
            get
            {
                if (ExpiresAtUtc == null)
                {
                    return false;
                }

                // Small skew to avoid edge-of-expiry failures.
                return DateTime.UtcNow >= ExpiresAtUtc.Value.AddSeconds(-30);
            }
        }

        public static void SetToken(string token)
        {
            Token = token;
            if (JwtHelper.TryGetExpiryUtc(token, out DateTime expiryUtc))
            {
                ExpiresAtUtc = expiryUtc;
            }
            else
            {
                ExpiresAtUtc = null;
            }
            ApiClient.SetBearerToken(token);
        }

        public static void Clear()
        {
            Token = null;
            ExpiresAtUtc = null;
            ApiClient.SetBearerToken(null);
        }

        public static void SetLoginPrefillUserId(int userId)
        {
            if (userId <= 0)
            {
                return;
            }

            LoginPrefillUserId = userId;
        }

        public static int? ConsumeLoginPrefillUserId()
        {
            int? userId = LoginPrefillUserId;
            LoginPrefillUserId = null;
            return userId;
        }
    }
}
