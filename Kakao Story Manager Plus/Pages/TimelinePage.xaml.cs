using StoryApi;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using KSMP.Controls;
using RestSharp;
using System.Linq;
using Windows.Security.Authentication.OnlineId;

namespace KSMP.Pages;

public sealed partial class TimelinePage : Page
{
    private const int TimelineControlWidth = 600;
    public string Id = null;
    private static TimelinePage _instance;
    public bool IsMyTimeline => Id == MainPage.Me.id;

    public TimelinePage()
    {
        InitializeComponent();
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

    public static void HidePostFromTimeline(TimelineControl control) => _instance.LvContent.Items.Remove(control);

    string lastFeed = null;
    public static async Task Refresh() => await _instance.Refresh();
    public void RemovePost(string postId)
    {
        foreach (FrameworkElement item in LvContent.Items)
        {
            var timelineControl = item as TimelineControl;
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
                    var control = new TimelineControl(feed);
                    control.Width = TimelineControlWidth;
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
                profileFrame.Width = TimelineControlWidth;
                LvContent.Items.Add(profileFrame);
            }

            var data = await ApiHandler.GetProfileFeed(Id, lastFeed);
            foreach (var feed in data.activities)
            {
                if (IsValidFeed(feed))
                {
                    var control = new TimelineControl(feed);
                    control.Width = TimelineControlWidth;
                    LvContent.Items.Add(control);
                }
                lastFeed = feed.id;
            }
        }
        ValidateTimeLineControlsSize(GdMain.ActualWidth);
        PrLoading.Visibility = Visibility.Collapsed;
    }

    private static bool IsValidFeed(ApiHandler.DataType.CommentData.PostData feed) => feed.deleted != true && (feed.@object?.deleted ?? false) != true && feed.blinded != true && (feed.@object?.blinded ?? false) != true && (feed.verb == "post" || feed.verb == "share");

    private bool _isRefreshing = false;
    private async void OnScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_isRefreshing) return;

        var scrollViewer = sender as ScrollViewer;
        var verticalOffset = scrollViewer.VerticalOffset;
        var maxVerticalOffset = scrollViewer.ScrollableHeight;

        if (maxVerticalOffset < 0 || verticalOffset == maxVerticalOffset)
        {
            _isRefreshing = true;
            LvContent.Items.Clear();
            Utility.DisposeAllMedias();
            GC.Collect();
            await Refresh(lastFeed);
            _isRefreshing = false;
        }
    }

    private void ValidateTimeLineControlsSize(double width)
    {
        if(width < TimelineControlWidth + 20)
            SetControlsSize(Math.Max(width - 20, 0));
        else
            SetControlsSize(TimelineControlWidth);
    }

    private void SetControlsSize(double width)
    {
        var controls = LvContent.Items.Select(x => x as Control).ToList();
        controls.RemoveAll(x => x == null);
        foreach (var control in controls)
        {
            if (control.Width == width) continue;
            control.Width = width;
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => ValidateTimeLineControlsSize(e.NewSize.Width);
}
