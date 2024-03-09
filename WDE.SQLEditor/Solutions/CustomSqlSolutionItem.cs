using System;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using WDE.Common;
using WDE.Common.Database;

namespace WDE.SQLEditor.Solutions
{
    public class CustomSqlSolutionItem : ISolutionItem, IRenameableSolutionItem, IEquatable<CustomSqlSolutionItem>
    {
        public CustomSqlSolutionItem(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public string Name { get; set; } = "Custom query";

        public DataDatabaseType Database { get; set; } = DataDatabaseType.World;
        
        public string Query { get; set; } = "";
        
        [JsonIgnore] public bool IsContainer => false;

        [JsonIgnore] public ObservableCollection<ISolutionItem>? Items => null;

        [JsonIgnore] public string? ExtraId => null;

        [JsonIgnore] public bool IsExportable => true;
        
        public ISolutionItem Clone()
        {
            return new CustomSqlSolutionItem(Id) { Query = Query, Database = Database };
        }

        public void Rename(string newName)
        {
            Name = newName;
        }

        public bool Equals(CustomSqlSolutionItem? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CustomSqlSolutionItem)obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}