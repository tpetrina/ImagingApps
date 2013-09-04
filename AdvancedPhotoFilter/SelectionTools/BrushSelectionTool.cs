using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AdvancedPhotoFilter.Common;

namespace AdvancedPhotoFilter.SelectionTools
{
    class BrushSelectionTool : ISelectionTool
    {
        private const int BrushRadius = 50;
        private const int Tolerance = 6;
        private byte[,] _brushMask;

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

        public BrushSelectionTool()
        {
            _filterThread = new Thread(ApplyMaskThread);
            _filterThread.Start();
            BuildBrush();
        }

        private void BuildBrush()
        {
            _brushMask = new byte[BrushRadius * 2 + 1, BrushRadius * 2 + 1];

            const double threshold = (BrushRadius - Tolerance) * (BrushRadius - Tolerance);
            const double fallout = BrushRadius * BrushRadius;
            const double delta = fallout - threshold;

            for (var i = 0; i < BrushRadius * 2 + 1; ++i)
            {
                for (var j = 0; j < BrushRadius * 2 + 1; ++j)
                {
                    // distance squared
                    var d2 = (i - BrushRadius) * (i - BrushRadius) + (j - BrushRadius) * (j - BrushRadius);
                    byte a;
                    if (d2 <= threshold)
                    {
                        a = 0xFF;
                    }
                    else if (d2 > fallout)
                    {
                        a = 0;
                    }
                    else
                    {
                        var t = fallout - d2;
                        a = (byte)(255.0 * t / delta);
                    }

                    _brushMask[i, j] = a;
                }
            }
        }

        public void Started(ManipulationStartedEventArgs e)
        {
            lock (_syncLock)
            {
                _touchQueue.Enqueue(Tuple.Create(e.ManipulationOrigin, new Point()));
            }

            _lastPoint = e.ManipulationOrigin;
        }

        public void Delta(ManipulationDeltaEventArgs e)
        {
            lock (_syncLock)
            {
                _touchQueue.Enqueue(Tuple.Create(
                    e.ManipulationOrigin,
                    new Point(e.ManipulationOrigin.X - _lastPoint.X, e.ManipulationOrigin.Y - _lastPoint.Y)));

                _lastPoint = e.ManipulationOrigin;
            }
        }

        public void Ended(ManipulationCompletedEventArgs e)
        {
        }

        #region Threading stuff

        private readonly Thread _filterThread;
        private readonly object _syncLock = new object();
        private readonly Queue<Tuple<Point, Point>> _touchQueue = new Queue<Tuple<Point, Point>>();

        private Point _lastPoint;

        private void ApplyMaskThread()
        {
            while (true)
            {
                // extract next point
                var next = new List<Tuple<Point, Point>>();
                while (!next.Any())
                {
                    lock (_syncLock)
                    {
                        while (_touchQueue.Any())
                        {
                            next.Add(_touchQueue.Dequeue());
                        }
                    }

                    Thread.Sleep(1);
                }

                foreach (var item in next)
                {
                    ApplyBrush(item.Item1, item.Item2);
                }

                Deployment.Current.Dispatcher.BeginInvoke(() => TargetImage.Invalidate());
            }
            // ReSharper disable FunctionNeverReturns
        }
        // ReSharper restore FunctionNeverReturns

