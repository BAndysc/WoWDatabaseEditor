using System.Collections.Generic;

namespace WDE.DbScriptsEditor.Models
{
    // A whole dbscript = all rows sharing one id within one dbscripts_on_* table.
    public class DbScript
    {
        public DbScriptType Type { get; }
        public uint Id { get; }
        public IReadOnlyList<DbScriptStep> Steps { get; }

        public DbScript(DbScriptType type, uint id, IReadOnlyList<DbScriptStep> steps)
        {
            Type = type;
            Id = id;
            Steps = steps;
        }
    }
}
