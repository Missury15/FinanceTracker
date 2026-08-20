using FinanceTracker.Models;

namespace FinanceTracker.Services;

public sealed class TransactionService
{
    private readonly List<Transaction> _transactions =
    [
        new("Salary", 8_000m, TransactionType.Income, new DateTime(2026, 8, 20)),
        new("Food", 250m, TransactionType.Expense, new DateTime(2026, 8, 20)),
        new("Transport", 120m, TransactionType.Expense, new DateTime(2026, 8, 19)),
        new("Freelance", 10_000m, TransactionType.Income, new DateTime(2026, 8, 18)),
        new("Books", 5_180m, TransactionType.Expense, new DateTime(2026, 8, 17))
    ];

    public IReadOnlyList<Transaction> GetRecentTransactions(int count = 10)
    {
        return _transactions
            .OrderByDescending(transaction => transaction.Date)
            .ThenByDescending(transaction => transaction.Type == TransactionType.Income)
            .Take(count)
            .ToList();
    }

    public decimal GetTotalIncome()
    {
        return _transactions
            .Where(transaction => transaction.Type == TransactionType.Income)
            .Sum(transaction => transaction.Amount);
    }

    public decimal GetTotalExpenses()
    {
        return _transactions
            .Where(transaction => transaction.Type == TransactionType.Expense)
            .Sum(transaction => transaction.Amount);
    }

    public decimal GetBalance()
    {
        return GetTotalIncome() - GetTotalExpenses();
    }

    public void AddTransaction(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        _transactions.Add(transaction);
    }
}
