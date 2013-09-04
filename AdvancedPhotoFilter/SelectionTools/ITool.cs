using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace AdvancedPhotoFilter.SelectionTools
{
    public interface ITool
    {
        double ImageWidth { get; set; }
        double ImageHeight { get; set; }
        WriteableBitmap TargetImage { get; set; }

        void Started(ManipulationStartedEventArgs e);
        void Delta(ManipulationDeltaEventArgs e);
        void Ended(ManipulationCompletedEventArgs e);
    }
}