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
using System.Collections.Generic;

namespace KSMP.Pages;

public sealed partial class TimelinePage : Page
{
    private const int TimelineControlWidth = 600;

    private static TimelinePage s_instance;

    public string Id = null;
    public bool IsMyTimeline => Id == MainPage.Me.id;
    private string _lastFeedId = null;
    public static string LastFeedId = null;

    public TimelinePage()
    {
        InitializeComponent();
        LastFeedId = null;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        string id = e.Parameter as string;
        await LoadId(id);
    }

    public List<TimelineControl> GetTimelineControls()
    {
        var list = LvContent.Items.Select(x => x as TimelineControl).ToList();
        list.RemoveAll(x => x is null);
        return list;
    }

    private async Task LoadId(string id)
    {
        if(!string.IsNullOrEmpty(id)) MainPage.SelectFriend(id);

        Id = id;
        s_instance = this;
        await Refresh(App.RecordedFirstFeedId);
        App.RecordedFirstFeedId = null;
        Border border = VisualTreeHelper.GetChild(LvContent, 0) as Border;
        if (border == null) return;
        ScrollViewer scrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
        scrollViewer.ViewChanged += OnScrollViewerViewChanged;
    }

    public static void HidePostFromTimeline(TimelineControl control) => s_instance.LvContent.Items.Remove(control);

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
        _lastFeedId = null;
        await Refresh();
    }

    public static async Task Refresh() => await s_instance.Refresh();
    private async Task Refresh(string from = null)
    {
        PrLoading.Visibility = Visibility.Visible;
        if (from == null)
        {
            LvContent.Items.Clear();
            //foreach (object item in LvContent.Items) (item as TimelineControl)?.DisposeMedias();
            //Utility.DisposeAllMedias();
        }

        if (string.IsNullOrEmpty(Id))
        {
            var data = await ApiHandler.GetFeed(from);
            foreach (var feed in data.feeds)
            {
                if (IsValidFeed(feed))
                {
                    var control = new TimelineControl(feed);
                    control.Width = TimelineControlWidth;
                    LvContent.Items.Add(control);
                }
            }
            LastFeedId = _lastFeedId;
            _lastFeedId = data.feeds.LastOrDefault()?.id;
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

            var data = await ApiHandler.GetProfileFeed(Id, from);
            foreach (var feed in data.activities)
            {
                if (IsValidFeed(feed))
                {
                    var control = new TimelineControl(feed);
                    control.Width = TimelineControlWidth;
                    LvContent.Items.Add(control);
                }
            }
            LastFeedId = _lastFeedId;
            if (data.activities.Count > 15) _lastFeedId = data.activities.LastOrDefault().id;
            else _lastFeedId = null;
        }
        ValidateTimeLineControlsSize(GdMain.ActualWidth);
        PrLoading.Visibility = Visibility.Collapsed;
    }

    private static bool IsValidFeed(ApiHandler.DataType.CommentData.PostData feed) => feed.deleted != true && (feed.@object?.deleted ?? false) != true && feed.blinded != true && (feed.@object?.blinded ?? false) != true && (feed.verb == "post" || feed.verb == "share");

    private bool _isRefreshing = false;
    private async void OnScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_lastFeedId == null) return;
        if (_isRefreshing) return;

        var scrollViewer = sender as ScrollViewer;
        var verticalOffset = scrollViewer.VerticalOffset;
        var maxVerticalOffset = scrollViewer.ScrollableHeight;

        if (maxVerticalOffset < 0 || verticalOffset == maxVerticalOffset)
        {
            _isRefreshing = true;
            LvContent.Items.Clear();
            await Refresh(_lastFeedId);
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
