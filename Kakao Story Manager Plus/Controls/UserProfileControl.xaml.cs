﻿using System.Threading.Tasks;
using KSMP.Extension;
using KSMP.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace KSMP.Controls;

public sealed partial class UserProfileControl : UserControl
{
    private readonly string _id;
    private string _name;

    public UserProfileControl(string id, bool isOverlay = false)
    {
        InitializeComponent();
        _id = id;
        _ = Refresh();

        var manager = UserTagManager.GetUserTagManager(_id);

        var history = manager.GetNicknameHistory();
        if (history.Count == 0) history.Add("닉네임 기록 없음");
        LvNicknameHistory.ItemsSource = history;

        TbxMemo.Text = manager.GetMemo();
        TbxCustomNickname.Text = manager.GetCustomNickname();

        if (isOverlay)
        {
            TbName.MaxWidth = 180;
            TbDescription.MaxWidth = 180;
        }
    }

    public void IndicateFavorite(bool isFavorite)
    {
        SolidColorBrush gray7 = Utility.GetSolidColorBrushFromHexString("#FF808080");

        SolidColorBrush white6;
        var requestedTheme = Utility.GetRequestedTheme();
        if (requestedTheme == ElementTheme.Light) white6 = Utility.GetSolidColorBrushFromHexString("#FFD3D3D3");
        else white6 = Utility.GetSolidColorBrushFromHexString("#FF888888");

        if (!isFavorite)
        {
            RtFavorite.Fill = gray7;
            FaFavorite.Foreground = white6;
        }
        else
        {
            RtFavorite.Fill = Utility.GetSolidColorBrushFromHexString("#FFD15F4E");
            FaFavorite.Foreground = Utility.GetSolidColorBrushFromHexString("#FFD7D7D7");
        }
    }

    public async Task SetFavorite()
    {
        var relationship = await ApiHandler.GetProfileRelationship(_id);
        await ApiHandler.RequestFavorite(_id, relationship.is_favorite);
        relationship = await ApiHandler.GetProfileRelationship(_id);
        IndicateFavorite(relationship.is_favorite);
        await MainPage.RefreshFriendList();
    }
    public async Task Refresh()
    {
        var isMe = _id == MainPage.Me.id;
        var user = await ApiHandler.GetProfileFeed(_id, null, true);
        var profile = user.profile;
        if (profile.blocked)
        {
            TbName.Text = $"제한된 사용자";
            TbDescription.Text = $"사용자 ID: {_id}";
            BtFriend.Visibility = Visibility.Collapsed;
            return;
        }

        Utility.SetPersonPictureUrlSource(PpProfilePicture, user.profile.GetValidUserProfileUrl());
        Utility.SetImageUrlSource(ImgProfileBackground, user.profile.bg_image_url);

        Utility.LoadedImages.Add(ImgProfileBackground);
        Utility.LoadedPersonPictures.Add(PpProfilePicture);

        _name = profile.display_name;
        RefreshUserName();

        string profileMessage = "";
        if ((profile.status_objects?.Count ?? 0) > 0)
            profileMessage = profile.status_objects?[0]?.message;

        string additionalDescription;
        if (isMe)
            additionalDescription = $"친구 {MainPage.Friends.profiles.Count}명";
        else
            additionalDescription = user.mutual_friend?.message ?? "함께 아는 친구 없음";

        additionalDescription += $" / {user.profile.activity_count}개의 스토리";

        if (string.IsNullOrEmpty(profileMessage))
            TbDescription.Text = additionalDescription;
        else
            TbDescription.Text = $"{profileMessage}\n" + additionalDescription;

        if (user.profile.relationship != "F")
            GdFavorite.Visibility = Visibility.Collapsed;
        else
            IndicateFavorite(profile.is_favorite);

        if (isMe)
        {
            BtFriend.Visibility = Visibility.Collapsed;
            SpUserTag.Visibility = Visibility.Collapsed;
        }

        RefreshFriendStatus(profile.relationship);
    }

