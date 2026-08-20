using System.Globalization;

namespace FinanceTracker.Models;

public sealed class Transaction
{
    private static readonly CultureInfo CzechCulture = CultureInfo.GetCultureInfo("cs-CZ");

    public Transaction(string title, decimal amount, TransactionType type, DateTime date)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        Title = title.Trim();
        Amount = amount;
        Type = type;
        Date = date.Date;
    }

    public string Title { get; }

    public decimal Amount { get; }

    public TransactionType Type { get; }

    public DateTime Date { get; }

    public decimal SignedAmount => Type == TransactionType.Income ? Amount : -Amount;

    public string AmountText => $"{SignedAmount.ToString("+#,##0;-#,##0;0", CzechCulture)} K\u010d";

    public string DateText => Date.ToString("d.M.yyyy", CzechCulture);
}
