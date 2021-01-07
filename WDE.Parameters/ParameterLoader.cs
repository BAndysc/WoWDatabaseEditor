using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Parameters.Models;
using Prism.Ioc;

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
            var data = File.ReadAllText("Data/parameters.json");
            var models = JsonConvert.DeserializeObject<Dictionary<string, ParameterSpecModel>>(data);
            foreach (var key in models.Keys)
                factory.Add(key, models[key]);

            factory.Register("FloatParameter", (s) => new FloatIntParameter(s));

            factory.Register("CreatureParameter", (s) => new CreatureParameter(s, database));

            factory.Register("QuestParameter", (s) => new QuestParameter(s, database));

            factory.Register("GameobjectParameter", (s) => new GameobjectParameter(s, database));

            factory.Register("BoolParameter", (s) => new BoolParameter(s));
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
            return new SwitchParameter(Name, Items) {Value = _value};
        }
    }

    public class BoolParameter : SwitchParameter
    {
        public BoolParameter(string name) : base(name, new Dictionary<int, SelectOption>(){{0, new SelectOption("No")}, { 1, new SelectOption("Yes") } })
        {
        }
        
        public override Parameter Clone()
        {
            return new BoolParameter(Name) {Value = _value};
        }
    }

    public class CreatureParameter : Parameter
    {
        private readonly IDatabaseProvider _database;

        public CreatureParameter(string name, IDatabaseProvider database) : base(name)
        {
            _database = database;
            Items = new Dictionary<int, SelectOption>();
            foreach (var item in database.GetCreatureTemplates())
                Items.Add((int)item.Entry, new SelectOption(item.Name));
        }
        
        public override Parameter Clone()
        {
            return new CreatureParameter(Name, _database) {Value = _value};
        }
    }

    public class QuestParameter : Parameter
    {
        private readonly IDatabaseProvider _database;

        public QuestParameter(string name, IDatabaseProvider database) : base(name)
        {
            _database = database;
            Items = new Dictionary<int, SelectOption>();
            foreach (var item in database.GetQuestTemplates())
                Items.Add((int)item.Entry, new SelectOption(item.Name));
        }
        
        public override Parameter Clone()
        {
            return new QuestParameter(Name, _database) {Value = _value};
        }
    }

    public class GameobjectParameter : Parameter
    {
        private readonly IDatabaseProvider _database;

        public GameobjectParameter(string name, IDatabaseProvider database) : base(name)
        {
            _database = database;
            Items = new Dictionary<int, SelectOption>();
            foreach (var item in database.GetGameObjectTemplates())
                Items.Add((int)item.Entry, new SelectOption(item.Name));
        }
        
        public override Parameter Clone()
        {
            return new GameobjectParameter(Name, _database) {Value = _value};
        }
    }
}
