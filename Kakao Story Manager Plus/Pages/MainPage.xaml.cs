using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using H.NotifyIcon;
using KSMP.Controls;
using KSMP.Extension;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using StoryApi;
using Windows.Media.Devices.Core;
using Windows.Media.MediaProperties;
using Windows.Security.Authentication.OnlineId;
using Windows.System.Threading;
using static KSMP.ClassManager;
using static KSMP.Controls.WritePostControl;
using static StoryApi.ApiHandler.DataType.FriendData;
using static StoryApi.ApiHandler.DataType.MailData;

namespace KSMP.Pages;

public sealed partial class MainPage : Page
{
    private static MainPage _instance;
    public static ApiHandler.DataType.UserProfile.ProfileData Me;
    public static Friends Friends = null;
    private Timer _notificationTimer = new();

    public MainPage()
    {
        InitializeComponent();
        _ = Refresh();
        _instance = this;
        InitializeWritePostFlyout();
        InitializeSettingsFlyout();
        _notificationTimer.Interval = 2000;
        _notificationTimer.Elapsed += OnNotificationTimerElapsed;
        _notificationTimer.Start();
    }

    private void InitializeSettingsFlyout()
    {
        var flyout = new Flyout();
        var control = new SettingsControl();
        flyout.Content = control;
        BtSettings.Flyout = flyout;
    }

    private void InitializeWritePostFlyout()
    {
        var flyout = new Flyout();
        var control = new WritePostControl(BtWrite);
        flyout.Content = control;
        BtWrite.Flyout = flyout;
        control.OnPostCompleted += OnPostCompleted;
    }

    public static MainPage GetInstance() => _instance;

    private void OnPostCompleted() => InitializeWritePostFlyout();

    private DateTime? _lastNotificationTimestamp = null;
    private async void OnNotificationTimerElapsed(object sender, ElapsedEventArgs e)
    {
        try
        {
            _notificationTimer.Stop();
            var notifications = await ApiHandler.GetNotifications();

            for (int i = 0; i < notifications.Count; i++)
            {
                ApiHandler.DataType.Notification notification = notifications[i];
                if (_lastNotificationTimestamp != null && notification?.created_at > _lastNotificationTimestamp)
                    ShowNotificationToast(notification);
                else break;
            }

            var first = notifications.FirstOrDefault();
            _lastNotificationTimestamp = first?.created_at;
        }
        catch (Exception) { } //Ignore
        finally { _notificationTimer.Start(); }
    }

    private static void ShowNotificationToast(ApiHandler.DataType.Notification notification)
    {
        string contentMessage = notification.content ?? "내용 없음";

        var willShow = true;
        var disableLike = !((Utils.Configuration.GetValue("EmotionalNotification") as bool?) ?? true);
        var disableVip = !((Utils.Configuration.GetValue("FavoriteFriendNotification") as bool?) ?? true);

        if (disableLike && notification.emotion != null)
            willShow = false;
        if (disableVip && notification.decorators != null && notification.decorators[0] != null && notification.decorators[0].text != null && notification.decorators[0].text.StartsWith("관심친구"))
            willShow = false;

        string profileId = GetProfileIdFromNotification(notification);
        string activityId = GetActivityIdFromNotification(notification);

        _ = _instance.RunOnMainThreadAsync(async () =>
        {
            var timelineControl = _instance.FrOverlay.Content as TimelineControl;
            if (timelineControl?.PostId == activityId) await timelineControl.RefreshContent();
        });

        if (willShow)
        {
            var commentId = notification.comment_id;

            var builder = new ToastContentBuilder()
            .AddText(notification.message)
            .AddText(contentMessage);

            var openButton = new ToastButton();
            openButton.SetContent("열기");
            openButton.AddArgument("Open");
            if (notification.scheme.StartsWith("kakaostory://profiles/"))
                openButton.AddArgument($"Profile={profileId}");
            else if (notification.scheme.StartsWith("kakaostory://activities/"))
                openButton.AddArgument($"Activity={activityId}");
            builder.AddButton(openButton);

            if (commentId != null)
            {
                var likeButton = new ToastButton();
                likeButton.SetContent("좋아요");
                likeButton.AddArgument("Like");
                likeButton.AddArgument($"Activity={activityId}");
                likeButton.AddArgument($"Comment={commentId}");
                builder.AddButton(likeButton);
            }

            if(!string.IsNullOrEmpty(notification?.thumbnail_url))
                builder.AddHeroImage(new System.Uri(notification.thumbnail_url));

            builder.Show();
        }
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

    public static async void ShowWindow()
    {
        if (!LoginPage.IsLoggedIn) return;
        MainWindow.Instance.Activate();
        MainWindow.Instance.Show();
        var appWindow = MainWindow.Instance.GetAppWindow();
        var presenter = appWindow.Presenter as OverlappedPresenter;
        appWindow.Show();
        if (presenter.State == OverlappedPresenterState.Minimized)
            presenter.Restore();
        presenter.IsAlwaysOnTop = true;
        await Task.Delay(2000);
        presenter.IsAlwaysOnTop = false;
    }

    public static void NavigateTimeline(string args = null)
    {
        Utility.FlushBitmapImages();
        if(args != null) _instance.FrContent.Navigate(typeof(TimelinePage), args);
        else _instance.FrContent.Navigate(typeof(TimelinePage));
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
        if (_instance == null) return;
        ShowWindow();
        HideOverlay();
        NavigateTimeline(Me.id);
    }

    public static void ShowNotifications()
    {
        ShowWindow();
        var flyout = new Flyout
        {
            Content = new Controls.NotificationControl(),
            Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft
        };
        flyout.ShowAt(_instance);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
            HideOverlay();
    }


    public static void SelectFriend(string id)
    {
        var page = _instance.FrContent.Content as TimelinePage;
        if (!string.IsNullOrEmpty(id) && page?.Id == id) return;

        var items = _instance.LvFriends.ItemsSource as List<FriendProfile>;
        var item = items.FirstOrDefault(x => x.Id == id);
        _instance.LvFriends.SelectedItem = item;
    }

    private static async Task RefreshFriends()
    {
        Friends = await ApiHandler.GetFriends();
    }

    public static void ShowOverlay(UIElement element, bool isSecond = false)
    {
        var overlay = isSecond ? _instance.GdOverlay2 : _instance.GdOverlay;
        var frame = isSecond ? _instance.FrOverlay2 : _instance.FrOverlay;
        overlay.Visibility = Visibility.Visible;
        frame.Content = element;
        _instance.GdRoot.Focus(FocusState.Keyboard);
    }
    public static void HideOverlay(bool willDispose = true)
    {
        var isSecond = _instance.GdOverlay2.Visibility == Visibility.Visible;
        var overlay = isSecond ? _instance.GdOverlay2 : _instance.GdOverlay;
        overlay.Visibility = Visibility.Collapsed;

        if (!willDispose) return;
        var frame = isSecond ? _instance.FrOverlay2 : _instance.FrOverlay;
        frame.Content = null;
    }

    private async Task Refresh()
    {
        await RefreshFriends();
        var friends = await ApiHandler.GetFriends();
        TbFriendCount.Text = $"내 친구 {friends.profiles.Count}";
        var friendProfiles = new List<FriendProfile>();
        foreach(var profile in friends.profiles)
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
        Me = await ApiHandler.GetProfileData();
        TbName.Text = Me.display_name;
        var profileUrl = Me.GetValidUserProfileUrl();
        if (!string.IsNullOrEmpty(profileUrl)) PpMyProfile.ProfilePicture = Utility.GenerateImageUrlSource(profileUrl, true);
        NavigateTimeline();
    }

    private void OnFriendListSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var listView = sender as ListView;
        var data = listView.SelectedItem as FriendProfile;
        if (data == null) return;
        NavigateTimeline(data.Id);
    }

