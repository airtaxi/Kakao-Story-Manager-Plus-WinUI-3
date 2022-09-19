using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using KSMP.Extension;
using static StoryApi.ApiHandler.DataType.CommentData;
using static KSMP.ClassManager;
using System;
using KSMP.Pages;
using static StoryApi.ApiHandler.DataType.ShareData;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Newtonsoft.Json;
using System.IO;
using Windows.System;
using StoryApi;
using Windows.Storage;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using static StoryApi.ApiHandler.DataType.TimeLineData;
using System.Diagnostics;
using Windows.Security.Authentication.OnlineId;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Media.Import;

namespace KSMP.Controls;


public sealed partial class TimelineControl : UserControl, IDisposable
{
    private PostData _post;
    private readonly bool _isOverlay;
    private readonly bool _isShare;

    private bool isBookmarking = false;
    private ApiHandler.DataType.UploadedImageProp _commentMedia = null;

    public string PostId => _post?.id;

    public TimelineControl(PostData post, bool isShare = false, bool isOverlay = false)
    {
        InitializeComponent();
        _post = post;
        _isOverlay = isOverlay;
        _isShare = isShare;
        if (isOverlay)
        {
            FaClose.Visibility = Visibility.Visible;
            BdCommentsHorizontal.Visibility = Visibility.Visible;
            RdComment.Height = new GridLength(1, GridUnitType.Star);
            Grid.SetRow(GdComment, 0);
            Grid.SetRowSpan(GdComment, 5);
            Grid.SetColumn(GdComment, 1);
            SvComments.Padding = new Thickness(5);
            SvComments.MaxHeight = double.MaxValue;
            SvComments.VerticalAlignment = VerticalAlignment.Stretch;

            var commentFrameMargin = FrComment.Margin;
            commentFrameMargin.Left = 10;
            FrComment.Margin = commentFrameMargin;

            var sendCommentButtonMargin = BtSendComment.Margin;
            sendCommentButtonMargin.Right = 10;
            BtSendComment.Margin = sendCommentButtonMargin;
        }
        else CdComment.Width = new GridLength(0);

        if (isShare)
        {
            GdComment.Visibility = Visibility.Collapsed;
            SpEmotions.Visibility = Visibility.Collapsed;
            SpMenu.Visibility = Visibility.Collapsed;
            SpShare.Visibility = Visibility.Visible;
            FrShareMargin.Visibility = Visibility.Visible;
            BdShare.Visibility = Visibility.Visible;
            SvContent.Padding = new Thickness(20, 0, 20, 20);
            GdMain.Margin = new Thickness(0);

            TbShareCount.Text = post.share_count.ToString();
        }

        if (post.@object != null && post.@object.id != null)
            FrShare.Content = new TimelineControl(post.@object, true);
        if (post.scrap != null)
            FrLink.Content = new LinkControl(post.scrap);

        Initialize();
        var inputControl = new InputControl("댓글을 입력하세요.");
        inputControl.AcceptReturn(true);
        inputControl.SetMaxHeight(120);
        inputControl.WrapText(true);
        inputControl.OnSubmitShortcutActivated += OnSubmitShortcutActivated;
        inputControl.OnImagePasted += OnImagePasted;
        FrComment.Content = inputControl;
        _ = RefreshContent();

        PpUser.Loaded += (s, e) => PpUser.ProfilePicture = Utility.GenerateImageUrlSource(post.actor?.GetValidUserProfileUrl());
        PpUser.Unloaded += (s, e) => PpUser.DisposeImage();

        Loaded += (s, e) => FvMedia.ItemsSource = Utility.GenerateMedias(post?.media?.Select(x => x.origin_url ?? x.url_hq));
        Unloaded += (s, e) => UnloadFlipViewImages();
    }

    private void UnloadFlipViewImages()
    {
        var images = FvMedia.ItemsSource as List<Image>;
        images?.ForEach(x => x.DisposeImage());
        FvMedia.ItemsSource = null;
    }

    public void Dispose()
    {
        UnloadFlipViewImages();
        var inputControl = FrComment.Content as InputControl;
        inputControl.OnSubmitShortcutActivated -= OnSubmitShortcutActivated;
        inputControl.OnImagePasted -= OnImagePasted;
        BtShare.Flyout = null;
        FrOverlay.Content = null;
        GdOverlay.Children.Clear();
    }

