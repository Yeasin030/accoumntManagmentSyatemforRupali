using AccountManagement.App.ViewModels;

namespace AccountManagement.App.Views;

public partial class LedgerPage : ContentPage
{
    private readonly LedgerViewModel _viewModel;

    public LedgerPage(LedgerViewModel viewModel)
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

    private async void OnNewTransactionClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//NewTransactionPage");
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//DashboardPage");
    }
}
