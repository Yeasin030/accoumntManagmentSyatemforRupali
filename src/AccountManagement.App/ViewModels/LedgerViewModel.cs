using System.Collections.ObjectModel;
using AccountManagement.Application.Common.Interfaces;
using AccountManagement.Application.DTOs;
using AccountManagement.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AccountManagement.App.ViewModels;

public partial class LedgerViewModel : ObservableObject
{
    private readonly ILedgerService _ledgerService;
    private readonly ICustomerService _customerService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // Filters
    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    [ObservableProperty]
    private CustomerDto? _selectedCustomer;

    [ObservableProperty]
    private TransactionStatus? _selectedStatus;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    // Pagination
    [ObservableProperty]
    private int _pageNumber = 1;

    [ObservableProperty]
    private int _pageSize = 15;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private bool _hasPreviousPage;

    [ObservableProperty]
    private bool _hasNextPage;

    public ObservableCollection<TransactionDto> Transactions { get; } = new();
    public ObservableCollection<CustomerDto> Customers { get; } = new();

    public LedgerViewModel(ILedgerService ledgerService, ICustomerService customerService)
    {
        _ledgerService = ledgerService;
        _customerService = customerService;

        // Default to current month date range
        var today = DateTime.Today;
        StartDate = new DateTime(today.Year, today.Month, 1);
        EndDate = today;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await LoadCustomersAsync();
        await LoadTransactionsAsync();
    }

    [RelayCommand]
    public async Task LoadCustomersAsync()
    {
        try
        {
            var customerList = await _customerService.GetAllCustomersAsync();
            Customers.Clear();
            foreach (var c in customerList)
            {
                Customers.Add(c);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading customers: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task LoadTransactionsAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            DateOnly? start = StartDate.HasValue ? DateOnly.FromDateTime(StartDate.Value) : null;
            DateOnly? end = EndDate.HasValue ? DateOnly.FromDateTime(EndDate.Value) : null;

            var result = await _ledgerService.GetTransactionsAsync(
                start,
                end,
                SelectedCustomer?.Id,
                SelectedStatus,
                SearchTerm,
                PageNumber,
                PageSize);

            Transactions.Clear();
            foreach (var item in result.Items)
            {
                Transactions.Add(item);
            }

            TotalCount = result.TotalCount;
            TotalPages = Math.Max(1, result.TotalPages);
            HasPreviousPage = result.HasPreviousPage;
            HasNextPage = result.HasNextPage;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading transactions: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task NextPageAsync()
    {
        if (HasNextPage)
        {
            PageNumber++;
            await LoadTransactionsAsync();
        }
    }

    [RelayCommand]
    public async Task PreviousPageAsync()
    {
        if (HasPreviousPage)
        {
            PageNumber--;
            await LoadTransactionsAsync();
        }
    }

    [RelayCommand]
    public async Task ResetFiltersAsync()
    {
        var today = DateTime.Today;
        StartDate = new DateTime(today.Year, today.Month, 1);
        EndDate = today;
        SelectedCustomer = null;
        SelectedStatus = null;
        SearchTerm = string.Empty;
        PageNumber = 1;

        await LoadTransactionsAsync();
    }

    [RelayCommand]
    public async Task CancelTransactionAsync(TransactionDto? transaction)
    {
        if (transaction == null || transaction.Status == TransactionStatus.Cancelled)
            return;

        string reason = await Shell.Current.DisplayPromptAsync(
            "Cancel Transaction",
            $"Enter reason for cancelling {transaction.TransactionNumber}:",
            "Confirm Cancel",
            "Abort",
            "Customer request",
            150);

        if (string.IsNullOrWhiteSpace(reason))
            return;

        try
        {
            bool success = await _ledgerService.CancelTransactionAsync(transaction.Id, reason.Trim());
            if (success)
            {
                await Shell.Current.DisplayAlert("Cancelled", $"Transaction {transaction.TransactionNumber} cancelled successfully.", "OK");
                await LoadTransactionsAsync();
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not cancel transaction: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task DeleteTransactionAsync(TransactionDto? transaction)
    {
        if (transaction == null) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Confirm Delete | লেনদেন মুছে ফেলুন",
            $"Are you sure you want to permanently delete transaction '{transaction.TransactionNumber}' ({transaction.CustomerName})?\n\nআপনি কি নিশ্চিত যে '{transaction.TransactionNumber}' নম্বর লেনদেনটি স্থায়ীভাবে মুছে ফেলতে চান? গ্রাহকের বকেয়া ব্যালেন্স স্বয়ংক্রিয়ভাবে সমন্বয় করা হবে।",
            "Delete | মুছে ফেলুন",
            "Cancel | বাতিল");

        if (!confirm) return;

        try
        {
            bool deleted = await _ledgerService.DeleteTransactionAsync(transaction.Id);
            if (deleted)
            {
                Transactions.Remove(transaction);
                TotalCount = Math.Max(0, TotalCount - 1);

                await Shell.Current.DisplayAlert(
                    "Deleted | মুছে ফেলা হয়েছে",
                    $"Transaction '{transaction.TransactionNumber}' has been permanently deleted.\nCustomer balance has been updated.",
                    "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Transaction could not be deleted.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not delete transaction: {ex.Message}", "OK");
        }
    }
}
