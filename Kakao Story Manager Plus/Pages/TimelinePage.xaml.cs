using StoryApi;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using KSMP.Controls;

namespace KSMP.Pages;

public sealed partial class TimelinePage : Page
{
    public string Id = null;
    private static TimelinePage _instance;
    public bool IsMyTimeline => Id == MainPage.Me.id;

    public TimelinePage()
    {
        InitializeComponent();
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Utility.FlushBitmapImages();
        foreach (var rawItem in LvContent.Items)
        {
            var item = rawItem as TimelineControl;
            if (item is null) return;
            item.Dispose();
        }
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        string id = e.Parameter as string;
        await LoadId(id);
    }

    private async Task LoadId(string id)
    {
        MainPage.SelectFriend(id);

        Id = id;
        _instance = this;
        await Refresh();
        Border border = VisualTreeHelper.GetChild(LvContent, 0) as Border;
        if (border == null) return;
        ScrollViewer scrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
        scrollViewer.ViewChanged += OnScrollViewerViewChanged;
    }

    public static void HidePostFromTimeline(Controls.TimelineControl control) => _instance.LvContent.Items.Remove(control);

    string lastFeed = null;
    public static async Task Refresh() => await _instance.Refresh();
    public void RemovePost(string postId)
    {
        foreach (FrameworkElement item in LvContent.Items)
        {
            var timelineControl = item as Controls.TimelineControl;
            if (timelineControl?.PostId == postId) LvContent.Items.Remove(item);
        }
    }
    public async Task Renew()
    {
        lastFeed = null;
        await Refresh();
    }
    private async Task Refresh(string from = null)
    {
        GC.Collect();
        PrLoading.Visibility = Visibility.Visible;
        if (from == null)
            LvContent.Items.Clear();
        if (Id == null)
        {
            var data = await ApiHandler.GetFeed(lastFeed);
            foreach (var feed in data.feeds)
            {
                if (IsValidFeed(feed))
                {
                    var control = new Controls.TimelineControl(feed);
                    control.Width = 600;
                    LvContent.Items.Add(control);
                }
                lastFeed = feed.id;
            }
        }
        else
        {
            if(from == null)
            {
                var profileFrame = new Frame
                {
                    Content = new Controls.UserProfileControl(Id),
                    Visibility = Visibility.Visible
                };
                profileFrame.Width = 600;
                LvContent.Items.Add(profileFrame);
            }

            var data = await ApiHandler.GetProfileFeed(Id, lastFeed);
            foreach (var feed in data.activities)
            {
                if (IsValidFeed(feed))
                {
                    var control = new Controls.TimelineControl(feed);
                    control.Width = 600;
                    LvContent.Items.Add(control);
                }
                lastFeed = feed.id;
            }
        }
        PrLoading.Visibility = Visibility.Collapsed;
    }

    private static bool IsValidFeed(ApiHandler.DataType.CommentData.PostData feed) => feed.deleted != true && (feed.@object?.deleted ?? false) != true && feed.blinded != true && (feed.@object?.blinded ?? false) != true && (feed.verb == "post" || feed.verb == "share");

    private bool isRefreshing = false;
    private async void OnScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (!isRefreshing)
        {
            var scrollViewer = sender as ScrollViewer;
            var verticalOffset = scrollViewer.VerticalOffset;
            var maxVerticalOffset = scrollViewer.ScrollableHeight;

            if (maxVerticalOffset < 0 || verticalOffset == maxVerticalOffset)
            {
                isRefreshing = true;
                await Refresh(lastFeed);
                isRefreshing = false;
            }
        }
    }
}
