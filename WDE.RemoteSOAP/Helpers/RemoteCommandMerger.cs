using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Common.Services;

namespace WDE.RemoteSOAP.Helpers
{
    public static class RemoteCommandMerger
    {
        public static IList<IRemoteCommand> Merge(IList<IRemoteCommand> commands)
        {
            if (commands == null)
                return new List<IRemoteCommand>();
            
            Dictionary<Type, IList<IRemoteCommand>> commandsByType = new();

            foreach (var command in commands)
            {
                var commandType = command.GetType();
                if (!commandsByType.TryGetValue(commandType, out var list))
                {
                    commandsByType[commandType] = new List<IRemoteCommand>() {command};
                }
                else
                {
                    if (list[^1].TryMerge(command, out var mergedCommand))
                    {
                        list[^1] = mergedCommand!;
                    }
                    else
                        list.Add(command);
                }
            }

            return commandsByType.Values.SelectMany(v => v).OrderBy(o => (int)o.Priority).ToList();
        }
    }
}