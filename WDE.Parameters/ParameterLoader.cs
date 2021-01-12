using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Parameters.Models;

namespace WDE.Parameters
{
    public class ParameterLoader
    {
        private readonly IDatabaseProvider database;

        public ParameterLoader(IDatabaseProvider database)
        {
            this.database = database;
        }

        public void Load(ParameterFactory factory)
        {
            string data = File.ReadAllText("Data/parameters.json");
            var models = JsonConvert.DeserializeObject<Dictionary<string, ParameterSpecModel>>(data);
            foreach (string key in models.Keys)
                factory.Add(key, models[key]);

            factory.Register("FloatParameter", s => new FloatIntParameter(s));

            factory.Register("CreatureParameter", s => new CreatureParameter(s, database));

            factory.Register("QuestParameter", s => new QuestParameter(s, database));

            factory.Register("GameobjectParameter", s => new GameobjectParameter(s, database));

            factory.Register("BoolParameter", s => new BoolParameter(s));
        }
    }

    public class SwitchParameter : Parameter
    {
        public SwitchParameter(string name, Dictionary<int, SelectOption> options) : base(name)
        {
            Items = options;
        }

        public override Parameter Clone()
        {
            return new SwitchParameter(Name, Items) {Value = value};
        }
    }

    public class BoolParameter : SwitchParameter
    {
        public BoolParameter(string name) : base(name,
            new Dictionary<int, SelectOption> {{0, new SelectOption("No")}, {1, new SelectOption("Yes")}})
        {
        }

        public override Parameter Clone()
        {
            return new BoolParameter(Name) {Value = value};
        }
    }

    public class CreatureParameter : Parameter
    {
        private readonly IDatabaseProvider database;

        public CreatureParameter(string name, IDatabaseProvider database) : base(name)
        {
            this.database = database;
            Items = new Dictionary<int, SelectOption>();
            foreach (ICreatureTemplate item in database.GetCreatureTemplates())
                Items.Add((int) item.Entry, new SelectOption(item.Name));
        }

        public override Parameter Clone()
        {
            return new CreatureParameter(Name, database) {Value = value};
        }
    }

    public class QuestParameter : Parameter
    {
        private readonly IDatabaseProvider database;

        public QuestParameter(string name, IDatabaseProvider database) : base(name)
        {
            this.database = database;
            Items = new Dictionary<int, SelectOption>();
            foreach (IQuestTemplate item in database.GetQuestTemplates())
                Items.Add((int) item.Entry, new SelectOption(item.Name));
        }

        public override Parameter Clone()
        {
            return new QuestParameter(Name, database) {Value = value};
        }
    }

    public class GameobjectParameter : Parameter
    {
        private readonly IDatabaseProvider database;

        public GameobjectParameter(string name, IDatabaseProvider database) : base(name)
        {
            this.database = database;
            Items = new Dictionary<int, SelectOption>();
            foreach (IGameObjectTemplate item in database.GetGameObjectTemplates())
                Items.Add((int) item.Entry, new SelectOption(item.Name));
        }

        public override Parameter Clone()
        {
            return new GameobjectParameter(Name, database) {Value = value};
        }
    }
}