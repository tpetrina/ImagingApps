using System;
using Nokia.Graphics.Imaging;

namespace AdvancedPhotoFilter.ViewModels
{
    public class FilterViewModel
    {
        public string Name { get; set; }
        public Func<IFilter> Creator { get; set; }

        public FilterViewModel(string name, Func<IFilter> creator)
        {
            Name = name;
            Creator = creator;
        }

        public static FilterViewModel Create(string name, Func<IFilter> creator)
        {
            return new FilterViewModel(name, creator);
        }

        public static FilterViewModel Create<T1>(string name, Func<T1, IFilter> creator, T1 t1)
        {
            return new FilterViewModel(name, () => creator(t1));
        }
    }
}