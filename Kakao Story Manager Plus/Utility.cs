using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using KSMP.Extension;
using Windows.Media.Core;
using Microsoft.UI.Xaml.Input;
using Windows.Storage;
using System.IO;
using RestSharp;
using KSMP.Pages;
using KSMP.Utils;
using Microsoft.UI.Windowing;
using Newtonsoft.Json;
using System.Diagnostics;
using Windows.Foundation;
using static KSMP.ApiHandler.DataType.CommentData;
using Newtonsoft.Json.Linq;
using Microsoft.Win32;
using static KSMP.MainWindow;
using System.Runtime.InteropServices;
using System.Net;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace KSMP;

public static class Utility
{
    private const int MaxImageLoadRetryCount = 5;

    public readonly static List<MediaPlayerElement> LoadedVideos = new();
    public readonly static List<Image> LoadedImages = new();
    public readonly static List<PersonPicture> LoadedPersonPictures = new();

    public static void ManuallyDisposeAllMedias()
    {
        LoadedPersonPictures.ForEach(image => image?.DisposeImage());
        LoadedPersonPictures.Clear();

        LoadedImages.ForEach(image =>
        {
            image.RightTapped -= OnImageRightTapped;
            image?.DisposeImage();
        });
        LoadedImages.Clear();

        LoadedVideos.ForEach(video =>
        {
            if (video != null)
            {
                video.PointerEntered -= OnVideoPointerEntered;
                video.PointerExited -= OnVideoPointerExited;
                video.RightTapped -= OnVideoRightTapped;
                video.Tapped -= OnVideoTapped;
            }
            video?.DisposeVideo();
        });
        LoadedVideos.Clear();
    }

    public static List<FrameworkElement> GenerateMedias(IEnumerable<Medium> mediums)
    {
        if (mediums == null) return null;

        var medias = new List<FrameworkElement>();
        foreach (var medium in mediums)
        {
            bool willUseGifInTimeline = (Configuration.GetValue("UseGifInTimeline") as bool?) ?? false;
            var defaultUrl = willUseGifInTimeline ? medium?.origin_url : medium.thumbnail_url;
            var url = defaultUrl ?? medium?.url_hq;
            if (medium?.url_hq != null) url = medium?.url_hq;
            if (url == null) continue;
            else if (url.Contains(".mp4"))
            {
                MediaPlayerElement video = GenerateVideoMediaPlayerElementFromUrl(url);
                LoadedVideos.Add(video);
                medias.Add(video);
            }
            else
            {
                Image image = GenerateImageFromUrl(medium);
                LoadedImages.Add(image);
                medias.Add(image);
            }
        }

        return medias;
    }

    private static Image GenerateImageFromUrl(Medium medium)
    {
        var image = new Image();

        bool willUseGifInTimeline = (Configuration.GetValue("UseGifInTimeline") as bool?) ?? false;
        var url = willUseGifInTimeline ? medium?.origin_url : medium.thumbnail_url;
        SetImageUrlSource(image, url);

        image.Tag = medium.origin_url;
        image.Stretch = Stretch.Uniform;
        image.RightTapped += OnImageRightTapped;

        return image;
    }

