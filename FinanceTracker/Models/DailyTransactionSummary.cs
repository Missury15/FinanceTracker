namespace FinanceTracker.Models;

public sealed class DailyTransactionSummary
{
    public DailyTransactionSummary(DateTime date, decimal income, decimal expenses)
    {
        Date = date.Date;
        Income = income;
        Expenses = expenses;
    }

    public DateTime Date { get; }

    public decimal Income { get; }

    public decimal Expenses { get; }
}
