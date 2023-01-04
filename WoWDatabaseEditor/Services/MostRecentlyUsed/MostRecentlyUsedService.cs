using System;
using System.Collections.Generic;
using Prism.Events;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Services;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.MostRecentlyUsed
{
    [AutoRegister]
    [SingleInstance]
    public class MostRecentlyUsedService : IMostRecentlyUsedService
    {
        private readonly ISolutionItemDeserializerRegistry deserializer;
        private readonly ISolutionItemSerializerRegistry serializer;

        private static int MaxMruEntries = 15;
        private IList<MruEntry> mostRecentlyUsed;

        public MostRecentlyUsedService(IEventAggregator eventAggregator,
            IUserSettings userSettings,
            ISolutionItemDeserializerRegistry deserializer,
            ISolutionItemSerializerRegistry serializer)
        {
            this.deserializer = deserializer;
            this.serializer = serializer;
            var previous = userSettings.Get<Data>(new Data(new List<MruEntry>()));
            mostRecentlyUsed = previous.Items ?? new List<MruEntry>();
            
            eventAggregator.GetEvent<EventRequestOpenItem>().Subscribe(item =>
            {
                if (item is MetaSolutionSQL)
                    return;
                
                var serialized = TrySerialize(item);
                if (serialized == null)
                    return;

                MruEntry entry = new MruEntry(serialized.Type, serialized.Value, serialized.Value2, serialized.StringValue);
                mostRecentlyUsed.Remove(entry);
                
                if (mostRecentlyUsed.Count >= MaxMruEntries)
                    mostRecentlyUsed.RemoveAt(mostRecentlyUsed.Count - 1);
                
                mostRecentlyUsed.Insert(0, entry);
                
                userSettings.Update<Data>(new Data(mostRecentlyUsed));
            }, true);
        }

        private ISmartScriptProjectItem? TrySerialize(ISolutionItem item)
        {
            try
            {
                return serializer.Serialize(item, true);
            }
            catch (Exception e)
            {
                LOG.LogError(e);
                return null;
            }
        }

        private struct Data : ISettings
        {
            public Data(IList<MruEntry> items)
            {
                Items = items;
            }

            public IList<MruEntry> Items { get; set; }
        }
        
        public class MruEntry
        {
            public readonly byte Type;
            public readonly int Value;
            public readonly int? Value2;
            public readonly string? StringValue;

            public MruEntry(byte type, int value, int? value2, string? stringValue)
            {
                Type = type;
                Value = value;
                Value2 = value2;
                StringValue = stringValue;
            }

            protected bool Equals(MruEntry other)
            {
                return Type == other.Type && Value == other.Value && Value2 == other.Value2 && StringValue == other.StringValue;
            }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((MruEntry) obj);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Type, Value, Value2, StringValue);
            }

            public static bool operator ==(MruEntry? left, MruEntry? right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(MruEntry? left, MruEntry? right)
            {
                return !Equals(left, right);
            }
        }

        public IEnumerable<ISolutionItem> MostRecentlyUsed
        {
            get
            {
                foreach (var entry in mostRecentlyUsed)
                {
                    var projectItem = new AbstractSmartScriptProjectItem()
                    {
                        Type = entry.Type,
                        Value = entry.Value,
                        Value2 = entry.Value2,
                        StringValue = entry.StringValue
                    };
                    
                    if (deserializer.TryDeserialize(projectItem, out var solutionItem))
                        yield return solutionItem!;
                }
            }
        }
    }
}