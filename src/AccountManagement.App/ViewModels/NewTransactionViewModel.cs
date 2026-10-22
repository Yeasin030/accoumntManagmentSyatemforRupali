using System.Collections.ObjectModel;
using AccountManagement.Application.Common.Interfaces;
using AccountManagement.Application.DTOs;
using AccountManagement.Domain.Enums;
using AccountManagement.Domain.Formatting;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AccountManagement.App.ViewModels;

public partial class NewTransactionViewModel : ObservableObject
{
    private readonly ILedgerService _ledgerService;
    private readonly ICustomerService _customerService;
    private readonly IRateService _rateService;
    private readonly IMemoService _memoService;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private DateTime _transactionDate = DateTime.Today;

    [ObservableProperty]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private string _customerPhone = string.Empty;

    [ObservableProperty]
    private CustomerDto? _selectedCustomer;

    partial void OnSelectedCustomerChanged(CustomerDto? value)
    {
        if (value != null)
        {
            CustomerName = value.Name;
            if (!string.IsNullOrWhiteSpace(value.PhoneNumber))
            {
                CustomerPhone = value.PhoneNumber;
            }
        }
    }

    [ObservableProperty]
    private string _workDescription = string.Empty;

    [ObservableProperty]
    private decimal _discount = 0m;

    [ObservableProperty]
    private string? _notes;

    // Advance Payment
    [ObservableProperty]
    private decimal _advancePaymentAmount = 0m;

    [ObservableProperty]
    private PaymentMethod _advancePaymentMethod = PaymentMethod.Cash;

    [ObservableProperty]
    private string _advancePaymentReference = string.Empty;

    // Computed Totals
    [ObservableProperty]
    private decimal _materialCostTotal = 0m;

    [ObservableProperty]
    private decimal _productionCostTotal = 0m;

    [ObservableProperty]
    private decimal _subTotal = 0m;

    [ObservableProperty]
    private decimal _totalBillAmount = 0m;

    [ObservableProperty]
    private decimal _balanceDue = 0m;

    [ObservableProperty]
    private string _totalBillFormatted = "৳ 0.00";

    [ObservableProperty]
    private string _balanceDueFormatted = "৳ 0.00";

    public ObservableCollection<CustomerDto> Customers { get; } = new();
    public ObservableCollection<MaterialDto> AvailableMaterials { get; } = new();
    public ObservableCollection<TransactionLineItemViewModel> Items { get; } = new();
    public ObservableCollection<CostEntryViewModel> CostEntries { get; } = new();

