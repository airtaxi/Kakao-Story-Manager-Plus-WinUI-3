using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using KSMP.Extension;
using static KSMP.ApiHandler.DataType.CommentData;
using System;
using KSMP.Pages;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Windows.Storage;
using System.IO;
using Windows.ApplicationModel.DataTransfer;
using System.Diagnostics;
using static KSMP.ApiHandler.DataType.EmoticonItems;
using Microsoft.UI.Xaml.Media.Imaging;
using KSMP.Utils;
using Microsoft.UI.Xaml.Media;
using ImageMagick;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using H.NotifyIcon;
using Microsoft.UI.Xaml.Documents;
using static KSMP.ApiHandler.DataType.ShareData;

namespace KSMP.Controls;

public sealed partial class TimelineWindowControl : UserControl
{
    private PostData _post;
    private readonly bool _isOverlay;
    private readonly bool _isShare;

    private bool _isUploading = false;
    private bool isBookmarking = false;

    public bool IsContentLoaded = false;

    private ApiHandler.DataType.UploadedImageProp _commentMedia = null;
    private ApiHandler.DataType.UploadedImageProp _commentDcCon = null;
    private (EmoticonItem, int) _commentEmoticon = (null, 0);

    private readonly Window _window;

    public string PostId => _post?.id;

    public TimelineWindowControl(Window window, PostData post, bool isShare = false, bool isOverlay = false)
    {
        _window = window;
		_post = post;
        _isOverlay = isOverlay;
        _isShare = isShare;
        InitializeComponent();
        if (isOverlay && !isShare)
        {
            RdComment.Height = new GridLength(1, GridUnitType.Star);

            Grid.SetRow(GdComment, 0);
            Grid.SetRowSpan(GdComment, 5);
            Grid.SetColumn(GdComment, 1);

            LvComments.Padding = new Thickness(5);
            LvComments.MaxHeight = double.MaxValue;
            LvComments.VerticalAlignment = VerticalAlignment.Stretch;

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
            SpMenu.Visibility = Visibility.Collapsed;
            SpShare.Visibility = Visibility.Visible;
            RtShare.Visibility = Visibility.Visible;
            FrShareMargin.Visibility = Visibility.Visible;
            LvContent.Padding = new Thickness(10, 20, 10, 0);
            GdMain.Margin = new Thickness(0, 0, 0, -15);
        }

        if (!isShare)
        {
            var inputControl = new InputControl("댓글을 입력하세요.");
            inputControl.AcceptReturn(true);
            inputControl.SetMaxHeight(120);
            inputControl.WrapText(true);
            inputControl.OnSubmitShortcutActivated += OnSubmitShortcutActivated;
            inputControl.OnImagePasted += OnImagePasted;
            if (isOverlay) inputControl.SetPopupDesiredPlacement(PopupPlacementMode.Top);

            FrComment.Content = inputControl;

			{
				void unloaded(object s, RoutedEventArgs e)
				{
					inputControl.OnSubmitShortcutActivated -= OnSubmitShortcutActivated;
					inputControl.OnImagePasted -= OnImagePasted;
					Unloaded -= unloaded;
				}

				Unloaded += unloaded;
			}
		}

        bool willUseDynamicTimelineLoading = (Configuration.GetValue("UseDynamicTimelineLoading") as bool?) ?? true;
        if (isOverlay || !willUseDynamicTimelineLoading) _ = RefreshContent();

        if (!isOverlay && !isShare) GdMain.MaxWidth = 600;
    }

	private void OnAddEmoticonButtonClicked(object sender, RoutedEventArgs e)
	{
		var button = sender as Button;
		if (_commentEmoticon.Item1 == null)
		{
			var emoticonListControl = Post.ShowEmoticonListToButton(button);
			void emoticonListControlSelected(EmoticonItem item, int index)
			{
				_commentEmoticon.Item1 = item;
				_commentEmoticon.Item2 = index;

				FiAddEmoticon.Glyph = "\ue74d";
				BtAddMedia.IsEnabled = false;
				BtAddDcCon.IsEnabled = false;
			}
			emoticonListControl.OnSelected += emoticonListControlSelected;

			{
				void unloaded(object s, RoutedEventArgs e)
				{
					emoticonListControl.OnSelected -= emoticonListControlSelected;
					Unloaded -= unloaded;
				}

				Unloaded += unloaded;
			}
		}
		else
		{
			button.Flyout = null;
			_commentEmoticon = (null, 0);
			FiAddEmoticon.Glyph = "\ue899";
			BtAddMedia.IsEnabled = true;
			BtAddDcCon.IsEnabled = true;
		}
	}

