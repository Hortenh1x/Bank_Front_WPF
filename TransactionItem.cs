namespace BankFrontEnd
{
    public enum TransactionKind
    {
        Deposit,
        Withdrawal,
        Transfer
    }

    public sealed class TransactionItem
    {
        public int Id { get; set; }
        public string Date_time { get; set; } = string.Empty;
        public double Deposit { get; set; }
        public int From_id { get; set; }
        public int To_id { get; set; }
        public TransactionKind Type { get; set; }
        public string From_owner_name { get; set; } = string.Empty;
        public string To_owner_name { get; set; } = string.Empty;

        // Compatibility for alternate naming conventions.
        public string Owner_from_name { get; set; } = string.Empty;
        public string Owner_to_name { get; set; } = string.Empty;

        public string AmountDisplay => EuroFormatter.Format(Deposit);
        public string FromOwnerDisplay => !string.IsNullOrWhiteSpace(From_owner_name) ? From_owner_name : Owner_from_name;
        public string ToOwnerDisplay => !string.IsNullOrWhiteSpace(To_owner_name) ? To_owner_name : Owner_to_name;
    }
}
