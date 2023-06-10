using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ByteSizeLib;
using ImageMagick;
using KSMP.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using static KSMP.ApiHandler.DataType;

namespace KSMP.Controls;

public sealed partial class WritePostControl : UserControl
{
    private InputControl _inputControl;
    public delegate void ControlDelegate();
    public ControlDelegate OnCloseRequested;
    public CommentData.PostData _postToShare = null;
    public CommentData.PostData _postToEdit = null;

    private class Media
    {
        public string Path { get; set; }
        public string Key { get; set; } = null;
        public string Type { get; set; }
        public bool IsVideo { get; set; }
        public BitmapImage Thumbnail { get; set; }
    }
    private ObservableCollection<Media> _medias = new();

    public WritePostControl(Button button = null)
    {
        InitializeComponent();
        InitializeInputControl();
        LvMedias.ItemsSource = _medias;
        AdjustDefaultPostWritingPermission();
        BtWriteClose.Visibility = Visibility.Visible;
    }


    public void AdjustDefaultPostWritingPermission()
    {
        string defaultPostWritingPermission = (Utils.Configuration.GetValue("DefaultPostWritingPermission") as string) ?? "F";
        CbxPermission.SelectedItem = CbxPermission.Items.FirstOrDefault(x => ((x as ComboBoxItem).Tag as string) == defaultPostWritingPermission);
    }

    public WritePostControl(CommentData.PostData postToShare)
    {
        InitializeComponent();
        _postToShare = postToShare;
        InitializeInputControl(false);
        TbWritePost.Text = "공유";
        BdMedia.Visibility = Visibility.Collapsed;
        if (postToShare.permission == "F")
            CbiShareAll.Visibility = Visibility.Collapsed;
        else if (postToShare.permission == "P")
        {
            CbiShareAll.Visibility = Visibility.Collapsed;
            CbiShareFriend.Visibility = Visibility.Collapsed;
        }
        else if (postToShare.permission == "M")
        {
            CbiShareAll.Visibility = Visibility.Collapsed;
            CbiShareFriend.Visibility = Visibility.Collapsed;
            //CbiSharePrivate.Visibility = Visibility.Collapsed;
        }
        AdjustDefaultPostWritingPermission();
        BtWriteClose.Visibility = Visibility.Collapsed;
    }

    public void FocusTextbox() => _inputControl.FocusTextBox();

    private void InitializeInputControl(bool canAddMedia = true)
    {
        _inputControl = new InputControl();
        FrInputControl.Content = _inputControl;
        _inputControl.SetMinHeight(200);
        _inputControl.SetWidth(Width);
        _inputControl.SetMaxHeight(300);
        _inputControl.AcceptReturn(true);
        _inputControl.WrapText(true);
        if (canAddMedia)
        {
            _inputControl.OnImagePasted += OnPasteImage;

			{
				RoutedEventHandler unloaded = null;
				unloaded = (s, e) =>
				{
					_inputControl.OnImagePasted -= OnPasteImage;
                    Unloaded -= unloaded;
				};
				Unloaded += unloaded;
			}
		}
        _inputControl.OnSubmitShortcutActivated += OnSubmitShortcutActivated;

		{
			RoutedEventHandler unloaded = null;
			unloaded = (s, e) =>
			{
				_inputControl.OnSubmitShortcutActivated -= OnSubmitShortcutActivated;
                Unloaded -= unloaded;
			};
			Unloaded += unloaded;
		}
	}

    private async void OnSubmitShortcutActivated() => await WritePostAsync();

    public async Task AddImageFromPath(string filePath)
    {
        GdLink.Visibility = Visibility.Collapsed;
        ResetLinkControl();
        var file = await StorageFile.GetFileFromPathAsync(filePath);
        await AddMediaFromFile(file);
    }
    private async void OnPasteImage(string filePath) => await AddImageFromPath(filePath);

    private readonly List<string> _permissons = new()
    {
        "A",
        "F",
        "M",
        "P"
    };

    private async void OnWriteButtonClicked(object sender, RoutedEventArgs e) => await WritePostAsync();