	public void UnloadMedia(bool unloadContents = false)
    {
        IsContentLoaded = false;
        GdLoading.Visibility = Visibility.Collapsed;

        (FrShare.Content as TimelineWindowControl)?.UnloadMedia();
        FrShare.Content = null;

        var paragraph = RTbContent.Blocks.Where(x => x is Paragraph).FirstOrDefault() as Paragraph;
        paragraph?.Inlines?.Clear();
        RTbContent.Blocks.Clear();
        var controls = GetCurrentCommentControls();
        controls.ForEach(x => x.UnloadMedia());
        if (unloadContents) LvContent.Items.Clear();
        LvComments.Items.Clear();

        PpUser?.DisposeImage();
        (FrLink.Content as LinkControl)?.UnloadMedia();

        if (FvMedia.ItemsSource is not List<FrameworkElement> medias) return;
        foreach (var media in medias)
        {
            if (media is MediaPlayerElement video) video.DisposeVideo();
            else if (media is Image image) image.DisposeImage();
        }
    }

    private async void OnImagePasted(string temporaryImageFilePath)
    {
        var file = await StorageFile.GetFileFromPathAsync(temporaryImageFilePath);
        await UploadCommentImageFile(file);
    }

    private void OnSubmitShortcutActivated() => OnSendCommentButtonClicked(BtSendComment, null);

    private void RefreshBookmarkButton() => FiFavorite.Glyph = _post.bookmarked ? "\ue735" : "\ue734";

    private async void OnSharePost(XamlUICommand sender, ExecuteRequestedEventArgs args) => await SharePost();

    public async Task SharePost()
    {
        if (!_post.sharable || _post.@object != null) return;

        var window = new WritePostWindow(_post);
        window.Activate();

        var control = window.Control;
        control.OnCloseRequested += OnCloseRequested;
        await Task.Delay(10); // Bugfix
        control.FocusTextbox();

        {
			void unloaded(object s, RoutedEventArgs e)
			{
				control.OnCloseRequested -= OnCloseRequested;
				Unloaded -= unloaded;
			}

			Unloaded += unloaded;
        }
	}

	private async void OnCloseRequested() => await RefreshContent();

	private async void CopyPostUrl(XamlUICommand sender, ExecuteRequestedEventArgs args)
    {
        string userId = _post.actor.id;
        string postId = _post.id.Split(new string[] { "." }, StringSplitOptions.None)[1];
        string postUrl = "https://story.kakao.com/" + userId + "/" + postId;
        var dataPackage = new DataPackage();
        dataPackage.SetText(postUrl);
        Clipboard.SetContent(dataPackage);
        await Utility.ShowMessageDialogAsync("포스트의 URL이 클립보드에 복사되었습니다", "안내");
    }

