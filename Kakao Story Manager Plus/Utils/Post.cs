using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using RestSharp;
using StoryApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using static StoryApi.ApiHandler.DataType;

namespace KSMP.Utils
{
    public static class Post
    {
        public static async void SetTextContent(List<QuoteData> contentDecorators, RichTextBlock richTextBlock)
        {
            bool hasEmoticon = false;
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
                    wordCount += decorator.text.Length;
                }
                else if (decorator.type.Equals("emoticon"))
                {
                    hasEmoticon = true;
                    var container = new InlineUIContainer();
                    var url = await ApiHandler.GetEmoticonUrl(decorator.item_id, decorator.resource_id.ToString());
                    var image = new Image();
                    await Utility.SetEmoticonImage(url, image);
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
            if (wordCount == 0 && !hasEmoticon)
                richTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}
