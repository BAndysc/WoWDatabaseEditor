using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.EventAiEditor.Acid
{
    [UniqueProvider]
    public interface IAcidFileService
    {
        /// <summary>True when the user configured the -db repository path and enabled the ACID sync.</summary>
        bool IsEnabled { get; }

        string? TryLocateAcidFile();

        /// <summary>
        /// Adds/updates (or, for an empty list, removes) the creature's rows in the ACID file.
        /// Returns the path of the updated file, throws on failure.
        /// </summary>
        Task<string> SaveScriptAsync(long creatureEntry, string? creatureName, IReadOnlyList<IEventAiLine> lines);
    }
}
