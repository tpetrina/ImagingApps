using System;
using System.Windows;

namespace AdvancedPhotoFilter.SelectionTools
{
    public interface IVisualTool
    {
        FrameworkElement Element { get; set; }
        Point Position { get; set; }

        event EventHandler ElementChanged;
        event EventHandler PositionChanged;
    }
}
