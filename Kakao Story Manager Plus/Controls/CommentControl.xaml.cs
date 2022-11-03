using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using KSMP.Extension;
using static KSMP.ApiHandler.DataType.CommentData;
using KSMP.Pages;
using static KSMP.ClassManager;
using KSMP;
using System.Threading.Tasks;
using KSMP.Utils;

namespace KSMP.Controls;

public sealed partial class CommentControl : UserControl
{
    public delegate void ReplyClick(Comment comment);
    public delegate void Deleted();
    public ReplyClick OnReplyClicked;
    public Deleted OnDeleted;
    public TaskCompletionSource LoadCommentCompletionSource { get; } = new();
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
        TbTime.Text = Api.Story.Utils.GetTimeString(comment.created_at) + (comment.updated_at.Year > 1 ? " (수정됨)" : "");
        Utility.SetPersonPictureUrlSource(PpUser, comment.writer.GetValidUserProfileUrl());
        Utility.LoadedPersonPictures.Add(PpUser);

        if (comment.liked) MfiLike.Text = "좋아요 취소";
        else MfiLike.Text = "좋아요";

        if (comment.like_count == 0)
            SpLike.Visibility = Visibility.Collapsed;
        else
        {
            SpLike.Visibility = Visibility.Visible;
            TbLike.Text = comment.like_count.ToString();
        }
        Utils.Post.SetTextContent(comment.decorators, RtbContent);
        var commentMedia = comment.decorators.FirstOrDefault(x => x.media?.thumbnail_url != null);
        if (!string.IsNullOrEmpty(commentMedia?.media?.origin_url))
        {
            ImgMain.Visibility = Visibility.Visible;

            bool willUseGifInTimeline = (Configuration.GetValue("UseGifInTimeline") as bool?) ?? false;
            var url = willUseGifInTimeline ? commentMedia.media.origin_url : commentMedia.media.thumbnail_url;
            Utility.SetImageUrlSource(ImgMain, url);

            ImgMain.Tapped += (s, e) =>
            {
                e.Handled = true;
                var medium = new Medium
                {
                    origin_url = commentMedia?.media?.origin_url
                };
                var control = new ImageViewerControl(new List<Medium> { medium }, 0);
                MainPage.ShowOverlay(control, _isOverlay);
            };
        }
        else
            ImgMain.Visibility = Visibility.Collapsed;
        LoadCommentCompletionSource.TrySetResult();
    }

    private async void OnLikeButtonClick(object sender, RoutedEventArgs e)
    {
        var comment = await ApiHandler.LikeComment(_postId, _comment.id, _comment.liked);
        if (comment != null)
        {
            comment.decorators = _comment.decorators;
            _comment = comment;
            Refresh(_comment);
        }
    }

    private void OnReplyTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
        OnReplyClicked.Invoke(_comment);
    }

    private void UserProfilePictureTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
        MainPage.ShowProfile(_comment.writer.id);
    }

    private async void LikeListTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
        var element = sender as FrameworkElement;
        var likeList = new FriendListControl();

        var friendProfiles = new List<FriendProfile>();
        var likes = await ApiHandler.GetCommentLikes(_postId, _comment.id);

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

    public void HideUnaccessableButton(bool isMyComment, bool isMyPost)
    {
        if (!isMyComment)
            MfiEdit.Visibility = Visibility.Collapsed;

        if (!isMyComment && !isMyPost)
            MfiDelete.Visibility = Visibility.Collapsed;
    }

    private async Task PublishEditedComment()
    {
        FrEditCommentParent.IsEnabled = false;
        PrrEditComment.Visibility = Visibility.Visible;
        var inputControl = FrEditComment.Content as InputControl;
        var quotas = inputControl.GetQuoteDatas();
        var text = string.Join(' ', quotas.Select(x => x.text));
        var comment = await ApiHandler.EditComment(_comment, _postId, quotas, text);
        _comment = comment;
        Refresh(_comment);
        PrrEditComment.Visibility = Visibility.Collapsed;
        FrEditCommentParent.Visibility = Visibility.Collapsed;
        FrEditCommentParent.IsEnabled = true;
    }
    
    private async void OnPublishEditCommentButtonClicked(object sender, RoutedEventArgs e) => await PublishEditedComment();
    private async void OnEditCommentButtonClicked(object sender, RoutedEventArgs e)
    {
        FrEditCommentParent.Visibility = Visibility.Visible;
        var inputControl = new InputControl("댓글 수정...");
        inputControl.AcceptReturn(true);
        inputControl.WrapText(true);
        inputControl.SetMaxHeight(100);
        FrEditComment.Content = inputControl;
        var text = Api.Story.Utils.GetStringFromQuoteData(_comment.decorators, true);
        inputControl.GetTextBox().Text = text;
        await Task.Delay(10);
        inputControl.FocusTextBox();
        inputControl.OnSubmitShortcutActivated += async () => await PublishEditedComment();
    }
    private async void OnDeleteCommentButtonClicked(object sender, RoutedEventArgs e)
    {
        var result = await this.ShowMessageDialogAsync("정말로 댓글을 지우시겠습니까?", "경고", true);
        if(result == ContentDialogResult.Primary)
        {
            await ApiHandler.DeleteComment(_comment.id, _postId);
            OnDeleted?.Invoke();
        }
    }
    private void OnReplyUserCommentButtonClicked(object sender, RoutedEventArgs e) => OnReplyClicked.Invoke(_comment);

    private void OnCommentMenuTapped(object sender, TappedRoutedEventArgs e) => e.Handled = true;
}
