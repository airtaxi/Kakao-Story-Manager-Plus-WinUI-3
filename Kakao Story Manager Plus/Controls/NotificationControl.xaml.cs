using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using H.NotifyIcon;
using KSMP.Extension;
using KSMP.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace KSMP.Controls;

public sealed partial class NotificationControl : UserControl
{
    private partial class NotificationData : ObservableObject
    {
        [ObservableProperty]
        private Visibility unreadBarVisiblity = Visibility.Collapsed;

        public string NotificationId { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Time { get; set; }
        public string Scheme { get; set; }
        public string ActorId { get; set; }
    }

    private static List<NotificationData> s_notificationDatas = new();
    public NotificationControl()
    {
        InitializeComponent();
        (Content as ListView).ItemsSource = s_notificationDatas;
    }

    private bool _isRefreshing = false;
    public async Task Refresh()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;
        var notificationDatas = new List<NotificationData>();
        var notifications = await ApiHandler.GetNotifications();
        MainPage.LatestNotificationId = notifications.FirstOrDefault()?.id;

        foreach (var notification in notifications)
        {
            string contentMessage = notification.content;
            if (string.IsNullOrEmpty(contentMessage)) contentMessage = "내용 없음";
            if (contentMessage.Contains('\n'))
                contentMessage = contentMessage.Split(new string[] { "\n" }, StringSplitOptions.None)[0];
            var notificationData = new NotificationData
            {
                Title = notification.message,
                Description = contentMessage,
                ProfilePictureUrl = notification.actor?.GetValidUserProfileUrl(),
                Time = Api.Story.Utils.GetTimeString(notification.created_at),
                UnreadBarVisiblity = notification.is_new ? Visibility.Visible : Visibility.Collapsed,
                Scheme = notification.scheme,
                ActorId = notification.actor.id,
                NotificationId = notification.id
            };
            notificationDatas.Add(notificationData);
        }
        s_notificationDatas = notificationDatas;
        (Content as ListView).ItemsSource = s_notificationDatas;
        _isRefreshing = false;
    }

    public static string LatestNotificationId => s_notificationDatas?.FirstOrDefault()?.NotificationId;
    
    private async void NotificationSelected(object sender, SelectionChangedEventArgs e)
    {
        var listView = sender as ListView;
        var notificationData = listView.SelectedItem as NotificationData;
        if (notificationData == null) return;
        notificationData.UnreadBarVisiblity = Visibility.Collapsed;
        var scheme = notificationData.Scheme;
        if (scheme.Contains("?profile_id="))
        {
            var objectStringStr = scheme.Split(new string[] { "?profile_id=" }, StringSplitOptions.None);
            var id = objectStringStr[0].Split(new string[] { "activities/" }, StringSplitOptions.None)[1];
            var post = await ApiHandler.GetPost(id);

            var window = TimelineWindow.GetTimelineWindow(post);
            window.Activate();
        }
        else if (scheme.Contains("kakaostory://profiles/"))
        {
            string id = scheme.Replace("kakaostory://profiles/", "");
            MainPage.HideOverlay();
            MainPage.ShowProfile(id);
        }
        var popup = (Parent as FlyoutPresenter)?.Parent as Popup;
        if (popup != null) popup.IsOpen = false;
    }
}
