using System;
using System.Threading.Tasks;
using KSMP.Extension;
using KSMP.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace KSMP.Controls;

public sealed partial class UserProfileControl : UserControl
{
    private readonly string _id;

    public UserProfileControl(string id)
    {
        InitializeComponent();
        _id = id;
        _ = Refresh();
    }

    public void IndicateFavorite(bool isFavorite)
    {
        if (!isFavorite)
        {
            RtFavorite.Fill = Utility.GetColorFromHexa("#FF808080");
            FaFavorite.Foreground = Utility.GetColorFromHexa("#FFD3D3D3");
        }
        else
        {
            RtFavorite.Fill = Utility.GetColorFromHexa("#FFD15F4E");
            FaFavorite.Foreground = Utility.GetColorFromHexa("#FFD7D7D7");
        }
    }

    public async Task SetFavorite()
    {
        var relationship = await StoryApi.ApiHandler.GetProfileRelationship(_id);
        await StoryApi.ApiHandler.RequestFavorite(_id, relationship.is_favorite);
        relationship = await StoryApi.ApiHandler.GetProfileRelationship(_id);
        IndicateFavorite(relationship.is_favorite);
    }
    public async Task Refresh()
    {
        var isMe = _id == MainPage.Me.id;
        var user = await StoryApi.ApiHandler.GetProfileFeed(_id, null, true);
        var profile = user.profile;

        PpProfilePicture.Loaded += (s, e) => PpProfilePicture.ProfilePicture = Utility.GenerateImageUrlSource(user.profile.GetValidUserProfileUrl());
        PpProfilePicture.Unloaded += (s, e) => PpProfilePicture.DisposeImage();

        ImgProfileBackground.Loaded += (s, e) => ImgProfileBackground.Source = Utility.GenerateImageUrlSource(user.profile.bg_image_url);
        ImgProfileBackground.Unloaded += (s, e) => PpProfilePicture.DisposeImage();

        TbName.Text = profile.display_name;

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
            BtFriend.Visibility = Visibility.Collapsed;

        RefreshFriendStatus(profile.relationship);
    }

    private void RefreshFriendStatus(string relationship)
    {
        if (relationship == "F")
            TbFriendStatus.Text = "친구 삭제";
        else if (relationship == "R")
            TbFriendStatus.Text = "친구 신청 취소";
        else if (relationship == "C")
            TbFriendStatus.Text = "친구 신청 수락";
        else if (relationship == "N")
            TbFriendStatus.Text = "친구 신청";
        else
            TbFriendStatus.Text = "!오류!";
    }


    private async void OnFavoriteTapped(object sender, TappedRoutedEventArgs e) => await SetFavorite();

    private void FavoritePointerEntered(object sender, PointerRoutedEventArgs e) => Utility.ChangeCursor(true);

    private void FavoritePointerExited(object sender, PointerRoutedEventArgs e) => Utility.ChangeCursor(false);

    private async void FriendButtonClicked(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        button.IsEnabled = false;

        var profileRelationship = await StoryApi.ApiHandler.GetProfileRelationship(_id);
        bool shouldRefresh = false;
        if (profileRelationship.relationship == "F")
        {
            var dialog = this.GenerateMessageDialog("친구를 삭제하시겠습니까?", "경고", true);
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await StoryApi.ApiHandler.DeleteFriend(profileRelationship.id);
                shouldRefresh = true;
            }
        }
        else if (profileRelationship.relationship.Equals("R"))
        {
            var dialog = this.GenerateMessageDialog("친구 신청을 취소하시겠습니까?", "경고", true);
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await StoryApi.ApiHandler.RequestFriend(profileRelationship.id, true);
                shouldRefresh = true;
            }
        }
        else if (profileRelationship.relationship.Equals("C"))
        {
            var dialog = this.GenerateMessageDialog("친구 신청을 수락하시겠습니까?", "안내", true);
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await StoryApi.ApiHandler.AcceptFriendRequest(profileRelationship.id, false);
                shouldRefresh = true;
            }
        }
        else if (profileRelationship.relationship.Equals("N"))
        {
            var dialog = this.GenerateMessageDialog("친구 신청을 보내시겠습니까?", "안내", true);
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await StoryApi.ApiHandler.RequestFriend(profileRelationship.id, false);
                shouldRefresh = true;
            }
        }

        if (shouldRefresh)
        {
            profileRelationship = await StoryApi.ApiHandler.GetProfileRelationship(_id);
            RefreshFriendStatus(profileRelationship.relationship);
            MainPage.ShowProfile(_id);
        }

        button.IsEnabled = true;
    }

    private void OnProfilePictureTapped(object sender, TappedRoutedEventArgs e) => MainPage.ShowProfile(_id);
}
