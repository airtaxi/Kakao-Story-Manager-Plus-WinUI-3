using H.NotifyIcon;
using KSMP.Controls;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace KSMP.Utils;

public class WindowHelper
{
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public static void ShowWindow(Window window)
    {
        var appWindow = window.AppWindow;
        var presenter = appWindow.Presenter as OverlappedPresenter;
        var previousState = presenter.State;

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        ShowWindow(hwnd, 0x00000009);
        SetForegroundWindow(hwnd);

        window.Show();
        appWindow.Show();
        if (presenter.State == OverlappedPresenterState.Minimized) presenter.Restore();
        presenter.IsAlwaysOnTop = true;
        presenter.IsAlwaysOnTop = false;

        if (previousState == OverlappedPresenterState.Maximized) presenter.Maximize();
    }

	public static void SetupWindowTheme(Window window)
	{
		FrameworkElement root = window.Content as FrameworkElement;
		var themeSetting = Configuration.GetValue("ThemeSetting") as string ?? "Default";

		Windows.UI.Color white;
		if (themeSetting == "Light")
		{
			root.RequestedTheme = ElementTheme.Light;
			white = Colors.White;
		}
		else if (themeSetting == "Dark")
		{
			root.RequestedTheme = ElementTheme.Dark;
			white = Windows.UI.Color.FromArgb(255, 52, 52, 52);
		}
		else white = Utility.IsSystemUsesLightTheme ? Colors.White : Windows.UI.Color.FromArgb(255, 52, 52, 52);

		window.AppWindow.TitleBar.ButtonBackgroundColor = white;
		window.AppWindow.TitleBar.ButtonInactiveBackgroundColor = white;
		window.AppWindow.TitleBar.BackgroundColor = white;

		TypedEventHandler<FrameworkElement, object> themeChanged = null;
		themeChanged = (s,e) => SetupWindowTheme(window);

		RoutedEventHandler unloaded = null;
		unloaded = (s, e) =>
		{
			root.ActualThemeChanged -= themeChanged;
			root.Unloaded -= unloaded;
		};

		root.ActualThemeChanged += themeChanged;
		root.Unloaded += unloaded;
	}
}
