using System.IO;
using System.Globalization;
using FinanceTracker.Models;
using Microsoft.Data.Sqlite;

namespace FinanceTracker.Services;

public sealed class TransactionService
{
    private const string DatabaseFileName = "finance-tracker.db";
    private readonly string _connectionString;

    public TransactionService()
    {
        var databaseDirectory = Path.Combine(GetProjectDirectory(), "Data");
        Directory.CreateDirectory(databaseDirectory);

        var databasePath = Path.Combine(databaseDirectory, DatabaseFileName);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        }.ToString();

        InitializeDatabase();
    }

    public IReadOnlyList<Transaction> GetRecentTransactions(int count = 10)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Title, AmountCents, Type, Date
            FROM Transactions
            ORDER BY Date DESC, Id DESC
            LIMIT $count;
            """;
        command.Parameters.AddWithValue("$count", count);

        using var reader = command.ExecuteReader();
        var transactions = new List<Transaction>();

        while (reader.Read())
        {
            transactions.Add(ReadTransaction(reader));
        }

        return transactions;
    }

    public decimal GetTotalIncome()
    {
        return GetTotalAmount(TransactionType.Income);
    }

    public decimal GetTotalExpenses()
    {
        return GetTotalAmount(TransactionType.Expense);
    }

    public decimal GetBalance()
    {
        return GetTotalIncome() - GetTotalExpenses();
    }

    public IReadOnlyList<DailyTransactionSummary> GetDailySummaries(DateTime startDate, DateTime endDate)
    {
        startDate = startDate.Date;
        endDate = endDate.Date;

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Date, Type, SUM(AmountCents)
            FROM Transactions
            WHERE Date BETWEEN $startDate AND $endDate
            GROUP BY Date, Type
            ORDER BY Date;
            """;
        command.Parameters.AddWithValue("$startDate", startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$endDate", endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        using var reader = command.ExecuteReader();
        var amountsByDate = new Dictionary<DateTime, DailyAmounts>();

        while (reader.Read())
        {
            var date = DateTime.ParseExact(reader.GetString(0), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var type = Enum.Parse<TransactionType>(reader.GetString(1));
            var amount = FromAmountCents(reader.GetInt64(2));

            if (!amountsByDate.TryGetValue(date, out var dailyAmounts))
            {
                dailyAmounts = new DailyAmounts();
                amountsByDate[date] = dailyAmounts;
            }

            if (type == TransactionType.Income)
            {
                dailyAmounts.Income = amount;
            }
            else
            {
                dailyAmounts.Expenses = amount;
            }
        }

        var summaries = new List<DailyTransactionSummary>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            amountsByDate.TryGetValue(date, out var dailyAmounts);
            summaries.Add(new DailyTransactionSummary(date, dailyAmounts?.Income ?? 0m, dailyAmounts?.Expenses ?? 0m));
        }

        return summaries;
    }

    public void AddTransaction(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Transactions (Title, AmountCents, Type, Date)
            VALUES ($title, $amountCents, $type, $date);
            """;
        command.Parameters.AddWithValue("$title", transaction.Title);
        command.Parameters.AddWithValue("$amountCents", ToAmountCents(transaction.Amount));
        command.Parameters.AddWithValue("$type", transaction.Type.ToString());
        command.Parameters.AddWithValue("$date", transaction.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        command.ExecuteNonQuery();
    }

    public void ResetAllTransactions()
    {
        using var connection = CreateConnection();
        connection.Open();

        using var deleteCommand = connection.CreateCommand();
        deleteCommand.CommandText = "DELETE FROM Transactions;";
        deleteCommand.ExecuteNonQuery();

        using var resetIdCommand = connection.CreateCommand();
        resetIdCommand.CommandText = "DELETE FROM sqlite_sequence WHERE name = 'Transactions';";
        resetIdCommand.ExecuteNonQuery();
    }

    private void InitializeDatabase()
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Transactions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                AmountCents INTEGER NOT NULL,
                Type TEXT NOT NULL CHECK (Type IN ('Income', 'Expense')),
                Date TEXT NOT NULL
            );
            """;

        command.ExecuteNonQuery();
    }

    private decimal GetTotalAmount(TransactionType type)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COALESCE(SUM(AmountCents), 0)
            FROM Transactions
            WHERE Type = $type;
            """;
        command.Parameters.AddWithValue("$type", type.ToString());

        var amountCents = Convert.ToInt64(command.ExecuteScalar());
        return FromAmountCents(amountCents);
    }

    private SqliteConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }

    private static Transaction ReadTransaction(SqliteDataReader reader)
    {
        var type = Enum.Parse<TransactionType>(reader.GetString(3));
        var date = DateTime.ParseExact(reader.GetString(4), "yyyy-MM-dd", CultureInfo.InvariantCulture);

        return new Transaction(
            reader.GetInt64(0),
            reader.GetString(1),
            FromAmountCents(reader.GetInt64(2)),
            type,
            date);
    }

    private static long ToAmountCents(decimal amount)
    {
        return decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));
    }

    private static decimal FromAmountCents(long amountCents)
    {
        return amountCents / 100m;
    }

    private static string GetProjectDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FinanceTracker.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return AppContext.BaseDirectory;
    }

    private sealed class DailyAmounts
    {
        public decimal Income { get; set; }

        public decimal Expenses { get; set; }
    }
}
