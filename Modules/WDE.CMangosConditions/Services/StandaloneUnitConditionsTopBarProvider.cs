using System.Collections.Generic;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.QuickAccess;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.CMangosConditions.Services
{
    [AutoRegister]
    [RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
    public class StandaloneUnitConditionsTopBarProvider : ITopBarQuickAccessProvider
    {
        public IEnumerable<ITopBarQuickAccessItem> Items { get; }
        public int Order => -1;

        public StandaloneUnitConditionsTopBarProvider(IStandaloneUnitConditionsService service)
        {
            Items = new List<ITopBarQuickAccessItem>
            {
                new TopBarQuickAccessItem("Unit conditions", new ImageUri("Icons/document_conditions.png"),
                    new DelegateCommand(() => service.OpenStandaloneUnitConditionsEditor().ListenErrors()))
            };
        }

        private class TopBarQuickAccessItem : ITopBarQuickAccessItem
        {
            public TopBarQuickAccessItem(string name, ImageUri icon, ICommand command)
            {
                Name = name;
                Icon = icon;
                Command = command;
            }

            public ICommand Command { get; }
            public string Name { get; }
            public ImageUri Icon { get; }
        }
    }
}
