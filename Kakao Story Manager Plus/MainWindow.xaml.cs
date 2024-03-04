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
using System.IO;
using System.Threading.Tasks;
using static KSMP.ClassManager;
using static KSMP.ApiHandler;
using Microsoft.UI;
using WinRT.Interop;
using System.Linq;
using Microsoft.UI.Xaml.Controls.Primitives;
using KSMP.Utils;
using Newtonsoft.Json;
using System.Net;

namespace KSMP;

public sealed partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; }

    private string _versionString = string.Empty;
    private bool _isFirst = true;
	private string _restartFlagPath;

	public MainWindow()
	{
		InitializeComponent();
		Instance = this;

		_versionString = Common.GetVersionString() ?? "VERSION ERROR";

		WindowHelper.SetupWindowTheme(this);
		SetupAppWindow();

		AppWindow.Changed += AppWindowChanged;
		if (_isFirst)
		{
			_isFirst = false;
			OnReloginRequired += ReloginAsync;
		}

		_restartFlagPath = Path.Combine(App.BinaryDirectory, "restart");
        var restartFlagExists = File.Exists(_restartFlagPath);
		RestartFlag flag = null;

		if (restartFlagExists)
		{
			var restartFlagString = File.ReadAllText(_restartFlagPath);
			flag = JsonConvert.DeserializeObject<RestartFlag>(restartFlagString);
			App.RecordedFirstFeedId = flag.LastFeedId;
            if (flag.ShowTimeline)
            {
                MainPage.IsStarup = false;
                flag.LastArgs = null;
            }

			var cookies = new List<Cookie>();
			var cookieContainer = new CookieContainer();

            if(flag.Cookies == null)
            {
                ApplyFlag(null);
                return;
            }
			foreach (var rawCookie in flag.Cookies)
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

			Init(cookieContainer, cookies, null);
            LoginPage.IsLoggedIn = true;
		}

		ApplyFlag(flag);
	}

	public void SetLoading(bool isLoading, string message = null, int progress = -1)
	{
		if (GdLoading == null) return;
		GdLoading.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
		if (isLoading && message != null)
		{
			FrMain.IsEnabled = false;
			TbLoading.Text = message;
			var showProgress = progress > 0;
			PrLoading.IsIndeterminate = !showProgress;
			if (showProgress) PrLoading.Value = progress;
		}
		else
			FrMain.IsEnabled = true;
	}

	private async void ApplyFlag(RestartFlag flag)
	{
		await Task.Delay(100);
		SetLoading(true, "버전 확인중");
		await Utility.CheckVersion(SetLoading);
		SetLoading(false);

		if (flag == null) FrMain.Navigate(typeof(LoginPage));
		else
		{
            await GetFriends();
			var wasMaximized = flag.WasMaximized;
			if (wasMaximized) (this.GetAppWindow().Presenter as OverlappedPresenter).Maximize();
			FrMain.Navigate(typeof(MainPage), flag.LastArgs);
		}
	}

	public static async Task<bool> ReloginAsync()
    {
        bool success = false;
        await Utility.RunOnMainThreadAsync(async () =>
        {
            if (Instance != null) Instance.FrMain.IsEnabled = false;
            try
			{
				var email = Configuration.GetValue("email") as string;
				var password = Configuration.GetValue("password") as string;
				success = LoginManager.LoginWithSelenium(email, password);
				if (!success) await ShowReloginErrorMessageAsync();
			}
			catch (Exception) { await ShowReloginErrorMessageAsync(); }
			finally { if (Instance != null) Instance.FrMain.IsEnabled = true; }
		});
        return success;
    }

	private static async Task<ContentDialogResult> ShowReloginErrorMessageAsync() =>
        await Utility.ShowMessageDialogAsync("재로그인 도중 문제가 발생하였습니다.", "오류");

	public static void Navigate(Type type) => Instance.FrMain.Navigate(type);

    private void SetupAppWindow()
    {
        TitleTextBlock.Text = $"카카오스토리 매니저 PLUS {_versionString}";
		AppWindow.Title = "카카오스토리 매니저 PLUS";
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppTitleBar.Loaded += AppTitleBarLoaded;
        AppTitleBar.SizeChanged += AppTitleBarSizeChanged;

        AppWindow.SetIcon(Path.Combine(App.BinaryDirectory, "icon.ico"));
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
            SetDragRegionForCustomTitleBar(AppWindow);
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

    private void SetDragRegionForCustomTitleBar(AppWindow AppWindow)
    {
        // Check to see if customization is supported.
        // Currently only supported on Windows 11.
        if (AppWindowTitleBar.IsCustomizationSupported()
            && AppWindow.TitleBar.ExtendsContentIntoTitleBar)
        {
            double scaleAdjustment = GetScaleAdjustment();

            if (AppWindow.TitleBar.RightInset < 0 || AppWindow.TitleBar.LeftInset < 0) return;

            RightPaddingColumn.Width = new GridLength(AppWindow.TitleBar.RightInset / scaleAdjustment);
            LeftPaddingColumn.Width = new GridLength(AppWindow.TitleBar.LeftInset / scaleAdjustment);

            List<Windows.Graphics.RectInt32> dragRectsList = new();

            Windows.Graphics.RectInt32 dragRectL;
            dragRectL.X = (int)((LeftPaddingColumn.ActualWidth + IconColumn.ActualWidth) * scaleAdjustment);
            dragRectL.Y = 0;
            dragRectL.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
            dragRectL.Width = (int)((TitleColumn.ActualWidth + LeftDragColumn.ActualWidth) * scaleAdjustment);
            dragRectsList.Add(dragRectL);

            Windows.Graphics.RectInt32[] dragRects = dragRectsList.ToArray();

            AppWindow.TitleBar.SetDragRectangles(dragRects);
        }
    }

    private void AppTitleBarSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (AppWindowTitleBar.IsCustomizationSupported()
            && AppWindow.TitleBar.ExtendsContentIntoTitleBar)
        {
            // Update drag region if the size of the title bar changes.
            SetDragRegionForCustomTitleBar(AppWindow);
        }
    }

    private void WindowClosed(object sender, WindowEventArgs args)
    {
        if (!LoginPage.IsLoggedIn)
        {
            args.Handled = true;
            return;
        }

        App.MainWindow = null;
        Utility.SaveCurrentState();
        Instance = null;
        MainPage.Instance = null;

		AppWindow.Changed -= AppWindowChanged;

		AppTitleBar.Loaded -= AppTitleBarLoaded;
		AppTitleBar.SizeChanged -= AppTitleBarSizeChanged;
	}

    private void OnPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var isControlDown = Common.IsModifierDown();

        //if (e.Key == Windows.System.VirtualKey.Escape)
        //    Close();
        //else if (isControlDown && e.Key == Windows.System.VirtualKey.W)
        //    Close();
        if (isControlDown && e.Key == Windows.System.VirtualKey.Q) Utility.SaveCurrentStateAndRestartProgram();
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
            var newFriends = MainPage.Friends.profiles.Where(x => x.display_name.ToLower().Contains(text) || x.id.ToLower().Contains(text)).Select(x => x.GetFriendData()).ToList();
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
        var result = await Utility.ShowMessageDialogAsync("정말로 프로그램을 종료하시겠습니까?", "경고", true);
        if (result == ContentDialogResult.Primary)
        {
			Utility.SaveCurrentState();
			Environment.Exit(0);
        }
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

    private void OnWritePostButtonClicked(object sender, RoutedEventArgs e) => new WritePostWindow().Activate();

	private async void OnLogoutButtonClicked(object sender, RoutedEventArgs e)
    {
        var result = await Utility.ShowMessageDialogAsync("정말로 로그아웃 하시겠습니까?", "경고", true);
        if (result == ContentDialogResult.Primary)
        {
            Configuration.SetValue("willRememberCredentials", false);
            if (File.Exists(_restartFlagPath)) File.Delete(_restartFlagPath);
            await Utility.ShowMessageDialogAsync("로그아웃되었습니다.\n프로그램을 재실행해주세요.", "안내");
			Environment.Exit(0);
		}
    }

	private void OnPointerEntered(object sender, PointerRoutedEventArgs e) => Utility.ChangeSystemMouseCursor(true);
    private void OnPointerExited(object sender, PointerRoutedEventArgs e) => Utility.ChangeSystemMouseCursor(false);

    private void OnProgramIconClicked(object sender, RoutedEventArgs e)
    {
        MainPage.HideOverlay();
        MainPage.HideOverlay();
        MainPage.NavigateTimeline();
    }

	private void OnTrayIconDoubleClicked(XamlUICommand sender, ExecuteRequestedEventArgs args)
	{
        App.DispatcherQueue.TryEnqueue(() =>
        {
            if (Instance == null) Utility.RestartProgram();
            var window = Instance ?? new MainWindow();
			window.Activate();
			this.Show();

			window.AppWindow.Show();

			var presenter = window.AppWindow.Presenter as OverlappedPresenter;
			if (presenter.State == OverlappedPresenterState.Minimized) presenter.Restore();
			presenter.IsAlwaysOnTop = true;
			presenter.IsAlwaysOnTop = false;
		});
	}

	private void OnTrayIconShowMyProfileExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args) => MainPage.ShowMyProfile();
	private void OnTrayIconShowTimelineExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args) => MainPage.ShowTimeline();

	private void OnTrayIconExitProgramExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
	{
        Utility.SaveCurrentState();
		Environment.Exit(0);
	}

	public static void EnableLoginRequiredMenuFlyoutItems()
	{
		Instance.MfiShowTimeline.IsEnabled = true;
		Instance.MfiShowMyProfile.IsEnabled = true;
		Instance.MfiWritePost.IsEnabled = true;
		Instance.MfiShowNotifications.IsEnabled = true;
	}

	public static void DisableLoginRequiredMenuFlyoutItems()
	{
        Instance.MfiShowTimeline.IsEnabled = false;
        Instance.MfiShowMyProfile.IsEnabled = false;
        Instance.MfiWritePost.IsEnabled = false;
        Instance.MfiShowNotifications.IsEnabled = false;
	}

	private void OnTrayIconWritePostExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
	{
        var window = new WritePostWindow();
        window.Activate();
	}

	private void OnTrayIconShowNotificationsRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
	{
        var window = new NotificationsWindow();
        window.Activate();
	}
}