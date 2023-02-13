// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using KSMP.Controls.ViewModels;
using KSMP.Extension;
using KSMP.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KSMP.Controls.Extras;

public sealed partial class BatchDeleteFriendsControl : UserControl
{
    private readonly ObservableCollection<BatchDeleteFriendsViewModel> _items = new();
    public BatchDeleteFriendsControl()
    {
        InitializeComponent();
        Initialize();
    }

    private async void Initialize()
    {
        IsEnabled = false;
        var friends = await ApiHandler.GetFriends();
        var profiles = friends.profiles.OrderByDescending(x => x.is_favorite).ThenBy(x => x.display_name);
       foreach (var profile in profiles)
        {
            _items.Add(new()
            {
                ProfileUrl = profile.GetValidUserProfileUrl(),
                Name = profile.display_name,
                IsChecked = false,
                IsBlindedUser = profile.blocked == true,
                IsFavoriteUser = profile.is_favorite == true,
                UserId = profile.id
            });
        }
        LvMain.ItemsSource = _items;
        TbLoading.Visibility = Visibility.Collapsed;
        IsEnabled = true;
    }

    private void OnReverseSelectionButtonClicked(object sender, RoutedEventArgs e)
    {
        foreach (var item in _items)
            item.IsChecked = !item.IsChecked;
    }

    private void OnSelectBlindedUserButtonClicked(object sender, RoutedEventArgs e)
    {
        foreach (var item in _items) 
            if (item.IsBlindedUser)
                item.IsChecked = true;
    }

    private void OnUnselectFavoriteUserButtonClicked(object sender, RoutedEventArgs e)
    {
        foreach (var item in _items) 
            if (item.IsFavoriteUser)
                item.IsChecked = false;
    }

    private void OnExitButtonClicked(object sender, RoutedEventArgs e) => MainPage.HideOverlay(false);

    private async void OnBatchDeleteFriendsButtonClicked(object sender, RoutedEventArgs e)
    {
        var friendsToDelete = _items.Where(x => x.IsChecked).ToList();
        var result = await this.ShowMessageDialogAsync($"정말로 {friendsToDelete.Count}명의 친구를 삭제하시겠습니까?", "경고", true, "삭제");
        if (result != ContentDialogResult.Primary) return;


        var total = friendsToDelete.Count;
        var count = 0;

        GdLoading.Visibility = Visibility.Visible;
        PrMain.Minimum = 0;
        PrMain.Maximum = total;
        PrMain.IsIndeterminate = false;

        foreach (var friend in friendsToDelete)
        {
            PrMain.Value = ++count;
            TbProgress.Text = $"삭제중... ({count}/{total})명";
            await ApiHandler.DeleteFriend(friend.UserId);
        }
        await MainPage.RefreshFriendList();
        await this.ShowMessageDialogAsync($"{friendsToDelete.Count}명의 친구를 삭제하였습니다.", "안내");
        MainPage.HideOverlay(false);
    }
}
