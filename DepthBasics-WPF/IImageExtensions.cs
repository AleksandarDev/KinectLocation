using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Emgu.CV;

namespace Microsoft.Samples.Kinect.DepthBasics
{
    /// <summary>
    /// The <see cref="IImage"/> extensions.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IImageExtensions
    {
        public static BitmapSource ToBitmapSource(this IImage image)
        {
            using (var source = image.Bitmap)
            {
                var ptr = source.GetHbitmap(); //obtain the Hbitmap

                var bs = Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);
    }
}