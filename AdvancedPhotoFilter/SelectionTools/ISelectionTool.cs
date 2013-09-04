namespace AdvancedPhotoFilter.SelectionTools
{
    public interface ISelectionTool : ITool
    {
        byte[] MaskBuffer { get; set; }
        int[] Source { get; set; }
        int[] Target { get; set; }
    }
}
