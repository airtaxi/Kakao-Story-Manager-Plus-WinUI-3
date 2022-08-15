using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using KSMP.Extension;
using static StoryApi.ApiHandler.DataType.CommentData;
using KSMP.Pages;
using static KSMP.ClassManager;
using System;
using Microsoft.UI.Xaml.Media;

namespace KSMP.Controls;

public sealed partial class CommentControl : UserControl
{
    public delegate void ReplyClick(Comment comment);
    public ReplyClick OnReplyClick;
    private Comment _comment;
    private readonly string _postId;
    private readonly bool _isOverlay;

    public CommentControl(Comment comment, string postId, bool isOverlay)
    {
        InitializeComponent();
        _comment = comment;
        _postId = postId;
        _isOverlay = isOverlay;
        TbName.FontSize = 13;
        TbTime.FontSize = 11.5;
        RtbContent.FontSize = 12.5;

        Refresh(comment);
    }

    private void Refresh(Comment comment)
    {
        RtbContent.Blocks.Clear();
        TbName.Text = comment.writer.display_name;
        TbTime.Text = StoryApi.Utils.GetTimeString(comment.created_at) + (comment.updated_at.Year > 1 ? " (수정됨)" : "");
        PpUser.ProfilePicture = Utility.GenerateImageUrlSource(comment.writer.GetValidUserProfileUrl());

        if (comment.liked)
            FaHeart.Visibility = Visibility.Visible;
        else
            FaHeart.Visibility = Visibility.Collapsed;

        if (comment.like_count == 0)
            SpLike.Visibility = Visibility.Collapsed;
        else
        {
            SpLike.Visibility = Visibility.Visible;
            TbLike.Text = comment.like_count.ToString();
        }
        Utility.SetTextContent(comment.decorators, RtbContent);
        var commentMedia = comment.decorators.FirstOrDefault(x => x.media?.origin_url != null);
        if (!string.IsNullOrEmpty(commentMedia?.media?.origin_url))
        {
            ImgMain.Visibility = Visibility.Visible;
            ImgMain.Source = Utility.GenerateImageUrlSource(commentMedia.media.origin_url);
            ImgMain.Tapped += (s, e) =>
            {
                var medium = new Medium
                {
                    origin_url = commentMedia?.media?.origin_url
                };
                var control = new ImageViewerControl(new List<Medium> { medium }, 0);
                Pages.MainPage.ShowOverlay(control, _isOverlay);
            };
        }
        else
            ImgMain.Visibility = Visibility.Collapsed;
    }

    private async void OnLikeButtonClick(object sender, RoutedEventArgs e)
    {
        var isSuccess = await StoryApi.ApiHandler.LikeComment(_postId, _comment.id, _comment.liked);
        if (isSuccess)
        {
            _comment = (await StoryApi.ApiHandler.GetPost(_postId)).comments.FirstOrDefault(x => x.id == _comment.id);
            Refresh(_comment);
        }
    }

    private void OnReplyTapped(object sender, TappedRoutedEventArgs e) => OnReplyClick.Invoke(_comment);
    private void OnPointerEntered(object sender, PointerRoutedEventArgs e) => Utility.ChangeCursor(true);
    private void OnPointerExited(object sender, PointerRoutedEventArgs e) => Utility.ChangeCursor(false);

    private void UserProfilePictureTapped(object sender, TappedRoutedEventArgs e) => MainPage.ShowProfile(_comment.writer.id);

    private async void LikeListTapped(object sender, TappedRoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        var likeList = new FriendListControl();

        var friendProfiles = new List<FriendProfile>();
        var likes = await StoryApi.ApiHandler.GetCommentLikes(_postId, _comment.id);

        foreach(var like in likes)
        {
            var actor = like.actor;
            friendProfiles.Add(new()
            {
                Id = actor.id,
                Name = actor.display_name,
                ProfileUrl = actor.GetValidUserProfileUrl()
            });
        }

        likeList.SetSource(friendProfiles);

        likeList.OnFriendSelected += (profile) => MainPage.ShowProfile(profile.Id);

        var flyout = new Flyout();
        flyout.Content = likeList;
        flyout.ShowAt(element);
    }
}
