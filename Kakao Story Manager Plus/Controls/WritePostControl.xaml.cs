using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WinRT.Interop;
using static StoryApi.ApiHandler.DataType;

namespace KSMP.Controls;

public sealed partial class WritePostControl : UserControl
{
    private readonly InputControl _inputControl;
    private readonly Button _button;
    public delegate void PostCompleted();
    public PostCompleted OnPostCompleted;
    private class Media
    {
        public string Path { get; set; }
        public bool IsVideo { get; set; }
        public BitmapImage Thumbnail { get; set; }
    }
    private ObservableCollection<Media> _medias = new();

    public WritePostControl(Button button = null)
    {
        InitializeComponent();
        _inputControl = new InputControl();
        _button = button;
        FrInputControl.Content = _inputControl;
        _inputControl.SetMinHeight(200);
        _inputControl.SetWidth(Width);
        _inputControl.SetMaxHeight(300);
        _inputControl.AcceptReturn(true);
        _inputControl.WrapText(true);
        _inputControl.OnImagePasted += OnImagePasted;
        _inputControl.OnSubmitShortcutActivated += OnSubmitShortcutActivated;
        LvMedias.ItemsSource = _medias;
    }

    private async void OnSubmitShortcutActivated() => await WritePostAsync();

    private async void OnImagePasted(string temporaryImageFilePath)
    {
        var file = await StorageFile.GetFileFromPathAsync(temporaryImageFilePath);
        await AddMediaFromFile(file);
    }

    private readonly List<string> _permissons = new()
    {
        "A",
        "F",
        "P",
        "M"
    };

    private async void OnWriteButtonClicked(object sender, RoutedEventArgs e) => await WritePostAsync();

    private async Task WritePostAsync()
    {
        BtWritePost.IsEnabled = false;
        var quoteDatas = StoryApi.Utils.GetQuoteDataFromString(_inputControl.GetTextBox().Text);
        PbMain.Visibility = Visibility.Visible;
        var mediaData = new MediaData();
        if (_medias.Count > 0)
        {
            var medias = new List<MediaData.MediaObject>();
            foreach (var rawMedia in _medias)
            {
                var media = new MediaData.MediaObject();
                var path = rawMedia.Path;
                if (!rawMedia.IsVideo)
                {
                    var type = path.ToLower().EndsWith(".gif") ? "gif" : "image";
                    var key = await StoryApi.ApiHandler.UploadImage(new AssetData()
                    {
                        Path = path,
                    });
                    media.media_path = key;
                    media.media_type = type;
                }
                else
                {
                    var key = await StoryApi.ApiHandler.UploadVideo(new()
                    {
                        Path = path,
                    });
                    media.media_path = key;
                    await StoryApi.ApiHandler.WaitForVideoUploadFinish(key);
                    media.media_type = "video";
                }
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
        await StoryApi.ApiHandler.WritePost(quoteDatas, mediaData, _permissons[CbxPermission.SelectedIndex], true, true, null, null);
        PbMain.Visibility = Visibility.Collapsed;
        BtWritePost.IsEnabled = true;
        _button?.Flyout.Hide();
        OnPostCompleted.Invoke();
    }

    private async void OnAddMediaButtonClicked(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        var fileOpenPicker = new FileOpenPicker();
        InitializeWithWindow.Initialize(fileOpenPicker, WindowNative.GetWindowHandle(MainWindow.Instance));
        fileOpenPicker.FileTypeFilter.Add(".jpg");
        fileOpenPicker.FileTypeFilter.Add(".png");
        fileOpenPicker.FileTypeFilter.Add(".gif");
        fileOpenPicker.FileTypeFilter.Add(".mp4");
        var files = (await fileOpenPicker.PickMultipleFilesAsync()).ToList();
        foreach(var file in files) await AddMediaFromFile(file);
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
            var image = Utility.GenerateImageUrlSource("ms-appx:///Assets/Video.png");
            media.Thumbnail = image;
        }

        _medias.Add(media);
        LvMedias.Visibility = Visibility.Visible;
    }

    private void OnMediaListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var listView = sender as ListView;
        var media = listView.SelectedItem as Media;
        if(media == null) return;
        _medias.Remove(media);
        listView.SelectedItem = null;
        if (_medias.Count == 0) listView.Visibility = Visibility.Collapsed;
    }
}
