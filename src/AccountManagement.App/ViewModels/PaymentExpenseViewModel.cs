using System.Collections.ObjectModel;
using AccountManagement.Application.Common.Interfaces;
using AccountManagement.Application.DTOs;
using AccountManagement.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AccountManagement.App.ViewModels;

public partial class PaymentExpenseViewModel : ObservableObject
{
    private readonly IPaymentService _paymentService;
    private readonly IExpenseService _expenseService;
    private readonly ICustomerService _customerService;

    [ObservableProperty]
    private bool _isLoading;

    // Payment Form
    [ObservableProperty]
    private DateTime _paymentDate = DateTime.Today;

    [ObservableProperty]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private CustomerDto? _selectedCustomer;

    partial void OnSelectedCustomerChanged(CustomerDto? value)
    {
        if (value != null)
        {
            CustomerName = value.Name;
        }
    }

    [ObservableProperty]
    private decimal _paymentAmount = 0m;

    [ObservableProperty]
    private PaymentMethod _paymentMethod = PaymentMethod.Cash;

    [ObservableProperty]
    private string _paymentReference = string.Empty;

    [ObservableProperty]
    private string _paymentRemarks = string.Empty;

    // Expense Form
    [ObservableProperty]
    private DateTime _expenseDate = DateTime.Today;

    [ObservableProperty]
    private string _expenseCategory = "Factory Rent";

    [ObservableProperty]
    private string _expenseDescription = string.Empty;

    [ObservableProperty]
    private decimal _expenseAmount = 0m;

    [ObservableProperty]
    private PaymentMethod _expensePaymentMethod = PaymentMethod.Cash;

    [ObservableProperty]
    private string _expensePaidTo = string.Empty;

    public ObservableCollection<CustomerDto> Customers { get; } = new();
    public ObservableCollection<PaymentDto> RecentPayments { get; } = new();
    public ObservableCollection<ExpenseDto> RecentExpenses { get; } = new();

    public List<string> StandardExpenseCategories { get; } = new()
    {
        "Factory Rent",
        "Electricity / Power",
        "Machine Maintenance",
        "Ink & Chemicals",
        "Staff Salaries / Wages",
        "Transport & Fare",
        "Tea & Refreshments",
        "Office Stationery",
        "Government Fees & Taxes",
        "Miscellaneous"
    };

