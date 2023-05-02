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
        return bitmapSrc.PixelWidth * ((bitmapSrc.Format.BitsPerPixel + 7) / 8);
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
                retB[i, j] = data[i * pixelWidth * 4 + j * 4];
                retG[i, j] = data[i * pixelWidth * 4 + j * 4 + 1];
                retR[i, j] = data[i * pixelWidth * 4 + j * 4 + 2];
                retA[i, j] = data[i * pixelWidth * 4 + j * 4 + 3];
            });
        });

        return (retR, retG, retB, retA);
    }

    /// <summary>
    /// Takes 2D arrays of 4 channels, flattens them and concatenates to single array to be used in <c>FromByteArray</c>.
    /// </summary>
    /// <param name="input">Tuple of 4 RGBA 2D arrays.</param>
    /// <returns>Flattened array.</returns>
    public static byte[] RGBAArraysToByte((byte[,], byte[,], byte[,], byte[,]) input)
    {
        var ret = new byte[input.Item1.Length * 4];
        var rows = input.Item1.GetLength(0);
        var cols = input.Item1.GetLength(1);
        var retB = input.Item3;
        var retA = input.Item4;
        var retR = input.Item1;
        var retG = input.Item2;

        Parallel.For(0, rows, i =>
        {
            Parallel.For(0, cols, j =>
            {
                ret[i * cols * 4 + j * 4] = retB[i, j];
                ret[i * cols * 4 + j * 4 + 1] = retG[i, j];
                ret[i * cols * 4 + j * 4 + 2] = retR[i, j];
                ret[i * cols * 4 + j * 4 + 3] = retA[i, j];
            });
        });

        return ret;
    }

    /// <summary>
    /// Creates a <c>BitMapSource</c> based on bytes of an image.
    /// </summary>
    /// <param name="array">Bytes to be used to create the <c>BitMap</c>.</param>
    /// <param name="pixelWidth">Width of the returned <c>BitMap</c> in pixels.</param>
    /// <param name="pixelHeight">Height of the returned <c>BitMap</c> in pixels.</param>
    /// <param name="dpiX">DPI of the X axis.</param>
    /// <param name="dpiY">DPI of the Y axis.</param>
    /// <param name="format">Pixel format</param>
    /// <param name="palette">Palette of the returned <c>BitMap</c>.</param>
    /// <returns><c>BitMapSource</c> from <c>WritableBitMap</c> created from the <c>byte</c> array.</returns>
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
