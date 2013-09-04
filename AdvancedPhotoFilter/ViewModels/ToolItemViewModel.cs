using AdvancedPhotoFilter.SelectionTools;

namespace AdvancedPhotoFilter.ViewModels
{
    public class ToolItemViewModel
    {
        public string Image { get; set; }
        public string Name { get; set; }
        public ISelectionTool Tool { get; set; }
    }
}
