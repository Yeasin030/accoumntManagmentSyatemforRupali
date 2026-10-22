using AccountManagement.App.ViewModels;

namespace AccountManagement.App.Views;

public partial class CustomerManagerPage : ContentPage
{
    private readonly CustomerManagerViewModel _viewModel;

    public CustomerManagerPage(CustomerManagerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadCustomersAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//DashboardPage");
    }
}
