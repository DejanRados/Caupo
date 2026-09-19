using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Caupo.Views
{
    public partial class ArticleImageCropWindow : Window
    {
        private readonly BitmapImage _bitmap;
        private readonly double _aspectRatio;
        private Rect _imageRect;
        private Rect _cropRect;
        private bool _dragging;
        private Point _dragStart;
        private Rect _dragStartCrop;

        public Int32Rect CropPixels { get; private set; }

        public ArticleImageCropWindow(string imagePath, int targetWidth, int targetHeight)
        {
            InitializeComponent ();

            _aspectRatio = (double)targetWidth / targetHeight;
            CropInfoText.Text = $"Konačna slika: {targetWidth} × {targetHeight} px. Pomjerite okvir ili povucite bijeli ugao za promjenu veličine.";

            _bitmap = new BitmapImage ();
            _bitmap.BeginInit ();
            _bitmap.CacheOption = BitmapCacheOption.OnLoad;
            _bitmap.UriSource = new Uri (imagePath, UriKind.Absolute);
            _bitmap.EndInit ();
            _bitmap.Freeze ();

            SourceImage.Source = _bitmap;
            Loaded += (_, _) => InitializeCrop ();
        }

        private void PreviewHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(IsLoaded)
                InitializeCrop ();
        }

        private void InitializeCrop()
        {
            double hostWidth = OverlayCanvas.ActualWidth;
            double hostHeight = OverlayCanvas.ActualHeight;

            if(hostWidth <= 0 || hostHeight <= 0 || _bitmap.PixelWidth <= 0 || _bitmap.PixelHeight <= 0)
                return;

            double imageAspect = (double)_bitmap.PixelWidth / _bitmap.PixelHeight;
            double displayWidth;
            double displayHeight;

            if(imageAspect > hostWidth / hostHeight)
            {
                displayWidth = hostWidth;
                displayHeight = hostWidth / imageAspect;
            }
            else
            {
                displayHeight = hostHeight;
                displayWidth = hostHeight * imageAspect;
            }

            _imageRect = new Rect ((hostWidth - displayWidth) / 2, (hostHeight - displayHeight) / 2, displayWidth, displayHeight);

            double cropWidth = _imageRect.Width;
            double cropHeight = cropWidth / _aspectRatio;

            if(cropHeight > _imageRect.Height)
            {
                cropHeight = _imageRect.Height;
                cropWidth = cropHeight * _aspectRatio;
            }

            cropWidth *= 0.9;
            cropHeight *= 0.9;
            _cropRect = new Rect (_imageRect.X + (_imageRect.Width - cropWidth) / 2, _imageRect.Y + (_imageRect.Height - cropHeight) / 2, cropWidth, cropHeight);
            UpdateOverlay ();
        }

        private void OverlayCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point point = e.GetPosition (OverlayCanvas);

            if(!_cropRect.Contains (point))
                return;

            _dragging = true;
            _dragStart = point;
            _dragStartCrop = _cropRect;
            OverlayCanvas.CaptureMouse ();
            e.Handled = true;
        }

        private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if(!_dragging || e.LeftButton != MouseButtonState.Pressed)
                return;

            Point point = e.GetPosition (OverlayCanvas);
            double x = _dragStartCrop.X + point.X - _dragStart.X;
            double y = _dragStartCrop.Y + point.Y - _dragStart.Y;

            x = Math.Max (_imageRect.Left, Math.Min (x, _imageRect.Right - _cropRect.Width));
            y = Math.Max (_imageRect.Top, Math.Min (y, _imageRect.Bottom - _cropRect.Height));

            _cropRect = new Rect (x, y, _cropRect.Width, _cropRect.Height);
            UpdateOverlay ();
        }

        private void OverlayCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _dragging = false;
            OverlayCanvas.ReleaseMouseCapture ();
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double desiredWidth = _cropRect.Width + e.HorizontalChange;
            double desiredHeightFromVertical = _cropRect.Height + e.VerticalChange;

            if(Math.Abs (e.VerticalChange) > Math.Abs (e.HorizontalChange))
                desiredWidth = desiredHeightFromVertical * _aspectRatio;

            double minWidth = 80;
            double maxWidth = Math.Min (_imageRect.Right - _cropRect.X, (_imageRect.Bottom - _cropRect.Y) * _aspectRatio);
            desiredWidth = Math.Max (minWidth, Math.Min (desiredWidth, maxWidth));

            double height = desiredWidth / _aspectRatio;
            _cropRect = new Rect (_cropRect.X, _cropRect.Y, desiredWidth, height);
            UpdateOverlay ();
        }

        private void UpdateOverlay()
        {
            SetRect (CropBorder, _cropRect);

            Canvas.SetLeft (ResizeThumb, _cropRect.Right - ResizeThumb.Width / 2);
            Canvas.SetTop (ResizeThumb, _cropRect.Bottom - ResizeThumb.Height / 2);

            SetRect (ShadeTop, new Rect (_imageRect.Left, _imageRect.Top, _imageRect.Width, Math.Max (0, _cropRect.Top - _imageRect.Top)));
            SetRect (ShadeBottom, new Rect (_imageRect.Left, _cropRect.Bottom, _imageRect.Width, Math.Max (0, _imageRect.Bottom - _cropRect.Bottom)));
            SetRect (ShadeLeft, new Rect (_imageRect.Left, _cropRect.Top, Math.Max (0, _cropRect.Left - _imageRect.Left), _cropRect.Height));
            SetRect (ShadeRight, new Rect (_cropRect.Right, _cropRect.Top, Math.Max (0, _imageRect.Right - _cropRect.Right), _cropRect.Height));
        }

        private static void SetRect(FrameworkElement element, Rect rect)
        {
            Canvas.SetLeft (element, rect.X);
            Canvas.SetTop (element, rect.Y);
            element.Width = Math.Max (0, rect.Width);
            element.Height = Math.Max (0, rect.Height);
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if(_imageRect.Width <= 0 || _imageRect.Height <= 0 || _cropRect.Width <= 0 || _cropRect.Height <= 0)
                return;

            double scaleX = _bitmap.PixelWidth / _imageRect.Width;
            double scaleY = _bitmap.PixelHeight / _imageRect.Height;

            int x = (int)Math.Round ((_cropRect.X - _imageRect.X) * scaleX);
            int y = (int)Math.Round ((_cropRect.Y - _imageRect.Y) * scaleY);
            int width = (int)Math.Round (_cropRect.Width * scaleX);
            int height = (int)Math.Round (_cropRect.Height * scaleY);

            x = Math.Max (0, Math.Min (x, _bitmap.PixelWidth - 1));
            y = Math.Max (0, Math.Min (y, _bitmap.PixelHeight - 1));
            width = Math.Max (1, Math.Min (width, _bitmap.PixelWidth - x));
            height = Math.Max (1, Math.Min (height, _bitmap.PixelHeight - y));

            CropPixels = new Int32Rect (x, y, width, height);
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
