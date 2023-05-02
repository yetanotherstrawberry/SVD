using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using SVD.Helpers;
using SVD.Resources;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SVD.ViewModels;

internal class ImageViewModel : BindableBase
{

    /// <summary>
    /// Asks the user to load the image.
    /// </summary>
    public ICommand LoadImageComm { get; }

    /// <summary>
    /// Saves the currently loaded image to a file.
    /// </summary>
    public ICommand SaveImageComm { get; }

    /// <summary>
    /// Compresses the loaded image and displays the result.
    /// </summary>
    public ICommand CompressComm { get; }

    /// <summary>
    /// Field for <c>Ratio</c>.
    /// </summary>
    private int ratio = 100;

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

    private void Test()
    {
        var matrix = new double[,]
        {
            {1,0,0,0,2,},
            {0,0,3,0,0,},
            {0,0,0,0,0,},
            {0,2,0,0,0,},
        };
    }

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
    /// Performs image compression.
    /// </summary>
    private void CompressImage()
    {
        var bytes = CurrentImageSource!.ToByteArray();

        (var r, var g, var b, var a) = CurrentImageSource!.To2DRGBAArrays();
        (var rSVD, var gSVD, var bSVD, var aSVD) = (r.ToDoubleSVD(), g.ToDoubleSVD(), b.ToDoubleSVD(), a.ToDoubleSVD());

        var rComp = ArrayHelper.ComposeSVD(rSVD);
        var gComp = ArrayHelper.ComposeSVD(gSVD);
        var bComp = ArrayHelper.ComposeSVD(bSVD);
        var aComp = ArrayHelper.ComposeSVD(aSVD);

        Console.WriteLine();
    }

    public ImageViewModel() : base()
    {
        LoadImageComm = new DelegateCommand(() =>
        {
            OpenFileDialog ofd = new()
            {
                Filter = $"{AppResources.IMAGES}|*.BMP;*.JPG;*.GIF,*.TIFF,*.PNG,*.EXIF|{AppResources.ALL_FILES}|*.*",
                FilterIndex = 0,
                Multiselect = false,
            };

            if (ofd.ShowDialog(Owner) ?? false)
            {
                using var image = new Bitmap(ofd.OpenFile());
                CurrentImageSource = image.ToBitMapSource();
            }
        });

        var canOperateOnImg = () => CurrentImageSource != null;
        var saveImageComm = new DelegateCommand(() =>
        {
            SaveFileDialog sfd = new()
            {
                Filter = $"{AppResources.IMAGES}|*.BMP;*.JPG;*.GIF,*.TIFF,*.PNG,*.EXIF|{AppResources.ALL_FILES}|*.*",
                FilterIndex = 0,
            };

            if (sfd.ShowDialog(Owner) ?? false)
            {
                var stream = sfd.OpenFile();
                stream.Write(CurrentImageSource!.ToByteArray());
                stream.Close();
            }
        }, canOperateOnImg);
        var compressComm = new DelegateCommand(CompressImage, canOperateOnImg);
        SaveImageComm = saveImageComm;
        CompressComm = compressComm;

        ImageChangedHandler = (object? sender, PropertyChangedEventArgs args) =>
        {
            if (string.Equals(args.PropertyName, nameof(CurrentImageSource)))
            {
                saveImageComm.RaiseCanExecuteChanged();
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
