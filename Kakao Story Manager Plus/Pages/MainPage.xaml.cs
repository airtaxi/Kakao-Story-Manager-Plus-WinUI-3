using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using KSMP.Controls;
using KSMP.Extension;
using KSMP.Utils;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using static KSMP.ApiHandler.DataType.FriendData;
using Microsoft.UI.Input;
using CommunityToolkit.WinUI.UI;

namespace KSMP.Pages;

public sealed partial class MainPage : Page
{
    public static ApiHandler.DataType.UserProfile.ProfileData Me;
    public static Friends Friends = null;
    public static string LastArgs = null;
    public static string LatestNotificationId = null;

    public static MainPage Instance;
    private static Timer s_notificationTimer = null;

    public MainPage()
    {
        InitializeComponent();
        Instance = this;

        if (s_notificationTimer == null)
        {
            s_notificationTimer = new();
            s_notificationTimer.Interval = 2500;
            s_notificationTimer.Elapsed += OnNotificationTimerElapsed;
            s_notificationTimer.Start();
        }
    }

    private bool _isRefreshed = false;
    public static bool IsStarup = true;
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        string id = e.Parameter as string;
        if (!_isRefreshed)
        {
            await Refresh();
            _isRefreshed = true;
        }

        if (IsStarup)
        {
            bool willShowMyProfileOnStartup = (Configuration.GetValue("ShowMyProfileOnStartup") as bool?) ?? false;
            if (willShowMyProfileOnStartup && string.IsNullOrEmpty(id)) id = Me.id;
            IsStarup = false;
        }

