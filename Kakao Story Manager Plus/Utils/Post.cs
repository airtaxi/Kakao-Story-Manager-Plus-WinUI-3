using KSMP.Controls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using static KSMP.ApiHandler.DataType;

namespace KSMP.Utils;

public static class Post
{
    public static async void SetTextContent(List<QuoteData> contentDecorators, RichTextBlock richTextBlock, bool isOverlay)
    {
        bool hasEmoticon = false;
        var wordCount = 0;
        Paragraph paragraph = new();
        Run newLineForEmoticon = null;
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
				TypedEventHandler<Hyperlink, HyperlinkClickEventArgs> hyperlinkClick = (s, e) =>
				{
					Pages.MainPage.HideOverlay();
					Pages.MainPage.ShowProfile(decorator.id);
				};
				hyperlink.Click += hyperlinkClick;

				{
					RoutedEventHandler unloaded = null;
					unloaded = (s, e) =>
					{
                        hyperlink.Click -= hyperlinkClick;
						richTextBlock.Unloaded -= unloaded;
					};
					richTextBlock.Unloaded += unloaded;
				}

				paragraph.Inlines.Add(hyperlink);
                wordCount += decorator.text.Length;
            }
            else if (decorator.type.Equals("emoticon"))
            {
                hasEmoticon = true;

                var image = new Image();
                var container = new InlineUIContainer();
                var url = "";
                var isAnimatedEmoticon = decorator.item_id.StartsWith("4");
                if (isAnimatedEmoticon && isOverlay)
                {
                    url = $"http://item-kr.talk.kakao.co.kr/dw/{decorator.item_id}.emot_{decorator.resource_id.ToString().PadLeft(3, '0')}.webp";
                    await Utility.SetAnimatedEmoticonImage(image, url);
                }
                else
                {
                    url = await ApiHandler.GetEmoticonUrl(decorator.item_id, decorator.resource_id.ToString());
                    await Utility.SetEmoticonImageAsync(image, url);
                }

                image.Width = 80;
                image.Height = 80;
                richTextBlock.Tag = image;
                container.Child = image;
                paragraph.Inlines.Add(container);
                newLineForEmoticon = new Run() { Text = "\n" };
                paragraph.Inlines.Add(newLineForEmoticon);
            }
            else
            {
                var run = new Run();
                var text = decorator.text ?? string.Empty;
                if (text == "(Image) ") text = string.Empty;
                run.Text = text;

                if (decorator.type == "hashtag")
                    run.FontWeight = FontWeights.Bold;

                paragraph.Inlines.Add(run);
                wordCount += text.Length;
            }
        }
        richTextBlock.Blocks.Clear();
        richTextBlock.Blocks.Add(paragraph);
        if (wordCount == 0 && !hasEmoticon)
            richTextBlock.Visibility = Visibility.Collapsed;
        else if (wordCount == 0) paragraph.Inlines.Remove(newLineForEmoticon);
    }

    public static EmoticonListControl ShowEmoticonListToButton(Button button, InputControl inputControl = null)
    {
        var flyout = new Flyout();
        button.Flyout = flyout;

        var emoticonListControl = new EmoticonListControl();
        flyout.Content = emoticonListControl;

		EmoticonListControl.Selected emoticonListControlOnSelected = (item, index) =>
		{
			inputControl?.AddEmoticon(item, index);
			flyout.Hide();
		};
		emoticonListControl.OnSelected += emoticonListControlOnSelected;

		{
			RoutedEventHandler unloaded = null;
			unloaded = (s, e) =>
			{
				emoticonListControl.OnSelected -= emoticonListControlOnSelected;
                inputControl.Unloaded -= unloaded;
			}
            if (inputControl != null) inputControl.Unloaded += unloaded;
		}

		flyout.ShowAt(button);
        return emoticonListControl;
    }
}
