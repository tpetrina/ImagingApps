using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AdvancedPhotoFilter.Common;

namespace AdvancedPhotoFilter.SelectionTools
{
    public class MagicWandTool : ISelectionTool
    {
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

        public void Started(ManipulationStartedEventArgs e)
        {
            Apply(e.ManipulationOrigin);
            TargetImage.Invalidate();
        }

        public void Delta(ManipulationDeltaEventArgs e)
        {
            Apply(e.ManipulationOrigin);
            TargetImage.Invalidate();
        }

        public void Ended(ManipulationCompletedEventArgs e)
        {
            Apply(e.ManipulationOrigin);
            TargetImage.Invalidate();
        }

        private void Apply(Point p)
        {
            // convert from screen space to image space
            // prefix o = original
            var ox = (int)(p.X * _targetWidth / ImageWidth);
            var oy = (int)(p.Y * _targetHeight / ImageHeight);
            if (ox < 0 || ox >= _targetWidth ||
                oy < 0 || oy >= _targetHeight)
            {
                return;
            }

            var originalPixel = _targetPixels[ox + oy * _targetWidth];
            var ouv = MathEx.GetUV(originalPixel);

            // apply flood fill algorithm
            var pointsToCheck = new Stack<Tuple<int, int>>();
            pointsToCheck.Push(Tuple.Create(ox, oy));

            while (pointsToCheck.Any())
            {
                var pointToCheck = pointsToCheck.Pop();
                var x = pointToCheck.Item1;
                var y = pointToCheck.Item2;

                var index = x + y * _targetWidth;

                // already processed
                if (MaskBuffer[index] == 255)
                    continue;

                // test similarity between colors
                var pixel = _targetPixels[x + y * _targetWidth];

                var uv = MathEx.GetUV(pixel);
                if (MathEx.Distance2(ouv, uv) > 150)
                    continue;

                // apply for this pixel
                MaskBuffer[index] = 255;
                Target[index] = Source[index];
                _targetPixels[index] = (Constants.White & 0xFFFFFF) | (0xFF << 24);

                // push neighbors to the stack (if reachable)
                if (x > 0)
                    pointsToCheck.Push(Tuple.Create(x - 1, y));
                if (x < _targetWidth - 1)
                    pointsToCheck.Push(Tuple.Create(x + 1, y));
                if (y > 0)
                    pointsToCheck.Push(Tuple.Create(x, y - 1));
                if (y < _targetHeight - 1)
                    pointsToCheck.Push(Tuple.Create(x, y + 1));
            }
        }
    }
}
