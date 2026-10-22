namespace AccountManagement.App;

public partial class App : Microsoft.Maui.Controls.Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new AppShell();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = base.CreateWindow(activationState);
		window.Title = "Rupali Account Management System (রূপালী প্রেস অ্যান্ড প্যাকেজিং)";
		window.Width = 1280;
		window.Height = 850;
		window.MinimumWidth = 1024;
		window.MinimumHeight = 700;
		return window;
	}
}
