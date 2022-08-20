using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KSMP.Extension;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace KSMP.Controls;

public sealed partial class NotificationControl : UserControl
{
    private class NotificationData
    {
        public string ProfilePictureUrl { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Time { get; set; }
        public string Scheme { get; set; }
        public string ActorId { get; set; }
        public Visibility UnreadBarVisiblity { get; set; }
    }
    public NotificationControl()
    {
        InitializeComponent();
        _ = Refresh();
    }

    private async Task Refresh()
    {
        var notificationDatas = new List<NotificationData>();
        var notifications = await StoryApi.ApiHandler.GetNotifications();
        foreach(var notification in notifications)
        {
            string contentMessage = notification.content ?? "내용 없음";
            if (contentMessage.Contains('\n'))
                contentMessage = contentMessage.Split(new string[] { "\n" }, StringSplitOptions.None)[0];
            var notificationData = new NotificationData
            {
                Title = notification.message,
                Description = contentMessage,
                ProfilePictureUrl = notification.actor?.GetValidUserProfileUrl(),
                Time = StoryApi.Utils.GetTimeString(notification.created_at),
                UnreadBarVisiblity = notification.is_new ? Visibility.Visible : Visibility.Collapsed,
                Scheme = notification.scheme,
                ActorId = notification.actor.id
            };
            notificationDatas.Add(notificationData);
        }
        (Content as ListView).ItemsSource = notificationDatas;
        Unloaded += (s, e) => (Content as ListView).ItemsSource = null;
    }

    private void PpProfileImage_Tapped(object sender, TappedRoutedEventArgs e)
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
        var scheme = notificationData.Scheme;
        if (scheme.Contains("?profile_id="))
        {
            var objectStringStr = scheme.Split(new string[] { "?profile_id=" }, StringSplitOptions.None);
            var id = objectStringStr[0].Split(new string[] { "activities/" }, StringSplitOptions.None)[1];
            var post = await StoryApi.ApiHandler.GetPost(id);
            Pages.MainPage.HideOverlay();
            Pages.MainPage.ShowOverlay(new TimelineControl(post, false, true));
        }
        else if (scheme.Contains("kakaostory://profiles/"))
        {
            string id = scheme.Replace("kakaostory://profiles/", "");
            Pages.MainPage.HideOverlay();
            Pages.MainPage.ShowProfile(id);
        }
    }
}
