using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;

namespace KSMP.Extension;

public static class BitmapImageExtension
{
    public static void DisposeSource(this BitmapImage bitmapImage)
    {
        if (bitmapImage == null) return;
        try
        {
            bitmapImage.UriSource = null;
            using var ms = new MemoryStream(new byte[] { 0x0 });
            bitmapImage.SetSource(ms.AsRandomAccessStream());
        }
        catch (Exception) { } //Ignore
    }
}
