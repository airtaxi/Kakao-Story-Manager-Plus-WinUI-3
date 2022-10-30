using Microsoft.UI.Xaml.Controls;
using Windows.Media.Core;

namespace KSMP.Extension
{
    public static class MediaPlayerElementExtension
    {
        public static void DisposeVideo(this MediaPlayerElement video)
        {
            (video.Source as MediaSource)?.Dispose();
            video.Source = null;
        }
    }
}
