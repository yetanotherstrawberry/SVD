using System;
using System.Drawing;
using System.Runtime.InteropServices;
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

    public static (byte[,], byte[,], byte[,], byte[,]) To2DRGBAArrays(this BitmapSource bitmapSrc)
    {
        var data = bitmapSrc.ToByteArray();
        var retR = new byte[bitmapSrc.PixelHeight, bitmapSrc.PixelWidth];
        var retG = new byte[bitmapSrc.PixelHeight, bitmapSrc.PixelWidth];
        var retB = new byte[bitmapSrc.PixelHeight, bitmapSrc.PixelWidth];
        var retA = new byte[bitmapSrc.PixelHeight, bitmapSrc.PixelWidth];

        for (int i = 0; i < bitmapSrc.PixelHeight; i++)
        {
            for (int j = 0; j < bitmapSrc.PixelWidth * 4; j += 4)
            {
                retB[i, j / 4] = data[i * bitmapSrc.PixelHeight + j];
                retG[i, j / 4] = data[i * bitmapSrc.PixelHeight + j + 1];
                retR[i, j / 4] = data[i * bitmapSrc.PixelHeight + j + 2];
                retA[i, j / 4] = data[i * bitmapSrc.PixelHeight + j + 3];
            };
        }

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
