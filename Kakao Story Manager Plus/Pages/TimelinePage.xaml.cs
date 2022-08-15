using StoryApi;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace KSMP.Pages;

public sealed partial class TimelinePage : Page
{
    public string Id = null;
    private static TimelinePage _instance;
    public bool IsMyTimeline => Id == MainPage.Me.id;

    public TimelinePage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        System.GC.Collect();
    }
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        System.GC.Collect();
        string id = e.Parameter as string;
        MainPage.SelectFriend(id);

        Id = id;
        _instance = this;
        await Refresh();
        Border border = VisualTreeHelper.GetChild(LvContent, 0) as Border;
        ScrollViewer scrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
        scrollViewer.ViewChanged += OnScrollViewerViewChanged;
    }


    public static void HidePostFromTimeline(Controls.TimelineControl control) => _instance.LvContent.Items.Remove(control);

    string lastFeed = null;
    public static async Task Refresh() => await _instance.Refresh();
    public async Task RemovePost(string postId)
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
        PrLoading.Visibility = Visibility.Visible;
            LvContent.Items.Clear();
        //if (from == null)
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