    public async Task RefreshContent(bool showLoading = true)
    {
        IsContentLoaded = true;
        if (showLoading) GdLoading.Visibility = Visibility.Visible;
        SpPostInformation.Visibility = Visibility.Collapsed;
        if (!_isShare) _post = await ApiHandler.GetPost(_post.id);

        TbName.Text = _post.actor.display_name;
        
        RefreshTimestamp();

        var timestampRefreshTimer = new DispatcherTimer();
        timestampRefreshTimer.Interval = TimeSpan.FromMinutes(1);
		void timestampRefreshTimerTickEventHandler(object s, object e) => RefreshTimestamp();
		timestampRefreshTimer.Tick += timestampRefreshTimerTickEventHandler;
        timestampRefreshTimer.Start();

        RtbEmotions.Visibility = Visibility.Visible;
        RtbShares.Visibility = Visibility.Visible;
        RtbUps.Visibility = Visibility.Visible;

        int commentCount = _post.comments?.Count ?? 0;
        if (commentCount > 0 && !_isShare)
        {
            RtbComments.Visibility = Visibility.Visible;
            LvComments.Visibility = Visibility.Visible;
            
            await LoadComments();

            RnComments.Text = _post.comment_count.ToString();

            LvComments.Visibility = Visibility.Visible;
        }
        else
        {
            RtbComments.Visibility = Visibility.Collapsed;
            LvComments.Visibility = Visibility.Collapsed;
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

        if ((_post.media?.Count ?? 0) > 0) GvMedia.Visibility = Visibility.Visible;
        else GvMedia.Visibility = Visibility.Collapsed;

        Post.SetTextContent(_post.content_decorators, RTbContent, _isOverlay);

        RefreshBookmarkButton();

        Utility.SetPersonPictureUrlSource(PpUser, _post.actor?.GetValidUserProfileUrl());
        FvMedia.ItemsSource = Utility.GenerateMedias(_post?.media, _isOverlay);

        TbShareCount.Text = _post.share_count.ToString();
        if (_post.@object != null && _post.@object.id != null)
        {
            var shareContent = new TimelineWindowControl(_window, _post.@object, true, _isOverlay);
			FrShare.Content = shareContent;
            await shareContent.RefreshContent();
		}
        else
            GdShare.Visibility = Visibility.Collapsed;
        if (_post.scrap != null)
            FrLink.Content = new LinkControl(_post.scrap);
        else
            FrLink.Visibility = Visibility.Collapsed;

        this.UpdateLayout();

        if (showLoading) GdLoading.Visibility = Visibility.Collapsed;
        
        var scrollViewer = Utility.GetScrollViewerFromBaseListView(LvComments);
        if(scrollViewer != null) scrollViewer.ViewChanged += OnCommentsScrollViewerViewChanged;

        var itemsCopy = LvContent.Items.ToArray();
        foreach (FrameworkElement item in itemsCopy)
            if (item.Visibility == Visibility.Collapsed)
                LvContent.Items.Remove(item);

        {
			void unloaded(object s, RoutedEventArgs e)
			{
				timestampRefreshTimer.Stop();
				timestampRefreshTimer.Tick -= timestampRefreshTimerTickEventHandler;
				if (scrollViewer != null) scrollViewer.ViewChanged -= OnCommentsScrollViewerViewChanged;
				Unloaded -= unloaded;
			}

			Unloaded += unloaded;
        }
	}

    private void RefreshTimestamp()
    {
        var timestampString = Api.Story.Utils.GetTimeString(_post.created_at);
        TbTime.Text = timestampString;
        if (_post.content_updated_at != DateTime.MinValue)
            TbTime.Text += " (수정됨)";
    }

    private async Task LoadComments(string since = null)
    {
        if (since == null) LvComments.Items.Clear();
        var comments = await ApiHandler.GetComments(_post.id, since);
        var currentComments = GetCurrentComments();
        if (since != null) comments.Reverse();

        foreach (var comment in comments)
        {
            if (currentComments.Any(x => x.id == comment.id)) continue;
            var control = new CommentControl(comment, _post.id, _isOverlay)
            {
                Tag = comment
            };
            control.OnReplyClicked += OnCommentReplyClicked;
            control.OnDeleted += OnCommentDeleted;
            await control.LoadCommentCompletionSource.Task;

            {
				void unloaded(object s, RoutedEventArgs e)
				{
					control.OnReplyClicked -= OnCommentReplyClicked;
					control.OnDeleted -= OnCommentDeleted;
					Unloaded -= unloaded;
				}

				Unloaded += unloaded;
            }

			bool isMyComment = comment.writer.id == MainPage.Me.id;
            bool isMyPost = _post.actor.id == MainPage.Me.id;
            control.HideUnaccessableButton(isMyComment, isMyPost);

            if (since == null)
                LvComments.Items.Add(control);
            else
                LvComments.Items.Insert(0, control);
		}
		LvComments.UpdateLayout();

		if (!_isShare)
		{
			await Task.Delay(500);
			var last = LvComments.Items.LastOrDefault();
			if (last != null) LvComments.ScrollIntoView(last);
		}
	}

    private void OnCommentReplyClicked(Comment sender)
    {
        var profile = sender.writer;
        var inputContol = FrComment.Content as InputControl;
        inputContol.AppendText("{!{{" + "\"id\":\"" + profile.id + "\", \"type\":\"profile\", \"text\":\"" + profile.display_name + "\"}}!} ");
    }
    private async void OnCommentDeleted() => await RefreshContent();

    private void OnDotMenuTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;

        var icon = sender as FontIcon;
        var flyout = new MenuFlyout();
        if(_post.actor.id != MainPage.Me.id)
        {
            var menuHidePost = new MenuFlyoutItem() { Text = "이 글 숨기기" };

			async void clickEventHandler(object o, RoutedEventArgs e2)
			{
				await ApiHandler.HidePost(_post.id);
				MainPage.GetTimelinePage()?.RemovePost(_post.id);
				_window.Close();
			}
			menuHidePost.Click += clickEventHandler;
			flyout.Items.Add(menuHidePost);

			{
				void unloaded(object s, RoutedEventArgs e)
				{
					menuHidePost.Click -= clickEventHandler;
					Unloaded -= unloaded;
				}

				Unloaded += unloaded;
			}
		}
        else
        {
            var menuDeletePost = new MenuFlyoutItem() { Text = "글 삭제하기" };
            menuDeletePost.Click += OnDeletePost;
            flyout.Items.Add(menuDeletePost);

            var menuEditPost = new MenuFlyoutItem() { Text = "글 수정하기" };
            menuEditPost.Click += OnEditPost;
            flyout.Items.Add(menuEditPost);

			{
				void unloaded(object s, RoutedEventArgs e)
				{
					menuDeletePost.Click -= OnDeletePost;
					menuEditPost.Click -= OnEditPost;
					Unloaded -= unloaded;
				}

				Unloaded += unloaded;
			}
		}

        var menuBlockProfile = new MenuFlyoutItem() { Text = _post.actor.is_feed_blocked ? $"'{_post.actor.display_name}' 글 받기" : $"'{_post.actor.display_name}' 글 안받기" };
		async void menuBlockProfileClicked(object o, RoutedEventArgs e2)
		{
			await ApiHandler.BlockProfile(_post.actor.id, _post.actor.is_feed_blocked);
			await RefreshContent();
		}
		menuBlockProfile.Click += menuBlockProfileClicked;
        flyout.Items.Add(menuBlockProfile);

        var menuMutePost = new MenuFlyoutItem() { Text = _post.push_mute ? "이 글 알림 받기" : "이 글 알림 받지 않기" };
		async void menuMutePostClicked(object o, RoutedEventArgs e2)
		{
			await ApiHandler.MutePost(_post.id, !_post.push_mute);
			await RefreshContent();
		}
		menuMutePost.Click += menuMutePostClicked;
        flyout.Items.Add(menuMutePost);
;
        flyout.Items.Add(new MenuFlyoutSeparator());
        var menuExportPost = new MenuFlyoutItem() { Text = "이미지로 내보내기" };
        menuExportPost.Click += OnExportPost;
        flyout.Items.Add(menuExportPost);

        flyout.ShowAt(icon);

        {
			void unloaded(object s, RoutedEventArgs e)
			{
				menuBlockProfile.Click -= menuBlockProfileClicked;
				menuMutePost.Click -= menuMutePostClicked;
				menuExportPost.Click -= OnExportPost;
				Unloaded -= unloaded;
			}

			Unloaded += unloaded;
        }
	}

