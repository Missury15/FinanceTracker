using System.Globalization;
using System.Windows;
using System.Windows.Media;
using FinanceTracker.Services;

namespace FinanceTracker;

public partial class MainWindow : Window
{
    private static readonly CultureInfo CzechCulture = CultureInfo.GetCultureInfo("cs-CZ");
    private readonly TransactionService _transactionService = new();

    public MainWindow()
    {
        InitializeComponent();
        RefreshDashboard();
    }

    private void AddTransactionButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddTransactionWindow
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true || dialog.Transaction is null)
        {
            return;
        }

        _transactionService.AddTransaction(dialog.Transaction);
        RefreshDashboard();
    }

    private void RefreshDashboard()
    {
        BalanceTextBlock.Text = FormatCurrency(_transactionService.GetBalance());
        IncomeTextBlock.Text = FormatCurrency(_transactionService.GetTotalIncome());
        ExpensesTextBlock.Text = FormatCurrency(_transactionService.GetTotalExpenses());
        ExpensesTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
        TransactionsListView.ItemsSource = _transactionService.GetRecentTransactions();
    }

    private static string FormatCurrency(decimal amount)
    {
        return $"{amount.ToString("#,##0", CzechCulture)} K\u010d";
    }
}
