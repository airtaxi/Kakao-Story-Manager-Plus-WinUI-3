using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static KSMP.ApiHandler.DataType.CommentData;

namespace KSMP.Controls;

public sealed partial class EmotionsWindowControl : UserControl
{
    private readonly PostData _post;
    private readonly TimelineWindow _window;
    public EmotionsWindowControl(PostData post, TimelineWindow window)
    {
        InitializeComponent();
        _post = post;
        _window = window;
    }

    private async void OnButtonClick(object sender, RoutedEventArgs e)
    {
        var emotion = (sender as Button).Tag as string;
        await ApiHandler.LikePost(_post.id, emotion);
        await _window.RefreshContent();
        _window.HideEmotionsButtonFlyout();
    }
}
