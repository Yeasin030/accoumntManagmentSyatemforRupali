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

#if WINDOWS
		window.Created += (s, e) =>
		{
			var nativeWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
			if (nativeWindow != null)
			{
				var handle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
				var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
				var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
				var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appicon.ico");
				if (System.IO.File.Exists(iconPath))
				{
					appWindow.SetIcon(iconPath);
				}
			}
		};
#endif

		return window;
	}
}
