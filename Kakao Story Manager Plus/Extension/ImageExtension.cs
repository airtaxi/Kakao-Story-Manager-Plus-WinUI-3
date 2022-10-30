using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace KSMP.Extension
{
    public static class ImageExtension
    {
        public static void DisposeImage(this Image image)
        {
            var bitmapSource = image.Source as BitmapImage;
            if (bitmapSource == null) return;
            bitmapSource.DisposeSource();
            image.Source = null;
        }
        public static void DisposeImage(this PersonPicture image)
        {
            var bitmapSource = image.ProfilePicture as BitmapImage;
            if (bitmapSource == null) return;
            bitmapSource.DisposeSource();
            image.ProfilePicture = null;
        }
    }
}
