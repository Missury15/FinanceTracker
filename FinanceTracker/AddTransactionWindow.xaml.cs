using System.Globalization;
using System.Windows;
using FinanceTracker.Models;

namespace FinanceTracker;

public partial class AddTransactionWindow : Window
{
    private static readonly CultureInfo CzechCulture = CultureInfo.GetCultureInfo("cs-CZ");

    public AddTransactionWindow()
    {
        InitializeComponent();
        DatePicker.SelectedDate = DateTime.Today;
        TitleTextBox.Focus();
    }

    public Transaction? Transaction { get; private set; }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
        {
            ShowValidationError("Enter a transaction title.");
            return;
        }

        if (!decimal.TryParse(AmountTextBox.Text, NumberStyles.Number, CzechCulture, out var amount)
            && !decimal.TryParse(AmountTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
        {
            ShowValidationError("Enter a valid amount.");
            return;
        }

        if (amount <= 0)
        {
            ShowValidationError("Amount must be greater than zero.");
            return;
        }

        var type = TypeComboBox.SelectedIndex == 0
            ? TransactionType.Income
            : TransactionType.Expense;

        Transaction = new Transaction(
            TitleTextBox.Text,
            amount,
            type,
            DatePicker.SelectedDate ?? DateTime.Today);

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private static void ShowValidationError(string message)
    {
        MessageBox.Show(message, "Invalid transaction", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