    public NewTransactionViewModel(ILedgerService ledgerService, ICustomerService customerService, IRateService rateService, IMemoService memoService)
    {
        _ledgerService = ledgerService;
        _customerService = customerService;
        _rateService = rateService;
        _memoService = memoService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        try
        {
            var customers = await _customerService.GetAllCustomersAsync();
            Customers.Clear();
            foreach (var c in customers) Customers.Add(c);

            var materials = await _rateService.GetMaterialsAsync();
            AvailableMaterials.Clear();
            foreach (var m in materials) AvailableMaterials.Add(m);

            // Add default first item
            if (Items.Count == 0)
            {
                AddItem();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error initializing form: {ex.Message}";
        }
    }

    [RelayCommand]
    public void AddItem()
    {
        var itemVm = new TransactionLineItemViewModel(this);
        itemVm.PropertyChanged += (s, e) => RecalculateTotals();
        Items.Add(itemVm);
        RecalculateTotals();
    }

    [RelayCommand]
    public void RemoveItem(TransactionLineItemViewModel? item)
    {
        if (item != null && Items.Contains(item))
        {
            Items.Remove(item);
            RecalculateTotals();
        }
    }

    [RelayCommand]
    public void AddCostEntry()
    {
        var costVm = new CostEntryViewModel(this);
        costVm.PropertyChanged += (s, e) => RecalculateTotals();
        CostEntries.Add(costVm);
        RecalculateTotals();
    }

    [RelayCommand]
    public void RemoveCostEntry(CostEntryViewModel? cost)
    {
        if (cost != null && CostEntries.Contains(cost))
        {
            CostEntries.Remove(cost);
            RecalculateTotals();
        }
    }

    [RelayCommand]
    public void AddQuickCost(string type)
    {
        var costVm = new CostEntryViewModel(this);
        costVm.PropertyChanged += (s, e) => RecalculateTotals();
        switch (type?.ToLowerInvariant())
        {
            case "single_plate":
                costVm.Category = CostCategory.Plate;
                costVm.Description = "Single Plate (সিঙ্গেল প্লেট)";
                costVm.Amount = 70m;
                break;
            case "full_plate":
                costVm.Category = CostCategory.Plate;
                costVm.Description = "Full Plate (ফুল প্লেট)";
                costVm.Amount = 140m;
                break;
            case "demy_plate":
                costVm.Category = CostCategory.Plate;
                costVm.Description = "Demy Plate (ডিমাই প্লেট)";
                costVm.Amount = 200m;
                break;
            case "print_1color":
                costVm.Category = CostCategory.Printing;
                costVm.Description = "Printing 1-Color / 1000 Pcs (১ কালার ছাপা)";
                costVm.Amount = 150m;
                break;
            case "print_2color":
                costVm.Category = CostCategory.Printing;
                costVm.Description = "Printing 2-Color / 1000 Pcs (২ কালার ছাপা)";
                costVm.Amount = 300m;
                break;
            case "print_3color":
                costVm.Category = CostCategory.Printing;
                costVm.Description = "Printing 3-Color / 1000 Pcs (৩ কালার ছাপা)";
                costVm.Amount = 450m;
                break;
            case "print_4color":
                costVm.Category = CostCategory.Printing;
                costVm.Description = "Printing 4-Color / 1000 Pcs (৪ কালার ছাপা)";
                costVm.Amount = 600m;
                break;
            case "cashmemo_binding":
                costVm.Category = CostCategory.Binding;
                costVm.Description = "Cash Memo 2-Part (ক্যাশ মেমো ২পারা ১টি বই)";
                costVm.Amount = 12m;
                break;
            default:
                break;
        }
        CostEntries.Add(costVm);
        RecalculateTotals();
    }

    public void RecalculateTotals()
    {
        MaterialCostTotal = Items.Sum(i => i.TotalCost);
        ProductionCostTotal = CostEntries.Sum(c => c.Amount);
        SubTotal = MaterialCostTotal + ProductionCostTotal;
        TotalBillAmount = Math.Max(0m, SubTotal - Discount);
        BalanceDue = Math.Max(0m, TotalBillAmount - AdvancePaymentAmount);

        TotalBillFormatted = CurrencyFormatter.FormatBdt(TotalBillAmount);
        BalanceDueFormatted = CurrencyFormatter.FormatBdt(BalanceDue);
    }

    partial void OnDiscountChanged(decimal value) => RecalculateTotals();
    partial void OnAdvancePaymentAmountChanged(decimal value) => RecalculateTotals();

    public List<string> StandardPaperSizes { get; } = new()
    {
        "18/23",
        "9/14",
        "9/11.5",
        "7.5/10",
        "7.5/9",
        "22/28",
        "20/30",
        "23/36"
    };

    public void ResetForm()
    {
        TransactionDate = DateTime.Today;
        CustomerName = string.Empty;
        CustomerPhone = string.Empty;
        SelectedCustomer = null;
        WorkDescription = string.Empty;
        Discount = 0m;
        Notes = null;
        AdvancePaymentAmount = 0m;
        AdvancePaymentMethod = PaymentMethod.Cash;
        AdvancePaymentReference = string.Empty;
        Items.Clear();
        CostEntries.Clear();
        AddItem();
        RecalculateTotals();
    }

    [RelayCommand]
    public async Task SaveTransactionAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomerName))
        {
            await Shell.Current.DisplayAlert("Validation", "Please enter customer name | গ্রাহকের নাম লিখুন।", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(WorkDescription))
        {
            await Shell.Current.DisplayAlert("Validation", "Please enter a work description | কাজের বিবরণ লিখুন।", "OK");
            return;
        }

        if (Items.Count == 0 && CostEntries.Count == 0)
        {
            await Shell.Current.DisplayAlert("Validation", "Please add at least one paper item or production cost entry.", "OK");
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            // Resolve existing customer or create a new one on the fly
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
                    var createdCustomer = await _customerService.CreateCustomerAsync(new CreateCustomerDto
                    {
                        Name = CustomerName.Trim(),
                        PhoneNumber = string.IsNullOrWhiteSpace(CustomerPhone) ? null : CustomerPhone.Trim()
                    });
                    customerId = createdCustomer.Id;
                    Customers.Add(createdCustomer);
                }
            }

            var dto = new CreateTransactionDto
            {
                TransactionDate = DateOnly.FromDateTime(TransactionDate),
                CustomerId = customerId,
                WorkDescription = WorkDescription.Trim(),
                Discount = Discount,
                Notes = Notes,
                Items = Items
                    .Where(i => !string.IsNullOrWhiteSpace(i.MaterialName) || i.SelectedMaterial != null)
                    .Select(i => new CreateTransactionItemDto
                    {
                        MaterialId = i.SelectedMaterial?.Id,
                        MaterialName = string.IsNullOrWhiteSpace(i.MaterialName) ? (i.SelectedMaterial?.Name ?? "Material") : i.MaterialName.Trim(),
                        Gsm = i.Gsm,
                        Size = i.Size,
                        Quantity = i.Quantity,
                        Unit = i.Unit,
                        UnitRate = i.UnitRate
                    }).ToList(),
                CostEntries = CostEntries
                    .Where(c => !string.IsNullOrWhiteSpace(c.Description) || c.Amount > 0)
                    .Select(c => new CreateCostEntryDto
                    {
                        Category = c.Category,
                        Description = string.IsNullOrWhiteSpace(c.Description) ? c.Category.ToString() : c.Description.Trim(),
                        Amount = c.Amount
                    }).ToList(),
                InitialPaymentAmount = AdvancePaymentAmount > 0 ? AdvancePaymentAmount : null,
                InitialPaymentMethod = AdvancePaymentAmount > 0 ? AdvancePaymentMethod : null,
                InitialPaymentReference = AdvancePaymentReference
            };

            var created = await _ledgerService.CreateTransactionAsync(dto);

            bool printMemo = await Shell.Current.DisplayAlert(
                "Transaction Saved | সফল",
                $"Job {created.TransactionNumber} saved successfully!\n\nDo you want to print / view the Customer Cash Memo now?\nকাস্টমার ক্যাশ মেমো প্রিন্ট করবেন?",
                "Print Memo (হ্যাঁ)",
                "Later (পরে)");

            if (printMemo)
            {
                try
                {
                    var html = await _memoService.GenerateTransactionMemoHtmlAsync(created.Id);
                    var filePath = await _memoService.SaveMemoHtmlToFileAsync(html, $"Memo_{created.TransactionNumber}.html");
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

            ResetForm();
            await Shell.Current.GoToAsync("//LedgerPage");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving transaction: {ex.Message}";
            await Shell.Current.DisplayAlert("Error", ErrorMessage, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<decimal?> ResolveRateAsync(Guid materialId, string? gsm, string? size, decimal quantity)
    {
        try
        {
            var date = DateOnly.FromDateTime(TransactionDate);
            return await _rateService.ResolveRateAsync(materialId, date, gsm, size, quantity);
        }
        catch
        {
            return null;
        }
    }
}

public partial class TransactionLineItemViewModel : ObservableObject
{
    private readonly NewTransactionViewModel _parent;

    [ObservableProperty]
    private MaterialDto? _selectedMaterial;

    [ObservableProperty]
    private string _materialName = string.Empty;

    [ObservableProperty]
    private string _gsm = string.Empty;

    [ObservableProperty]
    private string _size = string.Empty;

    [ObservableProperty]
    private decimal _quantity = 1m;

    [ObservableProperty]
    private UnitOfMeasure _unit = UnitOfMeasure.Ream;

    [ObservableProperty]
    private decimal _unitRate = 0m;

    [ObservableProperty]
    private decimal _totalCost = 0m;

    public TransactionLineItemViewModel(NewTransactionViewModel parent)
    {
        _parent = parent;
    }

    partial void OnSelectedMaterialChanged(MaterialDto? value)
    {
        if (value != null)
        {
            MaterialName = value.Name;
            Gsm = value.DefaultGsm ?? string.Empty;
            Size = string.IsNullOrWhiteSpace(value.DefaultSize) ? "18/23" : value.DefaultSize;
            Unit = value.DefaultUnit;
            if (value.CurrentEffectiveRate.HasValue && value.CurrentEffectiveRate.Value > 0)
            {
                UnitRate = value.CurrentEffectiveRate.Value;
            }
            _ = UpdateRateAsync();
        }
        Recalculate();
    }

    partial void OnSizeChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            string norm = value.Trim().Replace("x", "/").Replace("X", "/").Replace(" ", "");
            if (norm is "9/14" or "9/11.5" or "7.5/10" or "7.5/9")
            {
                if (Unit == UnitOfMeasure.Ream)
                {
                    Unit = UnitOfMeasure.Piece;
                }
            }
            else if (norm is "18/23" or "23/36")
            {
                if (Unit == UnitOfMeasure.Piece)
                {
                    Unit = UnitOfMeasure.Ream;
                }
            }
        }
        _ = UpdateRateAsync();
    }

    partial void OnUnitChanged(UnitOfMeasure value)
    {
        _ = UpdateRateAsync();
        Recalculate();
    }

    partial void OnGsmChanged(string value) => _ = UpdateRateAsync();
    partial void OnQuantityChanged(decimal value) => Recalculate();
    partial void OnUnitRateChanged(decimal value) => Recalculate();

    private async Task UpdateRateAsync()
    {
        if (SelectedMaterial != null)
        {
            var resolved = await _parent.ResolveRateAsync(SelectedMaterial.Id, Gsm, Size, Quantity);
            if (resolved.HasValue && resolved.Value > 0)
            {
                UnitRate = resolved.Value;
                Recalculate();
                _parent.RecalculateTotals();
            }
        }
    }

    private void Recalculate()
    {
        TotalCost = Math.Round(Quantity * UnitRate, 2, MidpointRounding.AwayFromZero);
    }
}