    private async Task WritePostAsync()
    {
        IsEnabled = false;
        PbMain.Visibility = Visibility.Visible;

        try
        {
            var hasWebP = _medias.Any(x => x.Key == null && x.Path.ToLower().EndsWith(".webp"));
            if (hasWebP)
            {
                var result = await Utility.ShowMessageDialogAsync("webp 파일을 gif로 변환하여 업로드하시겠습니까?", "안내", true);
                if (result == ContentDialogResult.Primary)
                {
                    var success = await ConvertWebPToGifAsync();
                    if (!success)
                    {
                        await Utility.ShowMessageDialogAsync("변환된 gif 파일의 크기가 20mb를 초과하여 업로드를 중단합니다.", "오류");
                        return;
                    }
                }
            }

            var quoteDatas = Api.Story.Utils.GetQuoteDataFromString(_inputControl.GetTextBox().Text);
            if (_postToShare != null)
                await ApiHandler.SharePost(_postToShare.id, quoteDatas, _permissons[CbxPermission.SelectedIndex], true, null, null);
            else
            {
                var mediaData = new MediaData();
                if (_medias.Count > 0)
                {
                    var medias = new List<MediaData.MediaObject>();
                    foreach (var rawMedia in _medias)
                    {
                        var media = new MediaData.MediaObject();
                        var path = rawMedia.Path;
                        if (rawMedia.Key != null)
                        {
                            media.media_path = rawMedia.Path;
                            media.media_type = rawMedia.Type.Split('/')[0];
                        }
                        else
                        {
                            if (!rawMedia.IsVideo)
                            {
                                var type = path.ToLower().EndsWith(".gif") ? "gif" : "image";
                                var key = await ApiHandler.UploadImage(new AssetData()
                                {
                                    Path = path,
                                });
                                media.media_path = key;
                                media.media_type = type;
                            }
                            else
                            {
                                var key = await ApiHandler.UploadVideo(new()
                                {
                                    Path = path,
                                });
                                media.media_path = key;
                                await ApiHandler.WaitForVideoUploadFinish(key);
                                media.media_type = "video";
                            }
                        }
                        media.caption = new();
                        medias.Add(media);
                    }
                    mediaData.media = medias;

                    string mediaType = null;
                    var imageExists = _medias.Any(x => !x.IsVideo);
                    var videoExists = _medias.Any(x => x.IsVideo);
                    if (imageExists && videoExists)
                        mediaType = "mixed";
                    else if (imageExists)
                        mediaType = "image";
                    else if (videoExists)
                        mediaType = "video";
                    mediaData.media_type = mediaType;
                }
                else mediaData = null;
                var oldPaths = _postToEdit?.media?.Select(x => x.media_path).ToList();
                var url = FiLink.Tag as string;
                await ApiHandler.WritePost(quoteDatas, mediaData, _permissons[CbxPermission.SelectedIndex], true, true, null, null, url, _postToEdit != null, oldPaths, _postToEdit?.id);
            }

            OnCloseRequested.Invoke();

            var timelinePage = MainPage.GetTimelinePage();
            if (timelinePage == null) return;
            
            if(timelinePage.IsMyTimeline || timelinePage.Id == null)
            {
                var feed = await ApiHandler.GetProfileFeed(MainPage.Me.id, null);
                await timelinePage.InsertPostAsync(feed.activities.FirstOrDefault());
            }
        }
        finally
        {
            PbMain.Visibility = Visibility.Collapsed;
            IsEnabled = true;
        }
    }

    private static readonly double FileSizeLimit = ByteSize.FromMegaBytes(20).Bytes;
    private async Task<bool> ConvertWebPToGifAsync()
    {
        foreach (var media in _medias)
        {
            if (media.Key != null) continue;
            var originalPath = media.Path;
            var fileName = Guid.NewGuid().ToString()[..8] + ".gif";
            using var animatedWebP = new MagickImageCollection(originalPath);
            var newGifPath = Path.Combine(Path.GetTempPath(), fileName);
            await Task.Run(async () => await animatedWebP.WriteAsync(newGifPath, MagickFormat.Gif));
            var info = new FileInfo(newGifPath);
            var size = info.Length;
            if (size > FileSizeLimit)
                return false;
            media.Path = newGifPath;
        }
        return true;
    }

    public async Task SetEditMedia(CommentData.PostData postToEdit)
    {
        BtWriteClose.Visibility = Visibility.Collapsed;
        var permissionIndex = 0;
        if (postToEdit.permission == "F")
            permissionIndex = 1;
        else if (postToEdit.permission == "M")
            permissionIndex = 2;

        CbxPermission.SelectedIndex = permissionIndex;
        _postToEdit = postToEdit;
        TbWritePost.Text = "글 수정";
        var text = Api.Story.Utils.GetStringFromQuoteData(postToEdit.content_decorators, true);
        _inputControl.GetTextBox().Text = text;

        var serverMedias = postToEdit.media ?? new();
        if (serverMedias.Count > 0) LvMedias.Visibility = Visibility.Visible;
        foreach (var serverMedia in serverMedias)
        {
            var isVideo = serverMedia.url_hq != null;
            var media = new Media()
            {
                IsVideo = isVideo,
                Key = serverMedia.key,
                Path = serverMedia.media_path,
                Type = serverMedia.content_type
            };
            if (!isVideo)
            {
                var path = Path.GetTempFileName();
                WebClient client = new();
                client.DownloadFile(serverMedia.origin_url, path);

                var storageFile = await StorageFile.GetFileFromPathAsync(path);
                using var stream = await storageFile.OpenAsync(FileAccessMode.Read);

                var image = new BitmapImage();
                image.SetSource(stream);
                media.Thumbnail = image;
            }
            else
            {
                var image = new BitmapImage();
                image.UriSource = new Uri("ms-appx:///Assets/Video.png");
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                media.Thumbnail = image;
            }
            _medias.Add(media);
        }
    }

