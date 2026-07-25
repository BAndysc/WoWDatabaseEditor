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
    public class StandaloneMangosConditionsTopBarProvider : ITopBarQuickAccessProvider
    {
        public IEnumerable<ITopBarQuickAccessItem> Items { get; }
        public int Order => -1;

        public StandaloneMangosConditionsTopBarProvider(IStandaloneMangosConditionsService service)
        {
            Items = new List<ITopBarQuickAccessItem>
            {
                new TopBarQuickAccessItem("Conditions", new ImageUri("Icons/document_conditions.png"),
                    new DelegateCommand(() => service.OpenStandaloneConditionsEditor().ListenErrors()))
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
