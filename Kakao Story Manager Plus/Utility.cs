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
using StoryApi;
using System.Runtime.CompilerServices;
using ImageMagick;
using static StoryApi.ApiHandler.DataType.CommentData;

namespace KSMP
{
    public static class Utility
    {
        public static List<MediaPlayerElement> LoadedVideos = new();
        public static List<Image> LoadedImages = new();
        public static List<PersonPicture> LoadedPersonPictures = new();

        public static void DisposeAllMedias()
        {
            LoadedPersonPictures.ForEach(image => image.DisposeImage());
            LoadedPersonPictures.Clear();

            LoadedImages.ForEach(image => image.DisposeImage());
            LoadedImages.Clear();

            LoadedVideos.ForEach(video =>
            {
                video.PointerEntered -= OnVideoPointerEntered;
                video.PointerExited -= OnVideoPointerExited;
                video.Tapped -= OnVideoTapped;
                video.DisposeVideo();
            });
            LoadedVideos.Clear();
        }

        public static List<FrameworkElement> GenerateMedias(IEnumerable<Medium> mediums)
        {
            if (mediums == null) return null;

            var medias = new List<FrameworkElement>();
            foreach (var medium in mediums)
            {
                var url = medium?.origin_url ?? medium?.url_hq;
                if (url == null) continue;
                else if (url.Contains(".mp4"))
                {
                    var video = new MediaPlayerElement();

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
                    video.Tapped += OnVideoTapped;

                    LoadedVideos.Add(video);
                    medias.Add(video);
                }
                else
                {
                    var image = new Image();
                    var finalUrl = medium.thumbnail_url3 ?? medium.origin_url;
                    SetImageUrlSource(image, finalUrl);
                    image.Tag = medium.origin_url;
                    image.Stretch = Stretch.Uniform;

                    LoadedImages.Add(image);
                    medias.Add(image);
                }
            }
            return medias;
        }

        public static async Task SetEmoticonImage(string url, Image image)
        {
            if (url == null) return;

            LoadedImages.Add(image);

            var path = Path.GetTempFileName();

            var client = new RestClient(url);
            var request = new RestRequest();
            request.Method = Method.Get;
            request.AddHeader("Referer", "https://story.kakao.com/");
            var bytes = await client.DownloadDataAsync(request);
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

        public static async Task SetAnimatedEmoticonImage(string url, Image image)
        {
            if (url == null) return;

            LoadedImages.Add(image);

            var path = Path.GetTempFileName();

            var client = new RestClient(url);
            var request = new RestRequest();
            request.Method = Method.Get;
            var bytes = await client.DownloadDataAsync(request);
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

        public static async void SetImageUrlSource(PersonPicture image, string url, bool shouldDispose = true)
        {
            if (shouldDispose) LoadedPersonPictures.Add(image);
            await LoadImage(image, url);
        }

        public static async void SetImageUrlSource(Image image, string url)
        {
            LoadedImages.Add(image);
            await LoadImage(image, url);
        }

        private static async Task LoadImage(Image image, string url)
        {
            if (url == null) return;

            var client = new RestClient(url);
            var bytes = await client.DownloadDataAsync(new());
            var path = Path.GetTempFileName();
            try
            {
                await File.WriteAllBytesAsync(path, bytes);

                var file = await StorageFile.GetFileFromPathAsync(path);
                var stream = await file.OpenAsync(FileAccessMode.Read);
                var bitmap = new BitmapImage();
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.Source = bitmap;
                await bitmap.SetSourceAsync(stream);
                stream.Dispose();
            }
            finally { File.Delete(path); }
        }

        private static async Task LoadImage(PersonPicture image, string url)
        {
            if (url == null) return;

            var client = new RestClient(url);
            var bytes = await client.DownloadDataAsync(new());
            var path = Path.GetTempFileName();
            try
            {
                await File.WriteAllBytesAsync(path, bytes);

                var file = await StorageFile.GetFileFromPathAsync(path);
                var stream = await file.OpenAsync(FileAccessMode.Read);
                var bitmap = new BitmapImage();
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.ProfilePicture = bitmap;
                await bitmap.SetSourceAsync(stream);
                stream.Dispose();
            }
            finally { File.Delete(path); }
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


        public static void ChangeCursor(bool isHand)
        {
            //TODO: Implement
        }

        public static SolidColorBrush GetColorFromHexa(string hexaColor)
        {
            return new SolidColorBrush(
                Color.FromArgb(
                    Convert.ToByte(hexaColor.Substring(1, 2), 16),
                    Convert.ToByte(hexaColor.Substring(3, 2), 16),
                    Convert.ToByte(hexaColor.Substring(5, 2), 16),
                    Convert.ToByte(hexaColor.Substring(7, 2), 16)
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


        public static async void RestartProgram()
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

        public static ScrollViewer GetScrollViewer(ListView listView)
        {
            Border border = VisualTreeHelper.GetChild(listView, 0) as Border;
            if (border == null) return null;
            return VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
        }
    }
}
