using System.Runtime.CompilerServices;
using WDE.Module;
using WDE.Module.Attributes;

[assembly: InternalsVisibleTo("WDE.WoWHeadConnector.Test")]
namespace WDE.WoWHeadConnector
{
    [AutoRegister]
    [SingleInstance]
    public class WoWHeadConnectorModule : ModuleBase
    {
    
    }
}