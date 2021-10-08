using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Services
{
    public enum MultiRowSplitMode
    {
        None,
        Horizontal,
        Vertical
    }
    
    [UniqueProvider]
    public interface IDatabaseEditorsSettings
    {
        MultiRowSplitMode MultiRowSplitMode { get; set; }
    }
}