using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Windows.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using static StoryApi.ApiHandler.DataType;
using static StoryApi.ApiHandler.DataType.CommentData;
using Windows.UI.Popups;
using Windows.UI.Core;
using System.Runtime.InteropServices;
using KSMP.Extension;
using Microsoft.UI;
using System.IO;
using Windows.Services.Maps;

namespace KSMP
{
    public static class Utility
    {
        public static List<BitmapImage> _generatedImages = new();
        public static void FlushBitmapImages()
        {
            _generatedImages.ForEach(x =>
            {
                x.UriSource = null;
                x.DisposeSource();
            });
            _generatedImages.Clear();
        }
        
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
                images.Add(image);
            }
            return images;
        }
        public static BitmapImage GenerateImageUrlSource(string url, bool shouldNotBeFlushed = false)
        {
            if (string.IsNullOrEmpty(url)) url = "ms-appx:///Assets/Error.png";
            var imageUrl = new Uri(url);
            var bitmap = new BitmapImage
            {
                UriSource = imageUrl
            };
            if (!shouldNotBeFlushed) _generatedImages.Add(bitmap);
            return bitmap;
        }

        public static void SetTextContent(List<QuoteData> contentDecorators, RichTextBlock richTextBlock)
        {
            var wordCount = 0;
            Paragraph paragraph = new();
            foreach (var decorator in contentDecorators)
            {
                if (decorator.type.Equals("profile"))
                {
                    var hyperlink = new Hyperlink
                    {
                        FontWeight = FontWeights.Bold,
                        UnderlineStyle = UnderlineStyle.None
                    };
                    hyperlink.Inlines.Add(new Run { Text = decorator.text });
                    hyperlink.Click += (s, e) =>
                    {
                        Pages.MainPage.HideOverlay();
                        Pages.MainPage.ShowProfile(decorator.id);
                    };
                    paragraph.Inlines.Add(hyperlink);
                }
                else
                {
                    var run = new Run();
                    var text = decorator.text;
                    run.Text = text;
                    if (decorator.type.Equals("hashtag"))
                        run.FontWeight = FontWeights.Bold;
                    paragraph.Inlines.Add(run);
                    wordCount += text.Length;
                }
            }
            richTextBlock.Blocks.Add(paragraph);
            if (wordCount == 0)
                richTextBlock.Visibility = Visibility.Collapsed;
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
