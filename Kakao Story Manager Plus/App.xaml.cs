using H.NotifyIcon;
using KSMP.Controls;
using KSMP.Extension;
using KSMP.Pages;
using KSMP.Utils;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Audio;
using Windows.Security.Isolation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KSMP;

public partial class App : Application
{
    public static DispatcherQueue DispatcherQueue { get; private set; }
    public static string BinaryDirectory = Path.GetDirectoryName(Process.GetCurrentProcess()?.MainModule?.FileName ?? "");

    public App()
    {
        UnhandledException += OnApplicationUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;

        if (CheckForExistingProcess()) Environment.Exit(0);
        InitializeComponent();
        ToastNotificationManagerCompat.OnActivated += OnToastNotificationActivated;
    }

    private static bool CheckForExistingProcess()
    {
        var process = Process.GetCurrentProcess();
        var processes = Process.GetProcesses();
        return processes.Any(x => x.ProcessName == process.ProcessName && x.Id != process.Id);
    }

    private async void OnTaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteException(e.Exception);
        e.SetObserved();
        await ShowErrorMessage(e.Exception);
    }

    private async void OnApplicationUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        WriteException(e.Exception);
        e.Handled = true;
        await ShowErrorMessage(e.Exception);
    }

    private async void OnAppDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        WriteException(e.ExceptionObject as Exception);
        await ShowErrorMessage(e.ExceptionObject as Exception);
    }

    private void WriteException(Exception exception)
    {
        var path = Path.Combine(BinaryDirectory, "error.log");
        var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {exception?.Message ?? "ERRMSG"}: {exception?.StackTrace ?? "ERRST"}\n\n";
        File.AppendAllText(path, text);
    }

    private async Task ShowErrorMessage(Exception exception)
    {
        try
        {
            await MainPage.GetInstance().ShowMessageDialogAsync($"{exception.Message}/{exception.StackTrace}", "런타임 오류");
        }
        catch (Exception) { } //Ignore

    }

    private bool _toastActivateFlag = true;
    private void OnToastNotificationActivated(ToastNotificationActivatedEventArgsCompat toastArgs)
    {
        var dispatcherQueue = m_window?.DispatcherQueue ?? DispatcherQueue;
        dispatcherQueue.TryEnqueue(async () =>
        {
            var wasToastActivated = ToastNotificationManagerCompat.WasCurrentProcessToastActivated() && _toastActivateFlag;
            _toastActivateFlag = false;
            if (!LoginPage.IsLoggedIn && !wasToastActivated) return;
            ToastArguments args = ToastArguments.Parse(toastArgs.Argument);
            var keys = args.Select(x => x.Key).ToList();
            if (keys.Count == 0) return;
            var action = keys[0];

            var activityId = keys.Where(x => x.StartsWith("Activity=")).SingleOrDefault();
            activityId = activityId?.Replace("Activity=", "");

            var profileId = keys.Where(x => x.StartsWith("Profile=")).SingleOrDefault();
            profileId = profileId?.Replace("Profile=", "");

            var commentId = keys.Where(x => x.StartsWith("Comment=")).SingleOrDefault();
            commentId = commentId?.Replace("Comment=", "");

            try
            {
                if (action == "Open")
                {
                    if (profileId != null)
                        MainPage.ShowProfile(profileId);
                    else if (activityId != null)
                    {
                        var post = await StoryApi.ApiHandler.GetPost(activityId);
                        MainPage.ShowOverlay(new TimelineControl(post, false, true));
                    }
                }
                else if (action == "Like") await StoryApi.ApiHandler.LikeComment(activityId, commentId, false);
            }
            catch (Exception)
            {
                LoginPage.OnLoginSuccess += () => OnToastNotificationActivated(toastArgs);
            }
            finally
            {
                LaunchAndBringToForegroundIfNeeded();
            }
        });
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // Get the app-level dispatcher
        DispatcherQueue = DispatcherQueue.GetForCurrentThread();

        // Register for toast activation. Requires Microsoft.Toolkit.Uwp.Notifications NuGet package version 7.0 or greater
        ToastNotificationManagerCompat.OnActivated += OnToastNotificationActivated; ;

        // If we weren't launched by a toast, launch our window like normal.
        // Otherwise if launched by a toast, our OnActivated callback will be triggered
        if (!ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
        {
            LaunchAndBringToForegroundIfNeeded();
        }
    }

    private void LaunchAndBringToForegroundIfNeeded()
    {
        if (m_window == null)
        {
            m_window = new MainWindow();
            m_window.Activate();

            // Additionally we show using our helper, since if activated via a toast, it doesn't
            // activate the window correctly
            WindowHelper.ShowWindow(m_window);
        }
        else
        {
            WindowHelper.ShowWindow(m_window);
        }
    }

    private Window m_window;
}