    private static async void OnImageRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var image = sender as Image;
        var url = image.Tag as string;
        if (url.Contains(".gif"))
        {
            var file = await ShowImageFileSaveDialogAsync(url);
            if (file == null) return;
            var path = file.Path;

            await new WebClient().DownloadFileTaskAsync(url, path);
        }
        else await SetImageClipboardFromUrl(image, url);
    }

    private static MediaPlayerElement GenerateVideoMediaPlayerElementFromUrl(string url)
    {
        var video = new MediaPlayerElement();

        video.Tag = url;

        video.Source = MediaSource.CreateFromUri(new Uri(url));

        video.TransportControls.IsVolumeEnabled = true;
        video.TransportControls.IsVolumeButtonVisible = true;
        video.TransportControls.IsZoomEnabled = true;
        video.TransportControls.IsZoomButtonVisible = true;

        video.AreTransportControlsEnabled = true;
        video.AutoPlay = false;

        video.MediaPlayer.IsLoopingEnabled = true;

        video.PointerEntered += OnVideoPointerEntered;
        video.PointerExited += OnVideoPointerExited;
        video.RightTapped += OnVideoRightTapped;
        video.Tapped += OnVideoTapped;

        return video;
    }

    private static async void OnVideoRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        var url = element.Tag as string;
        var dataPackage = new DataPackage();
        dataPackage.SetText(url);
        dataPackage.RequestedOperation = DataPackageOperation.Copy;
        Clipboard.SetContent(dataPackage);
        await element.ShowMessageDialogAsync("동영상 URL이 클립보드에 저장되었습니다,", "안내");
    }

    public static async Task SetEmoticonImageAsync(Image image, string url)
    {
        LoadedImages.Add(image);
        await LoadEmoticonImage(image, url);
    }

    public static async Task SetAnimatedEmoticonImage(Image image, string url)
    {
        LoadedImages.Add(image);
        await LoadAnimatedEmoticonImage(image, url);
    }

    private static void OnVideoPointerEntered(object sender, PointerRoutedEventArgs e) => (sender as MediaPlayerElement).TransportControls.Show();
    private static void OnVideoPointerExited(object sender, PointerRoutedEventArgs e) => (sender as MediaPlayerElement).TransportControls.Hide();
    private static void OnVideoTapped(object sender, TappedRoutedEventArgs e) => (sender as MediaPlayerElement).TransportControls.Show();

    public static async Task<BitmapImage> GenerateImageLocalFileStream(IRandomAccessStream fileStream, int width = 80, int height = 80)
    {
        var bitmap = new BitmapImage
        {
            DecodePixelWidth = width,
            DecodePixelHeight = height,
        };
        await bitmap.SetSourceAsync(fileStream);
        return bitmap;
    }

    public static void SetPersonPictureUrlSource(PersonPicture personPicture, string url, bool shouldDispose = true)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        if (shouldDispose) LoadedPersonPictures.Add(personPicture);
        LoadPersonPicture(personPicture, url);
    }

    public static void SetImageUrlSource(Image image, string url)
    {
        LoadedImages.Add(image);
        LoadImage(image, url);
    }

    private static async Task LoadEmoticonImage(Image image, string url, int retryCount = 0)
    {
        if (url == null) return;

        var path = Path.GetTempFileName();

        var client = new RestClient(url);
        var request = new RestRequest();
        request.Method = Method.Get;
        request.AddHeader("Referer", "https://story.kakao.com/");
        var bytes = await client.DownloadDataAsync(request);
        if (bytes == null && retryCount < MaxImageLoadRetryCount) await LoadEmoticonImage(image, url, ++retryCount);
        else if (bytes == null) return;

        try
        {
            File.WriteAllBytes(path, bytes);
            var file = await StorageFile.GetFileFromPathAsync(path);
            using var stream = await file.OpenAsync(FileAccessMode.Read);
            await MainPage.GetInstance().RunOnMainThreadAsync(async () => image.Source = await GenerateImageLocalFileStream(stream));
        }
        catch (Exception) { }
        finally { File.Delete(path); }
    }

    private static async Task LoadAnimatedEmoticonImage(Image image, string url, int retryCount = 0)
    {
        if (url == null) return;

        var client = new RestClient(url);
        var request = new RestRequest();
        request.Method = Method.Get;
        var bytes = await client.DownloadDataAsync(request);
        if (bytes == null && retryCount < MaxImageLoadRetryCount) await LoadAnimatedEmoticonImage(image, url, ++retryCount);
        else if (bytes == null) return;

        var path = Path.GetTempFileName();
        try
        {
            bytes = EmoticonDecryptor.DecodeImage(bytes);
            await Task.Run(() => EmoticonDecryptor.ConvertWebPToGif(bytes, path));
            var file = await StorageFile.GetFileFromPathAsync(path);
            using var stream = await file.OpenAsync(FileAccessMode.Read);
            await MainPage.GetInstance().RunOnMainThreadAsync(async () => image.Source = await GenerateImageLocalFileStream(stream));
        }
        catch (Exception) { }
        finally { File.Delete(path); }
    }

    private static void LoadImage(Image image, string url)
    {
        var bitmap = new BitmapImage();
        bitmap.UriSource = new Uri(url);
        image.Source = bitmap;
    }

    private static void LoadPersonPicture(PersonPicture personPicture, string url)
    {

        var bitmap = new BitmapImage();
        bitmap.UriSource = new Uri(url);
        personPicture.ProfilePicture = bitmap;
    }

    public static async Task SetTextClipboard(UIElement element, string text, string message = "복사되었습니다.")
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
        if (!string.IsNullOrEmpty(message))
            await element.ShowMessageDialogAsync(message, "안내");
    }

    public static async Task SetImageClipboardFromUrl(UIElement element, string url, string message = "이미지가 클립보드에 저장되었습니다.")
    {
        var dataPackage = new DataPackage();
        dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromUri(new Uri(url)));
        Clipboard.SetContent(dataPackage);
        if (!string.IsNullOrEmpty(message))
            await element.ShowMessageDialogAsync(message, "안내");
    }


    public static void ChangeSystemMouseCursor(bool isHand)
    {
        //TODO: Implement
    }

    public static SolidColorBrush GetSolidColorBrushFromHexString(string hex)
    {
        return new SolidColorBrush(
            Color.FromArgb(
                Convert.ToByte(hex.Substring(1, 2), 16),
                Convert.ToByte(hex.Substring(3, 2), 16),
                Convert.ToByte(hex.Substring(5, 2), 16),
                Convert.ToByte(hex.Substring(7, 2), 16)
            )
        );
    }

    public static async Task<string> GenerateClipboardImagePathAsync(DataPackageView dataPackageView = null)
    {
        dataPackageView ??= Clipboard.GetContent();

        var filePath = Path.GetTempFileName();

        var rawImage = await dataPackageView.GetBitmapAsync();

        using var imageStream = await rawImage.OpenReadAsync();
        var stream = await StorageFile.GetFileFromPathAsync(filePath);
        using var writeStream = await stream.OpenStreamForWriteAsync();
        await imageStream.AsStreamForRead().CopyToAsync(writeStream);
        writeStream.Close();

        return filePath;
    }


    public static async void SaveCurrentStateAndRestartProgram()
    {
        var appWindow = MainWindow.Instance.GetAppWindow();
        var presenter = appWindow.Presenter as OverlappedPresenter;
        var isMaximized = presenter.State == OverlappedPresenterState.Maximized;
        var overlay = MainPage.GetOverlayTimeLineControl();
        var postId = overlay?.PostId;

        var restartFlagPath = Path.Combine(App.BinaryDirectory, "restart");
        var RestartFlag = new ClassManager.RestartFlag
        {
            Cookies = ApiHandler.Cookies,
            LastArgs = MainPage.LastArgs,
            WasMaximized = isMaximized,
            PostId = postId,
            LastFeedId = TimelinePage.LastFeedId,
        };
        var restartFlagString = JsonConvert.SerializeObject(RestartFlag);
        File.WriteAllText(restartFlagPath, restartFlagString);

        var binaryPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        binaryPath = binaryPath[..^4];
        binaryPath += ".exe";
        Process.Start(binaryPath);

        MainWindow.Instance.SetClosable();
        await Task.Delay(100);
        MainWindow.Instance.Close();
    }

    public static ScrollViewer GetScrollViewerFromBaseListView(ListViewBase listViewBase)
    {
        Border border = VisualTreeHelper.GetChild(listViewBase, 0) as Border;
        if (border == null) return null;
        return VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
    }

    public static List<Api.DcCon.DataType.Package> GetCurrentDcConList()
    {
        var data = Configuration.GetValue("DcConList") as JArray ?? new();
        return data.ToObject<List<Api.DcCon.DataType.Package>>();
    }

    public static bool IsVisibleToUser(Control control, ScrollViewer scrollViewer, double margin = 0)
    {
        var top = control
            .TransformToVisual(scrollViewer)
            .TransformPoint(new Point(0, 0));

        var elementTop = top.Y;
        var elementBottom = top.Y + control.ActualHeight;

        var isVisible = elementBottom > -margin && elementTop < scrollViewer.ViewportHeight + margin;
        return isVisible;
    }

    public static bool IsSystemUsesLightTheme
    {
        get
        {
            var isLightTheme = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", "1");
            return isLightTheme?.ToString() == "1";
        }
    }

    [DllImport("Shcore.dll", SetLastError = true)]
    public static extern int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

    public static ElementTheme GetRequestedTheme()
    {
        var requestedTheme = (Instance.Content as FrameworkElement).RequestedTheme;
        ElementTheme theme;
        if (requestedTheme != ElementTheme.Default) theme = requestedTheme;
        else
        {
            if (IsSystemUsesLightTheme) theme = ElementTheme.Light;
            else theme = ElementTheme.Dark;
        }
        return theme;
    }

    public static async Task<StorageFile> ShowImageFileSaveDialogAsync(string url)
    {
        var fileSavePicker = new FileSavePicker();
        InitializeWithWindow.Initialize(fileSavePicker, WindowNative.GetWindowHandle(MainWindow.Instance));
        var extension = Path.GetExtension(url).Split("?")[0];
        fileSavePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        fileSavePicker.FileTypeChoices.Add("Image File", new List<string>() { extension });
        fileSavePicker.SuggestedFileName = "Image";
        var file = await fileSavePicker.PickSaveFileAsync();
        return file;
    }
}
