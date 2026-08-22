using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FinanceTracker.Models;
using FinanceTracker.Services;

namespace FinanceTracker;

public partial class MainWindow : Window
{
    private const double ChartLeftPadding = 54;
    private const double ChartTopPadding = 18;
    private const double ChartRightPadding = 24;
    private const double ChartBottomPadding = 48;
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

    private void ResetDataButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "This will permanently delete all transactions. Do you really want to reset all data?",
            "Reset all data?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _transactionService.ResetAllTransactions();
        RefreshDashboard();
    }

    private void ChartPeriodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DrawChart();
    }

    private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawChart();
    }

    private void RefreshDashboard()
    {
        BalanceTextBlock.Text = FormatCurrency(_transactionService.GetBalance());
        IncomeTextBlock.Text = FormatCurrency(_transactionService.GetTotalIncome());
        ExpensesTextBlock.Text = FormatCurrency(_transactionService.GetTotalExpenses());
        IncomeTextBlock.Foreground = CreateBrush(22, 163, 74);
        ExpensesTextBlock.Foreground = CreateBrush(220, 38, 38);
        TransactionsListView.ItemsSource = _transactionService.GetRecentTransactions();
        DrawChart();
    }

    private void DrawChart()
    {
        if (ChartCanvas is null || ChartPeriodComboBox is null)
        {
            return;
        }

        var width = ChartCanvas.ActualWidth;
        var height = ChartCanvas.ActualHeight;

        if (width <= 0 || height <= 0)
        {
            return;
        }

        ChartCanvas.Children.Clear();

        var endDate = DateTime.Today;
        var startDate = endDate.AddDays(-(GetSelectedPeriodDays() - 1));
        var summaries = _transactionService.GetDailySummaries(startDate, endDate);
        var maxAmount = summaries
            .Select(summary => Math.Max(summary.Income, summary.Expenses))
            .DefaultIfEmpty(0m)
            .Max();

        DrawChartGrid(width, height, maxAmount);
        DrawDateLabels(summaries, height);

        if (maxAmount <= 0m)
        {
            DrawEmptyChartMessage(width, height);
            return;
        }

        DrawChartLine(summaries, summary => summary.Income, maxAmount, CreateBrush(22, 163, 74));
        DrawChartLine(summaries, summary => summary.Expenses, maxAmount, CreateBrush(220, 38, 38));
    }

    private void DrawChartGrid(double width, double height, decimal maxAmount)
    {
        var plotWidth = GetPlotWidth(width);
        var plotHeight = GetPlotHeight(height);

        for (var i = 0; i <= 4; i++)
        {
            var y = ChartTopPadding + (plotHeight / 4 * i);
            AddLine(ChartLeftPadding, y, ChartLeftPadding + plotWidth, y, CreateBrush(226, 232, 240), 1);

            var labelAmount = maxAmount <= 0m
                ? 0m
                : maxAmount * (4 - i) / 4;

            AddText(FormatCurrency(labelAmount), 11, CreateBrush(100, 116, 139), 6, y - 9);
        }

        AddLine(ChartLeftPadding, ChartTopPadding, ChartLeftPadding, ChartTopPadding + plotHeight, CreateBrush(203, 213, 225), 1);
    }

    private void DrawDateLabels(IReadOnlyList<DailyTransactionSummary> summaries, double height)
    {
        if (summaries.Count == 0)
        {
            return;
        }

        var y = height - ChartBottomPadding + 18;
        var labelBrush = CreateBrush(100, 116, 139);

        if (GetSelectedPeriodDays() == 7)
        {
            for (var i = 0; i < summaries.Count; i++)
            {
                AddCenteredText(
                    summaries[i].Date.ToString("d.M.", CzechCulture),
                    11,
                    labelBrush,
                    GetChartX(i, summaries.Count),
                    y);
            }

            return;
        }

        AddCenteredText(
            summaries[0].Date.ToString("d.M.", CzechCulture),
            11,
            labelBrush,
            GetChartX(0, summaries.Count),
            y);
        AddCenteredText(
            summaries[^1].Date.ToString("d.M.", CzechCulture),
            11,
            labelBrush,
            GetChartX(summaries.Count - 1, summaries.Count),
            y);
    }

    private void DrawChartLine(
        IReadOnlyList<DailyTransactionSummary> summaries,
        Func<DailyTransactionSummary, decimal> valueSelector,
        decimal maxAmount,
        Brush brush)
    {
        var points = new List<Point>();

        for (var i = 0; i < summaries.Count; i++)
        {
            points.Add(GetChartPoint(valueSelector(summaries[i]), i, summaries.Count, maxAmount));
        }

        if (points.Count > 1)
        {
            ChartCanvas.Children.Add(new Path
            {
                Data = CreateSmoothGeometry(points),
                Stroke = brush,
                StrokeThickness = 3,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            });
        }

        if (summaries.Count > 31)
        {
            return;
        }

        foreach (var point in points)
        {
            var marker = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = brush,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };

            ChartCanvas.Children.Add(marker);
            Canvas.SetLeft(marker, point.X - 3);
            Canvas.SetTop(marker, point.Y - 3);
        }
    }

    private Point GetChartPoint(decimal amount, int index, int count, decimal maxAmount)
    {
        var plotHeight = GetPlotHeight(ChartCanvas.ActualHeight);
        var x = GetChartX(index, count);
        var amountRatio = (double)(amount / maxAmount);
        var y = ChartTopPadding + plotHeight - (plotHeight * amountRatio);

        return new Point(x, y);
    }

    private double GetChartX(int index, int count)
    {
        var plotWidth = GetPlotWidth(ChartCanvas.ActualWidth);

        return count <= 1
            ? ChartLeftPadding + (plotWidth / 2)
            : ChartLeftPadding + (plotWidth / (count - 1) * index);
    }

    private static Geometry CreateSmoothGeometry(IReadOnlyList<Point> points)
    {
        var figure = new PathFigure
        {
            StartPoint = points[0],
            IsClosed = false,
            IsFilled = false
        };

        for (var i = 0; i < points.Count - 1; i++)
        {
            var previous = i == 0 ? points[i] : points[i - 1];
            var current = points[i];
            var next = points[i + 1];
            var nextAfter = i + 2 < points.Count ? points[i + 2] : next;

            var controlPoint1 = new Point(
                current.X + (next.X - previous.X) / 6,
                current.Y + (next.Y - previous.Y) / 6);
            var controlPoint2 = new Point(
                next.X - (nextAfter.X - current.X) / 6,
                next.Y - (nextAfter.Y - current.Y) / 6);

            controlPoint1.Y = ClampToSegmentY(controlPoint1.Y, current, next);
            controlPoint2.Y = ClampToSegmentY(controlPoint2.Y, current, next);

            figure.Segments.Add(new BezierSegment(controlPoint1, controlPoint2, next, true));
        }

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        return geometry;
    }

    private static double ClampToSegmentY(double y, Point start, Point end)
    {
        var minY = Math.Min(start.Y, end.Y);
        var maxY = Math.Max(start.Y, end.Y);

        return Math.Clamp(y, minY, maxY);
    }

    private void DrawEmptyChartMessage(double width, double height)
    {
        var message = new TextBlock
        {
            Text = "No transactions in this period",
            FontSize = 14,
            Foreground = CreateBrush(100, 116, 139)
        };

        ChartCanvas.Children.Add(message);
        Canvas.SetLeft(message, width / 2 - 86);
        Canvas.SetTop(message, height / 2 - 12);
    }

    private void AddLine(double x1, double y1, double x2, double y2, Brush brush, double thickness)
    {
        ChartCanvas.Children.Add(new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = brush,
            StrokeThickness = thickness
        });
    }

    private void AddText(string text, double fontSize, Brush brush, double left, double top)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            Foreground = brush
        };

        ChartCanvas.Children.Add(textBlock);
        Canvas.SetLeft(textBlock, left);
        Canvas.SetTop(textBlock, top);
    }

    private void AddCenteredText(string text, double fontSize, Brush brush, double centerX, double top)
    {
        var textBlock = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            Foreground = brush
        };

        textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        var left = centerX - (textBlock.DesiredSize.Width / 2);
        left = Math.Clamp(left, 0, Math.Max(0, ChartCanvas.ActualWidth - textBlock.DesiredSize.Width));

        ChartCanvas.Children.Add(textBlock);
        Canvas.SetLeft(textBlock, left);
        Canvas.SetTop(textBlock, top);
    }

    private int GetSelectedPeriodDays()
    {
        if (ChartPeriodComboBox.SelectedItem is ComboBoxItem item
            && int.TryParse(item.Tag?.ToString(), out var days))
        {
            return days;
        }

        return 30;
    }

    private static double GetPlotWidth(double width)
    {
        return Math.Max(1, width - ChartLeftPadding - ChartRightPadding);
    }

    private static double GetPlotHeight(double height)
    {
        return Math.Max(1, height - ChartTopPadding - ChartBottomPadding);
    }

    private static string FormatCurrency(decimal amount)
    {
        return $"{amount.ToString("#,##0", CzechCulture)} K\u010d";
    }

    private static SolidColorBrush CreateBrush(byte red, byte green, byte blue)
    {
        return new SolidColorBrush(Color.FromRgb(red, green, blue));
    }
}