    private async void OnImagePasted(string temporaryImageFilePath)
    {
        var file = await StorageFile.GetFileFromPathAsync(temporaryImageFilePath);
        await UploadCommentImageFile(file);
    }

    private void OnSubmitShortcutActivated() => OnSendCommentButtonClicked(BtSendComment, null);

    private void RefreshBookmarkButton() => FiFavorite.Glyph = _post.bookmarked ? "\ue735" : "\ue734";
    public void HideEmotionsButtonFlyout() => BtEmotions.Flyout.Hide();
    public void RefreshEmotionsButton()
    {
        if (_post.liked)
        {
            var emotion = _post.liked_emotion;
            if (emotion == "like")
            {
                BtEmotions.Background = Utils.Common.GetColorFromHexa("#FFE25434");
                FiEmotions.Foreground = Utils.Common.GetColorFromHexa("#FFFFFFFF");
                FiEmotions.Glyph = "\xeb52";
            }
            else if (emotion == "good")
            {
                BtEmotions.Background = Utils.Common.GetColorFromHexa("#FFBCCB3C");
                FiEmotions.Foreground = Utils.Common.GetColorFromHexa("#FFFFFFFF");
                FiEmotions.Glyph = "\ue735";
            }
            else if (emotion == "pleasure")
            {
                BtEmotions.Background = Utils.Common.GetColorFromHexa("#FFEFBD30");
                FiEmotions.Foreground = Utils.Common.GetColorFromHexa("#FFFFFFFF");
                FiEmotions.Glyph = "\ued54";
            }
            else if (emotion == "sad")
            {
                BtEmotions.Background = Utils.Common.GetColorFromHexa("#FF359FB0");
                FiEmotions.Foreground = Utils.Common.GetColorFromHexa("#FFFFFFFF");
                FiEmotions.Glyph = "\ueb42";
            }
            else if (emotion == "cheerup")
            {
                BtEmotions.Background = Utils.Common.GetColorFromHexa("#FF9C62AE");
                FiEmotions.Foreground = Utils.Common.GetColorFromHexa("#FFFFFFFF");
                FiEmotions.Glyph = "\ue945";
            }
        }
        else
        {
            BtEmotions.Background = Utils.Common.GetColorFromHexa("#00000000");
            FiEmotions.Foreground = Utils.Common.GetColorFromHexa("#FF888D94");
            FiEmotions.Glyph = "\ueb52";
        }
    }
    private void RefreshUpButton()
    {
        if (_post.sympathized)
        {
            BtUp.Background = Utils.Common.GetColorFromHexa("#FF838383");
            FaUp.Foreground = Utils.Common.GetColorFromHexa("#FFFFFFFF");
        }
        else
        {
            BtUp.Background = Utils.Common.GetColorFromHexa("#00000000");
            FaUp.Foreground = Utils.Common.GetColorFromHexa("#FF888D94");
        }
    }
    private void Initialize()
    {
        var emotionsFlyout = new Flyout
        {
            Content = new EmotionsControl(_post, this)
        };
        BtEmotions.Flyout = emotionsFlyout;
        BtEmotions.Flyout.Opening += async (s, e) =>
        {
            if (_post.liked)
            {
                await ApiHandler.LikePost(_post.id, null);
                await RefreshContent();
                HideEmotionsButtonFlyout();
            }
        };
        BtEmotions.Flyout.Placement = FlyoutPlacementMode.TopEdgeAlignedLeft;

        var shareFlyout = new MenuFlyout();
        var sharePostMenuFlyoutItem = new MenuFlyoutItem() { Text = "스토리로 공유" };
        var sharePostCommand = new XamlUICommand();
        sharePostCommand.ExecuteRequested += OnSharePost;
        sharePostMenuFlyoutItem.Command = sharePostCommand;
        shareFlyout.Items.Add(sharePostMenuFlyoutItem);

        if (!_post.sharable || _post.@object != null) 
            sharePostMenuFlyoutItem.Visibility = Visibility.Collapsed;

        var copyUrlPostMenuFlyoutItem = new MenuFlyoutItem() { Text = $"URL 복사하기" };
        var copyUrlCommand = new XamlUICommand();
        copyUrlCommand.ExecuteRequested += CopyPostUrl;
        copyUrlPostMenuFlyoutItem.Command = copyUrlCommand;
        shareFlyout.Items.Add(copyUrlPostMenuFlyoutItem);
        BtShare.Flyout = shareFlyout;

    }

