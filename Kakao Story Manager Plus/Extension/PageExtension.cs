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
    public static AppWindow GetAppWindow(this Microsoft.UI.Xaml.Window window)
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
            await page.GenerateMessageDialog(message, "안내").ShowAsync();
    }

    public static async Task SetTextClipboard(this Page page, string text, string message = "복사되었습니다.")
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
        if (!string.IsNullOrEmpty(message))
            await page.GenerateMessageDialog(message, "안내").ShowAsync();
    }

    public static async Task RunOnMainThreadAsync(this Page page, Action method)
    {
        var taskCompletionSource = new TaskCompletionSource();
        page.DispatcherQueue.TryEnqueue(() =>
        {
            method();
            taskCompletionSource.SetResult();
        });
        await taskCompletionSource.Task;
    }

    public static async Task RunOnMainThreadAsync(this Page page, Func<Task> method)
    {
        var taskCompletionSource = new TaskCompletionSource();
        page.DispatcherQueue.TryEnqueue(async () =>
        {
            await method();
            taskCompletionSource.SetResult();
        });
        await taskCompletionSource.Task;
    }

    public static async Task ShowMessageDialogAsync(this UIElement page, string description, string title, bool showCancel = false)
    {
        var dialog = GenerateMessageDialog(page, description, title, showCancel);
        await dialog.ShowAsync();
    }

    public static ContentDialog GenerateMessageDialog(this UIElement page, string description, string title, bool showCancel = false)
    {
        ContentDialog dialog = new()
        {
            Title = title,
            Content = description,

            PrimaryButtonText = "확인"
        };
        if (showCancel)
            dialog.SecondaryButtonText = "취소";

        dialog.XamlRoot = page.XamlRoot;
        return dialog;
    }
}
