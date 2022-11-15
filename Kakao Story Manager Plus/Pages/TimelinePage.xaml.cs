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
    private string _lastFeedId = null;

    public string Id = null;
    public bool IsMyTimeline => Id == MainPage.Me.id;
    public readonly ListViewBase BaseListView;

    public static string LastFeedId = null; 

    public TimelinePage()
    {
        InitializeComponent();

        bool willUseResponsiveTimeline = (Utils.Configuration.GetValue("UseResponsiveTimeline") as bool?) ?? true;
        if (willUseResponsiveTimeline)
        {
            LvContent.Visibility = Visibility.Collapsed;
            BaseListView = GvContent;
        }
        else
        {
            GvContent.Visibility = Visibility.Collapsed;
            BaseListView = LvContent;
        }

        LastFeedId = null;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        string id = e.Parameter as string;
        await LoadId(id);
        var scrollViewer = Utility.GetScrollViewerFromBaseListView(BaseListView);
        scrollViewer.ViewChanged += OnScrollViewerViewChanged;
    }

    public List<TimelineControl> GetTimelineControls()
    {
        var list = BaseListView.Items.Select(x => x as TimelineControl).ToList();
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

    public static void HidePostFromTimeline(TimelineControl control) => s_instance.BaseListView.Items.Remove(control);

    public void RemovePost(string postId)
    {
        foreach (Control item in BaseListView.Items)
        {
            var timelineControl = item as TimelineControl;
            if (timelineControl?.PostId == postId) BaseListView.Items.Remove(item);
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
            BaseListView.Items.Clear();
            foreach (object item in BaseListView.Items) (item as TimelineControl)?.UnloadMedia();
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
                    BaseListView.Items.Add(control);
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
                    Content = new UserProfileControl(Id),
                    Visibility = Visibility.Visible
                };
                BaseListView.Items.Add(profileFrame);
                await Task.Delay(500);
            }

            var data = await ApiHandler.GetProfileFeed(Id, from);
            foreach (var feed in data.activities)
            {
                if (IsValidFeed(feed))
                {
                    var control = new TimelineControl(feed);
                    BaseListView.Items.Add(control);
                }
            }
            LastFeedId = _lastFeedId;
            if (data.activities.Count > 15) _lastFeedId = data.activities.LastOrDefault().id;
            else _lastFeedId = null;
        }

        await Task.Delay(250);
        BaseListView.UpdateLayout();
        ValidateTimelineContent();
        PrLoading.Visibility = Visibility.Collapsed;
        bool willClearTimelineOnRefresh = (Utils.Configuration.GetValue("ClearTimelineOnRefresh") as bool?) ?? true;
        var first = BaseListView.Items.FirstOrDefault();
        if (willClearTimelineOnRefresh && first != null) BaseListView.ScrollIntoView(first);
        IsEnabled = true;
    }

    private static bool IsValidFeed(ApiHandler.DataType.CommentData.PostData feed) => feed.deleted != true && (feed.@object?.deleted ?? false) != true && feed.blinded != true && (feed.@object?.blinded ?? false) != true && (feed.verb == "post" || feed.verb == "share");

    private bool _isRefreshing = false;
    private async void OnScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_isRefreshing) return;
        ValidateTimelineContent();
        if (_lastFeedId == null) return;

        var scrollViewer = sender as ScrollViewer;
        var verticalOffset = scrollViewer.VerticalOffset;
        var maxVerticalOffset = scrollViewer.ScrollableHeight;
        if (maxVerticalOffset < 0 || verticalOffset == maxVerticalOffset)
        {
            _isRefreshing = true;
            bool willClearTimelineOnRefresh = (Utils.Configuration.GetValue("ClearTimelineOnRefresh") as bool?) ?? true;
            if (willClearTimelineOnRefresh)
            {
                Utility.ManuallyDisposeAllMedias();
                BaseListView.Items.Clear();
            }

            await Refresh(_lastFeedId);
            _isRefreshing = false;
        }
    }

    private void ValidateTimelineContent()
    {
        var scrollViewer = Utility.GetScrollViewerFromBaseListView(BaseListView);
        foreach (Control control in BaseListView.Items)
        {
            if (control is not TimelineControl) continue;
            var timelineControl = control as TimelineControl;
            if (Utility.IsVisibleToUser(control, scrollViewer) && !timelineControl.IsContentLoaded) _ = timelineControl.RefreshContent();
            else if (!Utility.IsVisibleToUser(control, scrollViewer) && timelineControl.IsContentLoaded) timelineControl.UnloadMedia();
        }
    }

    private void OnPageSizeChanged(object sender, SizeChangedEventArgs e) => ValidateTimelineContent();

}
