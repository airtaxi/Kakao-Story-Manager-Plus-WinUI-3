using H.NotifyIcon;
using KSMP.Extension;
using KSMP.Pages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using StoryApi;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Windows.Devices.PointOfService.Provider;
using static StoryApi.ApiHandler;

namespace KSMP;

public sealed partial class MainWindow : Microsoft.UI.Xaml.Window
{
    public static MainWindow Instance { get; private set; }
    public static TaskCompletionSource ReloginTaskCompletionSource = null;
    private static List<MenuFlyoutItem> LoginRequiredMenuFlyoutItems = new();
    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
        SetupAppWindow();
        SetupTrayIcon();
        FrMain.Navigate(typeof(LoginPage));
        ApiHandler.OnReloginRequired += OnReloginRequiredHandler;
    }

    private async Task OnReloginRequiredHandler()
    {
        ReloginTaskCompletionSource = new();
        Navigate(typeof(LoginPage));
        await ReloginTaskCompletionSource.Task;
    }

    public static void Navigate(Type type) => Instance.FrMain.Navigate(type);

    public static void EnableLoginRequiredMenuFlyoutItems() => LoginRequiredMenuFlyoutItems.ForEach(x => x.IsEnabled = true);
    public static void DisableLoginRequiredMenuFlyoutItems() => LoginRequiredMenuFlyoutItems.ForEach(x => x.IsEnabled = false);

    private void SetupAppWindow()
    {
        var appWindow = this.GetAppWindow();
        var versionString = Utils.Common.GetVersionString();
        appWindow.Title = $"카카오스토리 매니저 PLUS {versionString}";
        appWindow.SetIcon("icon.ico");
    }


    private void SetupTrayIcon()
    {
        LoginRequiredMenuFlyoutItems.Clear();

        MenuFlyout menuFlyout = new();

        MenuFlyoutItem showTimeline = new() { Text="타임라인" };
        var showTimelineCommand = new XamlUICommand();
        showTimelineCommand.ExecuteRequested += (s, e) => MainPage.ShowTimeline();
        showTimeline.Command = showTimelineCommand;
        LoginRequiredMenuFlyoutItems.Add(showTimeline);

        MenuFlyoutItem showMyProfile = new() { Text="내 프로필" };
        var showMyProfileCommand = new XamlUICommand();
        showMyProfileCommand.ExecuteRequested += (s, e) => MainPage.ShowMyProfile();
        showMyProfile.Command = showMyProfileCommand;
        LoginRequiredMenuFlyoutItems.Add(showMyProfile);

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
        var icon = new Icon("icon.ico");
        TiMain.Icon = icon;
        TiMain.ContextMenuMode = ContextMenuMode.PopupMenu;
    }

    public static TaskbarIcon TaskbarIcon() => Instance.TiMain;

    private void WindowClosed(object sender, WindowEventArgs args)
    {
        args.Handled = true;
        var appWindow = this.GetAppWindow();
        appWindow.Hide();
    }

    private void TaskbarIconTrayMouseDoubleClicked(object sender, RoutedEventArgs e)
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
        else if (isControlDown && e.Key == Windows.System.VirtualKey.R)
            await (MainPage.GetOverlayTimeLineControl()?.RefreshContent(true) ?? Task.CompletedTask);
        else if (isControlDown && e.Key == Windows.System.VirtualKey.S)
            await (MainPage.GetOverlayTimeLineControl()?.SharePost() ?? Task.CompletedTask);
        else if (isControlDown && e.Key == Windows.System.VirtualKey.M)
            await (MainPage.GetOverlayTimeLineControl()?.EditPost() ?? Task.CompletedTask);
        else if (isControlDown && e.Key == Windows.System.VirtualKey.D)
            await (MainPage.GetOverlayTimeLineControl()?.DeletePost() ?? Task.CompletedTask);
    }
}