    private async Task SaveToBitmapImageAsync(string path, RenderTargetBitmap rtb)
    {
        using var fileStream = new FileStream(path, FileMode.Create);
        using var stream = fileStream.AsRandomAccessStream();

        var pixels = await rtb.GetPixelsAsync();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
        var bytes = pixels.ToArray();

        var hWnd = WindowNative.GetWindowHandle(_window);
        var dpi = PInvoke.User32.GetDpiForWindow(hWnd);

        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)rtb.PixelWidth, (uint)rtb.PixelHeight, dpi, dpi, bytes);

        await encoder.FlushAsync();
    }

    private async void OnExportPost(object sender, RoutedEventArgs e)
    {
        GdLoading.Visibility = Visibility.Visible;
        IsEnabled = false;

        var canvas = new Canvas();
        var control = new TimelineControl(_window, _post);
        canvas.Children.Add(control);
        var window = new Window();
        window.Activate();
        window.Hide();
        window.Content = canvas;
        control.Width = 600;
        control.Height = double.NaN;
        control.AdjustControlForCaptureWindow();
        control.UpdateLayout();
        await control.RefreshContent(false);
        await Task.Delay(1000);

        RenderTargetBitmap renderTarget = new RenderTargetBitmap();

        await renderTarget.RenderAsync(canvas);

        var path = Path.GetTempFileName();
        await SaveToBitmapImageAsync(path, renderTarget);
        window.Close();

        var dataPackage = new DataPackage();
        var file = await StorageFile.GetFileFromPathAsync(path);
        dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(file));
        dataPackage.RequestedOperation = DataPackageOperation.Copy;
        Clipboard.SetContent(dataPackage);

        IsEnabled = true;
        GdLoading.Visibility = Visibility.Collapsed;
        await Utility.ShowMessageDialogAsync("내보내진 이미지가 클립보드에 저장되었습니다.", "안내");
    }

    private async void OnDeletePost(object sender, RoutedEventArgs e) => await DeletePost();
    private async void OnEditPost(object sender, RoutedEventArgs e) => await EditPost();

    public async Task DeletePost()
    {
        if (_post.actor.id != MainPage.Me.id) return;
        var result = await Utility.ShowMessageDialogAsync("정말로 글을 삭제하실건가요?", "경고", true);
        if (result != ContentDialogResult.Primary) return;
        await ApiHandler.DeletePost(_post.id);

        MainPage.GetTimelinePage()?.RemovePost(_post.id);
    }

    public async Task EditPost()
    {
        if (_post.actor.id != MainPage.Me.id) return;
        GdLoading.Visibility = Visibility.Visible;

        var window = new WritePostWindow();
        window.Activate();
        window.Title = "글 수정";
        var control = window.Control;
		await control.SetEditMedia(_post);

        control.OnCloseRequested += OnCloseRequested;
        await Task.Delay(10);
        control.FocusTextbox();
        GdLoading.Visibility = Visibility.Collapsed;

		{
			void unloaded(object s, RoutedEventArgs e)
			{
				control.OnCloseRequested -= OnCloseRequested;
				Unloaded -= unloaded;
			}

			Unloaded += unloaded;
		}
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

    private void OnMediaTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;

        var media = FvMedia.SelectedItem as Image;
        if (media == null) return;
        var url = media.Tag as string;
        if (url == null) return;
        else if (url.Contains(".mp4"))
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return;
        }

        var medias = _post.media.ToList();
        var index = FvMedia.SelectedIndex;
        medias.RemoveAll(x => x.origin_url == null);
        medias.RemoveAll(x => x.origin_url.Contains(".mp4"));

        var window = new ImageViewerWindow(medias, index);
        window.Activate();
    }

    private async void OnTimeTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
        if (!(_post.actor.relationship == "F" || _post.actor.relationship == "S" || _post.permission == "A"))
        {
            await Dialog.ShowPermissionRequiredMessageDialog(this, _post.actor.id);
            return;
        }
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e) => Utility.ChangeSystemMouseCursor(true);
    private void OnPointerExited(object sender, PointerRoutedEventArgs e) => Utility.ChangeSystemMouseCursor(false);

    private void OnUserProfilePictureTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
        new Flyout() { Content = new UserProfileControl(_post.actor.id, true) { Width = 450, Margin = new Thickness(-27) } }.ShowAt(sender as FrameworkElement);
    }

    private void OnEmotionsTextBlockTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;

        var control = new EmotionsListControl(_post.likes);
        var flyout = new Flyout
        {
            Content = control
        };
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
        flyout.Placement = FlyoutPlacementMode.Bottom;
		flyout.ShowAt(RtbShares);

        var shares = await ApiHandler.GetShares(isUp, _post, null);
        var control = new FriendListControl();
        List<FriendProfile> friendProfiles = new();
        foreach (var share in shares)
        {
            var friendProfile = share.actor.GetFriendData();
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

		{
			void unloaded(object s, RoutedEventArgs e)
			{
				control.OnFriendSelected -= OnSharedFriendSelected;
				Unloaded -= unloaded;
			}

			Unloaded += unloaded;
		}
	}

    private async void OnSharedFriendSelected(FriendProfile profile)
    {
        if (!(profile.Relationship == "F" || profile.Relationship == "S" || _post.permission == "A"))
        {
            await Dialog.ShowPermissionRequiredMessageDialog(this, profile.Id);
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
                var window = TimelineWindow.GetTimelineWindow(post);
                window.Activate();
			}
            else await Utility.ShowMessageDialogAsync("글을 볼 수 없거나 나만 보기로 설정된 글입니다.", "오류");
        }
    }

    private async void OnSharePostTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;

        if (!(_post.@object.actor.relationship == "F" || _post.@object.actor.relationship == "S" || _post.@object.permission == "A"))
        {
            await Dialog.ShowPermissionRequiredMessageDialog(this, _post.@object.actor.id);
            return;
        }
        e.Handled = true;
        var newPost = await ApiHandler.GetPost(_post.@object.id);
        var window = TimelineWindow.GetTimelineWindow(newPost);
        window.Activate();
    }

    private async void OnSendCommentButtonClicked(object sender, RoutedEventArgs e)
    {
        if (_isUploading) return;
        var button = sender as Button;
        BtCommentMore.IsEnabled = false;
        BtAddEmoticon.IsEnabled = false;
        BtAddMedia.IsEnabled = false;
        BtAddDcCon.IsEnabled = false;
        button.IsEnabled = false;
        var inputContol = FrComment.Content as InputControl;
        var textBox = inputContol.GetTextBox();
        var textBoxString = textBox.Text;
        var quotas = Api.Story.Utils.GetQuoteDataFromString(textBoxString, true);
        var emoticonItem = _commentEmoticon.Item1;
        if (emoticonItem != null)
        {
            quotas.Insert(0, new()
            {
                item_id = emoticonItem.Id,
                item_sub_type = emoticonItem.ItemSubType,
                item_ver = emoticonItem.Version,
                resource_id = _commentEmoticon.Item2,
                text = "(Emoticon) ",
                type = "emoticon"
            });
        }
        var text = string.Join(' ', quotas.Select(x => x.text));

        if (string.IsNullOrWhiteSpace(text) && _commentMedia == null && _commentDcCon == null && _commentEmoticon.Item1== null)
        {
            await Utility.ShowMessageDialogAsync("댓글 내용을 입력해주세요.", "오류");
            return;
        }

        await ApiHandler.ReplyToPost(_post.id, text, quotas, _commentMedia ?? _commentDcCon);
        await RefreshContent();
        _commentMedia = null;
        _commentDcCon = null;
        _commentEmoticon = (null, 0);

        BtAddEmoticon.IsEnabled = true;
        FiAddEmoticon.Glyph = "\ue899";

        FiAddMedia.Glyph = "\ue7c5";
        BtAddMedia.IsEnabled = true;
        BtCommentMore.IsEnabled = true;

        BtAddDcCon.IsEnabled = true;
        VbMandu.Visibility = Visibility.Visible;
        VbDelete.Visibility = Visibility.Collapsed;

        textBox.Text = "";
        button.IsEnabled = true;
    }

    private async Task UploadCommentImageFile(StorageFile file)
    {
        _isUploading = true;
        if (BtAddMedia.IsEnabled == false) return;

        BtAddMedia.IsEnabled = false;
        FiAddMedia.Glyph = "\ue895";

        BtAddEmoticon.IsEnabled = false;
        BtAddDcCon.IsEnabled = false;

        BtCommentMore.IsEnabled = false;
        BtSendComment.IsEnabled = false;
        FiCommentMore.Glyph = "\ue895";
        _commentMedia = await ApiHandler.UploadImage(file.Path);
        FiCommentMore.Glyph = "\ue712";
        BtCommentMore.IsEnabled = true;
        BtSendComment.IsEnabled = true;

        FiAddMedia.Glyph = "\ue74d";
        BtAddMedia.IsEnabled = true;
        BtAddDcCon.IsEnabled = true;
        _isUploading = false;
    }

    private async void OnSharedPostShareCountTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;

        if (SpShare.Visibility == Visibility.Collapsed) return;
        var relationship = _post.actor.relationship;
        if (!(relationship == "F" || relationship == "S" || _post.permission == "A"))
        {
            await Dialog.ShowPermissionRequiredMessageDialog(this, _post.actor.id, "해당 사용자와 친구를 맺어야 공유 리스트를 확인할 수 있습니다.");
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
            var friendProfile = share.actor.GetFriendData();
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

		{
			void unloaded(object s, RoutedEventArgs e)
			{
				control.OnFriendSelected -= OnSharedFriendSelected;
				Unloaded -= unloaded;
			}

			Unloaded += unloaded;
		}
    }

    private bool _isRefreshing = false;
    private async void OnCommentsScrollViewerViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_post.comment_count < 30) return;
        if (_isRefreshing || e.IsIntermediate) return;
        var scrollViewer = sender as ScrollViewer;
        var verticalOffset = scrollViewer.VerticalOffset;
        if(verticalOffset == 0)
        {
            GdLoading.Visibility = Visibility.Visible;
            _isRefreshing = true;
            try
            {
                var comments = GetCurrentComments();
                if (comments.Count == _post.comment_count) return;
                var lastComment = comments.FirstOrDefault();
                if (lastComment == null) return;
                await LoadComments(lastComment.id);
            }
            finally
            {
                GdLoading.Visibility = Visibility.Collapsed;
                _isRefreshing = false;
            }
        }
    }

    private List<CommentControl> GetCurrentCommentControls() => LvComments.Items.Select(x => x as CommentControl).ToList();
    private List<Comment> GetCurrentComments() => GetCurrentCommentControls().Select(x => x.Tag as Comment).ToList();

    private async void OnAddMediaButtonClicked(object sender, RoutedEventArgs e)
    {
        if (_commentMedia == null)
        {
            var fileOpenPicker = new FileOpenPicker();
            InitializeWithWindow.Initialize(fileOpenPicker, WindowNative.GetWindowHandle(_window));
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
            BtAddEmoticon.IsEnabled = true;
            BtAddDcCon.IsEnabled = true;
        }
    }

    private async void OnAddDcConButtonClicked(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (_commentDcCon == null)
        {
            var list = Utility.GetCurrentDcConList();
            if(list.Count == 0)
            {
                await Utility.ShowMessageDialogAsync("설정된 디시콘이 없습니다", "오류");
                return;
            }
            var flyout = new Flyout();

            var dcConListControl = new DcConListControl();
			async void dcConListControlSelected(Api.DcCon.DataType.Package.Detail item)
			{
				BtAddDcCon.IsEnabled = false;
				BtAddEmoticon.IsEnabled = false;
				BtAddMedia.IsEnabled = false;
				flyout.Hide();


				BtCommentMore.IsEnabled = false;
				FiCommentMore.Glyph = "\ue898";

				var path = Path.Combine(Path.GetTempPath(), $"{item.PackageIndex}_{item.Index}.{item.Extension}");
				try
				{

					var data = await Api.DcCon.ApiHandler.GetDcDonImage(item.Path);

					if (item.Extension != "gif")
					{
						await Task.Run(() =>
						{
							using var image = new MagickImageCollection(data);
							image[0].Resize(400, 400);
							image.Write(path, MagickFormat.Png);
						});
					}
					else await File.WriteAllBytesAsync(path, data);
					_isUploading = true;
					_commentDcCon = await ApiHandler.UploadImage(path);
				}
				catch (ArgumentNullException) { } //Ignore
				finally
				{
					_isUploading = false;
					File.Delete(path);
					BtCommentMore.IsEnabled = true;
					FiCommentMore.Glyph = "\ue712";

					BtAddDcCon.IsEnabled = true;
					VbMandu.Visibility = Visibility.Collapsed;
					VbDelete.Visibility = Visibility.Visible;
				}
			}
			dcConListControl.OnSelected += dcConListControlSelected;
            flyout.Content = dcConListControl;
            flyout.ShowAt(button);

			{
				void unloaded(object s, RoutedEventArgs e)
				{
					dcConListControl.OnSelected -= dcConListControlSelected;
					Unloaded -= unloaded;
				}

				Unloaded += unloaded;
			}
		}
		else
        {
            _commentDcCon = null;
            VbMandu.Visibility = Visibility.Visible;
            VbDelete.Visibility = Visibility.Collapsed;
            BtAddEmoticon.IsEnabled = true;
        }
    }

    private void OnMediaFlipViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var flipView = sender as FlipView;
        var item = flipView.SelectedItem as FrameworkElement;
        var image = item as Image;
        var bitmap = image?.Source as BitmapImage;
        if (bitmap == null) return;

        void SetHeight()
        {
            var currentItem = flipView.SelectedItem as FrameworkElement;
            flipView.Height = 400;
            flipView.UpdateLayout();
            if (currentItem == item) flipView.Height = Math.Min(image.ActualHeight, 400);
        }

        if (bitmap.PixelHeight == 0)
        {
			void imageOpenedEventHandler(object s, RoutedEventArgs e) => SetHeight();
			bitmap.ImageOpened += imageOpenedEventHandler;

			{
				void unloaded(object s, RoutedEventArgs e)
				{
					bitmap.ImageOpened -= imageOpenedEventHandler;
					Unloaded += unloaded;
				}

				Unloaded += unloaded;
			}

		}
        else SetHeight();
    }
}