        private void ApplyBrush(Point origin, Point delta)
        {
            var stopwatch = Stopwatch.StartNew();

            var x1 = Math.Min(origin.X, origin.X + delta.X);
            var x2 = Math.Max(origin.X, origin.X + delta.X);

            // alias
            var y1 = origin.Y;
            var dx = delta.X;
            var dy = delta.Y;

            if (Math.Abs(dx - 0) > 0.001)
            {
                for (var x0 = x1; x0 <= x2; ++x0)
                {
                    var y0 = y1 + (x0 - x1) * dy / dx;

                    var x = (int)(x0 * _targetWidth / ImageWidth);
                    var y = (int)(y0 * _targetHeight / ImageHeight);

                    // determine brush region
                    var sx1 = x - BrushRadius;
                    var sx2 = x + BrushRadius;
                    var sy1 = y - BrushRadius;
                    var sy2 = y + BrushRadius;

                    // clip it to image
                    var cx1 = Math.Max(0, sx1);
                    var cx2 = Math.Min(_targetWidth - 1, sx2);
                    var cy1 = Math.Max(0, sy1);
                    var cy2 = Math.Min(_targetHeight - 1, sy2);

                    // calculate offsets
                    var offsetX = cx1 - sx1;
                    var offsetY = cy1 - sy1;

                    for (var i = 0; i < cx2 - cx1; ++i)
                    {
                        for (var j = 0; j < cy2 - cy1; ++j)
                        {
                            var index = i + cx1 + (j + cy1) * _targetWidth;
                            if (MaskBuffer[index] != 255)
                                SetPixel(i, j, offsetX, offsetY, cx1, cy1);
                        }
                    }
                }
            }
            else
            {
                y1 = Math.Min(origin.Y, origin.Y + delta.Y);
                var y2 = Math.Max(origin.Y, origin.Y + delta.Y);

                for (var y0 = y1; y0 <= y2; ++y0)
                {
                    var x = (int)(x1 * _targetWidth / ImageWidth);
                    var y = (int)(y0 * _targetHeight / ImageHeight);

                    // determine brush region
                    var sx1 = x - BrushRadius;
                    var sx2 = x + BrushRadius;
                    var sy1 = y - BrushRadius;
                    var sy2 = y + BrushRadius;

                    // clip it to image
                    var cx1 = Math.Max(0, sx1);
                    var cx2 = Math.Min(_targetWidth - 1, sx2);
                    var cy1 = Math.Max(0, sy1);
                    var cy2 = Math.Min(_targetHeight - 1, sy2);

                    // calculate offsets
                    var offsetX = cx1 - sx1;
                    var offsetY = cy1 - sy1;

                    for (var i = 0; i < cx2 - cx1; ++i)
                    {
                        for (var j = 0; j < cy2 - cy1; ++j)
                        {
                            var index = i + cx1 + (j + cy1) * _targetWidth;
                            if (MaskBuffer[index] != 255)
                                SetPixel(i, j, offsetX, offsetY, cx1, cy1);
                        }
                    }
                }
            }

            stopwatch.Stop();
            Debug.WriteLine("brush {0}", stopwatch.ElapsedMilliseconds);
        }

        private void SetPixel(int i, int j, int offsetX, int offsetY, int cx1, int cy1)
        {
            var alpha = _brushMask[i + offsetX, j + offsetY];
            if (alpha == 0)
                return;

            var index = i + cx1 + (j + cy1) * _targetWidth;

            alpha = Math.Max(alpha, MaskBuffer[index]);
            MaskBuffer[index] = alpha;
            var brushpixel = (alpha << 24) | (Constants.White & 0xFFFFFF);

            // apply brush pixel (also mind the alpha channel)
            if (alpha == 255)
            {
                Target[index] = Source[index];
                _targetPixels[index] = brushpixel;
            }
            else
            {
                Target[index] = (Source[index] & 0xFFFFFF) | ((255 - alpha) << 24);

                var pixel = Source[index];
                var r = (pixel >> 16) & 0xFF;
                var g = (pixel >> 8) & 0xFF;
                var b = (pixel) & 0xFF;

                _targetPixels[index] =
                    ((255 - alpha) << 24) |
                    (MathEx.Clamp(r * (255 - alpha) / 255, 0, 255) << 16) |
                    (MathEx.Clamp(g * (255 - alpha) / 255, 0, 255) << 8) |
                    (MathEx.Clamp(b * (255 - alpha) / 255, 0, 255));
            }
        }

        #endregion
    }
}
