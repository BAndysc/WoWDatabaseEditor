using System.Collections.ObjectModel;
using Newtonsoft.Json;
using WDE.Common;
using WDE.DbScriptsEditor.Models;

namespace WDE.DbScriptsEditor
{
    public class DbScriptSolutionItem : ISolutionItem
    {
        public DbScriptSolutionItem(DbScriptType scriptType, uint scriptId)
        {
            ScriptType = scriptType;
            ScriptId = scriptId;
        }

        public DbScriptType ScriptType { get; }
        public uint ScriptId { get; }

        [JsonIgnore] public bool IsContainer => false;
        [JsonIgnore] public ObservableCollection<ISolutionItem>? Items { get; set; } = null;
        public string? ExtraId => $"{(int)ScriptType}/{ScriptId}";
        [JsonIgnore] public bool IsExportable => true;

        public ISolutionItem Clone() => new DbScriptSolutionItem(ScriptType, ScriptId);

        protected bool Equals(DbScriptSolutionItem other) =>
            ScriptType == other.ScriptType && ScriptId == other.ScriptId;

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((DbScriptSolutionItem)obj);
        }

        public override int GetHashCode() => System.HashCode.Combine((int)ScriptType, ScriptId);
    }
}
