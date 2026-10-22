using System.Collections.ObjectModel;
using AccountManagement.Application.Common.Interfaces;
using AccountManagement.Application.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AccountManagement.App.ViewModels;

public partial class CustomerManagerViewModel : ObservableObject
{
    private readonly ICustomerService _customerService;
    private readonly IMemoService _memoService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    partial void OnSearchTermChanged(string value) => _ = LoadCustomersAsync();

    [ObservableProperty]
    private bool _isEditing;

    public string FormTitleText => IsEditing ? "Edit Customer | তথ্য পরিবর্তন" : "Add Customer | নতুন কাস্টমার";
    public string SaveButtonText => IsEditing ? "💾 Update Customer | আপডেট করুন" : "+ Save Customer | সংরক্ষণ করুন";

    partial void OnIsEditingChanged(bool value)
    {
        OnPropertyChanged(nameof(FormTitleText));
        OnPropertyChanged(nameof(SaveButtonText));
    }

    [ObservableProperty]
    private CustomerDto? _selectedCustomerForEdit;

    // Form fields for adding/editing customer
    [ObservableProperty]
    private string _newCustomerName = string.Empty;

    [ObservableProperty]
    private string _newContactPerson = string.Empty;

    [ObservableProperty]
    private string _newPhoneNumber = string.Empty;

    [ObservableProperty]
    private string _newEmail = string.Empty;

    [ObservableProperty]
    private string _newAddress = string.Empty;

    [ObservableProperty]
    private decimal _newOpeningBalance = 0m;

    [ObservableProperty]
    private string _newNotes = string.Empty;

    public ObservableCollection<CustomerDto> Customers { get; } = new();

    public CustomerManagerViewModel(ICustomerService customerService, IMemoService memoService)
    {
        _customerService = customerService;
        _memoService = memoService;
    }

    [RelayCommand]
    public async Task LoadCustomersAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _customerService.GetAllCustomersAsync(SearchTerm);
            Customers.Clear();
            foreach (var c in list) Customers.Add(c);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not load customers: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void SelectForEdit(CustomerDto? customer)
    {
        if (customer == null) return;

        IsEditing = true;
        SelectedCustomerForEdit = customer;
        NewCustomerName = customer.Name;
        NewContactPerson = customer.ContactPerson ?? string.Empty;
        NewPhoneNumber = customer.PhoneNumber ?? string.Empty;
        NewEmail = customer.Email ?? string.Empty;
        NewAddress = customer.Address ?? string.Empty;
        NewOpeningBalance = customer.OpeningBalance;
        NewNotes = customer.Notes ?? string.Empty;
    }

    [RelayCommand]
    public void CancelEdit()
    {
        IsEditing = false;
        SelectedCustomerForEdit = null;
        ResetForm();
    }

    private void ResetForm()
    {
        NewCustomerName = string.Empty;
        NewContactPerson = string.Empty;
        NewPhoneNumber = string.Empty;
        NewEmail = string.Empty;
        NewAddress = string.Empty;
        NewOpeningBalance = 0m;
        NewNotes = string.Empty;
    }

    [RelayCommand]
    public async Task SaveCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCustomerName))
        {
            await Shell.Current.DisplayAlert("Validation", "Customer name is required | গ্রাহকের নাম আবশ্যক।", "OK");
            return;
        }

        try
        {
            var dto = new CreateCustomerDto
            {
                Name = NewCustomerName.Trim(),
                ContactPerson = string.IsNullOrWhiteSpace(NewContactPerson) ? null : NewContactPerson.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(NewPhoneNumber) ? null : NewPhoneNumber.Trim(),
                Email = string.IsNullOrWhiteSpace(NewEmail) ? null : NewEmail.Trim(),
                Address = string.IsNullOrWhiteSpace(NewAddress) ? null : NewAddress.Trim(),
                OpeningBalance = NewOpeningBalance,
                Notes = string.IsNullOrWhiteSpace(NewNotes) ? null : NewNotes.Trim()
            };

            if (IsEditing && SelectedCustomerForEdit != null)
            {
                var updated = await _customerService.UpdateCustomerAsync(SelectedCustomerForEdit.Id, dto);
                
                int index = Customers.IndexOf(SelectedCustomerForEdit);
                if (index >= 0)
                {
                    Customers[index] = updated;
                }
                else
                {
                    await LoadCustomersAsync();
                }

                IsEditing = false;
                SelectedCustomerForEdit = null;
                ResetForm();
                await Shell.Current.DisplayAlert("Success", $"Customer '{updated.Name}' updated successfully!", "OK");
            }
            else
            {
                var created = await _customerService.CreateCustomerAsync(dto);
                Customers.Insert(0, created);
                ResetForm();
                await Shell.Current.DisplayAlert("Success", $"Customer '{created.Name}' added successfully!", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not save customer: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task DeleteCustomerAsync(CustomerDto? customer)
    {
        if (customer == null) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Confirm Delete | গ্রাহক মুছে ফেলুন",
            $"Are you sure you want to delete '{customer.Name}'?\n\nআপনি কি নিশ্চিত যে গ্রাহক '{customer.Name}' মুছে ফেলতে চান?",
            "Delete | মুছে ফেলুন",
            "Cancel | বাতিল");

        if (!confirm) return;

        try
        {
            bool deleted = await _customerService.DeleteCustomerAsync(customer.Id);
            if (deleted)
            {
                Customers.Remove(customer);

                if (IsEditing && SelectedCustomerForEdit?.Id == customer.Id)
                {
                    CancelEdit();
                }

                await Shell.Current.DisplayAlert(
                    "Deleted | মুছে ফেলা হয়েছে",
                    $"Customer '{customer.Name}' has been removed successfully.\n(Associated ledger audit records are preserved).",
                    "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Customer could not be deleted.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Could not delete customer: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    public async Task PrintStatementAsync(CustomerDto? customer)
    {
        if (customer == null) return;

        try
        {
            var html = await _memoService.GenerateCustomerStatementMemoHtmlAsync(customer.Id);
            var safeName = string.Join("_", customer.Name.Split(Path.GetInvalidFileNameChars()));
            var filePath = await _memoService.SaveMemoHtmlToFileAsync(html, $"Statement_{safeName}_{DateTime.Now:yyyyMMdd}.html");

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Statement Error", $"Could not generate customer statement: {ex.Message}", "OK");
        }
    }
}
