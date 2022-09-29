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

namespace KSMP
{
    public static class Utility
    {   
        public static List<Image> GenerateMedias(IEnumerable<string> urls)
        {
            if (urls == null) return null;
            var images = new List<Image>();
            foreach(var url in urls)
            {
                var image = new Image();
                var finalUrl = url;
                if (url.Contains(".mp4")) finalUrl = "ms-appx:///Assets/VideoThumbnail.png";
                image.Source = GenerateImageUrlSource(finalUrl);
                image.Tag = url;
                image.Stretch = Stretch.Uniform;
                images.Add(image);
            }
            return images;
        }
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
    }
}
