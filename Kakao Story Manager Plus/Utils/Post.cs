using ABI.System;
using KSMP.Extension;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using RestSharp;
using StoryApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Windows.Services.Maps;
using static StoryApi.ApiHandler.DataType;
using static StoryApi.ApiHandler.DataType.CommentData;
using Uri = System.Uri;

namespace KSMP.Utils
{
    public static class Post
    {
        public static async void SetTextContent(List<QuoteData> contentDecorators, RichTextBlock richTextBlock)
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
                else if (decorator.type.Equals("emoticon"))
                {
                    var container = new InlineUIContainer();
                    var url = await ApiHandler.GetEmoticonUrl(decorator.item_id, decorator.resource_id);
                    var image = new Image();
                    var path = Path.Combine(Path.GetTempPath(), $"thumb_{decorator.item_id}_{decorator.resource_id.PadLeft(3, '0')}.png");
                    if (!File.Exists(path))
                    {
                        var client = new RestClient(url);
                        var request = new RestRequest();
                        request.Method = Method.Get;
                        request.AddHeader("Referer", "https://story.kakao.com/");
                        var bytes = await client.DownloadDataAsync(request);
                        File.WriteAllBytes(path, bytes);
                    }

                    image.Source = Utility.GenerateImageUrlSource(path);
                    image.Width = 80;
                    image.Height = 80;
                    container.Child = image;
                    paragraph.Inlines.Add(container);
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
    }
}