    private void ProfilePointerEntered(object sender, PointerRoutedEventArgs e) => Utility.ChangeCursor(true);

    private void ProfilePointerExited(object sender, PointerRoutedEventArgs e) => Utility.ChangeCursor(false);

    private async void OnLogoutButtonClicked(object sender, RoutedEventArgs e)
    {
        _notificationTimer.Stop();
        _notificationTimer.Dispose();
        Utils.Configuration.SetValue("willRememberCredentials", false);
        await this.ShowMessageDialogAsync("로그아웃되었습니다.\n프로그램을 재실행해주세요.", "안내");
        Environment.Exit(0);
    }

    public static void ShowProfile(string id)
    {
        var itemsSource = _instance.LvFriends.ItemsSource as List<FriendProfile>;
        var item = itemsSource.Where(x => x.Id == id);
        _instance.LvFriends.SelectedItem = item;

        HideOverlay();
        NavigateTimeline(id);
    }

    public static TimelinePage GetTimelinePage() => _instance.FrContent.Content as TimelinePage;

    private void FriendPointerEntered(object sender, PointerRoutedEventArgs e) => Utility.ChangeCursor(true);

    private void FriendPointerExited(object sender, PointerRoutedEventArgs e) => Utility.ChangeCursor(false);

    private void ProfileTapped(object sender, TappedRoutedEventArgs e) => NavigateTimeline(Me.id);

    private void TitleTapped(object sender, TappedRoutedEventArgs e)
    {
        HideOverlay();
        NavigateTimeline();
    }

    private void OnNotificationsButtonClicked(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var flyout = new Flyout
        {
            Content = new Controls.NotificationControl(),
            Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft
        };
        flyout.ShowAt(button);
    }

    private static void SearchFriend(AutoSuggestBox sender)
    {
        var text = sender.Text.ToLower();
        if (!string.IsNullOrEmpty(text))
        {
            var newFriends = Friends.profiles.Where(x => x.display_name.ToLower().Contains(text)).Select(x => new FriendProfile { Name = x.display_name, ProfileUrl = x.GetValidUserProfileUrl(), Id = x.id }).ToList();
            sender.ItemsSource = newFriends;
        }
        else
            sender.ItemsSource = null;
    }
    private void SearchFriendTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) => SearchFriend(sender);

    private void SearchFriendSelected(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        var friend = args.SelectedItem as FriendProfile;
        var id = friend.Id;
        NavigateTimeline(id);
        sender.Text = "";
        sender.ItemsSource = null;
    }

    private void SearchFriendQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => SearchFriend(sender);

    private async void OnClearMemoryButtonClicked(object sender, RoutedEventArgs e)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var process = Process.GetCurrentProcess();
        var memorySize = process.PrivateMemorySize64/1024/1024;
        await this.ShowMessageDialogAsync($"메모리가 정리되었습니다.\n사용중 메모리: {memorySize:N0}MB", "안내");
    }

    private void OnWritePostButtonClicked(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var flyout = button.Flyout as Flyout;
        var control = flyout.Content as WritePostControl;
        control.AdjustDefaultPostWritingPermission();
    }
}
