using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;

namespace KSMP.Extension
{
    public static class BitmapImageExtension
    {
        public static void DisposeSource(this BitmapImage image)
        {
            if (image != null)
            {
                try
                {
                    image.UriSource = null;
                    using (var ms = new MemoryStream(new byte[] { 0x0 }))
                    {
                        image.SetSource(ms.AsRandomAccessStream());
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
