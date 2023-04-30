using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using SVD.Helpers;
using SVD.Resources;
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
    /// Field for <c>Image</c>.
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

        }, canOperateOnImg);
        var compressComm = new DelegateCommand(() =>
        {

        }, canOperateOnImg);
        SaveImageComm = saveImageComm;
        CompressComm = compressComm;

        ImageChangedHandler = (object? sender, PropertyChangedEventArgs args) =>
        {
            if (string.Equals(args.PropertyName, nameof(CurrentImageSource)))
            {
                saveImageComm.RaiseCanExecuteChanged();
                compressComm.RaiseCanExecuteChanged();
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
