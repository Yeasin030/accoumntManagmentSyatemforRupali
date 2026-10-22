using AccountManagement.App.ViewModels;

namespace AccountManagement.App.Views;

public partial class PaymentExpensePage : ContentPage
{
    private readonly PaymentExpenseViewModel _viewModel;

    public PaymentExpensePage(PaymentExpenseViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//DashboardPage");
    }
}
