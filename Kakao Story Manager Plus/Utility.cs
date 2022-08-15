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

namespace KSMP
{
    public static class Utility
    {
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

        public static void SetMediaContent(UIElement element, List<Medium> mediums, FlipView flipView)
        {
            var medias = new List<UIElement>();
            foreach (var media in mediums)
            {
                var isVideo = media.content_type == "video/mp4";
                if (isVideo)
                {
                    //var videoMedia = new MediaElement();
                    //videoMedia.Source = new Uri(media.url_hq);
                    //videoMedia.AreTransportControlsEnabled = true;
                    //videoMedia.IsMuted = true;
                    //videoMedia.AutoPlay = false;
                    //videoMedia.RightTapped += async (s, e) =>
                    //{
                    //    await Utils.SetTextClipboard(media.url_hq, "링크가 복사되었습니다.");
                    //};
                    var tempTextBlock = new TextBlock() { Text = "(영상 미지원)", FontSize = 20, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Foreground = new SolidColorBrush(Colors.Black) };
                    medias.Add(tempTextBlock);
                }
                else
                {
                    var imageMedia = new Image
                    {
                        Stretch = Stretch.UniformToFill,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    var bitmapImage = new BitmapImage
                    {
                        UriSource = new Uri(media.origin_url)
                    };
                    imageMedia.Source = bitmapImage;
                    imageMedia.RightTapped += async (s, e) =>
                    {
                        await SetImageClipboardFromUrl(element, media.origin_url);
                    };
                    medias.Add(imageMedia);
                }
            }
            flipView.Visibility = Visibility.Visible;
            flipView.ItemsSource = medias;
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