    private async void OnSharePost(XamlUICommand sender, ExecuteRequestedEventArgs args) => await SharePost();

    public async Task SharePost()
    {
        if (!_post.sharable || _post.@object != null) return;

        GdOverlay.Visibility = Visibility.Visible;
        var control = new WritePostControl(_post);
        FrOverlay.Content = control;
        control.OnPostCompleted += OnPostCompleted;
        await Task.Delay(10); // Bugfix
        control.FocusTextbox();
    }

    private async void OnPostCompleted()
    {
        HideOverlay();
        await RefreshContent();
    }

    private void HideOverlay()
    {
        GdOverlay.Visibility = Visibility.Collapsed;
        FrOverlay.Content = null;
    }

    private async void CopyPostUrl(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        string userId = _post.actor.id;
        string postId = _post.id.Split(new string[] { "." }, StringSplitOptions.None)[1];
        string postUrl = "https://story.kakao.com/" + userId + "/" + postId;
        var dataPackage = new DataPackage();
        dataPackage.SetText(postUrl);
        Clipboard.SetContent(dataPackage);
        await this.ShowMessageDialogAsync("포스트의 URL이 클립보드에 복사되었습니다", "안내");
    }

    public async Task RefreshContent(bool showLoading = true)
    {
        if (showLoading) GdLoading.Visibility = Visibility.Visible;
        SpPostInformation.Visibility = Visibility.Collapsed;
        if (!_isShare) _post = await ApiHandler.GetPost(_post.id);

        TbName.Text = _post.actor.display_name;
        var timestampString = StoryApi.Utils.GetTimeString(_post.created_at);
        TbTime.Text = timestampString;
        if(_post.content_updated_at != DateTime.MinValue)
            TbTime.Text += " (수정됨)";

        RtbEmotions.Visibility = Visibility.Visible;
        RtbShares.Visibility = Visibility.Visible;
        RtbUps.Visibility = Visibility.Visible;

        int commentCount = _post.comments?.Count ?? 0;
        if (commentCount > 0)
        {
            RtbComments.Visibility = Visibility.Visible;
            SvComments.Visibility = Visibility.Visible;
            BdComments.Visibility = Visibility.Visible;
            RnComments.Text = _post.comment_count.ToString();

            SpComments.Children.Clear();
            var comments = await ApiHandler.GetComments(_post.id, null);
            SvComments.Visibility = Visibility.Visible;
            BdComments.Visibility = Visibility.Visible;
            var lastComment = comments.Last();
            foreach (var comment in comments)
            {
                var control = new CommentControl(comment, _post.id, _isOverlay);
                
                control.OnReplyClick += (Comment sender) =>
                {
                    var profile = sender.writer;
                    var inputContol = FrComment.Content as InputControl;
                    inputContol.AppendText("{!{{" + "\"id\":\"" + profile.id + "\", \"type\":\"profile\", \"text\":\"" + profile.display_name + "\"}}!} ");
                };

                control.OnDeleted += async () => await RefreshContent();


                bool isMyComment = comment.writer.id == MainPage.Me.id;
                bool isMyPost = _post.actor.id == MainPage.Me.id;
                control.HideUnaccessableButton(isMyComment, isMyPost);

                SpComments.Children.Add(control);

                if (lastComment != comment)
                {
                    var border = new Border()
                    {
                        BorderBrush = Utility.GetColorFromHexa("#FFF2F2F2"),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        BorderThickness = new Thickness(0.5),
                    };
                    SpComments.Children.Add(border);
                }
            }
            await Task.Delay(100);
            SvComments.UpdateLayout();
            SvComments.ChangeView(0.0, double.MaxValue, 1.0f, true);
        }
        else
        {
            RtbComments.Visibility = Visibility.Collapsed;
            SvComments.Visibility = Visibility.Collapsed;
            BdComments.Visibility = Visibility.Collapsed;
        }

        if (_post.like_count > 0) RnEmotions.Text = _post.like_count.ToString();
        else RtbEmotions.Visibility = Visibility.Collapsed;

        var shareCount = _post.share_count - _post.sympathy_count;
        if (shareCount > 0) RnShares.Text = shareCount.ToString();
        else RtbShares.Visibility = Visibility.Collapsed;

        if (_post.sympathy_count > 0) RnUps.Text = _post.sympathy_count.ToString();
        else RtbUps.Visibility = Visibility.Collapsed;

        if (_isShare || (RtbComments.Visibility == Visibility.Collapsed && RtbEmotions.Visibility == Visibility.Collapsed
            && RtbShares.Visibility == Visibility.Collapsed && RtbUps.Visibility == Visibility.Collapsed))
            SpPostInformation.Visibility = Visibility.Collapsed;
        else SpPostInformation.Visibility = Visibility.Visible;

        if (_isOverlay)
            SpPostInformation.Padding = new Thickness(0, 5, 0, 5);

        if ((_post.media?.Count ?? 0) > 0) FvMedia.Visibility = Visibility.Visible;
        else FvMedia.Visibility = Visibility.Collapsed;

        Utils.Post.SetTextContent(_post.content_decorators, RTbContent);

        RefreshUpButton();
        RefreshBookmarkButton();
        RefreshEmotionsButton();

        if (showLoading) GdLoading.Visibility = Visibility.Collapsed;
    }

