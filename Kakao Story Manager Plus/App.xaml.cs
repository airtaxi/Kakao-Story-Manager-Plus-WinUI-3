using KSMP.Controls;
using KSMP.Extension;
using KSMP.Pages;
using KSMP.Utils;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static KSMP.ClassManager;

namespace KSMP;

public partial class App : Application
{
    public static DispatcherQueue DispatcherQueue { get; private set; }
    public static string BinaryDirectory = null;
    public static string RecordedFirstFeedId = null;

    private static Window s_window;
    private static RestartFlag s_restartFlag = null;

    public App()
    {
        try
        {
            BinaryDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Current.UnhandledException += OnApplicationUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;
            var restartFlagPath = Path.Combine(BinaryDirectory, "restart");
            var checkProcess = File.Exists(restartFlagPath) == false;

            if (!checkProcess)
            {
                var restartFlagString = File.ReadAllText(restartFlagPath);
                var restartFlag = JsonConvert.DeserializeObject<RestartFlag>(restartFlagString);
                RecordedFirstFeedId = restartFlag.LastFeedId;

                var cookies = new List<Cookie>();
                var cookieContainer = new CookieContainer();

                foreach (var rawCookie in restartFlag.Cookies)
                {
                    var cookie = new Cookie()
                    {
                        Name = rawCookie.Name,
                        Domain = rawCookie.Domain,
                        Path = rawCookie.Path,
                        Value = rawCookie.Value
                    };
                    cookieContainer.Add(cookie);
                    cookies.Add(cookie);
                }

                ApiHandler.Init(cookieContainer, cookies, null);
                s_restartFlag = restartFlag;
                LoginPage.IsLoggedIn = true;
            }
            if (checkProcess && CheckForExistingProcess()) ExitProgramByExistingProcess();
            else InitializeComponent();
            File.Delete(restartFlagPath);
        }
        catch (Exception exception) { _ = HandleException(exception); }
    }

    private void ExitProgramByExistingProcess()
    {
        var builder = new ToastContentBuilder()
        .AddText("안내")
        .AddText("프로그램이 이미 실행중입니다.");
        builder.Show();
        Environment.Exit(0);
    }

    private static bool CheckForExistingProcess()
    {
        var process = Process.GetCurrentProcess();
        var processes = Process.GetProcesses();
        return processes.Any(x => x.ProcessName == process.ProcessName && x.Id != process.Id);
    }


    private async Task HandleException(Exception exception)
    {
        WriteException(exception);
        await ShowErrorMessage(exception);
    }

    private async void OnTaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e) => await HandleException(e.Exception);
    private async void OnAppDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e) => await HandleException(e.ExceptionObject as Exception);
    private async void OnApplicationUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        await HandleException(e.Exception);
    }

    private static bool ExceptionWritten = false;
    private static void WriteException(Exception exception)
    {
        if (!ExceptionWritten && !string.IsNullOrEmpty(BinaryDirectory))
        {
            ExceptionWritten = true;
            var path = Path.Combine(BinaryDirectory, "error.log");
            var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {exception?.Message ?? "ERRMSG"}: {exception?.StackTrace ?? "ERRST"}\n(BDIR: {BinaryDirectory})\n\n";
            File.AppendAllText(path, text);
        }
    }

    private static async Task ShowErrorMessage(Exception exception)
    {
        try
        {
            var message = exception.Message ?? string.Empty;
            if (message.Contains("E_FAIL")) return;
            await MainPage.GetInstance().ShowMessageDialogAsync($"{exception.Message}/{exception.StackTrace}", "런타임 오류");
        }
        catch (Exception) { } //Ignore
    }

    private static bool _toastActivateFlag = true;
    private static void OnToastNotificationActivated(ToastNotificationActivatedEventArgsCompat toastArgs)
    {
        var dispatcherQueue = s_window?.DispatcherQueue ?? DispatcherQueue;
        dispatcherQueue.TryEnqueue(async () =>
        {
            try
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
                if (action == "Open")
                {
                    MainPage.HideOverlay();
                    if (profileId != null)
                        MainPage.ShowProfile(profileId);
                    else if (activityId != null)
                    {
                        var instance = MainPage.GetInstance();

                        var post = await KSMP.ApiHandler.GetPost(activityId);
                        if (post != null) MainPage.ShowOverlay(new TimelineControl(post, false, true));
                        else await instance.ShowMessageDialogAsync("글을 볼 수 없거나 나만 보기로 설정된 글입니다.", "오류");
                    }
                    else if (action == "Like") await KSMP.ApiHandler.LikeComment(activityId, commentId, false);
                }
            }
            catch (Exception) { }
            finally
            {
                LaunchAndBringToForegroundIfNeeded();
            }
        });
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            DispatcherQueue = DispatcherQueue.GetForCurrentThread();

            ToastNotificationManagerCompat.OnActivated += OnToastNotificationActivated;
            if (!ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
                LaunchAndBringToForegroundIfNeeded();
        }
        catch (Exception exception) { await HandleException(exception); }
    }

    private static void LaunchAndBringToForegroundIfNeeded()
    {
        if (s_window == null)
        {
            s_window = new MainWindow(s_restartFlag);
            s_window.Activate();
            WindowHelper.ShowWindow(s_window);
        }
        else WindowHelper.ShowWindow(s_window);
    }
}
