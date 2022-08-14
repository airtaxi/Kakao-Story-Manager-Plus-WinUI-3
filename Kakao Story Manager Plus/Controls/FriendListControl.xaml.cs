using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static KSMP.ClassManager;

namespace KSMP.Controls;

public sealed partial class FriendListControl : UserControl
{
    public int MaxItems { get; set; } = 10;
    public delegate void OnSelected(FriendProfile profile);
    public OnSelected OnFriendSelected;
    public FriendListControl()
    {
        InitializeComponent();
    }

    public void ShowLoading() => GdLoading.Visibility = Visibility.Visible;
    public void HideLoading() => GdLoading.Visibility = Visibility.Collapsed;

    public void SetSource(List<FriendProfile> source)
    {
        var grid = Content as Grid;
        var listView = LvFriends;
        if (source.Count == 0)
            grid.Visibility = Visibility.Collapsed;
        else
        {
            var max = Math.Min(source.Count, MaxItems);
            source = source.GetRange(0, max);
            grid.Visibility = Visibility.Visible;
            listView.ItemsSource = source;
        }
    }

    public int SearchFriendList(string nameQuery)
    {
        var listView = LvFriends;
        if (string.IsNullOrEmpty(nameQuery))
            listView.Visibility = Visibility.Collapsed;
        else
        {
            listView.Visibility = Visibility.Visible;
            var source = Pages.MainPage.Friends.profiles.Where(x => x.display_name.ToLower().Contains(nameQuery.ToLower())).Select(x => new FriendProfile { Id = x.id, Name = x.display_name, ProfileUrl = x.profile_video_url_square_small ?? x.profile_thumbnail_url }).ToList();
            SetSource(source);
            return source.Count;
        }
        return 0;
    }

    private void SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var listView = LvFriends;
        var friendProfile = listView.SelectedItem as FriendProfile;
        if (friendProfile == null) return;
        OnFriendSelected?.Invoke(friendProfile);
        listView.SelectedItem = null;
    }
}
