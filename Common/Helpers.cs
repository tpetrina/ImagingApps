using System;

namespace Common
{
    public static class Helpers
    {
        public static void SafeDispose<T>(ref T disposable) where T : class,IDisposable
        {
            if (disposable == null)
                return;

            try
            {
                disposable.Dispose();
                disposable = null;
            }
            catch { }
        }
    }
}
