using KSMP;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using KSMP.Controls;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KSMP.Pages;

public sealed partial class TimelinePage : Page
{
    private static TimelinePage s_instance;

    public string Id = null;
    public bool IsMyTimeline => Id == MainPage.Me.id;
    private string _lastFeedId = null;
    public static string LastFeedId = null; 
    private readonly ObservableCollection<Control> _timeline = new();

    public TimelinePage()
    {
        InitializeComponent();
        LastFeedId = null;
        GvContent.ItemsSource = _timeline;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        string id = e.Parameter as string;
        await LoadId(id);
        var scrollViewer = Utility.GetScrollViewerFromGridView(GvContent);
        scrollViewer.ViewChanged += OnScrollViewerViewChanged;
    }

    public List<TimelineControl> GetTimelineControls()
    {
        var list = _timeline.Select(x => x as TimelineControl).ToList();
        list.RemoveAll(x => x is null);
        return list;
    }

    public async Task LoadId(string id)
    {
        if(!string.IsNullOrEmpty(id)) MainPage.SelectFriend(id);

        Id = id;
        s_instance = this;
        await Refresh(App.RecordedFirstFeedId);
        App.RecordedFirstFeedId = null;
    }

    public static void HidePostFromTimeline(TimelineControl control) => s_instance._timeline.Remove(control);

    public void RemovePost(string postId)
    {
        foreach (Control item in _timeline)
        {
            var timelineControl = item as TimelineControl;
            if (timelineControl?.PostId == postId) _timeline.Remove(item);
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
        IsEnabled = false;
        PrLoading.Visibility = Visibility.Visible;
        if (from == null)
        {
            _timeline.Clear();
            foreach (object item in _timeline) (item as TimelineControl)?.UnloadMedia();
            Utility.ManuallyDisposeAllMedias();
        }

        if (string.IsNullOrEmpty(Id))
        {
            var data = await ApiHandler.GetFeed(from);
            foreach (var feed in data.feeds)
            {
                if (IsValidFeed(feed))
                {
                    var control = new TimelineControl(feed);
                    _timeline.Add(control);
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
                _timeline.Add(profileFrame);
            }

            var data = await ApiHandler.GetProfileFeed(Id, from);
            foreach (var feed in data.activities)
            {
                if (IsValidFeed(feed))
                {
                    var control = new TimelineControl(feed);
                    _timeline.Add(control);
                }
            }
            LastFeedId = _lastFeedId;
            if (data.activities.Count > 15) _lastFeedId = data.activities.LastOrDefault().id;
            else _lastFeedId = null;
        }

        await Task.Delay(250);
        GvContent.UpdateLayout();
        ValidateTimelineContent();
        PrLoading.Visibility = Visibility.Collapsed;
        IsEnabled = true;
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
        ValidateTimelineContent();
        if (maxVerticalOffset < 0 || verticalOffset == maxVerticalOffset)
        {
            _isRefreshing = true;
            bool willClearTimelineOnRefresh = (Utils.Configuration.GetValue("ClearTimelineOnRefresh") as bool?) ?? true;
            if (willClearTimelineOnRefresh) _timeline.Clear();
            await Refresh(_lastFeedId);
            _isRefreshing = false;
        }
    }

    private void ValidateTimelineContent()
    {
        var scrollViewer = Utility.GetScrollViewerFromGridView(GvContent);
        foreach (var control in _timeline)
        {
            if (control is not TimelineControl) continue;
            var timelineControl = control as TimelineControl;
            if (Utility.IsVisibleToUser(control, scrollViewer) && !timelineControl.IsContentLoaded) _ = timelineControl.RefreshContent();
            else if (!Utility.IsVisibleToUser(control, scrollViewer) && timelineControl.IsContentLoaded) timelineControl.UnloadMedia();
        }
    }
}
