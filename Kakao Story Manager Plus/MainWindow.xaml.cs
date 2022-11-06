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
using static KSMP.ApiHandler;
using Microsoft.UI;
using WinRT.Interop;
using Windows.ApplicationModel.Appointments;

namespace KSMP;

public sealed partial class MainWindow : Microsoft.UI.Xaml.Window
{
    public static MainWindow Instance { get; private set; }
    public static TaskCompletionSource ReloginTaskCompletionSource = null;
    private static List<MenuFlyoutItem> s_loginRequiredMenuFlyoutItems = new();
    private bool _shouldClose { get; set; } = false;
    private AppWindow appWindow;

    public MainWindow(RestartFlag flag = null)
    {
        Instance = this;
        InitializeComponent();

        appWindow = this.GetAppWindow();
        appWindow.Changed += AppWindowChanged;

        (Content as FrameworkElement).ActualThemeChanged += OnThemeChanged;

        SetupTheme();
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

    private void OnThemeChanged(FrameworkElement sender, object args) => SetupTheme();

    private void SetupTheme()
    {
        FrameworkElement root = Content as FrameworkElement;
        var themeSetting = Utils.Configuration.GetValue("ThemeSetting") as string ?? "Default";
        if (themeSetting == "Light")
        {
            root.RequestedTheme = ElementTheme.Light;
            appWindow.TitleBar.ButtonBackgroundColor = Colors.White;
        }
        else if (themeSetting == "Dark")
        {
            root.RequestedTheme = ElementTheme.Dark;
            appWindow.TitleBar.ButtonBackgroundColor = Colors.Black;
        }
        else appWindow.TitleBar.ButtonBackgroundColor = Utility.IsSystemUsesLightTheme ? Colors.White : Colors.Black;
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
        appWindow.Title = $"카카오스토리 매니저 PLUS";
        appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppTitleBar.Loaded += AppTitleBarLoaded;
        AppTitleBar.SizeChanged += AppTitleBarSizeChanged;

        var versionString = Utils.Common.GetVersionString() ?? "VERSION ERROR";
        TitleTextBlock.Text = $"카카오스토리 매니저 PLUS {versionString}";
        appWindow.SetIcon(Path.Combine(App.BinaryDirectory, "icon.ico"));
    }

    private void AppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidPresenterChange && AppWindowTitleBar.IsCustomizationSupported())
        {
            switch (sender.Presenter.Kind)
            {
                case AppWindowPresenterKind.CompactOverlay:
                    // Compact overlay - hide custom title bar
                    // and use the default system title bar instead.
                    AppTitleBar.Visibility = Visibility.Collapsed;
                    sender.TitleBar.ResetToDefault();
                    break;

                case AppWindowPresenterKind.FullScreen:
                    // Full screen - hide the custom title bar
                    // and the default system title bar.
                    AppTitleBar.Visibility = Visibility.Collapsed;
                    sender.TitleBar.ExtendsContentIntoTitleBar = true;
                    break;

                case AppWindowPresenterKind.Overlapped:
                    // Normal - hide the system title bar
                    // and use the custom title bar instead.
                    AppTitleBar.Visibility = Visibility.Visible;
                    sender.TitleBar.ExtendsContentIntoTitleBar = true;
                    SetDragRegionForCustomTitleBar(sender);
                    break;

                default:
                    // Use the default system title bar.
                    sender.TitleBar.ResetToDefault();
                    break;
            }
        }
    }

    private void AppTitleBarLoaded(object sender, RoutedEventArgs e)
    {
        if (AppWindowTitleBar.IsCustomizationSupported())
            SetDragRegionForCustomTitleBar(appWindow);
    }

    public enum Monitor_DPI_Type : int
    {
        MDT_Effective_DPI = 0,
        MDT_Angular_DPI = 1,
        MDT_Raw_DPI = 2,
        MDT_Default = MDT_Effective_DPI
    }

    private double GetScaleAdjustment()
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(this);
        WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
        DisplayArea displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
        IntPtr hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

        // Get DPI.
        int result = Utility.GetDpiForMonitor(hMonitor, Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint _);
        if (result != 0)
            throw new Exception("Could not get DPI for monitor.");

        uint scaleFactorPercent = (uint)(((long)dpiX * 100 + (96 >> 1)) / 96);
        return scaleFactorPercent / 100.0;
    }

    private void SetDragRegionForCustomTitleBar(AppWindow appWindow)
    {
        // Check to see if customization is supported.
        // Currently only supported on Windows 11.
        if (AppWindowTitleBar.IsCustomizationSupported()
            && appWindow.TitleBar.ExtendsContentIntoTitleBar)
        {
            double scaleAdjustment = GetScaleAdjustment();

            RightPaddingColumn.Width = new GridLength(appWindow.TitleBar.RightInset / scaleAdjustment);
            LeftPaddingColumn.Width = new GridLength(appWindow.TitleBar.LeftInset / scaleAdjustment);

            List<Windows.Graphics.RectInt32> dragRectsList = new();

            Windows.Graphics.RectInt32 dragRectL;
            dragRectL.X = (int)((LeftPaddingColumn.ActualWidth) * scaleAdjustment);
            dragRectL.Y = 0;
            dragRectL.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
            dragRectL.Width = (int)((IconColumn.ActualWidth
                                    + TitleColumn.ActualWidth
                                    + LeftDragColumn.ActualWidth) * scaleAdjustment);
            dragRectsList.Add(dragRectL);

            Windows.Graphics.RectInt32 dragRectR;
            dragRectR.X = (int)((LeftPaddingColumn.ActualWidth
                                + IconColumn.ActualWidth
                                + TitleTextBlock.ActualWidth
                                + LeftDragColumn.ActualWidth
                                + SearchColumn.ActualWidth) * scaleAdjustment);
            dragRectR.Y = 0;
            dragRectR.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
            dragRectR.Width = (int)(RightDragColumn.ActualWidth * scaleAdjustment);
            dragRectsList.Add(dragRectR);

            Windows.Graphics.RectInt32[] dragRects = dragRectsList.ToArray();

            appWindow.TitleBar.SetDragRectangles(dragRects);
        }
    }

    private void AppTitleBarSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (AppWindowTitleBar.IsCustomizationSupported()
            && appWindow.TitleBar.ExtendsContentIntoTitleBar)
        {
            // Update drag region if the size of the title bar changes.
            SetDragRegionForCustomTitleBar(appWindow);
        }
    }

    private void SetupTrayIcon()
    {
        s_loginRequiredMenuFlyoutItems.Clear();

        MenuFlyout menuFlyout = new();

        MenuFlyoutItem showTimeline = new() { Text = "타임라인" };
        var showTimelineCommand = new XamlUICommand();
        showTimelineCommand.ExecuteRequested += (s, e) => MainPage.ShowTimeline();
        showTimeline.Command = showTimelineCommand;
        s_loginRequiredMenuFlyoutItems.Add(showTimeline);

        MenuFlyoutItem showMyProfile = new() { Text = "내 프로필" };
        var showMyProfileCommand = new XamlUICommand();
        showMyProfileCommand.ExecuteRequested += (s, e) => MainPage.ShowMyProfile();
        showMyProfile.Command = showMyProfileCommand;
        s_loginRequiredMenuFlyoutItems.Add(showMyProfile);

        MenuFlyoutItem quit = new() { Text = "프로그램 종료" };
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
            Utility.ManuallyDisposeAllMedias();
        else if (isControlDown && e.Key == Windows.System.VirtualKey.Q) Utility.SaveCurrentStateAndRestartProgram();
    }
}
