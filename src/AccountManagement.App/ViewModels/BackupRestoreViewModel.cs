using AccountManagement.Application.Common.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AccountManagement.App.ViewModels;

public partial class BackupRestoreViewModel : ObservableObject
{
    private readonly IBackupService _backupService;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _backupDirectory = @"C:\RupaliAccountBackups";

    [ObservableProperty]
    private string _statusMessage = "Ready to create or restore database backup.";

    [ObservableProperty]
    private string _lastBackupFile = string.Empty;

    public BackupRestoreViewModel(IBackupService backupService)
    {
        _backupService = backupService;
    }

    [RelayCommand]
    public async Task CreateBackupAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        StatusMessage = "Creating verified database backup bundle...";

        try
        {
            string backupPath = await _backupService.CreateBackupAsync(BackupDirectory);
            LastBackupFile = backupPath;
            StatusMessage = $"✓ Backup created and verified successfully:\n{backupPath}";

            await Shell.Current.DisplayAlert("Backup Success", $"Database backup successfully written to:\n{backupPath}", "OK");
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Backup creation failed: {ex.Message}";
            await Shell.Current.DisplayAlert("Backup Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RestoreBackupAsync()
    {
        if (IsBusy) return;

        string backupPath = await Shell.Current.DisplayPromptAsync(
            "Restore Database",
            "Enter full path to verified .json backup file:",
            "Proceed Restore",
            "Cancel",
            LastBackupFile);

        if (string.IsNullOrWhiteSpace(backupPath)) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Warning: Overwrite Existing Data",
            "Restoring this backup will replace current database tables with data from the backup file.\n\nAre you sure you want to proceed?",
            "Yes, Restore",
            "Cancel");

        if (!confirm) return;

        IsBusy = true;
        StatusMessage = "Validating checksum and restoring tables...";

        try
        {
            bool success = await _backupService.RestoreBackupAsync(backupPath.Trim());
            if (success)
            {
                StatusMessage = "✓ Database restored successfully and verified!";
                await Shell.Current.DisplayAlert("Restoration Complete", "Database was restored successfully from the backup archive.", "OK");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Restoration failed: {ex.Message}";
            await Shell.Current.DisplayAlert("Restoration Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
