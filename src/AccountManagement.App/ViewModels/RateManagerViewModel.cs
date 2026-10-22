using System.Collections.ObjectModel;
using AccountManagement.Application.Common.Interfaces;
using AccountManagement.Application.DTOs;
using AccountManagement.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AccountManagement.App.ViewModels;

public partial class RateManagerViewModel : ObservableObject
{
    private readonly IRateService _rateService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private RatePeriodDto? _selectedPeriod;

    // New/Edit Rate Entry form fields
    [ObservableProperty]
    private MaterialDto? _selectedMaterial;

    [ObservableProperty]
    private string _gsm = string.Empty;

    [ObservableProperty]
    private string _size = string.Empty;

    [ObservableProperty]
    private decimal _minQuantityTier = 1m;

    [ObservableProperty]
    private decimal _ratePerUnit = 0m;

    [ObservableProperty]
    private UnitOfMeasure _unit = UnitOfMeasure.Ream;

    [ObservableProperty]
    private string _remarks = string.Empty;

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

    public ObservableCollection<RatePeriodDto> RatePeriods { get; } = new();
    public ObservableCollection<MaterialDto> Materials { get; } = new();
    public ObservableCollection<MaterialRateDto> CurrentRates { get; } = new();

    public RateManagerViewModel(IRateService rateService)
    {
        _rateService = rateService;
    }

    [RelayCommand]
    public void SelectRate(MaterialRateDto? rate)
    {
        if (rate == null) return;
        SelectedMaterial = Materials.FirstOrDefault(m => m.Id == rate.MaterialId);
        Gsm = rate.Gsm ?? string.Empty;
        Size = rate.Size ?? string.Empty;
        RatePerUnit = rate.RatePerUnit;
        Unit = rate.Unit;
        MinQuantityTier = rate.MinQuantityTier;
        Remarks = rate.Remarks ?? string.Empty;
        StatusMessage = $"Selected {rate.MaterialName} ({rate.Size}) for editing.";
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var periods = await _rateService.GetRatePeriodsAsync();
            RatePeriods.Clear();
            foreach (var p in periods) RatePeriods.Add(p);

            var materials = await _rateService.GetMaterialsAsync();
            Materials.Clear();
            foreach (var m in materials) Materials.Add(m);

            if (RatePeriods.Count > 0 && SelectedPeriod == null)
            {
                SelectedPeriod = RatePeriods[0];
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedPeriodChanged(RatePeriodDto? value)
    {
        if (value != null)
        {
            _ = LoadRatesForPeriodAsync(value.Id);
        }
    }

    private async Task LoadRatesForPeriodAsync(Guid periodId)
    {
        IsLoading = true;
        try
        {
            var rates = await _rateService.GetRatesForPeriodAsync(periodId);
            CurrentRates.Clear();
            foreach (var r in rates) CurrentRates.Add(r);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading rates: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task CreateNewPeriodAsync()
    {
        string monthName = await Shell.Current.DisplayPromptAsync(
            "New Rate Period",
            "Enter Period Name (e.g., November 2026):",
            "Create",
            "Cancel",
            "Next Month Rates");

        if (string.IsNullOrWhiteSpace(monthName)) return;

        var now = DateTime.UtcNow.AddMonths(1);
        var startDate = new DateOnly(now.Year, now.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        try
        {
            var period = await _rateService.CreateRatePeriodAsync(monthName.Trim(), now.Year, now.Month, startDate, endDate);
            RatePeriods.Insert(0, period);
            SelectedPeriod = period;
            await Shell.Current.DisplayAlert("Created", $"Rate period '{period.Name}' created successfully.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not create period: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task SaveRateAsync()
    {
        if (SelectedPeriod == null)
        {
            await Shell.Current.DisplayAlert("Validation", "Please select a rate period.", "OK");
            return;
        }

        if (SelectedMaterial == null)
        {
            await Shell.Current.DisplayAlert("Validation", "Please select a material.", "OK");
            return;
        }

        if (RatePerUnit <= 0)
        {
            await Shell.Current.DisplayAlert("Validation", "Rate per unit must be greater than zero.", "OK");
            return;
        }

        try
        {
            var dto = new CreateOrUpdateMaterialRateDto
            {
                RatePeriodId = SelectedPeriod.Id,
                MaterialId = SelectedMaterial.Id,
                Gsm = string.IsNullOrWhiteSpace(Gsm) ? SelectedMaterial.DefaultGsm : Gsm.Trim(),
                Size = string.IsNullOrWhiteSpace(Size) ? SelectedMaterial.DefaultSize : Size.Trim(),
                MinQuantityTier = MinQuantityTier > 0 ? MinQuantityTier : 1m,
                RatePerUnit = RatePerUnit,
                Unit = Unit,
                Remarks = Remarks
            };

            var saved = await _rateService.SaveRateAsync(dto);
            await LoadRatesForPeriodAsync(SelectedPeriod.Id);

            RatePerUnit = 0m;
            Remarks = string.Empty;
            StatusMessage = $"Saved rate for {saved.MaterialName}: ৳ {saved.RatePerUnit:N2}";
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not save rate: {ex.Message}", "OK");
        }
    }
}
