using System.Collections.ObjectModel;
using AccountManagement.Application.Common.Interfaces;
using AccountManagement.Application.DTOs;
using AccountManagement.Domain.Formatting;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AccountManagement.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardService _dashboardService;
    private readonly IMemoService _memoService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // Today Stats
    [ObservableProperty]
    private int _todayTransactionCount;

    [ObservableProperty]
    private string _todayRevenueFormatted = "৳ 0.00";

    [ObservableProperty]
    private string _todayDirectCostsFormatted = "৳ 0.00";

    [ObservableProperty]
    private string _todayPaymentsFormatted = "৳ 0.00";

    [ObservableProperty]
    private string _todayExpensesFormatted = "৳ 0.00";

    [ObservableProperty]
    private string _todayNetCashFormatted = "৳ 0.00";

    // Month Stats
    [ObservableProperty]
    private string _monthRevenueFormatted = "৳ 0.00";

    [ObservableProperty]
    private string _monthDirectCostsFormatted = "৳ 0.00";

    [ObservableProperty]
    private string _monthExpensesFormatted = "৳ 0.00";

    [ObservableProperty]
    private string _monthGrossProfitFormatted = "৳ 0.00";

    [ObservableProperty]
    private string _monthNetProfitFormatted = "৳ 0.00";

    [ObservableProperty]
    private string _profitMarginFormatted = "0.0% Margin";

    // Overall
    [ObservableProperty]
    private string _totalOutstandingDueFormatted = "৳ 0.00";

    [ObservableProperty]
    private int _activeCustomerCount;

    [ObservableProperty]
    private int _totalMaterialCount;

    // Header metadata
    [ObservableProperty]
    private string _currentDateText = DateTime.Today.ToString("dddd, dd MMMM yyyy");

    [ObservableProperty]
    private string _databaseModeText = "🟢 Local Offline SQLite Storage Active";

    public ObservableCollection<TransactionDto> RecentTransactions { get; } = new();

    public DashboardViewModel(IDashboardService dashboardService, IMemoService memoService)
    {
        _dashboardService = dashboardService;
        _memoService = memoService;
    }

    [RelayCommand]
    public async Task PrintMemoAsync(TransactionDto? transaction)
    {
        if (transaction == null) return;

        try
        {
            var html = await _memoService.GenerateTransactionMemoHtmlAsync(transaction.Id);
            var filePath = await _memoService.SaveMemoHtmlToFileAsync(html, $"Memo_{transaction.TransactionNumber}.html");

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Memo Notice", $"Could not launch memo: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task LoadDashboardAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var summary = await _dashboardService.GetDashboardSummaryAsync();

            TodayTransactionCount = summary.TodayTransactionCount;
            TodayRevenueFormatted = CurrencyFormatter.FormatBdt(summary.TodayRevenue);
            TodayDirectCostsFormatted = CurrencyFormatter.FormatBdt(summary.TodayDirectCosts);
            TodayPaymentsFormatted = CurrencyFormatter.FormatBdt(summary.TodayPaymentsReceived);
            TodayExpensesFormatted = CurrencyFormatter.FormatBdt(summary.TodayExpenses);

            decimal todayNetCash = summary.TodayPaymentsReceived - summary.TodayExpenses;
            TodayNetCashFormatted = CurrencyFormatter.FormatBdt(todayNetCash);

            MonthRevenueFormatted = CurrencyFormatter.FormatBdt(summary.MonthRevenue);
            MonthDirectCostsFormatted = CurrencyFormatter.FormatBdt(summary.MonthDirectCosts);
            MonthExpensesFormatted = CurrencyFormatter.FormatBdt(summary.MonthExpenses);
            MonthGrossProfitFormatted = CurrencyFormatter.FormatBdt(summary.MonthGrossProfit);
            MonthNetProfitFormatted = CurrencyFormatter.FormatBdt(summary.MonthNetProfit);

            decimal margin = summary.MonthRevenue > 0
                ? Math.Round((summary.MonthNetProfit / summary.MonthRevenue) * 100, 1)
                : 0m;
            ProfitMarginFormatted = $"{margin:F1}% Net Margin";

            TotalOutstandingDueFormatted = CurrencyFormatter.FormatBdt(summary.TotalOutstandingCustomerDue);
            ActiveCustomerCount = summary.ActiveCustomerCount;
            TotalMaterialCount = summary.TotalMaterialCount;
            CurrentDateText = DateTime.Today.ToString("dddd, dd MMMM yyyy");

            RecentTransactions.Clear();
            foreach (var txn in summary.RecentTransactions)
            {
                RecentTransactions.Add(txn);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to load dashboard data: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
