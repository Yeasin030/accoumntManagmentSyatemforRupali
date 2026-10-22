using AccountManagement.App.ViewModels;

namespace AccountManagement.App.Views;

public partial class RateManagerPage : ContentPage
{
    private readonly RateManagerViewModel _viewModel;

    public RateManagerPage(RateManagerViewModel viewModel)
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
