using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;

namespace KSMP.Extension;

public static class PageExtension
{
    public static AppWindow GetAppWindow(this Window window)
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        return appWindow;
    }

    public static async Task SetImageClipboardFromUrl(this Page page, string url, string message = "이미지가 클립보드에 저장되었습니다.")
    {
        var dataPackage = new DataPackage();
        dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromUri(new Uri(url)));
        Clipboard.SetContent(dataPackage);
        if (!string.IsNullOrEmpty(message))
            await Utility.ShowMessageDialogAsync(message, "안내");
    }

    public static async Task SetTextClipboard(this Page page, string text, string message = "복사되었습니다.")
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
        if (!string.IsNullOrEmpty(message))
            await Utility.ShowMessageDialogAsync(message, "안내");
    }

    public static async Task RunOnMainThreadAsync(this Window window, Func<Task> method)
    {
        var taskCompletionSource = new TaskCompletionSource();
        window.DispatcherQueue.TryEnqueue(async () =>
        {
            await method();
            taskCompletionSource.SetResult();
        });
        await taskCompletionSource.Task;
    }

	private static ElementTheme GetCurrentTheme()
	{
		ElementTheme elementTheme;
		var theme = Application.Current.RequestedTheme;
		if (theme == ApplicationTheme.Light)
			elementTheme = ElementTheme.Light;
		else
			elementTheme = ElementTheme.Dark;
		return elementTheme;
	}
}
