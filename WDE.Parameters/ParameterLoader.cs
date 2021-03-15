using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Parameters.Models;
using WDE.Parameters.Providers;

namespace WDE.Parameters
{
    public class ParameterLoader
    {
        private readonly IDatabaseProvider database;
        private readonly IParameterDefinitionProvider parameterDefinitionProvider;

        public ParameterLoader(IDatabaseProvider database, IParameterDefinitionProvider parameterDefinitionProvider)
        {
            this.database = database;
            this.parameterDefinitionProvider = parameterDefinitionProvider;
        }

        public void Load(ParameterFactory factory)
        {
            foreach (var pair in parameterDefinitionProvider.Parameters)
            {
                Parameter p = pair.Value.IsFlag ? new FlagParameter() : new Parameter();
                p.Items = pair.Value.Values;
                factory.Register(pair.Key, p);
            }
            
            factory.Register("FloatParameter", new FloatIntParameter(1000));
            factory.Register("DecifloatParameter", new FloatIntParameter(100));
            factory.Register("GameEventParameter", new GameEventParameter(database));
            factory.Register("CreatureParameter", new CreatureParameter(database));
            factory.Register("QuestParameter", new QuestParameter(database));
            factory.Register("GameobjectParameter", new GameobjectParameter(database));
            factory.Register("ConversationTemplateParameter", new ConversationTemplateParameter(database));
            factory.Register("BoolParameter", new BoolParameter());
            factory.Register("FlagParameter", new FlagParameter());
        }
    }

    public class BoolParameter : Parameter
    {
        public BoolParameter()
        {
            Items = new Dictionary<long, SelectOption> {{0, new SelectOption("No")}, {1, new SelectOption("Yes")}};
        }
    }

    public class CreatureParameter : Parameter
    {
        private readonly IDatabaseProvider database;

        public CreatureParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override string ToString(long key)
        {
            if (Items == null)
            {
                Items = new Dictionary<long, SelectOption>();
                foreach (ICreatureTemplate item in database.GetCreatureTemplates())
                    Items.Add(item.Entry, new SelectOption(item.Name));
            }
            return base.ToString(key);
        }
    }

    public class QuestParameter : Parameter
    {
        private readonly IDatabaseProvider database;

        public QuestParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override string ToString(long key)
        {
            if (Items == null)
            {
                Items = new Dictionary<long, SelectOption>();
                foreach (IQuestTemplate item in database.GetQuestTemplates())
                    Items.Add(item.Entry, new SelectOption(item.Name));
            }
            return base.ToString(key);
        }
    }
    
    public class GameEventParameter : Parameter
    {
        private readonly IDatabaseProvider database;

        public GameEventParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override string ToString(long key)
        {
            if (Items == null)
            {
                Items = new Dictionary<long, SelectOption>();
                foreach (IGameEvent item in database.GetGameEvents())
                    Items.Add(item.Entry, new SelectOption(item.Description));
            }
            return base.ToString(key);
        }
    }

    public class GameobjectParameter : Parameter
    {
        private readonly IDatabaseProvider database;

        public GameobjectParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override string ToString(long key)
        {
            if (Items == null)
            {
                Items = new Dictionary<long, SelectOption>();
                foreach (IGameObjectTemplate item in database.GetGameObjectTemplates())
                    Items.Add(item.Entry, new SelectOption(item.Name));
            }
            return base.ToString(key);
        }
    }
    
    
    public class ConversationTemplateParameter : Parameter
    {
        private readonly IDatabaseProvider database;

        public ConversationTemplateParameter(IDatabaseProvider database)
        {
            this.database = database;
        }

        public override string ToString(long key)
        {
            if (Items == null)
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
            return base.ToString(key);
        }
    }
    
}