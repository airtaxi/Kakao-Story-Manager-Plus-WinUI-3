using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using KSMP.Extension;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.Media.Devices;

namespace KSMP.Controls;

public sealed partial class NotificationControl : UserControl
{
    private partial class NotificationData : ObservableObject
    {
        [ObservableProperty]
        private Visibility unreadBarVisiblity = Visibility.Collapsed;

        public string ProfilePictureUrl { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Time { get; set; }
        public string Scheme { get; set; }
        public string ActorId { get; set; }
    }
    public NotificationControl()
    {
        InitializeComponent();
        _ = Refresh();
    }

    private async Task Refresh()
    {
        var notificationDatas = new List<NotificationData>();
        var notifications = await KSMP.ApiHandler.GetNotifications();
        foreach(var notification in notifications)
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
                ActorId = notification.actor.id
            };
            notificationDatas.Add(notificationData);
        }
        (Content as ListView).ItemsSource = notificationDatas;
    }

    private void ProfileImageTapped(object sender, TappedRoutedEventArgs e)
    {
        var id = (sender as PersonPicture).Tag as string;
        Pages.MainPage.ShowProfile(id);
        e.Handled = true;
    }

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
            var post = await KSMP.ApiHandler.GetPost(id);
            Pages.MainPage.HideOverlay();
            Pages.MainPage.ShowOverlay(new TimelineControl(post, false, true));
        }
        else if (scheme.Contains("kakaostory://profiles/"))
        {
            string id = scheme.Replace("kakaostory://profiles/", "");
            Pages.MainPage.HideOverlay();
            Pages.MainPage.ShowProfile(id);
        }
        var popup = (Parent as FlyoutPresenter)?.Parent as Popup;
        if (popup != null) popup.IsOpen = false;
    }
}
