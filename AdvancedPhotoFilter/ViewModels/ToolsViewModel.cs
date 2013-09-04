using AdvancedPhotoFilter.SelectionTools;
using Common;
using System.Collections.Generic;
using System.Linq;

namespace AdvancedPhotoFilter.ViewModels
{
    public class ToolsViewModel : ViewModelBase
    {
        private bool _isShown;

        public List<ToolItemViewModel> ToolItems { get; set; }

        public bool IsShown
        {
            get { return _isShown; }
            set { Set(ref _isShown, value); }
        }

        public ToolsViewModel(List<ISelectionTool> tools)
        {
            ToolItems = new List<ToolItemViewModel>
                {
                    new ToolItemViewModel
                        {
                            Image = "/Assets/ToolIcons/brush.png",
                            Name = "brush",
                            Tool = tools.OfType<BrushSelectionTool>().First()
                        },

                    new ToolItemViewModel
                        {
                            Image = "/Assets/ToolIcons/wand.png",
                            Name = "wand",
                            Tool = tools.OfType<MagicWandTool>().First()
                        },

                    new ToolItemViewModel
                        {
                            Image = "/Assets/ToolIcons/rect.png",
                            Name = "rect",
                            Tool = tools.OfType<RectSelectionTool>().First()
                        }
                };
        }
    }
}
