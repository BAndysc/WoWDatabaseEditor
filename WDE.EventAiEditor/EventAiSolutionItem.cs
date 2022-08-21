using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using WDE.Common;
using WDE.Common.Database;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor
{
    public class EventAiSolutionItem : IEventAiSolutionItem
    {
        public EventAiSolutionItem(int entryOrGuid)
        {
            EntryOrGuid = entryOrGuid;
        }

        public int EntryOrGuid { get; }

        [JsonIgnore] 
        public bool IsContainer => false;

        [JsonIgnore] 
        public ObservableCollection<ISolutionItem>? Items { get; set; } = null;

        public string? ExtraId => EntryOrGuid.ToString();

        [JsonIgnore]
        public bool IsExportable => true;

        public ISolutionItem Clone() => new EventAiSolutionItem(EntryOrGuid);

        protected bool Equals(EventAiSolutionItem other)
        {
            return EntryOrGuid == other.EntryOrGuid;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((EventAiSolutionItem)obj);
        }

        public override int GetHashCode()
        {
            return EntryOrGuid.GetHashCode();
        }
    }
}