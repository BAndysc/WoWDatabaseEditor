using System;
using WDE.Common.Services;

namespace WDE.SmartScriptEditor.Models
{
    public class SmartScriptReloadRemoteCommand : IRemoteCommand
    {
        public static SmartScriptReloadRemoteCommand Singleton { get; } = new();
        
        public string GenerateCommand() => "reload smart_scripts";

        public bool TryMerge(IRemoteCommand other, out IRemoteCommand mergedCommand)
        {
            mergedCommand = Singleton;
            return true;
        }
    }
}