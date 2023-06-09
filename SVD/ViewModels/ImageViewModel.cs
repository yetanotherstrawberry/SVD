﻿using MathNet.Numerics.LinearAlgebra;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using SVD.Helpers;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SVD.ViewModels;

internal class ImageViewModel : BindableBase
{

    /// <summary>
    /// Default size (percentage from 1 to 100) of the singular values vector.
    /// </summary>
    private const int DefaultRatio = 70;

    /// <summary>
    /// Asks the user to load the image.
    /// </summary>
    public ICommand LoadImageComm { get; }

    /// <summary>
    /// Compresses the loaded image and displays the result.
    /// </summary>
    public ICommand CompressComm { get; }

    /// <summary>
    /// Field for <c>Ratio</c>.
    /// </summary>
    private int ratio = DefaultRatio;

    /// <summary>
    /// Compression (clamped between 1 and 100 inclusive) ratio set by the user. Notifies when value changed.
    /// </summary>
    public int Ratio
    {
        get => ratio;
        set
        {
            ratio = Math.Clamp(value, 1, 100);
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Field for <c>SliderEnabled</c>.
    /// </summary>
    private bool sliderEnabled = false;

    /// <summary>
    /// Controls whether user can change the value of <c>Ratio</c>. Notifies when value changed.
    /// </summary>
    public bool SliderEnabled
    {
        get => sliderEnabled;
        set
        {
            sliderEnabled = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// Field for <c>CurrentImageSource</c>.
    /// </summary>
    private BitmapSource? currentImageSource = null;

    /// <summary>
    /// <c>BitmapSource</c> of the currently loaded image. Notifies when value changed.
    /// </summary>
    public BitmapSource? CurrentImageSource
    {
        get => currentImageSource;
        set
        {
            currentImageSource = value;
            RaisePropertyChanged();
            Ratio = DefaultRatio;
        }
    }

    /// <summary>
    /// Field for <c>IsEnabled</c>.
    /// </summary>
    private bool isEnabled = true;

    /// <summary>
    /// Controls whether the program is performing an async operation and UI should be disabled.
    /// </summary>
    public bool IsEnabled
    {
        get => isEnabled;
        set
        {
            isEnabled = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// <c>EventHandler</c> that enables buttons after loading an image.
    /// </summary>
    private readonly PropertyChangedEventHandler ImageChangedHandler;

    /// <summary>
    /// <c>Window</c> used as owner for the <c>OpenFileDialog</c>.
    /// </summary>
    public Window? Owner { get; set; } = null;

    /// <summary>
    /// Creates new <c>BitMapSource</c> from bytes. Uses existing <c>CurrentImageSource</c> for parameters.
    /// </summary>
    /// <param name="bytes">Bytes of the image.</param>
    private void SetNewImageSource(byte[] bytes)
    {
        CurrentImageSource = ImageSrcHelper.FromByteArray(
            bytes,
            CurrentImageSource!.PixelWidth,
            CurrentImageSource.PixelHeight,
            CurrentImageSource.DpiX,
            CurrentImageSource.DpiY,
            CurrentImageSource.Format,
            CurrentImageSource.Palette);
    }

    /// <summary>
    /// Performs image compression. Disables the UI during operation.
    /// </summary>
    private async Task CompressImage(BitmapSource bitmapSrc)
    {
        try
        {
            IsEnabled = false;

            var bytes = bitmapSrc.ToByteArray();
            var pixelHeight = bitmapSrc.PixelHeight;
            var pixelWidth = bitmapSrc.PixelWidth;
            (var r, var g, var b, var a) = await Task.Run(() => ImageSrcHelper.ByteTo2DRGBAArrays(bytes, pixelHeight, pixelWidth));

            var tasksSVD = new
            {
                rSVD = Task.Run(r.ToDoubleSVD),
                gSVD = Task.Run(g.ToDoubleSVD),
                bSVD = Task.Run(b.ToDoubleSVD),
                aSVD = Task.Run(a.ToDoubleSVD),
            };

            var svd = new
            {
                r = await tasksSVD.rSVD,
                g = await tasksSVD.gSVD,
                b = await tasksSVD.bSVD,
                a = await tasksSVD.aSVD,
            };

            var diagLen = Math.Max((int)(svd.a.W.Diagonal().Count * (Ratio / (double)100)), 1);

            // Resizes the matrices in order to compress the image.
            static (Matrix<double> U, Matrix<double> W, Matrix<double> VT) resize((Matrix<double> U, Matrix<double> W, Matrix<double> VT) tupleSVD, int diagLen)
            {
                return (tupleSVD.U.Resize(tupleSVD.U.RowCount, diagLen), tupleSVD.W.Resize(diagLen, diagLen), tupleSVD.VT.Resize(diagLen, tupleSVD.VT.ColumnCount));
            }

            var resizedSvd = new
            {
                r = resize(svd.r, diagLen),
                g = resize(svd.g, diagLen),
                b = resize(svd.b, diagLen),
                a = resize(svd.a, diagLen),
            };

            var tasksCompose = new
            {
                rComp = Task.Run(() => ArrayHelper.ComposeSVD(resizedSvd.r)),
                gComp = Task.Run(() => ArrayHelper.ComposeSVD(resizedSvd.g)),
                bComp = Task.Run(() => ArrayHelper.ComposeSVD(resizedSvd.b)),
                aComp = Task.Run(() => ArrayHelper.ComposeSVD(resizedSvd.a)),
            };

            await tasksCompose.rComp;
            await tasksCompose.gComp;
            await tasksCompose.bComp;
            await tasksCompose.aComp;

            var rgbaResult = (tasksCompose.rComp.Result, tasksCompose.gComp.Result, tasksCompose.bComp.Result, tasksCompose.aComp.Result);

            var retBytes = ImageSrcHelper.RGBAArraysToByte(rgbaResult);
            SetNewImageSource(retBytes);
            var totalLen = (resizedSvd.a.U.RowCount * resizedSvd.a.U.ColumnCount + resizedSvd.a.VT.RowCount * resizedSvd.a.VT.ColumnCount + resizedSvd.a.W.Diagonal().Count) * 4;
            var preLen = bytes.Length;
            MessageBox.Show($"All done!\nBytes before compression: {preLen}\nBytes after compression: {totalLen}\nRatio: {(double)totalLen / preLen * 100}%");
        }
        finally
        {
            IsEnabled = true;
        }
    }

    public ImageViewModel() : base()
    {
        LoadImageComm = new DelegateCommand(() =>
        {
            OpenFileDialog ofd = new()
            {
                Multiselect = false,
            };

            if (ofd.ShowDialog(Owner) ?? false)
            {
                using var image = new Bitmap(ofd.OpenFile());
                CurrentImageSource = image.ToBitMapSource();
            }
        });

        var canOperateOnImg = () => CurrentImageSource != null;
        var compressComm = new DelegateCommand(async () =>
        {
            await CompressImage(CurrentImageSource!);
        }, canOperateOnImg);
        CompressComm = compressComm;

        ImageChangedHandler = (object? sender, PropertyChangedEventArgs args) =>
        {
            if (string.Equals(args.PropertyName, nameof(CurrentImageSource)))
            {
                compressComm.RaiseCanExecuteChanged();
                SliderEnabled = canOperateOnImg();
            }
        };
        PropertyChanged += ImageChangedHandler;
    }

    /// <summary>
    /// Unregisters event handlers.
    /// </summary>
    ~ImageViewModel()
    {
        PropertyChanged -= ImageChangedHandler;
    }

}
