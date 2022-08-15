using ABI.System;
using KSMP.Extension;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static StoryApi.ApiHandler.DataType;
using static StoryApi.ApiHandler.DataType.CommentData;
using Uri = System.Uri;

namespace KSMP.Utils
{
    public static class Post
    {
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
            richTextBlock.Blocks.Clear();
            richTextBlock.Blocks.Add(paragraph);
            if (wordCount == 0)
                richTextBlock.Visibility = Visibility.Collapsed;
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
                    //    await pageContext.SetTextClipboard(media.url_hq, "링크가 복사되었습니다.");
                    //};
                    //medias.Add(videoMedia);

                    var videoThumbnail = new Image
                    {
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    var bitmapImage = new BitmapImage
                    {
                        UriSource = new Uri("ms-appx:///Assets/VideoThumbnail.png")
                    };
                    videoThumbnail.Source = bitmapImage;
                    videoThumbnail.Tag = media.url_hq;
                    videoThumbnail.Tapped += (s, e) => Process.Start(new ProcessStartInfo(media.url_hq) { UseShellExecute = true });
                    medias.Add(videoThumbnail);
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
                        await Utility.SetImageClipboardFromUrl(element, media.origin_url);
                    };
                    medias.Add(imageMedia);
                }
            }
            flipView.Visibility = Visibility.Visible;
            flipView.ItemsSource = medias;
        }
    }
}
