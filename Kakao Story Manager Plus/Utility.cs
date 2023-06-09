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
using System.Security.Cryptography;
using System.Text.Unicode;
using System.Text;

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
            image.PointerEntered -= OnPointerEntered;
            image.PointerExited -= OnPointerExited;
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

    public static List<FrameworkElement> GenerateMedias(IEnumerable<Medium> mediums, bool overrideQuality = false)
    {
        if (mediums == null) return null;

        var medias = new List<FrameworkElement>();
        bool willUseEmbeddedVideoPlayer = (Configuration.GetValue("UseEmbeddedVideoPlayer") as bool?) ?? false;
        bool willUseRealGifInTimeline = (Configuration.GetValue("UseRealGifInTimeline") as bool?) ?? false;
        bool willUseGifInTimeline = (Configuration.GetValue("UseGifInTimeline") as bool?) ?? false;
        willUseGifInTimeline = willUseGifInTimeline || overrideQuality;
        willUseRealGifInTimeline = willUseRealGifInTimeline || overrideQuality;
        
        foreach (var medium in mediums)
        {
            var defaultUrl = willUseGifInTimeline ? medium?.origin_url : medium.thumbnail_url;
            
            //영상인 경우
            var url = defaultUrl ?? medium?.url_hq;
            if (medium?.url_hq != null) url = medium?.url_hq;
            
            // gif인 경우
            var isGifImage = medium?.origin_url?.Contains(".gif") ?? false;
            if (isGifImage)
                url = willUseRealGifInTimeline ? medium?.origin_url : medium.thumbnail_url;
            
            if (url == null) continue;
            else if (url.Contains(".mp4"))
            {
                if (willUseEmbeddedVideoPlayer)
                {
                    MediaPlayerElement video = GenerateVideoMediaPlayerElementFromUrl(url);
                    LoadedVideos.Add(video);
                    medias.Add(video);
                }
                else
                {
                    Image image = new() { Tag = url };

                    var bitmap = new BitmapImage();
                    bitmap.UriSource = new Uri("ms-appx:///Assets/Video.png");
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    image.Source = bitmap;
                    image.PointerEntered += OnPointerEntered;
                    image.PointerExited += OnPointerExited;
                    image.RightTapped += OnImageRightTapped;
                    
                    LoadedImages.Add(image);
                    medias.Add(image);
                }
            }
            else
            {
                Image image = new()
                {
                    Tag = medium.origin_url,
                    Stretch = Stretch.Uniform
                };
                image.PointerEntered += OnPointerEntered;
                image.PointerExited += OnPointerExited;
                image.RightTapped += OnImageRightTapped;
                SetImageUrlSource(image, url);
                
                medias.Add(image);
            }
        }

        return medias;
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
        else await SetImageClipboardFromUrl(url);
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
        await ShowMessageDialogAsync("동영상 URL이 클립보드에 저장되었습니다,", "안내");
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

    private static void OnPointerEntered(object sender, PointerRoutedEventArgs e) => MainPage.SetCursor(Microsoft.UI.Input.InputSystemCursorShape.Hand);
    private static void OnPointerExited(object sender, PointerRoutedEventArgs e) => MainPage.SetCursor(Microsoft.UI.Input.InputSystemCursorShape.Arrow);
    
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
        var hash = GetStringHashFromUrl(url, "KSMP_EMT_");
        var path = Path.Combine(Path.GetTempPath(), hash);
        var cachedImageExist = File.Exists(path);

        if (!cachedImageExist)
        {
            var client = new RestClient(url);
            var request = new RestRequest();
            request.Method = Method.Get;
            request.AddHeader("Referer", "https://story.kakao.com/");
            var bytes = await client.DownloadDataAsync(request);
            if (bytes == null && retryCount < MaxImageLoadRetryCount) await LoadEmoticonImage(image, url, ++retryCount);
            else if (bytes == null) return;
            File.WriteAllBytes(path, bytes);
        }

        var file = await StorageFile.GetFileFromPathAsync(path);
        using var stream = await file.OpenAsync(FileAccessMode.Read);
        await RunOnMainThreadAsync(async () => image.Source = await GenerateImageLocalFileStream(stream));
    }

    private static async Task LoadAnimatedEmoticonImage(Image image, string url, int retryCount = 0)
    {
        if (url == null) return;
        var hash = GetStringHashFromUrl(url, "KSMP_EMT_");
        var path = Path.Combine(Path.GetTempPath(), hash);
        var cachedImageExist = File.Exists(path);

        if (!cachedImageExist)
        {
            var client = new RestClient(url);
            var request = new RestRequest();
            request.Method = Method.Get;
            var bytes = await client.DownloadDataAsync(request);
            if (bytes == null && retryCount < MaxImageLoadRetryCount) await LoadAnimatedEmoticonImage(image, url, ++retryCount);
            else if (bytes == null) return;
            bytes = EmoticonDecryptor.DecodeImage(bytes);
            await Task.Run(() => EmoticonDecryptor.ConvertWebPToGif(bytes, path));
        }

        var file = await StorageFile.GetFileFromPathAsync(path);
        using var stream = await file.OpenAsync(FileAccessMode.Read);
        await RunOnMainThreadAsync(async () => image.Source = await GenerateImageLocalFileStream(stream));
    }

    private static string GetStringHashFromUrl(string url, string header = "")
    {
        if (url.Contains("?") && !url.Contains("dcinside.com")) url = url.Substring(0, url.IndexOf('?'));
        return header + Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(url))).Replace("/", "").Replace("+", "").Replace("=", "");
    }

    private static void LoadImage(Image image, string url)
    {
        var bitmap = new BitmapImage();
        image.Source = bitmap;
        bitmap.UriSource = new Uri(url);
        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
    }

    private static void LoadPersonPicture(PersonPicture personPicture, string url)
    {

        var bitmap = new BitmapImage();
        personPicture.ProfilePicture = bitmap;
        bitmap.UriSource = new Uri(url);
        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
    }

    public static async Task SetTextClipboard(string text, string message = "복사되었습니다.")
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
        if (!string.IsNullOrEmpty(message))
            await ShowMessageDialogAsync(message, "안내");
    }

    public static async Task SetImageClipboardFromUrl(string url, string message = "이미지가 클립보드에 저장되었습니다.")
    {
        var dataPackage = new DataPackage();
        dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromUri(new Uri(url)));
        Clipboard.SetContent(dataPackage);
        if (!string.IsNullOrEmpty(message))
            await ShowMessageDialogAsync(message, "안내");
    }


    public static void ChangeSystemMouseCursor(bool isHand)
    {
            if (isHand)
            MainPage.SetCursor(Microsoft.UI.Input.InputSystemCursorShape.Hand);
        else
            MainPage.SetCursor(Microsoft.UI.Input.InputSystemCursorShape.Arrow);
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


    public static void SaveCurrentStateAndRestartProgram()
	{
		SaveCurrentState();
		RestartProgram();
	}

	public static async Task RunOnMainThreadAsync(Func<Task> method)
	{
		var taskCompletionSource = new TaskCompletionSource();
		App.DispatcherQueue.TryEnqueue(async () =>
		{
			await method();
			taskCompletionSource.SetResult();
		});
		await taskCompletionSource.Task;
	}

	public static async Task RunOnMainThreadAsync(Action method)
	{
		var taskCompletionSource = new TaskCompletionSource();
		App.DispatcherQueue.TryEnqueue(() =>
		{
			method();
			taskCompletionSource.SetResult();
		});
		await taskCompletionSource.Task;
	}

	public static void RestartProgram()
	{
		var binaryPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
		binaryPath = binaryPath[..^4];
		binaryPath += ".exe";
		Process.Start(binaryPath);

		Environment.Exit(0);
	}

	public static async Task CheckVersion(Action<bool, string, int> progressFuncion)
	{
		var client = new WebClient();
		var remoteVersionString = await client.DownloadStringTaskAsync(new Uri("https://kagamine-rin.com/KSMP/version"));
		var localVersionString = Utils.Common.GetVersionString();
		if (localVersionString == null)
		{
			await ShowMessageDialogAsync("프로그램의 버전을 확인할 수 없습니다", "오류");
			return;
		}

		var remoteVersion = new Version(remoteVersionString);
		var localVersion = new Version(localVersionString);
		var result = remoteVersion.CompareTo(localVersion);

		if (result > 0)
		{
			await ShowMessageDialogAsync($"프로그램 업데이트가 필요합니다.\n확인을 누르시면 업데이트를 진행합니다.\n\n클라이언트 버전: {localVersionString}\n최신 버전: {remoteVersionString}", "안내");
			progressFuncion(true, "업데이터 다운로드 초기화중", -1);

			var tempFile = Path.Combine(Path.GetTempPath(), $"KSMP_{remoteVersionString}.msi");

			client.DownloadFileCompleted += (_, _) =>
			{
				Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
				Environment.Exit(0);
			};

			client.DownloadProgressChanged += (_, e) =>
				progressFuncion(true, $"업데이터 다운로드중 ({e.ProgressPercentage}%)", e.ProgressPercentage);

			await client.DownloadFileTaskAsync(new Uri("https://kagamine-rin.com/KSMP/Installer.msi"), tempFile);
		}
	}

	public static void SaveCurrentState(bool showTimeline = false, string id = null)
	{
		var appWindow = Instance.AppWindow;
		var presenter = appWindow.Presenter as OverlappedPresenter;
		var isMaximized = presenter.State == OverlappedPresenterState.Maximized;

		var restartFlagPath = Path.Combine(App.BinaryDirectory, "restart");
		var restartFlag = new ClassManager.RestartFlag
		{
			Cookies = ApiHandler.Cookies,
			LastArgs = MainPage.LastArgs,
			WasMaximized = isMaximized,
			LastFeedId = TimelinePage.LastFeedId
		};

        if (showTimeline)
        {
            restartFlag.ShowTimeline = true;
            restartFlag.LastFeedId = null;
        }
		else if (!string.IsNullOrEmpty(id))
		{
            restartFlag.LastArgs = id;
            restartFlag.LastFeedId = null;
        }
		var restartFlagString = JsonConvert.SerializeObject(restartFlag);
		File.WriteAllText(restartFlagPath, restartFlagString);
	}

	public static ScrollViewer GetScrollViewerFromBaseListView(ListViewBase listViewBase)
    {
        var child = VisualTreeHelper.GetChild(listViewBase, 0);
        Border border = child as Border;
        if (border == null) return null;
        return VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
    }

	public static async Task<ContentDialogResult> ShowMessageDialogAsync(string description, string title, bool showCancel = false, string primaryText = "확인", string secondaryText = "취소")
	{
		var window = new DialogWindow(title, description, showCancel, primaryText, secondaryText);
		window.Activate();

		var taskCompletionSource = new TaskCompletionSource<ContentDialogResult>();

		DialogWindow.ButtonClicked primaryButtonClicked = null;
		DialogWindow.ButtonClicked secondaryButtonClicked = null;

		primaryButtonClicked = () =>
		{
			taskCompletionSource.SetResult(ContentDialogResult.Primary);
			window.Close();
			window.PrimaryButtonClicked -= primaryButtonClicked;
			window.SecondaryButtonClicked -= secondaryButtonClicked;
		};

		secondaryButtonClicked = () =>
		{
			taskCompletionSource.SetResult(ContentDialogResult.Secondary);
			window.Close();
			window.PrimaryButtonClicked -= primaryButtonClicked;
			window.SecondaryButtonClicked -= secondaryButtonClicked;
		};

		window.PrimaryButtonClicked += primaryButtonClicked;
		window.SecondaryButtonClicked += secondaryButtonClicked;
		return await taskCompletionSource.Task;
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
        if (Instance == null) return ElementTheme.Light;

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
        InitializeWithWindow.Initialize(fileSavePicker, WindowNative.GetWindowHandle(Instance));
        var extension = Path.GetExtension(url).Split("?")[0];
        fileSavePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        fileSavePicker.FileTypeChoices.Add("Image File", new List<string>() { extension });
        fileSavePicker.SuggestedFileName = "Image";
        var file = await fileSavePicker.PickSaveFileAsync();
        return file;
    }
}
