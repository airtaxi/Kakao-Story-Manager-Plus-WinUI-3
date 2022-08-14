using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using ABI.System;
using H.NotifyIcon;
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
    private Timer notificationTimer = new();

    public MainPage()
    {
        InitializeComponent();
        _ = Refresh();
        _instance = this;
        InitializeWriteFlyout();
        notificationTimer.Interval = 2000;
        notificationTimer.Elapsed += OnNotificationTimerElapsed;
        notificationTimer.Start();
    }

    private void InitializeWriteFlyout()
    {
        var writeFlyout = new Flyout();
        var writePostControl = new Controls.WritePostControl(BtWrite);
        writeFlyout.Content = writePostControl;
        BtWrite.Flyout = writeFlyout;
        writePostControl.OnPostCompleted += OnPostCompleted;
    }

    private void OnPostCompleted() => InitializeWriteFlyout();

    private DateTime? _lastNotificationTimestamp = null;
    private async void OnNotificationTimerElapsed(object sender, ElapsedEventArgs e)
    {
        var notifications = await ApiHandler.GetNotifications();


        notifications.Reverse();
        foreach (var notification in notifications)
        {
            if(_lastNotificationTimestamp != null && notification?.created_at > _lastNotificationTimestamp)
                ShowNotificationToast(notification);
        }

        var last = notifications.LastOrDefault();
        _lastNotificationTimestamp = last?.created_at;
    }

    private static void ShowNotificationToast(ApiHandler.DataType.Notification notification)
    {
        string contentMessage = notification.content ?? "내용 없음";

        var willShow = true;
        var disableLike = false;
        var disableVip = false;

        if (disableLike && notification.emotion != null)
            willShow = false;
        if (disableVip && notification.decorators != null && notification.decorators[0] != null && notification.decorators[0].text != null && notification.decorators[0].text.StartsWith("관심친구"))
            willShow = false;

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
            {
                string profileId = GetProfileIdFromNotification(notification);
                openButton.AddArgument($"Profile={profileId}");
            }
            else if (notification.scheme.StartsWith("kakaostory://activities/"))
            {
                string activityId = GetActivityIdFromNotification(notification);
                openButton.AddArgument($"Activity={activityId}");
            }
            builder.AddButton(openButton);

            if (commentId != null)
            {
                string activityId = GetActivityIdFromNotification(notification);

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

        //TODO: 자동 새로고침
        //if(_instance.FrContent is TimelinePage) (_instance.FrContent as TimelinePage)
    }

    private static string GetProfileIdFromNotification(ApiHandler.DataType.Notification notification) => notification.scheme.Replace("kakaostory://profiles/", "");
    private static string GetActivityIdFromNotification(ApiHandler.DataType.Notification notification)
    {
        var activityId = notification.scheme.Replace("kakaostory://activities/", "");
        if (activityId.Contains("?profile_id="))
        {
            activityId = activityId.Split("?profile_id=")[0];
        }

        return activityId;
    }

    public static void ShowWindow()
    {
        if (!LoginPage.IsLoggedIn) return;
        MainWindow.Instance.Activate();
        MainWindow.Instance.Show();
        var appWindow = MainWindow.Instance.GetAppWindow();
        appWindow.Show();
        var presenter = appWindow.Presenter as OverlappedPresenter;
        if (presenter.State == OverlappedPresenterState.Minimized) presenter.Restore();
        presenter.IsAlwaysOnTop = true;
        presenter.IsAlwaysOnTop = false;
    }

    public static void ShowTimeline()
    {
        if (!LoginPage.IsLoggedIn) return;
        ShowWindow();
        HideOverlay();
        _instance.FrContent.Navigate(typeof(TimelinePage));
    }

    public static void ShowMyProfile()
    {
        if (_instance == null) return;
        ShowWindow();
        HideOverlay();
        _instance.FrContent.Navigate(typeof(TimelinePage), Me.id);
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
        if (!string.IsNullOrEmpty(profileUrl))
            PpMyProfile.ProfilePicture = Utility.GenerateImageUrlSource(profileUrl);
        FrContent.Navigate(typeof(TimelinePage), null);
    }

    private void OnFriendListSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var listView = sender as ListView;
        var data = listView.SelectedItem as FriendProfile;
        if (data == null) return;
        FrContent.Navigate(typeof(TimelinePage), data.Id);
    }

    private void ProfilePointerEntered(object sender, PointerRoutedEventArgs e) => Utility.ChangeCursor(true);

    private void ProfilePointerExited(object sender, PointerRoutedEventArgs e) => Utility.ChangeCursor(false);

    private void OnLogoutButtonClicked(object sender, RoutedEventArgs e)
    {
        notificationTimer.Stop();
        notificationTimer.Dispose();
        //if(Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey("Password"))
        //   Windows.Storage.ApplicationData.Current.LocalSettings.Values.Remove("Password");
        //if(Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey("Email"))
        //   Windows.Storage.ApplicationData.Current.LocalSettings.Values.Remove("Email");
        Frame.Navigate(typeof(LoginPage));
    }

    public static void ShowProfile(string id)
    {
        var itemsSource = _instance.LvFriends.ItemsSource as List<FriendProfile>;
        var item = itemsSource.Where(x => x.Id == id);
        _instance.LvFriends.SelectedItem = item;

        HideOverlay();
        _instance.FrContent.Navigate(typeof(TimelinePage), id);
    }

    private void FriendPointerEntered(object sender, PointerRoutedEventArgs e) => Utility.ChangeCursor(true);

    private void FriendPointerExited(object sender, PointerRoutedEventArgs e) => Utility.ChangeCursor(false);

    private void ProfileTapped(object sender, TappedRoutedEventArgs e) => FrContent.Navigate(typeof(TimelinePage), Me.id);

    private void TitleTapped(object sender, TappedRoutedEventArgs e)
    {
        HideOverlay();
        FrContent.Navigate(typeof(TimelinePage));
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
        FrContent.Navigate(typeof(TimelinePage), id);
        sender.Text = "";
        sender.ItemsSource = null;
    }

    private void SearchFriendQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => SearchFriend(sender);
}
