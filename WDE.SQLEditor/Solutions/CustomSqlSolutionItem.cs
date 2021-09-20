using System;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using WDE.Common;

namespace WDE.SQLEditor.Solutions
{
    public class CustomSqlSolutionItem : ISolutionItem, IEquatable<CustomSqlSolutionItem>
    {
        public string Id { get; set; }

        public string Name { get; set; } = "Custom query";

        public string Query { get; set; } = "";
        
        [JsonIgnore] public bool IsContainer => false;

        [JsonIgnore] public ObservableCollection<ISolutionItem>? Items => null;

        [JsonIgnore] public string? ExtraId => null;

        [JsonIgnore] public bool IsExportable => true;
        
        public ISolutionItem Clone()
        {
            return new CustomSqlSolutionItem() { Id = Id, Query = Query };
        }

        public bool Equals(CustomSqlSolutionItem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj)
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