    public PaymentExpenseViewModel(IPaymentService paymentService, IExpenseService expenseService, ICustomerService customerService)
    {
        _paymentService = paymentService;
        _expenseService = expenseService;
        _customerService = customerService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var customers = await _customerService.GetAllCustomersAsync();
            Customers.Clear();
            foreach (var c in customers) Customers.Add(c);

            var payments = await _paymentService.GetPaymentsAsync();
            RecentPayments.Clear();
            foreach (var p in payments.Take(20)) RecentPayments.Add(p);

            var expenses = await _expenseService.GetExpensesAsync();
            RecentExpenses.Clear();
            foreach (var e in expenses.Take(20)) RecentExpenses.Add(e);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not load data: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RecordPaymentAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomerName))
        {
            await Shell.Current.DisplayAlert("Validation", "Please enter customer name | গ্রাহকের নাম লিখুন।", "OK");
            return;
        }

        if (PaymentAmount <= 0)
        {
            await Shell.Current.DisplayAlert("Validation", "Payment amount must be greater than zero.", "OK");
            return;
        }

        try
        {
            Guid customerId;
            if (SelectedCustomer != null && SelectedCustomer.Name.Equals(CustomerName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                customerId = SelectedCustomer.Id;
            }
            else
            {
                var existing = Customers.FirstOrDefault(c => c.Name.Equals(CustomerName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    customerId = existing.Id;
                }
                else
                {
                    var created = await _customerService.CreateCustomerAsync(new CreateCustomerDto
                    {
                        Name = CustomerName.Trim()
                    });
                    customerId = created.Id;
                    Customers.Add(created);
                }
            }

            var dto = new CreatePaymentDto
            {
                PaymentDate = DateOnly.FromDateTime(PaymentDate),
                CustomerId = customerId,
                Amount = PaymentAmount,
                PaymentMethod = PaymentMethod,
                ReferenceNumber = PaymentReference,
                Remarks = PaymentRemarks
            };

            var p = await _paymentService.RecordPaymentAsync(dto);
            RecentPayments.Insert(0, p);

            // Reset form
            PaymentAmount = 0m;
            PaymentReference = string.Empty;
            PaymentRemarks = string.Empty;

            await Shell.Current.DisplayAlert("Success", $"Payment of ৳ {p.Amount:N2} recorded for {p.CustomerName}!", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not record payment: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task RecordExpenseAsync()
    {
        if (string.IsNullOrWhiteSpace(ExpenseCategory))
        {
            await Shell.Current.DisplayAlert("Validation", "Expense category is required.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(ExpenseDescription))
        {
            await Shell.Current.DisplayAlert("Validation", "Expense description is required.", "OK");
            return;
        }

        if (ExpenseAmount <= 0)
        {
            await Shell.Current.DisplayAlert("Validation", "Expense amount must be greater than zero.", "OK");
            return;
        }

        try
        {
            var dto = new CreateExpenseDto
            {
                ExpenseDate = DateOnly.FromDateTime(ExpenseDate),
                Category = ExpenseCategory,
                Description = ExpenseDescription.Trim(),
                Amount = ExpenseAmount,
                PaymentMethod = ExpensePaymentMethod,
                PaidTo = ExpensePaidTo.Trim()
            };

            var exp = await _expenseService.RecordExpenseAsync(dto);
            RecentExpenses.Insert(0, exp);

            // Reset form
            ExpenseDescription = string.Empty;
            ExpenseAmount = 0m;
            ExpensePaidTo = string.Empty;

            await Shell.Current.DisplayAlert("Success", $"Expense of ৳ {exp.Amount:N2} recorded under {exp.Category}!", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not record expense: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task DeletePaymentAsync(PaymentDto? payment)
    {
        if (payment == null) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Confirm Delete | জমা রেকর্ড মুছে ফেলুন",
            $"Are you sure you want to delete payment receipt '{payment.PaymentNumber}' of ৳ {payment.Amount:N2} for '{payment.CustomerName}'?\n\nআপনি কি নিশ্চিত যে '{payment.PaymentNumber}' জমা রেকর্ডটি মুছে ফেলতে চান? গ্রাহকের বকেয়া ব্যালেন্সে এটি সমন্বয় (পুনরায় যোগ) হবে।",
            "Delete | মুছে ফেলুন",
            "Cancel | বাতিল");

        if (!confirm) return;

        try
        {
            bool deleted = await _paymentService.DeletePaymentAsync(payment.Id);
            if (deleted)
            {
                RecentPayments.Remove(payment);
                await Shell.Current.DisplayAlert(
                    "Deleted | মুছে ফেলা হয়েছে",
                    $"Payment receipt '{payment.PaymentNumber}' deleted successfully.\nCustomer balance updated.",
                    "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Payment record could not be deleted.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not delete payment: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task DeleteExpenseAsync(ExpenseDto? expense)
    {
        if (expense == null) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Confirm Delete | খরচ রেকর্ড মুছে ফেলুন",
            $"Are you sure you want to delete expense record '{expense.ExpenseNumber}' ({expense.Category} - ৳ {expense.Amount:N2})?\n\nআপনি কি নিশ্চিত যে '{expense.ExpenseNumber}' খরচের রেকর্ডটি মুছে ফেলতে চান?",
            "Delete | মুছে ফেলুন",
            "Cancel | বাতিল");

        if (!confirm) return;

        try
        {
            bool deleted = await _expenseService.DeleteExpenseAsync(expense.Id);
            if (deleted)
            {
                RecentExpenses.Remove(expense);
                await Shell.Current.DisplayAlert(
                    "Deleted | মুছে ফেলা হয়েছে",
                    $"Expense record '{expense.ExpenseNumber}' deleted successfully.",
                    "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Expense record could not be deleted.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not delete expense: {ex.Message}", "OK");
        }
    }
}
