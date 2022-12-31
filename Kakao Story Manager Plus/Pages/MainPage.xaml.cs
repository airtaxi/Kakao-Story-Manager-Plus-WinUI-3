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
using static KSMP.ClassManager;
using static KSMP.ApiHandler.DataType.FriendData;
using Microsoft.UI.Input;

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
    private bool _isStarup = true;
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        string id = e.Parameter as string;
        if (!_isRefreshed)
        {
            await Refresh();
            _isRefreshed = true;
        }

        if (_isStarup)
        {
            bool willShowMyProfileOnStartup = (Utils.Configuration.GetValue("ShowMyProfileOnStartup") as bool?) ?? false;
            if (willShowMyProfileOnStartup && string.IsNullOrEmpty(id)) id = Me.id;
            _isStarup = false;
        }

        if (!string.IsNullOrEmpty(id)) NavigateTimeline(id);
        else NavigateTimeline();
        
    }

    public static void HideSettingsFlyout() => (Instance.BtSettings.Tag as Flyout)?.Hide();

    private DateTime? _lastNotificationTimestamp = null;

    private async void OnNotificationTimerElapsed(object sender, ElapsedEventArgs e)
    {
        s_notificationTimer.Stop();
        try
        {
            var status = await ApiHandler.GetNotificationStatus();
            if(status == null)
            {
                await MainWindow.OnReloginRequiredHandler();
                return;
            }
            if (status.NotificationCount == 0 && LatestNotificationId != null) return;

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
        catch (Exception) { await MainWindow.OnReloginRequiredHandler(); }
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

        _ = Instance.RunOnMainThreadAsync(async () =>
        {
            var timelineControl = Instance.FrOverlay.Content as TimelineControl;
            if (timelineControl == null) return;
            else if (timelineControl.PostId == activityId) await timelineControl.RefreshContent();
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
        WindowHelper.ShowWindow(MainWindow.Instance);
    }

    public static void NavigateTimeline(string args = null)
    {
        LastArgs = args;
        Utility.ManuallyDisposeAllMedias();
        if (args != null) Instance.FrContent.Navigate(typeof(TimelinePage), args);
        else Instance.FrContent.Navigate(typeof(TimelinePage));
    }

    public static void ShowTimeline()
    {
        if (!LoginPage.IsLoggedIn) return;
        ShowWindow();
        HideOverlay();
        NavigateTimeline();
    }

    public static void ShowMyProfile()
    {
        if (Instance == null) return;
        ShowWindow();
        HideOverlay();
        NavigateTimeline(Me.id);
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
        var writePostButton = MainWindow.GetWritePostButton();
        var isWritePostFlyoutOpened = writePostButton?.Flyout?.IsOpen ?? false;
        if(isWritePostFlyoutOpened)
        {
            ((writePostButton.Flyout as Flyout).Content as WritePostControl).PreventClose = false;
            MainWindow.IsWritePostFlyoutOpened = false;
            writePostButton.Flyout.Hide();
            return;
        }
        var isSecond = Instance.GdOverlay2.Visibility == Visibility.Visible;
        var overlay = isSecond ? Instance.GdOverlay2 : Instance.GdOverlay;
        overlay.Visibility = Visibility.Collapsed;

        if (!willDispose) return;
        var frame = isSecond ? Instance.FrOverlay2 : Instance.FrOverlay;
        (frame.Content as TimelineControl)?.UnloadMedia();
        frame.Content = null;
    }

    private async Task Refresh()
    {
        await RefreshFriends();
        TbFriendCount.Text = $"내 친구 {Friends.profiles.Count}";
        var friendProfiles = new List<FriendProfile>();
        foreach(var profile in Friends.profiles)
        {
            var friendProfile = new FriendProfile
            {
                ProfileUrl = profile.GetValidUserProfileUrl(),
                Name = profile.display_name,
                Id = profile.id
            };
            friendProfiles.Add(friendProfile);
        }
        LvFriends.ItemsSource = friendProfiles;
        Me ??= await ApiHandler.GetProfileData();
        TbName.Text = Me.display_name;
        var profileUrl = Me.GetValidUserProfileUrl();
        if (!string.IsNullOrEmpty(profileUrl)) Utility.SetPersonPictureUrlSource(PpMyProfile, profileUrl, false);
        MainWindow.ShowMenus();
    }

    private void OnFriendListSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var listView = sender as ListView;
        var data = listView.SelectedItem as FriendProfile;
        if (data == null) return;
        NavigateTimeline(data.Id);
    }

    public static void SetCursor(InputSystemCursorShape shape) => Instance.ProtectedCursor = InputSystemCursor.Create(shape);

    private void ProfilePointerEntered(object sender, PointerRoutedEventArgs e) => Utility.ChangeSystemMouseCursor(true);
    private void ProfilePointerExited(object sender, PointerRoutedEventArgs e) => Utility.ChangeSystemMouseCursor(false);

    public static void ShowProfile(string id)
    {
        var itemsSource = Instance.LvFriends.ItemsSource as List<FriendProfile>;
        var item = itemsSource.Where(x => x.Id == id);
        Instance.LvFriends.SelectedItem = item;

        HideOverlay();
        NavigateTimeline(id);
    }

    public static TimelinePage GetTimelinePage() => Instance.FrContent.Content as TimelinePage;

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
}