    private void RefreshUserName()
    {
        var manager = UserTagManager.GetUserTagManager(_id);
        var customNickname = manager.GetCustomNickname();

        if (!string.IsNullOrEmpty(customNickname)) TbName.Text = $"{_name} ({customNickname})";
        else TbName.Text = _name;
    }

    private void RefreshFriendStatus(string relationship)
    {
        if (relationship == "F")
            TbFriendStatus.Text = "친구 삭제";
        else if (relationship == "R")
            TbFriendStatus.Text = "친구 신청 취소";
        else if (relationship == "C")
            TbFriendStatus.Text = "친구 신청 수락/거절";
        else if (relationship == "N")
            TbFriendStatus.Text = "친구 신청";
        else
            TbFriendStatus.Text = "!오류!";
    }


    private async void OnFavoriteTapped(object sender, TappedRoutedEventArgs e) => await SetFavorite();

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e) => Utility.ChangeSystemMouseCursor(true);
    private void OnPointerExited(object sender, PointerRoutedEventArgs e) => Utility.ChangeSystemMouseCursor(false);

    private async void FriendButtonClicked(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        button.IsEnabled = false;

        var profileRelationship = await ApiHandler.GetProfileRelationship(_id);
        bool shouldRefresh = false;
        if (profileRelationship.relationship == "F")
        {
			var result = await Utility.ShowMessageDialogAsync("친구를 삭제하시겠습니까?", "경고", true);
            if (result == ContentDialogResult.Primary)
            {
                await ApiHandler.DeleteFriend(profileRelationship.id);
                shouldRefresh = true;
            }
        }
        else if (profileRelationship.relationship.Equals("R"))
        {
			var result = await Utility.ShowMessageDialogAsync("친구 신청을 취소하시겠습니까?", "경고", true);
            if (result == ContentDialogResult.Primary)
            {
                await ApiHandler.RequestFriend(profileRelationship.id, true);
                shouldRefresh = true;
            }
        }
        else if (profileRelationship.relationship.Equals("C"))
        {
            var result = await Utility.ShowMessageDialogAsync("친구 신청을 수락하시겠습니까?", "안내", true, "수락", "거절");
            if (result == ContentDialogResult.Primary)
            {
                await ApiHandler.AcceptFriendRequest(profileRelationship.id, false);
                shouldRefresh = true;
            }
            else
            {
                await ApiHandler.AcceptFriendRequest(profileRelationship.id, true);
                shouldRefresh = true;
            }
        }
        else if (profileRelationship.relationship.Equals("N"))
        {
            var result = await Utility.ShowMessageDialogAsync("친구 신청을 보내시겠습니까?", "안내", true);
			if (result == ContentDialogResult.Primary)
            {
                await ApiHandler.RequestFriend(profileRelationship.id, false);
                shouldRefresh = true;
            }
        }

        if (shouldRefresh)
        {
            profileRelationship = await ApiHandler.GetProfileRelationship(_id);
            RefreshFriendStatus(profileRelationship.relationship);
            MainPage.ShowProfile(_id);
            await MainPage.RefreshFriendList();
        }

        button.IsEnabled = true;
    }

    private async void OnProfilePictureTapped(object sender, TappedRoutedEventArgs e)
    {
        var popup = (Parent as FlyoutPresenter)?.Parent as Popup;
        if (popup != null) popup.IsOpen = false;
        await Task.Delay(100);
        MainPage.ShowProfile(_id);
    }

    private void OnSaveMemoButtonClicked(object sender, RoutedEventArgs e)
    {
        var manager = UserTagManager.GetUserTagManager(_id);
        manager.SetMemo(TbxMemo.Text);
        FlMemo.Hide();
    }

    private void OnSetCustomUserNicknameButtonClicked(object sender, RoutedEventArgs e)
    {
        var manager = UserTagManager.GetUserTagManager(_id);
        manager.SetCustomNickname(TbxCustomNickname.Text);
        RefreshUserName();
        FlCustomNickname.Hide();
    }
}
