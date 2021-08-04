using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.MVVM.Observable;
using WDE.Parameters.Providers;

namespace WDE.Parameters
{
    public class ParameterLoader
    {
        private readonly IDatabaseProvider database;
        private readonly IParameterDefinitionProvider parameterDefinitionProvider;
        private readonly IServerIntegration serverIntegration;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly ILoadingEventAggregator loadingEventAggregator;

        private List<LateLoadParameter> databaseParameters = new();
        public ParameterLoader(IDatabaseProvider database, 
            IParameterDefinitionProvider parameterDefinitionProvider,
            IServerIntegration serverIntegration,
            IItemFromListProvider itemFromListProvider,
            IEventAggregator eventAggregator,
            ILoadingEventAggregator loadingEventAggregator)
        {
            this.database = database;
            this.parameterDefinitionProvider = parameterDefinitionProvider;
            this.serverIntegration = serverIntegration;
            this.itemFromListProvider = itemFromListProvider;
            this.eventAggregator = eventAggregator;
            this.loadingEventAggregator = loadingEventAggregator;
        }

        public void Load(ParameterFactory factory)
        {
            foreach (var pair in parameterDefinitionProvider.Parameters)
            {
                if (pair.Value.StringValues != null)
                {
                    SwitchStringParameter stringParameter = new SwitchStringParameter(pair.Value.StringValues);
                    factory.Register(pair.Key, stringParameter);
                }
                else if (pair.Value.Values != null)
                {
                    Parameter p = pair.Value.IsFlag ? new FlagParameter() : new Parameter();
                    p.Items = pair.Value.Values;
                    factory.Register(pair.Key, p);   
                }
            }

            factory.Register("FloatParameter", new FloatIntParameter(1000));
            factory.Register("DecifloatParameter", new FloatIntParameter(100));
            factory.Register("GameEventParameter", AddDatabaseParameter(new GameEventParameter(database)));
            factory.Register("CreatureParameter", AddDatabaseParameter(new CreatureParameter(database, serverIntegration)));
            factory.Register("CreatureGameobjectParameter", AddDatabaseParameter(new CreatureGameobjectParameter(database)));
            factory.Register("QuestParameter", AddDatabaseParameter(new QuestParameter(database)));
            factory.Register("PrevQuestParameter", AddDatabaseParameter(new PrevQuestParameter(database)));
            factory.Register("GameobjectParameter", AddDatabaseParameter(new GameobjectParameter(database, serverIntegration, itemFromListProvider)));
            factory.Register("GossipMenuParameter", AddDatabaseParameter(new GossipMenuParameter(database)));
            factory.Register("NpcTextParameter", AddDatabaseParameter(new NpcTextParameter(database)));
            factory.Register("ConversationTemplateParameter", new ConversationTemplateParameter(database));
            factory.Register("BoolParameter", new BoolParameter());
            factory.Register("FlagParameter", new FlagParameter());
            factory.Register("PercentageParameter", new PercentageParameter());

            loadingEventAggregator.OnEvent<DatabaseLoadedEvent>().SubscribeAction(_ =>
            {
                foreach (var p in databaseParameters)
                {
                    p.LateLoad();
                    factory.Updated(p);
                }
            });
        }
    
        private T AddDatabaseParameter<T>(T t) where T : LateLoadParameter
        {
            databaseParameters.Add(t);
            return t;
        }
    }

    public class PercentageParameter : Parameter
    {
        public override string ToString(long key)
        {
            return key + "%";
        }
    }

    public class BoolParameter : Parameter
    {
        public new static BoolParameter Instance { get; } = new BoolParameter();
        
        public BoolParameter()
        {
            Items = new Dictionary<long, SelectOption> {{0, new SelectOption("No")}, {1, new SelectOption("Yes")}};
        }
    }

    public abstract class LateLoadParameter : ParameterNumbered
    {
        public abstract void LateLoad();
    }

    public abstract class LazyLoadParameter : ParameterNumbered
    {
        public override bool HasItems
        {
            get
            {
                if (Items == null)
                    LazyLoad();
                return Items!.Count > 0;
            }
        }

        public override string ToString(long key)
        {
            if (Items == null)
                LazyLoad();
            return base.ToString(key);
        }

        protected abstract void LazyLoad();
    }

    public class CreatureParameter : LateLoadParameter
    {
        private readonly IDatabaseProvider database;
        
        public override Func<Task<object?>>? SpecialCommand { get; }

        public CreatureParameter(IDatabaseProvider database, IServerIntegration serverIntegration)
        {
            Items = new Dictionary<long, SelectOption>();
            this.database = database;
            SpecialCommand = async () =>
            {
                var entry = await serverIntegration.GetSelectedEntry();
                if (entry.HasValue)
                    return entry;
                return null;
            };
        }

