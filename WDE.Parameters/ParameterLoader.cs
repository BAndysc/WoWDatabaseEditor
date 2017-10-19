using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Parameters.Models;

namespace WDE.Parameters
{
    public class ParameterLoader
    {
        private readonly IUnityContainer _container;

        public ParameterLoader(IUnityContainer container)
        {
            _container = container;
        }

        public void Load(ParameterFactory factory)
        {
            var data = File.ReadAllText("Data/parameters.json");
            var models = JsonConvert.DeserializeObject<Dictionary<string, ParameterSpecModel>>(data);
            foreach (var key in models.Keys)
                factory.Add(key, models[key]);

            factory.Register("FloatParameter", (s) => new FloatIntParameter(s));

            factory.Register("CreatureParameter", (s) => new CreatureParameter(s, _container));

            factory.Register("QuestParameter", (s) => new QuestParameter(s, _container));

            factory.Register("GameobjectParameter", (s) => new GameobjectParameter(s, _container));

            factory.Register("BoolParameter", (s) => new BoolParameter(s));
        }
    }

    public class SwitchParameter : Parameter
    {
        public SwitchParameter(string name, Dictionary<int, SelectOption> options) : base(name)
        {
            Items = options;
        }
    }

    public class BoolParameter : SwitchParameter
    {
        public BoolParameter(string name) : base(name, new Dictionary<int, SelectOption>(){{0, new SelectOption("No")}, { 1, new SelectOption("Yes") } })
        {
        }
    }

    public class CreatureParameter : Parameter
    {
        private readonly IUnityContainer _container;

        public CreatureParameter(string name, IUnityContainer container) : base(name)
        {
            _container = container;
            Items = new Dictionary<int, SelectOption>();
            foreach (var item in _container.Resolve<IDatabaseProvider>().GetCreatureTemplates())
                Items.Add((int)item.Entry, new SelectOption(item.Name));
        }
    }

    public class QuestParameter : Parameter
    {
        private readonly IUnityContainer _container;

        public QuestParameter(string name, IUnityContainer container) : base(name)
        {
            _container = container;
            Items = new Dictionary<int, SelectOption>();
            foreach (var item in _container.Resolve<IDatabaseProvider>().GetQuestTemplates())
                Items.Add((int)item.Entry, new SelectOption(item.Name));
        }
    }

    public class GameobjectParameter : Parameter
    {
        private readonly IUnityContainer _container;

        public GameobjectParameter(string name, IUnityContainer container) : base(name)
        {
            _container = container;
            Items = new Dictionary<int, SelectOption>();
            foreach (var item in _container.Resolve<IDatabaseProvider>().GetGameObjectTemplates())
                Items.Add((int)item.Entry, new SelectOption(item.Name));
        }
    }
}
