using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using KSMP.Controls;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using static KSMP.ApiHandler.DataType.CommentData;

namespace KSMP.Pages;

public sealed partial class TimelinePage : Page
{
    private static TimelinePage s_instance;
    private string _lastFeedId = null;

    public string Id = null;
    public bool IsMyTimeline => Id == MainPage.Me.id;
    public readonly ListViewBase BaseListView;

    private readonly ObservableCollection<FrameworkElement> _items = new();
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
        BaseListView.ItemsSource = _items;

        LastFeedId = null;
    }

    public async Task InsertPostAsync(PostData feed)
    {
        if (feed == null) return;
        var first = _items.FirstOrDefault(x => x is TimelineControl) as TimelineControl;
        if (first.PostId != feed.id)
        {
            var control = new TimelineControl(feed);
            _items.Insert(_items.ToList().FindIndex(x => x is TimelineControl), control);;
            await control.RefreshContent();
            await Task.Delay(500);
            ValidateTimelineContent();
        }
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
        var list = _items.Select(x => x as TimelineControl).ToList();
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

    public static void HidePostFromTimeline(TimelineControl control) => s_instance._items.Remove(control);

    public void RemovePost(string postId)
    {
        foreach (Control item in _items.ToArray())
        {
            var timelineControl = item as TimelineControl;
            if (timelineControl?.PostId == postId) _items.Remove(item);
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
            _items.Clear();
            foreach (object item in _items) (item as TimelineControl)?.UnloadMedia();
            Utility.ManuallyDisposeAllMedias();
        }

        if (string.IsNullOrEmpty(Id))
        {
            var items = new List<FrameworkElement>();
            var data = await ApiHandler.GetFeed(from);
            foreach (var feed in data.feeds)
            {
                if (IsValidFeed(feed))
                {
                    var control = new TimelineControl(feed);
                    items.Add(control);
                }
            }
            items.ForEach(x => _items.Add(x));
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
                _items.Add(profileFrame);
                await Task.Delay(500);
            }

            var items = new List<FrameworkElement>();
            var data = await ApiHandler.GetProfileFeed(Id, from);
            foreach (var feed in data.activities)
            {
                if (IsValidFeed(feed))
                {
                    var control = new TimelineControl(feed);
                    items.Add(control);
                }
            }
            items.ForEach(x => _items.Add(x));
            
            LastFeedId = _lastFeedId;
            if (data.activities.Count > 15) _lastFeedId = data.activities.LastOrDefault().id;
            else _lastFeedId = null;
        }

        await Task.Delay(500);
        BaseListView.UpdateLayout();
        ValidateTimelineContent();
        PrLoading.Visibility = Visibility.Collapsed;
        bool willClearTimelineOnRefresh = (Utils.Configuration.GetValue("ClearTimelineOnRefresh") as bool?) ?? true;
        var first = _items.FirstOrDefault();
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
                _items.Clear();
            }

            await Refresh(_lastFeedId);
            _isRefreshing = false;
        }
    }

    public static bool WillUseDynamicTimelineLoading = (Utils.Configuration.GetValue("UseDynamicTimelineLoading") as bool?) ?? false;
    private void ValidateTimelineContent()
    {
        if (!WillUseDynamicTimelineLoading) return;
        double margin = 0;

        var scrollViewer = Utility.GetScrollViewerFromBaseListView(BaseListView);
        foreach (Control control in _items)
        {
            if (control is not TimelineControl) continue;
            var timelineControl = control as TimelineControl;
            if (Utility.IsVisibleToUser(control, scrollViewer, margin) && !timelineControl.IsContentLoaded) _ = timelineControl.RefreshContent();
            else if (!Utility.IsVisibleToUser(control, scrollViewer, margin) && timelineControl.IsContentLoaded) timelineControl.UnloadMedia();
        }
    }

    private void OnPageSizeChanged(object sender, SizeChangedEventArgs e) => ValidateTimelineContent();

}
