using AccountManagement.App.ViewModels;

namespace AccountManagement.App.Views;

public partial class BackupRestorePage : ContentPage
{
    private readonly BackupRestoreViewModel _viewModel;

    public BackupRestorePage(BackupRestoreViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//DashboardPage");
    }
}