    private async Task AddMediaFromFile(StorageFile file)
    {
        var path = file.Path;
        var isVideo = Path.GetExtension(file.Path).ToLower() == ".mp4";
        var media = new Media()
        {
            Path = path,
            IsVideo = isVideo
        };
        if (!isVideo)
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(path);
            using var stream = await storageFile.OpenAsync(FileAccessMode.Read);

            var image = new BitmapImage();
            image.SetSource(stream);
            media.Thumbnail = image;
        }
        else
        {
            var image = new BitmapImage();
            image.UriSource = new Uri("ms-appx:///Assets/Video.png");
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            media.Thumbnail = image;
        }

        _medias.Add(media);
        LvMedias.Visibility = Visibility.Visible;
    }

    private void OnMediaListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var listView = sender as ListView;
        var media = listView.SelectedItem as Media;
        if (media == null) return;
        _medias.Remove(media);
        listView.SelectedItem = null;
        if (_medias.Count == 0) listView.Visibility = Visibility.Collapsed;
    }

    private async void OnAddMediaButtonClicked(object sender, RoutedEventArgs e)
    {
        ResetLinkControl();
        GdLink.Visibility = Visibility.Collapsed;

        var fileOpenPicker = new FileOpenPicker();
        InitializeWithWindow.Initialize(fileOpenPicker, WindowNative.GetWindowHandle(MainWindow.Instance));
        fileOpenPicker.FileTypeFilter.Add(".jpg");
        fileOpenPicker.FileTypeFilter.Add(".png");
        fileOpenPicker.FileTypeFilter.Add(".webp");
        fileOpenPicker.FileTypeFilter.Add(".gif");
        fileOpenPicker.FileTypeFilter.Add(".mp4");
        var files = (await fileOpenPicker.PickMultipleFilesAsync()).ToList();
        foreach (var file in files) await AddMediaFromFile(file);
    }

    private void OnAddLinkButtonClicked(object sender, RoutedEventArgs e)
    {
        if (GdLink.Visibility == Visibility.Visible)
        {
            GdLink.Visibility = Visibility.Collapsed;
            return;
        }
        LvMedias.Visibility = Visibility.Collapsed;
        GdLink.Visibility = Visibility.Visible;
        ResetLinkControl();
        _medias.Clear();
    }

    private void ResetLinkControl()
    {
        TbxLink.Text = "";
        TbxLink.IsEnabled = true;
        FiLink.Glyph = "\uf6fa";
        FiLink.Tag = null;
    }

    private async void OnGetScrapDataButtonClicked(object sender, RoutedEventArgs e) => await PrepareScrap();

    private async Task PrepareScrap()
    {
        var tag = FiLink.Tag as string;
        if (string.IsNullOrEmpty(tag))
        {
            IsEnabled = false;
            try
            {
                FiLink.Glyph = "\ue895";
                var url = TbxLink.Text;
                var scrap = await ApiHandler.GetScrapData(url);
                if (scrap == null)
                {
                    FiLink.Glyph = "\uf6fa";
                    await Utility.ShowMessageDialogAsync("오류가 발생했습니다.\n다시 시도해보세요.", "오류");
                    return;
                }
                FiLink.Glyph = "\ue74d";
                FiLink.Tag = scrap;
                TbxLink.IsEnabled = false;
            }
            finally
            {
                IsEnabled = true;
            }
        }
        else ResetLinkControl();
    }

    private async void OnLinkTextBoxKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter) await PrepareScrap();
    }

	private void OnCloseButtonClicked(object sender, RoutedEventArgs e) => OnCloseRequested?.Invoke();

	private void OnEmoticonButtonClicked(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        Utils.Post.ShowEmoticonListToButton(button, _inputControl);
    }

    public double GetHeight() => FrInputControl.ActualHeight + BdMedia.ActualHeight + GdMedia.ActualHeight + GdMenu.ActualHeight;
}