    private void OnDotMenuTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;

        var icon = sender as FontIcon;
        var flyout = new MenuFlyout();
        if(_post.actor.id != MainPage.Me.id)
        {
            var menuHidePost = new MenuFlyoutItem() { Text = "이 글 숨기기" };
            menuHidePost.Click += async (o, e2) =>
            {
                await ApiHandler.HidePost(_post.id);
                MainPage.HideOverlay();
                MainPage.GetTimelinePage()?.RemovePost(_post.id);
            };
            flyout.Items.Add(menuHidePost);
        }
        else
        {
            var menuDeletePost = new MenuFlyoutItem() { Text = "글 삭제하기" };
            menuDeletePost.Click += OnDeletePost;
            flyout.Items.Add(menuDeletePost);

            var menuEditPost = new MenuFlyoutItem() { Text = "글 수정하기" };
            menuEditPost.Click += OnEditPost;
            flyout.Items.Add(menuEditPost);
        }
        var menuBlockProfile = new MenuFlyoutItem() { Text = _post.actor.is_feed_blocked ? $"'{_post.actor.display_name}' 글 받기" : $"'{_post.actor.display_name}' 글 안받기" };
        menuBlockProfile.Click += async (o, e2) =>
        {
            await ApiHandler.BlockProfile(_post.actor.id, _post.actor.is_feed_blocked);
            await RefreshContent();
        };
        flyout.Items.Add(menuBlockProfile);
        var menuMutePost = new MenuFlyoutItem() { Text = _post.push_mute ? "이 글 알림 받기" : "이 글 알림 받지 않기" };
        menuMutePost.Click += async (o, e2) =>
        {
            await ApiHandler.MutePost(_post.id, !_post.push_mute);
            await RefreshContent();
        };
        flyout.Items.Add(menuMutePost);
        flyout.ShowAt(icon);
    }

    private async void OnDeletePost(object sender, RoutedEventArgs e) => await DeletePost();
    private async void OnEditPost(object sender, RoutedEventArgs e) => await EditPost();

    public async Task DeletePost()
    {
        if (_post.actor.id != MainPage.Me.id) return;
        var result = await this.ShowMessageDialogAsync("정말로 글을 삭제하실건가요?", "경고", true);
        if (result != ContentDialogResult.Primary) return;
        await ApiHandler.DeletePost(_post.id);
        MainPage.HideOverlay();
        MainPage.GetTimelinePage()?.RemovePost(_post.id);
    }

    public async Task EditPost()
    {
        if (_post.actor.id != MainPage.Me.id) return;
        GdLoading.Visibility = Visibility.Visible;
        GdOverlay.Visibility = Visibility.Visible;
        var control = new WritePostControl();
        await control.SetEditMedia(_post);
        FrOverlay.Content = control;
        control.OnPostCompleted += OnPostCompleted;
        await Task.Delay(10);
        control.FocusTextbox();
        GdLoading.Visibility = Visibility.Collapsed;
    }

    private async void OnRefreshTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
        await RefreshContent();
    }

    private async void OnAddBookmarkTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;

        if (!isBookmarking)
        {
            isBookmarking = true;
            var isBookmarked = _post.bookmarked;
            await ApiHandler.PinPost(_post.id, isBookmarked);
            isBookmarking = false;
            await RefreshContent();
            RefreshBookmarkButton();
        }
    }

    private async void OnUpButtonClicked(object sender, RoutedEventArgs e)
    {
        var isUp = _post.sympathized;
        BtUp.IsEnabled = false;
        await ApiHandler.UpPost(_post.id, isUp);
        BtUp.IsEnabled = true;
        await RefreshContent();
    }

    private void OnMediaTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;

        var media = FvMedia.SelectedItem as Image;
        var url = media.Tag as string;
        if (url.Contains(".mp4"))
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return;
        }

        var medias = _post.media;
        var index = FvMedia.SelectedIndex;
        var control = new ImageViewerControl(medias, index);
        MainPage.ShowOverlay(control, true);
    }

    private async void OnTimeTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;

        if (!(_post.actor.relationship == "F" || _post.actor.relationship == "S" || _post.permission == "A"))
        {
            await Utils.Dialog.ShowPermissionRequiredMessageDialog(this, _post.actor.id);
            return;
        }

        if (!_isOverlay)
            MainPage.ShowOverlay(new TimelineControl(_post, false, true));
    }

    private void PointerEnteredShowHand(object sender, PointerRoutedEventArgs e) => Utility.ChangeCursor(true);

    private void PointerExitedShowHand(object sender, PointerRoutedEventArgs e) => Utility.ChangeCursor(false);

    private void CloseButtonClicked(object sender, TappedRoutedEventArgs e) => MainPage.HideOverlay();

    private void OnUserProfilePictureTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
        MainPage.ShowProfile(_post.actor.id);
    }

    private void OnEmotionsTextBlockTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;

        var control = new EmotionsListControl(_post.likes);
        var flyout = new Flyout();
        flyout.Content = control;
        flyout.ShowAt(RtbShares);
    }
    private async void OnShareCountTextBlockTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;

        var isUp = (sender as RichTextBlock).Tag as string == "Up";
        var flyout = new Flyout();
        var progressRing = new ProgressRing()
        {
            IsIndeterminate = true,
            IsActive = true
        };
        flyout.Content = progressRing;
        flyout.ShowAt(RtbShares);

        var shares = await ApiHandler.GetShares(isUp, _post, null);
        var control = new FriendListControl();
        List<FriendProfile> friendProfiles = new();
        foreach (var share in shares)
        {
            var friendProfile = new FriendProfile();
            friendProfile.ProfileUrl = share.actor.GetValidUserProfileUrl();
            friendProfile.Name = share.actor.display_name;
            friendProfile.Id = share.actor.id;
            friendProfile.Relationship = share.actor.relationship;
            friendProfile.Metadata.Tag = share.activity_id;
            friendProfile.Metadata.Control = control;
            friendProfile.Metadata.Flyout = flyout;
            friendProfile.Metadata.IsUp = isUp;
            friendProfiles.Add(friendProfile);
        }
        control.MaxItems = int.MaxValue;
        control.SetSource(friendProfiles);
        control.OnFriendSelected += OnSharedFriendSelected;
        flyout.Content = control;
    }

    private async void OnSharedFriendSelected(FriendProfile profile)
    {
        if (!(profile.Relationship == "F" || profile.Relationship == "S" || _post.permission == "A"))
        {
            await Utils.Dialog.ShowPermissionRequiredMessageDialog(this, profile.Id);
            return;
        }
        profile.Metadata?.Flyout?.Hide();

        var postId = profile.Metadata.Tag as string;
        var control = profile.Metadata.Control as FriendListControl;

        control.ShowLoading();
        var post = await ApiHandler.GetPost(postId);
        control.HideLoading();

        if (profile.Metadata.IsUp) MainPage.ShowProfile(profile.Id);
        else
        {
            if (post != null)
            {
                MainPage.HideOverlay();
                var overlay = new TimelineControl(post, false, true);
                MainPage.ShowOverlay(overlay);
            }
            else await this.ShowMessageDialogAsync("글을 볼 수 없거나 나만 보기로 설정된 글입니다.", "오류");
        }
    }

    private async void OnSharePostTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;

        if (!(_post.@object.actor.relationship == "F" || _post.@object.actor.relationship == "S" || _post.@object.permission == "A"))
        {
            await Utils.Dialog.ShowPermissionRequiredMessageDialog(this, _post.@object.actor.id);
            return;
        }
        e.Handled = true;
        var newPost = await ApiHandler.GetPost(_post.@object.id);
        var control = new TimelineControl(newPost, false, true);
        MainPage.ShowOverlay(control);
    }

    private async void OnSendCommentButtonClicked(object sender, RoutedEventArgs e)
    {
        if (BtAddMedia.IsEnabled == false) return;
        var button = sender as Button;
        BtAddMedia.IsEnabled = false;
        button.IsEnabled = false;
        var inputContol = FrComment.Content as InputControl;
        var textBox = inputContol.GetTextBox();
        var textBoxString = textBox.Text;
        var quotas = StoryApi.Utils.GetQuoteDataFromString(textBoxString, true);
        var text = string.Join(' ', quotas.Select(x => x.text));
        await ApiHandler.ReplyToPost(_post.id, text, quotas, _commentMedia);
        await RefreshContent();
        _commentMedia = null;
        FiAddMedia.Glyph = "\ue7c5";
        BtAddMedia.IsEnabled = true;
        textBox.Text = "";
        button.IsEnabled = true;
    }


    private async void OnAddMediaButtonClicked(object sender, RoutedEventArgs e)
    {
        if (_commentMedia == null)
        {
            var fileOpenPicker = new FileOpenPicker();
            InitializeWithWindow.Initialize(fileOpenPicker, WindowNative.GetWindowHandle(MainWindow.Instance));
            fileOpenPicker.FileTypeFilter.Add(".jpg");
            fileOpenPicker.FileTypeFilter.Add(".png");
            fileOpenPicker.FileTypeFilter.Add(".gif");
            var file = await fileOpenPicker.PickSingleFileAsync();
            if (file != null) await UploadCommentImageFile(file);
        }
        else
        {
            _commentMedia = null;
            FiAddMedia.Glyph = "\ue7c5";
        }
    }

    private async Task UploadCommentImageFile(StorageFile file)
    {
        if (BtAddMedia.IsEnabled == false) return;
        BtAddMedia.IsEnabled = false;
        FiAddMedia.Glyph = "\ue895";
        _commentMedia = await ApiHandler.UploadImage(file.Path);
        FiAddMedia.Glyph = "\ue74d";
        BtAddMedia.IsEnabled = true;
    }

    private void OnOverlayCloseButtonClicked(object sender, RoutedEventArgs e) => HideOverlay();

    private async void OnSharedPostShareCountTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;

        if (SpShare.Visibility == Visibility.Collapsed) return;
        var relationship = _post.@object.actor.relationship;
        if (!(relationship == "F" || relationship == "S" || _post.permission == "A"))
        {
            await Utils.Dialog.ShowPermissionRequiredMessageDialog(this, _post.@object.actor.id, "해당 사용자와 친구를 맺어야 공유 리스트를 확인할 수 있습니다.");
            return;
        }

        var senderControl = sender as FrameworkElement;
        var flyout = new Flyout();
        var progressRing = new ProgressRing()
        {
            IsIndeterminate = true,
            IsActive = true
        };
        flyout.Content = progressRing;
        flyout.ShowAt(senderControl);

        var shares = await ApiHandler.GetShares(false, _post, null);
        var control = new FriendListControl();
        List<FriendProfile> friendProfiles = new();
        foreach (var share in shares)
        {
            var friendProfile = new FriendProfile();
            friendProfile.ProfileUrl = share.actor.GetValidUserProfileUrl();
            friendProfile.Name = share.actor.display_name;
            friendProfile.Id = share.actor.id;
            friendProfile.Relationship = share.actor.relationship;
            friendProfile.Metadata.Tag = share.activity_id;
            friendProfile.Metadata.Control = control;
            friendProfile.Metadata.Flyout = flyout;
            friendProfile.Metadata.IsUp = false;
            friendProfiles.Add(friendProfile);
        }
        control.MaxItems = int.MaxValue;
        control.SetSource(friendProfiles);
        control.OnFriendSelected += OnSharedFriendSelected;
        flyout.Content = control;
    }
}