public partial class CostEntryViewModel : ObservableObject
{
    private readonly NewTransactionViewModel _parent;

    [ObservableProperty]
    private CostCategory _category = CostCategory.Miscellaneous;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private decimal _amount = 0m;

    public CostEntryViewModel(NewTransactionViewModel parent)
    {
        _parent = parent;
    }

    partial void OnCategoryChanged(CostCategory value)
    {
        if (string.IsNullOrWhiteSpace(Description) || Description == "Miscellaneous")
        {
            Description = value switch
            {
                CostCategory.Plate => "Single Plate (সিঙ্গেল প্লেট)",
                CostCategory.Printing => "Printing 1-Color / 1000 Pcs (ছাপা ১ কালার)",
                CostCategory.Binding => "Cash Memo Binding (ক্যাশ মেমো বাইন্ডিং)",
                _ => value.ToString()
            };
        }

        if (Amount == 0m)
        {
            Amount = value switch
            {
                CostCategory.Plate => 70m,       // Standard Single Plate
                CostCategory.Printing => 150m,   // Standard 1 Color per 1000
                CostCategory.Binding => 12m,     // Standard 2-part Cash Memo book
                _ => Amount
            };
        }

        _parent.RecalculateTotals();
    }

    partial void OnAmountChanged(decimal value)
    {
        _parent.RecalculateTotals();
    }
}
