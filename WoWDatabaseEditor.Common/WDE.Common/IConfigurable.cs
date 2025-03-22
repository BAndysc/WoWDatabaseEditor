using System.ComponentModel;
using System.Windows.Input;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.Common
{
    [NonUniqueProvider]
    public interface IConfigurable : INotifyPropertyChanged
    {
        ICommand Save { get; }
        string Name { get; }
        ImageUri Icon { get; }
        string? ShortDescription { get; }
        bool IsModified { get; }
        bool IsRestartRequired { get; }
        ConfigurableGroup Group { get; }
        
        void ConfigurableOpened() { }
    }
    
    [NonUniqueProvider]
    public interface IFirstTimeWizardConfigurable : IConfigurable
    {
    }

    [UniqueProvider]
    public interface ICoreVersionConfigurable : IConfigurable
    {
        
    }

    public enum ConfigurableGroup
    {
        Basic,
        Advanced
    }
}