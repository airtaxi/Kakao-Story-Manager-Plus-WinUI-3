using H.NotifyIcon;
using KSMP.Extension;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;

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
        var appWindow = window.GetAppWindow();
        var presenter = appWindow.Presenter as OverlappedPresenter;
        var previousState = presenter.State;

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        ShowWindow(hwnd, 0x00000009);
        SetForegroundWindow(hwnd);

        MainWindow.Instance.Show();
        appWindow.Show();
        if (presenter.State == OverlappedPresenterState.Minimized) presenter.Restore();
        presenter.IsAlwaysOnTop = true;
        presenter.IsAlwaysOnTop = false;

        if (previousState == OverlappedPresenterState.Maximized) presenter.Maximize();
    }

}
