using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SVD.Helpers;

internal static partial class ImageSrcHelper
{

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteObject(IntPtr hObject);

    public static BitmapSource ToBitMapSource(this Bitmap bitmap)
    {
        var handle = bitmap.GetHbitmap();

        try
        {
            return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            DeleteObject(handle);
        }
    }

    private static int GetStride(BitmapSource bitmapSrc)
    {
        return bitmapSrc.PixelWidth * (bitmapSrc.Format.BitsPerPixel + 7) / 8;
    }

    public static byte[] ToByteArray(this BitmapSource bitmapSrc)
    {
        var stride = GetStride(bitmapSrc);
        var arrayLength = stride * bitmapSrc.PixelHeight;
        var ret = new byte[arrayLength];
        bitmapSrc.CopyPixels(ret, stride, 0);

        return ret;
    }

    public static (byte[,], byte[,], byte[,], byte[,]) ByteTo2DRGBAArrays(byte[] data, int pixelHeight, int pixelWidth)
    {
        var retR = new byte[pixelHeight, pixelWidth];
        var retG = new byte[pixelHeight, pixelWidth];
        var retB = new byte[pixelHeight, pixelWidth];
        var retA = new byte[pixelHeight, pixelWidth];

        Parallel.For(0, pixelHeight, i =>
        {
            Parallel.For(0, pixelWidth, j =>
            {
                retB[i, j] = data[i * pixelHeight + j * 4];
                retG[i, j] = data[i * pixelHeight + j * 4 + 1];
                retR[i, j] = data[i * pixelHeight + j * 4 + 2];
                retA[i, j] = data[i * pixelHeight + j * 4 + 3];
            });
        });

        return (retR, retG, retB, retA);
    }

    public static BitmapSource FromByteArray(
        byte[] array,
        int pixelWidth,
        int pixelHeight,
        double dpiX,
        double dpiY,
        PixelFormat format,
        BitmapPalette palette)
    {
        var ret = new WriteableBitmap(pixelWidth, pixelHeight, dpiX, dpiY, format, palette);
        var rect = new Int32Rect(0, 0, pixelWidth, pixelHeight);

        ret.WritePixels(rect, array, GetStride(ret), 0);

        return ret;
    }

}
