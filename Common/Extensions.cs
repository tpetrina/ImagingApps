using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Tasks;

namespace Common
{
    public static class Extensions
    {
        public static Task<PhotoResult> ShowAsync(this PhotoChooserTask task)
        {
            if (task == null)
                throw new NullReferenceException();

            var tcs = new TaskCompletionSource<PhotoResult>();

            EventHandler<PhotoResult> handler = null;
            handler = (sender, result) =>
            {
                tcs.TrySetResult(result);
                task.Completed -= handler;
            };
            task.Completed += handler;
            task.Show();

            return tcs.Task;
        }

        public static Stream ToStream(this WriteableBitmap wb)
        {
            if (wb == null)
                throw new NullReferenceException();

            var ms = new MemoryStream();
            wb.SaveJpeg(ms, wb.PixelWidth, wb.PixelHeight, 0, 100);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        public static Stream ToStream(this BitmapImage bi)
        {
            if (bi == null)
                throw new NullReferenceException();

            var wb = new WriteableBitmap(bi);
            var ms = new MemoryStream();
            wb.SaveJpeg(ms, wb.PixelWidth, wb.PixelHeight, 0, 100);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        public static BitmapImage ToBitmapImage(this WriteableBitmap wb)
        {
            if (wb == null)
                throw new NullReferenceException();

            using (var ms = new MemoryStream())
            {
                wb.SaveJpeg(ms, wb.PixelWidth, wb.PixelHeight, 0, 100);
                var bmp = new BitmapImage();
                bmp.SetSource(ms);
                return bmp;
            }
        }
    }
}
