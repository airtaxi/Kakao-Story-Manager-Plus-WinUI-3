using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Timers;
using KSMP.Controls;
using KSMP.Extension;
using KSMP.Utils;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using StoryApi;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Core;
using static KSMP.ClassManager;
using static StoryApi.ApiHandler.DataType.FriendData;

namespace KSMP.Pages;

public sealed partial class MainPage : Page
{
    public static ApiHandler.DataType.UserProfile.ProfileData Me;
    public static Friends Friends = null;
    public static string LastArgs = null;

    private static MainPage s_instance;
    private static Timer s_notificationTimer = null;

    private bool _isWritePostFlyoutOpened = false;

    public MainPage()
    {
        InitializeComponent();
        s_instance = this;
        InitializeWritePostFlyout();
        InitializeSettingsFlyout();

        if (s_notificationTimer == null)
        {
            s_notificationTimer = new();
            s_notificationTimer.Interval = 2250;
            s_notificationTimer.Elapsed += OnNotificationTimerElapsed;
            s_notificationTimer.Start();
        }
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        string id = e.Parameter as string;
        await Refresh();
        if (!string.IsNullOrEmpty(id)) NavigateTimeline(id);
        else NavigateTimeline();
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
        _isWritePostFlyoutOpened = false;
        var previousFlyout = BtWrite.Flyout as Flyout;
        if (previousFlyout != null)
        {
            previousFlyout.Opened -= OnWritePostFlyoutOpened;
            previousFlyout.Closed -= OnWritePostFlyoutClosed;
            var previousControl = previousFlyout.Content as WritePostControl;
            if(previousControl != null) previousControl.OnPostCompleted -= OnPostCompleted;
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
        _isWritePostFlyoutOpened = true;
    }

    private void OnWritePostFlyoutClosed(object sender, object e) => _isWritePostFlyoutOpened = false;

    public static MainPage GetInstance() => s_instance;

    private void OnPostCompleted() => InitializeWritePostFlyout();

    private DateTime? _lastNotificationTimestamp = null;

    private async void OnNotificationTimerElapsed(object sender, ElapsedEventArgs e)
    {
        try
        {
            s_notificationTimer.Stop();

            var notifications = await ApiHandler.GetNotifications();
            for (int i = 0; i < notifications.Count; i++)
            {
                ApiHandler.DataType.Notification notification = notifications[i];
                if (_lastNotificationTimestamp != null && notification?.created_at > _lastNotificationTimestamp && notification.is_new)
                    ShowNotificationToast(notification);
                else break;
            }

            var first = notifications.FirstOrDefault();
            _lastNotificationTimestamp = first?.created_at;
        }
        catch (Exception) { } //Ignore
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

        _ = s_instance.RunOnMainThreadAsync(async () =>
        {
            var timelineControl = s_instance.FrOverlay.Content as TimelineControl;
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
        //Utility.DisposeAllMedias();
        if (args != null) s_instance.FrContent.Navigate(typeof(TimelinePage), args);
        else s_instance.FrContent.Navigate(typeof(TimelinePage));
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
        if (s_instance == null) return;
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
        flyout.ShowAt(s_instance);
    }


    public static void SelectFriend(string id)
    {
        var page = s_instance.FrContent.Content as TimelinePage;
        if (!string.IsNullOrEmpty(id) && page?.Id == id) return;

        var items = s_instance.LvFriends.ItemsSource as List<FriendProfile>;
        var item = items.FirstOrDefault(x => x.Id == id);
        s_instance.LvFriends.SelectedItem = item;
    }

    private static async Task RefreshFriends()
    {
        Friends = await ApiHandler.GetFriends();
    }

    public static TimelineControl GetOverlayTimeLineControl() => s_instance?.FrOverlay?.Content as TimelineControl;
    public static void ShowOverlay(UIElement element, bool isSecond = false)
    {
        var overlay = isSecond ? s_instance.GdOverlay2 : s_instance.GdOverlay;
        var frame = isSecond ? s_instance.FrOverlay2 : s_instance.FrOverlay;
        overlay.Visibility = Visibility.Visible;
        (frame.Content as TimelineControl)?.DisposeMedias();
        frame.Content = element;
        s_instance.GdRoot.Focus(FocusState.Keyboard);
    }
    public static void HideOverlay(bool willDispose = true)
    {
        if (s_instance == null) return;
        var isWritePostFlyoutOpened = s_instance?.BtWrite?.Flyout?.IsOpen ?? false;
        if(isWritePostFlyoutOpened)
        {
            ((s_instance.BtWrite.Flyout as Flyout).Content as WritePostControl).PreventClose = false;
            s_instance._isWritePostFlyoutOpened = false;
            s_instance.BtWrite.Flyout.Hide();
            return;
        }
        var isSecond = s_instance.GdOverlay2.Visibility == Visibility.Visible;
        var overlay = isSecond ? s_instance.GdOverlay2 : s_instance.GdOverlay;
        overlay.Visibility = Visibility.Collapsed;

        if (!willDispose) return;
        var frame = isSecond ? s_instance.FrOverlay2 : s_instance.FrOverlay;
        (frame.Content as TimelineControl)?.DisposeMedias();
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
        Me ??= await ApiHandler.GetProfileData();
        TbName.Text = Me.display_name;
        var profileUrl = Me.GetValidUserProfileUrl();
        if (!string.IsNullOrEmpty(profileUrl)) Utility.SetImageUrlSource(PpMyProfile, profileUrl, false);
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
        var dialogResult = await this.ShowMessageDialogAsync("정말로 로그아웃 하시겠습니까?", "경고", true);
        if(dialogResult == ContentDialogResult.Primary)
        {
            Configuration.SetValue("willRememberCredentials", false);
            await this.ShowMessageDialogAsync("로그아웃되었습니다.\n프로그램을 재실행해주세요.", "안내");
            Environment.Exit(0);
        }
    }

    public static void ShowProfile(string id)
    {
        var itemsSource = s_instance.LvFriends.ItemsSource as List<FriendProfile>;
        var item = itemsSource.Where(x => x.Id == id);
        s_instance.LvFriends.SelectedItem = item;

        HideOverlay();
        NavigateTimeline(id);
    }

    public static TimelinePage GetTimelinePage() => s_instance.FrContent.Content as TimelinePage;

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
            var result = await this.ShowMessageDialogAsync("클립보드에 이미지가 있습니다.\n이미지를 추가할까요?", "안내", true);
            if (result != ContentDialogResult.Primary) return;

            var filePath = await Utility.GenerateClipboardImagePathAsync(dataPackageView);
            await control.AddImageFromPath(filePath);
        }
    }

    private async void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_isWritePostFlyoutOpened)
        {
            await Task.Delay(250);
            var flyout = BtWrite?.Flyout as Flyout;
            flyout?.ShowAt(BtWrite);
        }
    }

    private async void OnExitButtonClicked(object sender, RoutedEventArgs e)
    {
        var dialogResult = await this.ShowMessageDialogAsync("정말로 프로그램을 종료하시곘습니까?", "경고", true);
        if (dialogResult == ContentDialogResult.Primary) Environment.Exit(0);
    }

    private void OnMoreButtonClicked(object sender, RoutedEventArgs e)
    {
        GC.Collect(GC.MaxGeneration);
        GC.WaitForPendingFinalizers();
    }

    private void OnRestartButtonClicked(object sender, RoutedEventArgs e) => MainWindow.Restart();
}