        if (!string.IsNullOrEmpty(id)) NavigateTimeline(id);
        else NavigateTimeline();
        
    }

    public static void HideSettingsFlyout() => (Instance.BtSettings.Tag as Flyout)?.Hide();
    public static void HideExtrasFlyout() => (Instance.BtExtras.Tag as Flyout)?.Hide();

    private DateTime? _lastNotificationTimestamp = null;

	public void SetLoading(bool isLoading, string message = null, int progress = -1)
	{
		if (GdLoading == null) return;
		GdLoading.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
		if (isLoading && message != null)
		{
			IsEnabled = false;
			TbLoading.Text = message;
			var showProgress = progress > 0;
			PrLoading.IsIndeterminate = !showProgress;
			if (showProgress) PrLoading.Value = progress;
		}
		else
			IsEnabled = true;
	}

	private async void OnNotificationTimerElapsed(object sender, ElapsedEventArgs e)
    {
        s_notificationTimer.Stop();
        try
        {
            var status = await ApiHandler.GetNotificationStatus();
            if ((status?.NotificationCount ?? 0) == 0 && LatestNotificationId != null)
                return;

            var notifications = await ApiHandler.GetNotifications();

            var first = notifications.FirstOrDefault();
            LatestNotificationId = first.id;

            for (int i = 0; i < notifications.Count; i++)
            {
                ApiHandler.DataType.Notification notification = notifications[i];
                if (_lastNotificationTimestamp != null && notification?.created_at > _lastNotificationTimestamp && notification.is_new)
                    ShowNotificationToast(notification);
                else break;
            }

            _lastNotificationTimestamp = first?.created_at;
        }
        catch (Exception) { }
        finally { s_notificationTimer.Start(); }
    }

    private static async void ShowNotificationToast(ApiHandler.DataType.Notification notification)
    {
        string contentMessage = notification.content ?? "내용 없음";

        var willShow = true;
        var disableLike = !((Configuration.GetValue("EmotionalNotification") as bool?) ?? true);
        var disableVip = !((Configuration.GetValue("FavoriteFriendNotification") as bool?) ?? true);

        if (disableLike && notification.emotion != null)
            willShow = false;
        if (disableVip && notification.decorators != null && notification.decorators[0] != null && notification.decorators[0].text != null && notification.decorators[0].text.StartsWith("관심친구"))
            willShow = false;

        if (!willShow) return;

        string profileId = GetProfileIdFromNotification(notification);
        string activityId = GetActivityIdFromNotification(notification);

        _ = Utility.RunOnMainThreadAsync(async () =>
        {
            var timelineWindow = TimelineWindow.FindTimelineWindowByPostId(activityId);
			if (timelineWindow== null) return;
            await timelineWindow.Control.RefreshContent();
		});

        var builder = new ToastContentBuilder()
        .AddText(notification.message)
        .AddText(contentMessage)
        .AddArgument("Open");

        var thumbnailUrl = notification.thumbnail_url;

        if (notification.scheme.StartsWith("kakaostory://activities/"))
        {
            if (string.IsNullOrEmpty(thumbnailUrl))
            {
                var post = await ApiHandler.GetPost(activityId);
                var mediaCount = post?.media?.Count ?? 0;
                var mediaType = post?.media_type;
                if (mediaCount > 0 && mediaType != "video")
                    thumbnailUrl = post?.media[0]?.origin_url ?? thumbnailUrl;
            }
            var argument = $"Activity={activityId}";
            builder.AddArgument(argument);
        }
        else if (notification.scheme.StartsWith("kakaostory://profiles/"))
        {
            var argument = $"Profile={profileId}";
            builder.AddArgument(argument);
        }

        if (!string.IsNullOrEmpty(thumbnailUrl))
            builder.AddHeroImage(new Uri(thumbnailUrl));

        builder.Show();
    }

    private static string GetProfileIdFromNotification(ApiHandler.DataType.Notification notification)
    {
        var scheme = notification.scheme;
        if (!scheme.StartsWith("kakaostory://profiles/")) return null;

        return scheme.Replace("kakaostory://profiles/", "");
    }

    private static string GetActivityIdFromNotification(ApiHandler.DataType.Notification notification)
    {
        var scheme = notification.scheme;
        if (!scheme.StartsWith("kakaostory://activities/")) return null;

        var activityId = scheme.Replace("kakaostory://activities/", "");
        if (activityId.Contains("?profile_id="))
            activityId = activityId.Split("?profile_id=")[0];

        return activityId;
    }

    public static void ShowWindow()
    {
        if (!LoginPage.IsLoggedIn) return;

        if (MainWindow.Instance == null)
        {
            var window = new MainWindow();
            window.Activate();
		}
        else WindowHelper.ShowWindow(MainWindow.Instance);
    }

    public static void NavigateTimeline(string args = null)
    {
        LastArgs = args;
        if (MainWindow.Instance == null) return;
        if (args != null) Instance.FrContent.Navigate(typeof(TimelinePage), args);
        else Instance.FrContent.Navigate(typeof(TimelinePage));
    }

    public static void ShowTimeline()
    {
        if (!LoginPage.IsLoggedIn) return;
        if (MainWindow.Instance == null)
		{
			Utility.SaveCurrentState(true);
			Utility.RestartProgram();
		}
        else
        {
            ShowWindow();
            HideOverlay();
            NavigateTimeline();
        }
    }

    public static void ShowMyProfile()
	{
		if (!LoginPage.IsLoggedIn) return;
		if (MainWindow.Instance == null)
		{
            Utility.SaveCurrentState(id: Me.id);
			Utility.RestartProgram();
		}
        else
        {
		    ShowWindow();
            HideOverlay();
            NavigateTimeline(Me.id);
        }
    }

    public static bool IsFriendListViewLoaded => (Instance.LvFriends.ItemsSource as List<FriendProfile>) != null;

    public static void SelectFriend(string id)
    {
        var page = Instance.FrContent.Content as TimelinePage;
        if (!string.IsNullOrEmpty(id) && page?.Id == id) return;

        var items = Instance.LvFriends.ItemsSource as List<FriendProfile>;
        var item = items.FirstOrDefault(x => x.Id == id);
        Instance.LvFriends.SelectedItem = item;
    }

    private static async Task RefreshFriends()
    {
        Friends = await ApiHandler.GetFriends();
        await UserTagManager.InitializeAsync(Friends.profiles);
    }

    public static TimelineControl GetOverlayTimeLineControl() => Instance?.FrOverlay?.Content as TimelineControl;
    public static void ShowOverlay(UIElement element, bool isSecond = false)
    {
        var overlay = isSecond ? Instance.GdOverlay2 : Instance.GdOverlay;
        var frame = isSecond ? Instance.FrOverlay2 : Instance.FrOverlay;
        overlay.Visibility = Visibility.Visible;
        (frame.Content as TimelineControl)?.UnloadMedia();
        frame.Content = element;
    }

    public static void HideOverlay(bool willDispose = true)
    {
        if (Instance == null) return;

        var isSecond = Instance.GdOverlay2.Visibility == Visibility.Visible;
        var overlay = isSecond ? Instance.GdOverlay2 : Instance.GdOverlay;
        overlay.Visibility = Visibility.Collapsed;

        if (!willDispose) return;
        var frame = isSecond ? Instance.FrOverlay2 : Instance.FrOverlay;
        (frame.Content as TimelineControl)?.UnloadMedia();
        frame.Content = null;
    }

    public static async Task RefreshFriendList() => await Instance.RefreshFriendListAsync();
    
    private async Task Refresh()
    {
        await RefreshFriendListAsync();
        MainWindow.ShowMenus();
    }
    
    private async Task RefreshFriendListAsync()
    {
        TbFriendCount.Text = "로딩중...";
        LvFriends.ItemsSource = null;
        await Task.Delay(100);
        
        Me ??= await ApiHandler.GetProfileData();
        await RefreshFriends();

        var friendProfiles = new List<FriendProfile>();
        foreach (var profile in Friends.profiles)
            friendProfiles.Add(profile.GetFriendData());
        friendProfiles = friendProfiles.OrderByDescending(x => x.IsBirthday).ThenByDescending(x => x.IsFavorite).ThenBy(x => x.Name).ToList();

        LvFriends.ItemsSource = friendProfiles;
        TbName.Text = Me.display_name;
        TbFriendCount.Text = $"내 친구 {Friends.profiles.Count}";
        var profileUrl = Me.GetValidUserProfileUrl();
        if (!string.IsNullOrEmpty(profileUrl)) Utility.SetPersonPictureUrlSource(PpMyProfile, profileUrl, false);

        return;
    }

    private void OnFriendListSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var listView = sender as ListView;
        var data = listView.SelectedItem as FriendProfile;
        if (data == null) return;
        NavigateTimeline(data.Id);
    }

    public static void SetCursor(InputSystemCursorShape shape)
    {

    }

    private void ProfilePointerEntered(object sender, PointerRoutedEventArgs e) => Utility.ChangeSystemMouseCursor(true);
    private void ProfilePointerExited(object sender, PointerRoutedEventArgs e) => Utility.ChangeSystemMouseCursor(false);

    public static void ShowProfile(string id)
	{
        if(MainWindow.Instance == null)
        {
		    _ = Utility.ShowMessageDialogAsync("메인 창이 열려있어야만 작동하는 기능입니다.", "오류");
        }
        else
        {
		    var itemsSource = Instance.LvFriends.ItemsSource as List<FriendProfile>;
            var item = itemsSource.Where(x => x.Id == id);
            Instance.LvFriends.SelectedItem = item;

            HideOverlay();
            NavigateTimeline(id);
        }
    }

    public static TimelinePage GetTimelinePage() => Instance?.FrContent?.Content as TimelinePage;

    private void FriendPointerEntered(object sender, PointerRoutedEventArgs e) => Utility.ChangeSystemMouseCursor(true);

    private void FriendPointerExited(object sender, PointerRoutedEventArgs e) => Utility.ChangeSystemMouseCursor(false);

    private void ProfileTapped(object sender, TappedRoutedEventArgs e) => NavigateTimeline(Me.id);

    private void OnSettingsButtonClicked(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var settingsControl = new SettingsControl();
        var flyout = new Flyout();
        flyout.Content = settingsControl;
        flyout.ShowAt(button);
        button.Tag = flyout;
    }

    private void OnExtrasButtonClicked(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var settingsControl = new ExtrasControl();
        var flyout = new Flyout();
        flyout.Content = settingsControl;
        flyout.ShowAt(button);
        button.Tag = flyout;
    }

    private async void OnRefreshFriendListButtonClicked(object sender, RoutedEventArgs e) => await RefreshFriendListAsync();
}
