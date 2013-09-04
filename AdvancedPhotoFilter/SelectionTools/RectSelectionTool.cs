using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AdvancedPhotoFilter.Common;

namespace AdvancedPhotoFilter.SelectionTools
{
    class RectSelectionTool : ISelectionTool, IVisualTool
    {
        private Point _startPoint;

        public byte[] MaskBuffer { get; set; }
        public int[] Source { get; set; }
        public int[] Target { get; set; }

        public double ImageWidth { get; set; }
        public double ImageHeight { get; set; }

        private WriteableBitmap _targetImage;
        private int _targetWidth;
        private int _targetHeight;
        private int[] _targetPixels;
        public WriteableBitmap TargetImage
        {
            get { return _targetImage; }
            set
            {
                _targetImage = value;
                if (_targetImage != null)
                {
                    _targetWidth = TargetImage.PixelWidth;
                    _targetHeight = TargetImage.PixelHeight;
                    _targetPixels = TargetImage.Pixels;
                }
            }
        }

        public FrameworkElement Element { get; set; }
        public Point Position { get; set; }
        public event EventHandler ElementChanged;
        public event EventHandler PositionChanged;

        protected void RaiseElementChanged()
        {
            if (ElementChanged != null)
                ElementChanged(this, EventArgs.Empty);
        }

        protected void RaisePositionChanged()
        {
            if (PositionChanged != null)
                PositionChanged(this, EventArgs.Empty);
        }

        public void Started(ManipulationStartedEventArgs e)
        {
            _startPoint = e.ManipulationOrigin;

            Position = e.ManipulationOrigin;
            Element = new Rectangle
                {
                    Fill = new SolidColorBrush(Colors.White),
                    Width = 1,
                    Height = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };

            RaisePositionChanged();
            RaiseElementChanged();
        }

        public void Delta(ManipulationDeltaEventArgs e)
        {
            Position = new Point(Math.Min(_startPoint.X, e.ManipulationOrigin.X),
                                 Math.Min(_startPoint.Y, e.ManipulationOrigin.Y));

            Element.Width = Math.Abs(_startPoint.X - e.ManipulationOrigin.X);
            Element.Height = Math.Abs(_startPoint.Y - e.ManipulationOrigin.Y);

            RaisePositionChanged();
        }

        public void Ended(ManipulationCompletedEventArgs e)
        {
            Apply(e.ManipulationOrigin);

            Element = null;
            RaiseElementChanged();
            TargetImage.Invalidate();
        }

        private void Apply(Point finalPoint)
        {
            // burn onto image
            var startx = (int)(Math.Min(_startPoint.X, finalPoint.X) * _targetWidth / ImageWidth);
            var starty = (int)(Math.Min(_startPoint.Y, finalPoint.Y) * _targetHeight / ImageHeight);

            var endx = startx + (int)(Math.Abs(_startPoint.X - finalPoint.X) * _targetWidth / ImageWidth);
            var endy = starty + (int)(Math.Abs(_startPoint.Y - finalPoint.Y) * _targetHeight / ImageHeight);

            for (var x = startx; x < endx; ++x)
            {
                for (var y = starty; y < endy; ++y)
                {
                    var index = x + y * _targetWidth;
                    Target[index] = Source[index];
                    _targetPixels[index] = Constants.White;
                }
            }
        }
    }
}
