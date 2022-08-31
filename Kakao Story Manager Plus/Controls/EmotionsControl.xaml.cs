using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static StoryApi.ApiHandler.DataType.CommentData;

namespace KSMP.Controls;

public sealed partial class EmotionsControl : UserControl
{
    private readonly PostData _post;
    private readonly TimelineControl _timeline;
    public EmotionsControl(PostData post, TimelineControl timeline)
    {
        InitializeComponent();
        _post = post;
        _timeline = timeline;
    }

    private async void OnEmotionButtonClick(object sender, RoutedEventArgs e)
    {
        var emotion = (sender as Button).Tag as string;
        await StoryApi.ApiHandler.LikePost(_post.id, emotion);
        await _timeline.RefreshContent();
        _timeline.HideEmotionsButtonFlyout();
    }
}
