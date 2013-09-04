using System.Runtime.CompilerServices;

namespace Common
{
    public class ViewModelBase : GalaSoft.MvvmLight.ViewModelBase
    {
        protected bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            return Set(propertyName, ref field, value);
        }
    }
}
