using H.NotifyIcon;
using KSMP.Controls;
using KSMP.Extension;
using KSMP.Pages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using static KSMP.ClassManager;
using static StoryApi.ApiHandler;

namespace KSMP;

public sealed partial class MainWindow : Microsoft.UI.Xaml.Window
{
    public static MainWindow Instance { get; private set; }
    public static TaskCompletionSource ReloginTaskCompletionSource = null;
    private static List<MenuFlyoutItem> s_loginRequiredMenuFlyoutItems = new();
    private bool _shouldClose { get; set; } = false;
    private string _lastArgs { get; set; } = null;

    public MainWindow(RestartFlag flag = null)
    {
        Instance = this;
        InitializeComponent();
        SetupAppWindow();
        SetupTrayIcon();
        if (flag == null) FrMain.Navigate(typeof(LoginPage));
        else
        {
            var wasMaximized = flag.WasMaximized;
            if (wasMaximized) (this.GetAppWindow().Presenter as OverlappedPresenter).Maximize();
            FrMain.Navigate(typeof(MainPage), flag.LastArgs);

            var postId = flag.PostId;
            if (!string.IsNullOrEmpty(postId)) ShowPost(postId);
        }

        OnReloginRequired += OnReloginRequiredHandler;
        Closed += (s, e) =>
        {
            LoginManager.SeleniumDriver?.Close();
            LoginManager.SeleniumDriver?.Dispose();
            LoginManager.SeleniumDriver = null;
        };
    }

    private static async void ShowPost(string postId)
    {
        MainPage.Me ??= await GetProfileData();
        var post = await GetPost(postId);
        MainPage.ShowOverlay(new TimelineControl(post, false, true));
    }

    public void SetClosable(bool shouldClose = true) => _shouldClose = shouldClose;

    private async Task<bool> OnReloginRequiredHandler()
    {
        FrMain.IsEnabled = false;
        var email = Utils.Configuration.GetValue("email") as string;
        var password = Utils.Configuration.GetValue("password") as string;
        var success = LoginManager.LoginWithSelenium(email, password);
        if (!success) await FrMain.ShowMessageDialogAsync("재로그인 도중 문제가 발생하였습니다.", "오류");
        FrMain.IsEnabled = true;
        return success;
    }

    public static void Navigate(Type type) => Instance.FrMain.Navigate(type);
    public static void Navigate(Type type, object args) => Instance.FrMain.Navigate(type, args);

    public static void EnableLoginRequiredMenuFlyoutItems() => s_loginRequiredMenuFlyoutItems.ForEach(x => x.IsEnabled = true);
    public static void DisableLoginRequiredMenuFlyoutItems() => s_loginRequiredMenuFlyoutItems.ForEach(x => x.IsEnabled = false);

    private void SetupAppWindow()
    {
        var appWindow = this.GetAppWindow();
        var versionString = Utils.Common.GetVersionString() ?? "VERSION ERROR";
        appWindow.Title = $"카카오스토리 매니저 PLUS {versionString}";
        appWindow.SetIcon(Path.Combine(App.BinaryDirectory, "icon.ico"));
    }


    private void SetupTrayIcon()
    {
        s_loginRequiredMenuFlyoutItems.Clear();

        MenuFlyout menuFlyout = new();

        MenuFlyoutItem showTimeline = new() { Text="타임라인" };
        var showTimelineCommand = new XamlUICommand();
        showTimelineCommand.ExecuteRequested += (s, e) => MainPage.ShowTimeline();
        showTimeline.Command = showTimelineCommand;
        s_loginRequiredMenuFlyoutItems.Add(showTimeline);

        MenuFlyoutItem showMyProfile = new() { Text="내 프로필" };
        var showMyProfileCommand = new XamlUICommand();
        showMyProfileCommand.ExecuteRequested += (s, e) => MainPage.ShowMyProfile();
        showMyProfile.Command = showMyProfileCommand;
        s_loginRequiredMenuFlyoutItems.Add(showMyProfile);

        MenuFlyoutItem quit = new() { Text="프로그램 종료" };
        var quitCommand = new XamlUICommand();
        quitCommand.ExecuteRequested += (s, e) => Environment.Exit(0);
        quit.Command = quitCommand;


        menuFlyout.Items.Add(showTimeline);
        menuFlyout.Items.Add(showMyProfile);
        menuFlyout.Items.Add(new MenuFlyoutSeparator());
        menuFlyout.Items.Add(quit);
        TiMain.ContextFlyout = menuFlyout;
        TiMain.MenuActivation = H.NotifyIcon.Core.PopupActivationMode.LeftOrRightClick;
        var icon = new Icon(Path.Combine(App.BinaryDirectory, "icon.ico"));
        TiMain.Icon = icon;
        TiMain.ContextMenuMode = ContextMenuMode.PopupMenu;
    }

    public static TaskbarIcon TaskbarIcon() => Instance.TiMain;

    private void WindowClosed(object sender, WindowEventArgs args)
    {
        if (_shouldClose)
        {
            OnReloginRequired -= OnReloginRequiredHandler;
            return;
        }

        args.Handled = true;
        var appWindow = this.GetAppWindow();
        appWindow.Hide();
    }

    private void TaskbarIconTrayMouseDoubleTapped(object sender, RoutedEventArgs e)
    {
        Activate();
        this.Show();

        var appWindow = this.GetAppWindow();
        appWindow.Show();

        var presenter = appWindow.Presenter as OverlappedPresenter;
        if (presenter.State == OverlappedPresenterState.Minimized) presenter.Restore();
        presenter.IsAlwaysOnTop = true;
        presenter.IsAlwaysOnTop = false;
    }

    private async void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var isControlDown = Utils.Common.IsModifierDown();

        if (e.Key == Windows.System.VirtualKey.Escape)
            MainPage.HideOverlay();
        else if ((isControlDown && e.Key == Windows.System.VirtualKey.R) || e.Key == Windows.System.VirtualKey.F5)
            await (MainPage.GetOverlayTimeLineControl()?.RefreshContent(true) ?? Task.CompletedTask);
        else if (isControlDown && e.Key == Windows.System.VirtualKey.S)
            await (MainPage.GetOverlayTimeLineControl()?.SharePost() ?? Task.CompletedTask);
        else if (isControlDown && e.Key == Windows.System.VirtualKey.E)
            await (MainPage.GetOverlayTimeLineControl()?.EditPost() ?? Task.CompletedTask);
        else if (isControlDown && e.Key == Windows.System.VirtualKey.D)
            await (MainPage.GetOverlayTimeLineControl()?.DeletePost() ?? Task.CompletedTask);
        else if (isControlDown && e.Key == Windows.System.VirtualKey.W)
            Utility.DisposeAllMedias();
        else if (isControlDown && e.Key == Windows.System.VirtualKey.Q) Restart();
    }
}
