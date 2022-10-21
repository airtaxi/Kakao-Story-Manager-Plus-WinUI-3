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
using OpenQA.Selenium.DevTools.V104.Page;
using Windows.Media.Core;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using static StoryApi.ApiHandler.DataType.VideoData;
using Windows.Storage;
using System.IO;
using RestSharp;
using KSMP.Pages;

namespace KSMP
{
    public static class Utility
    {
        public static List<MediaPlayerElement> LoadedVideos = new();
        public static List<Image> LoadedImges = new();

        public static void DisposeAllMedias()
        {
            LoadedImges.ForEach(image => image.DisposeImage());
            LoadedImges.Clear();

            LoadedVideos.ForEach(video =>
            {
                video.PointerEntered -= OnVideoPointerEntered;
                video.PointerExited -= OnVideoPointerExited;
                (video.Source as MediaSource)?.Dispose();
                video.Source = null;
            });
            LoadedVideos.Clear();
        }

        public static List<FrameworkElement> GenerateMedias(IEnumerable<string> urls)
        {
            if (urls == null) return null;
            var medias = new List<FrameworkElement>();
            foreach(var url in urls)
            {
                if (url.Contains(".mp4"))
                {
                    var video = new MediaPlayerElement();

                    video.Source = MediaSource.CreateFromUri(new Uri(url));

                    video.TransportControls.IsVolumeEnabled = true;
                    video.TransportControls.IsVolumeButtonVisible = true;
                    video.TransportControls.IsZoomEnabled = true;
                    video.TransportControls.IsZoomButtonVisible = true;

                    video.AreTransportControlsEnabled = true;
                    video.MediaPlayer.IsLoopingEnabled = true;
                    video.AutoPlay = false;

                    video.PointerEntered += OnVideoPointerEntered;
                    video.PointerExited += OnVideoPointerExited;

                    LoadedVideos.Add(video);
                    medias.Add(video);
                }
                else
                {
                    var image = new Image();

                    image.Source = GenerateImageUrlSource(url);
                    image.Tag = url;
                    image.Stretch = Stretch.Uniform;

                    LoadedImges.Add(image);
                    medias.Add(image);
                }
            }
            return medias;
        }


        public static async Task SetEmoticonImage(string url, Image image)
        {
            LoadedImges.Add(image);
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

        public static void OnVideoPointerEntered(object sender, PointerRoutedEventArgs e) => (sender as MediaPlayerElement).TransportControls.Show();
        public static void OnVideoPointerExited(object sender, PointerRoutedEventArgs e) => (sender as MediaPlayerElement).TransportControls.Hide();

        public static async Task<BitmapImage> GenerateImageLocalFileStream(IRandomAccessStream fileStream, int width = 80, int height = 80)
        {
            var bitmap = new BitmapImage();
            bitmap.DecodePixelWidth = width;
            bitmap.DecodePixelHeight = height;
            await bitmap.SetSourceAsync(fileStream);
            return bitmap;
        }

        public static BitmapImage GenerateImageUrlSource(string url)
        {
            if (string.IsNullOrEmpty(url)) url = "ms-appx:///Assets/Error.png";
            var imageUrl = new Uri(url);
            var bitmap = new BitmapImage
            {
                UriSource = imageUrl
            };
            return bitmap;
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
    }
}
