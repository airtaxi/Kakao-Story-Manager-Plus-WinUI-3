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
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel.DataTransfer;
using KSMP.Utils;

namespace KSMP;

public sealed partial class MainWindow : Microsoft.UI.Xaml.Window
{
    public static MainWindow Instance { get; private set; }

    public static bool IsWritePostFlyoutOpened = false;

    private static List<MenuFlyoutItem> s_loginRequiredMenuFlyoutItems = new();
    private static Process _process;

    private bool _shouldClose { get; set; } = false;
    private string _versionString = string.Empty;
    private AppWindow appWindow;


    public MainWindow(RestartFlag flag = null)
    {
        Instance = this;
        InitializeComponent();
        _versionString = Common.GetVersionString() ?? "VERSION ERROR";
        InitializeWritePostFlyout();

        appWindow = this.GetAppWindow();
        appWindow.Changed += AppWindowChanged;

        (Content as FrameworkElement).ActualThemeChanged += OnThemeChanged;

        SetupTitleBarMemoryWatcherTimer();
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

    private void SetupTitleBarMemoryWatcherTimer()
    {
        _process ??= Process.GetCurrentProcess();
        DispatcherTimer memoryWatcherTimer = new()
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        memoryWatcherTimer.Tick += OnMemoryWatcherTimerTick;
        memoryWatcherTimer.Start();
        UpdateMemoryUsageTextBlock();
    }

    private void OnMemoryWatcherTimerTick(object sender, object e) => UpdateMemoryUsageTextBlock();

    private bool _isMemoryWarningDialogShown = false;
    private int _warnCount = 0;
    private async void UpdateMemoryUsageTextBlock()
    {
        _process.Refresh();
        var memoryUsageInBytes = _process.PrivateMemorySize64;
        var memoryUsageInMegabytes = memoryUsageInBytes / 1024 / 1024;
        TitleTextBlock.Text = $"카카오스토리 매니저 PLUS {_versionString} ({memoryUsageInMegabytes:N0}MiB)";
        if (memoryUsageInMegabytes < 1024 || _isMemoryWarningDialogShown)
        {
            _warnCount = 0;
            return;
        }
        _warnCount++;
        if (_warnCount <= 3) return;

        bool willWarnOnHighMemoryUsage = (Utils.Configuration.GetValue("WarnOnHighMemoryUsage") as bool?) ?? true;
        if (!willWarnOnHighMemoryUsage) return;
        _isMemoryWarningDialogShown = true;
        var result = await MainPage.GetInstance()?.ShowMessageDialogAsync("WinUI 프레임워크 버그로 인하여 누수된 메모리가 다량(1GiB)으로 누적되었습니다.\n이 경우, 시스템 성능을 저해할 수 있습니다.\n프로그램을 재실행하여 메모리를 정리하시겠습니까?\n이 메시지 표시 설정은 프로필 우측 상단 옵션에서 변경할 수 있습니다.", "경고", true);
        if (result == ContentDialogResult.Primary) Utility.SaveCurrentStateAndRestartProgram();
    }

    private void OnThemeChanged(FrameworkElement sender, object args) => SetupTheme();

    private void SetupTheme()
    {
        FrameworkElement root = Content as FrameworkElement;
        var themeSetting = Utils.Configuration.GetValue("ThemeSetting") as string ?? "Default";

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

        appWindow.TitleBar.ButtonBackgroundColor = white;
        appWindow.TitleBar.ButtonInactiveBackgroundColor = white;
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

            if (appWindow.TitleBar.RightInset < 0 || appWindow.TitleBar.LeftInset < 0) return;

            RightPaddingColumn.Width = new GridLength(appWindow.TitleBar.RightInset / scaleAdjustment);
            LeftPaddingColumn.Width = new GridLength(appWindow.TitleBar.LeftInset / scaleAdjustment);

            List<Windows.Graphics.RectInt32> dragRectsList = new();

            Windows.Graphics.RectInt32 dragRectL;
            dragRectL.X = (int)((LeftPaddingColumn.ActualWidth + IconColumn.ActualWidth) * scaleAdjustment);
            dragRectL.Y = 0;
            dragRectL.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
            dragRectL.Width = (int)((TitleColumn.ActualWidth + LeftDragColumn.ActualWidth) * scaleAdjustment);
            dragRectsList.Add(dragRectL);

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

    public static async void ShowMenus()
    {
        Instance.BtProgramIcon.IsEnabled = true;
        Instance.AsbSearchFriend.Visibility = Visibility.Visible;
        Instance.SpButtons.Visibility = Visibility.Visible;
        await Task.Delay(500);
    }

    private static void SearchFriend(AutoSuggestBox sender)
    {
        var text = sender.Text.ToLower();
        if (!string.IsNullOrEmpty(text))
        {
            var newFriends = MainPage.Friends.profiles.Where(x => x.display_name.ToLower().Contains(text) || x.id.ToLower().Contains(text)).Select(x => new FriendProfile { Name = x.display_name, ProfileUrl = x.GetValidUserProfileUrl(), Id = x.id }).ToList();
            sender.ItemsSource = newFriends;
        }
        else
            sender.ItemsSource = null;
    }
    
    private void SearchFriendQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => SearchFriend(sender);
    private void SearchFriendTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) => SearchFriend(sender);

    private void SearchFriendSelected(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        var friend = args.SelectedItem as FriendProfile;
        var id = friend.Id;
        MainPage.NavigateTimeline(id);
        sender.Text = "";
        sender.ItemsSource = null;
    }

    private async void OnExitButtonClicked(object sender, RoutedEventArgs e)
    {
        var dialogResult = await MainPage.GetInstance().ShowMessageDialogAsync("정말로 프로그램을 종료하시겠습니까?", "경고", true);
        if (dialogResult == ContentDialogResult.Primary) Environment.Exit(0);
    }

    private void OnMoreButtonClicked(object sender, RoutedEventArgs e)
    {
        GC.Collect(GC.MaxGeneration);
        GC.WaitForPendingFinalizers();
    }

    private void OnRestartButtonClicked(object sender, RoutedEventArgs e) => Utility.SaveCurrentStateAndRestartProgram();
    private void OnImageUnloadButtonClicked(object sender, RoutedEventArgs e) => Utility.ManuallyDisposeAllMedias();

    private async Task ShowNotification(Button button)
    {
        var flyout = new Flyout
        {
            Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft
        };
        flyout.ShowAt(button);

        var notificationControl = new NotificationControl();
        if (MainPage.LatestNotificationId != NotificationControl.LatestNotificationId)
        {
            flyout.Content  = new ProgressRing { IsIndeterminate = true, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Width = 50, Height = 50 };
            await notificationControl.Refresh();
        }
        flyout.Content = notificationControl;
    }
    private async void OnNotificationsButtonClicked(object sender, RoutedEventArgs e) => await ShowNotification(sender as Button);

    public static Button GetWritePostButton() => Instance.BtWrite;

    private async void OnWritePostButtonClicked(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var flyout = button.Flyout as Flyout;
        var control = flyout.Content as WritePostControl;
        control.AdjustDefaultPostWritingPermission();

        DataPackageView dataPackageView = Clipboard.GetContent();
        var hasImage = dataPackageView.Contains(StandardDataFormats.Bitmap);
        if (hasImage)
        {
            await Task.Delay(400);
            var result = await MainPage.GetInstance().ShowMessageDialogAsync("클립보드에 이미지가 있습니다.\n이미지를 추가할까요?", "안내", true);
            if (result != ContentDialogResult.Primary) return;

            var filePath = await Utility.GenerateClipboardImagePathAsync(dataPackageView);
            await control.AddImageFromPath(filePath);
        }
    }

    private async void OnLogoutButtonClicked(object sender, RoutedEventArgs e)
    {
        var dialogResult = await MainPage.GetInstance().ShowMessageDialogAsync("정말로 로그아웃 하시겠습니까?", "경고", true);
        if (dialogResult == ContentDialogResult.Primary)
        {
            Configuration.SetValue("willRememberCredentials", false);
            await MainPage.GetInstance().ShowMessageDialogAsync("로그아웃되었습니다.\n프로그램을 재실행해주세요.", "안내");
            Environment.Exit(0);
        }
    }


    private async void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsWritePostFlyoutOpened)
        {
            await Task.Delay(250);
            var flyout = BtWrite?.Flyout as Flyout;
            flyout?.ShowAt(BtWrite);
        }
    }

    private void OnWritePostFlyoutClosed(object sender, object e) => IsWritePostFlyoutOpened = false;

    private void OnPostCompleted() => InitializeWritePostFlyout();

    private void InitializeWritePostFlyout()
    {
        IsWritePostFlyoutOpened = false;
        var previousFlyout = BtWrite.Flyout as Flyout;
        if (previousFlyout != null)
        {
            previousFlyout.Opened -= OnWritePostFlyoutOpened;
            previousFlyout.Closed -= OnWritePostFlyoutClosed;
            var previousControl = previousFlyout.Content as WritePostControl;
            if (previousControl != null) previousControl.OnPostCompleted -= OnPostCompleted;
        }

        var flyout = new Flyout();
        flyout.Opened += OnWritePostFlyoutOpened;
        flyout.Closed += OnWritePostFlyoutClosed;
        BtWrite.Flyout = flyout;
        var control = new WritePostControl(BtWrite);
        flyout.Content = control;
        control.OnPostCompleted += OnPostCompleted;
    }

    private void OnWritePostFlyoutOpened(object sender, object e)
    {
        ((BtWrite.Flyout as Flyout).Content as WritePostControl).PreventClose = true;
        IsWritePostFlyoutOpened = true;
    }

    private void OnProgramIconClicked(object sender, RoutedEventArgs e)
    {
        MainPage.HideOverlay();
        MainPage.HideOverlay();
        MainPage.NavigateTimeline();
    }
}
