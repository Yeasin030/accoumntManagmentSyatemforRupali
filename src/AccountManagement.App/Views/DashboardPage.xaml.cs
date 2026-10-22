using AccountManagement.App.ViewModels;

namespace AccountManagement.App.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDashboardAsync();
    }

    private async void OnNewJobOrderClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//NewTransactionPage");
    }

    private async void OnViewFullLedgerClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//LedgerPage");
    }

    private async void OnReceivePaymentClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//PaymentExpensePage");
    }

    private async void OnManageRatesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//RateManagerPage");
    }

    private async void OnViewCustomersClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//CustomerManagerPage");
    }

    private async void OnBackupRestoreClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//BackupRestorePage");
    }
}
