using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;

namespace KSMP.Extension;

public static class BitmapImageExtension
{
    public static void DisposeSource(this BitmapImage bitmapImage)
    {
        if (bitmapImage == null) return;
        try { bitmapImage.UriSource = null; }
        catch (Exception) { } //Ignore
    }
}
