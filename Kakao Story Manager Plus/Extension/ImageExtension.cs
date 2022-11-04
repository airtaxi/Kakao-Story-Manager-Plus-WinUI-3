using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace KSMP.Extension;

public static class ImageExtension
{
    public static void DisposeImage(this Image image)
    {
        (image.Source as BitmapImage)?.DisposeSource();
        image.Source = null;
    }

    public static void DisposeImage(this PersonPicture image)
    {
        (image.ProfilePicture as BitmapImage)?.DisposeSource();
        image.ProfilePicture = null;
    }
}