        public override void LateLoad()
        {
            foreach (ICreatureTemplate item in database.GetCreatureTemplates())
                Items!.Add(item.Entry, new SelectOption(item.Name));
        }
    }

    public class CreatureGameobjectParameter : LateLoadParameter
    {
        private readonly IDatabaseProvider database;

        public CreatureGameobjectParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override void LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (ICreatureTemplate item in database.GetCreatureTemplates())
                Items.Add(item.Entry, new SelectOption(item.Name));
            foreach (IGameObjectTemplate item in database.GetGameObjectTemplates())
                Items.Add(-item.Entry, new SelectOption(item.Name));
        }
    }
    
    public class GossipMenuParameter : LateLoadParameter
    {
        private readonly IDatabaseProvider database;

        public GossipMenuParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override void LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IGossipMenu item in database.GetGossipMenus())
                Items.Add(item.MenuId, new SelectOption((item.Text
                        .Select(t => t.Text0_0 ?? t.Text0_1 ?? "")
                        .FirstOrDefault() ?? "")
                        .Replace("\n", "")
                        .Truncate(100)));
        }
    }

    internal static class StringExtensions
    {
        public static string Truncate(this string str, int length)
        {
            if (str.Length >= length + 20)
                return str.Substring(0, length) + "...";
            return str;
        }
    }

    public class NpcTextParameter : LateLoadParameter
    {
        private readonly IDatabaseProvider database;

        public NpcTextParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override void LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (INpcText item in database.GetNpcTexts())
                Items.Add(item.Id, new SelectOption(item.Text0_0 ?? item.Text0_1 ?? ""));
        }
    }
    
    public class QuestParameter : LateLoadParameter
    {
        private readonly IDatabaseProvider database;

        public QuestParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override void LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IQuestTemplate item in database.GetQuestTemplates())
                Items.Add(item.Entry, new SelectOption(item.Name));
        }
    }
    
    public class PrevQuestParameter : LateLoadParameter
    {
        private readonly IDatabaseProvider database;

        public PrevQuestParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override void LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IQuestTemplate item in database.GetQuestTemplates())
            {
                Items.Add(-item.Entry, new SelectOption(item.Name, "quest must be active"));
                Items.Add(item.Entry, new SelectOption(item.Name, "quest must be completed"));
            }
        }
    }

    public class GameEventParameter : LateLoadParameter
    {
        private readonly IDatabaseProvider database;

        public GameEventParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override void LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IGameEvent item in database.GetGameEvents())
                Items.Add(item.Entry, new SelectOption(item.Description ?? "(null)"));
        }
    }

    public class GameobjectParameter : LateLoadParameter
    {
        private readonly IDatabaseProvider database;
        
        public override Func<Task<object?>>? SpecialCommand { get; }

        public GameobjectParameter(IDatabaseProvider database,
            IServerIntegration serverIntegration,
            IItemFromListProvider itemFromListProvider)
        {
            this.database = database;
            SpecialCommand = async () =>
            {
                var entry = await serverIntegration.GetNearestGameObjects();
                if (entry == null || entry.Count == 0)
                    return null;
                
                var options = entry.GroupBy(e => e.Entry)
                    .OrderBy(group => group.Key)
                    .ToDictionary(g => (long)g.Key,
                        g => new SelectOption(database.GetGameObjectTemplate(g.Key)?.Name ?? "Unknown name",
                            $"{g.First().Distance} yd away"));

                if (options.Count == 1)
                    return (uint)options.Keys.First();

                var pick = await itemFromListProvider.GetItemFromList(options, false);

                if (pick.HasValue)
                    return (uint)pick.Value;
                
                return null;
            };
        }

        public override void LateLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IGameObjectTemplate item in database.GetGameObjectTemplates())
                Items.Add(item.Entry, new SelectOption(item.Name));
        }
    }
    
    
    public class ConversationTemplateParameter : LazyLoadParameter
    {
        private readonly IDatabaseProvider database;

        public ConversationTemplateParameter(IDatabaseProvider database)
        {
            this.database = database;
        }
        
        protected override void LazyLoad()
        {
            Items = new Dictionary<long, SelectOption>();
            foreach (IConversationTemplate item in database.GetConversationTemplates())
            {
                var name = $"conversation with first line id: {item.FirstLineId}";
                if (!string.IsNullOrEmpty(item.ScriptName))
                    name += $", script name: {item.ScriptName}";
                Items.Add(item.Id, new SelectOption(name));
            }
        }
    }
    
}