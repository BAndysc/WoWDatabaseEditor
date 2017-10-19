using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Practices.Unity;
using WDE.Common;
using WDE.Common.Database;

namespace WDE.SmartScriptEditor
{
    public abstract class SmartScriptSolutionItemProvider : ISolutionItemProvider
    {
        protected IUnityContainer Container;

        private readonly string _name;
        private readonly string _desc;
        private readonly ImageSource _icon;
        private readonly SmartScriptType _type;

        protected SmartScriptSolutionItemProvider(string name, string desc, string icon, SmartScriptType type, IUnityContainer container)
        {
            _name = name;
            _desc = desc;
            _type = type;
            _icon = new BitmapImage(new Uri($"/WDE.SmartScriptEditor;component/Resources/{icon}.png", UriKind.Relative));

            Container = container;
        }

        public string GetName()
        {
            return _name;
        }

        public ImageSource GetImage()
        {
            return _icon;
        }

        public string GetDescription()
        {
            return _desc;
        }

        public virtual ISolutionItem CreateSolutionItem()
        {
            return new SmartScriptSolutionItem(0, _type, null);
        }
    }

    public class SmartScriptCreatureProvider : SmartScriptSolutionItemProvider
    {
        public SmartScriptCreatureProvider(IUnityContainer container)
            : base("Creature Script", "Script any npc in game.", "SmartScriptCreatureIcon", SmartScriptType.Creature, container)
        {
        }

        public override ISolutionItem CreateSolutionItem()
        {
            ICreatureEntryProviderService service = Container.Resolve<ICreatureEntryProviderService>();
            uint? entry = service.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int)entry.Value, SmartScriptType.Creature, Container);
        }
    }

    public class SmartScriptGameobjectProvider : SmartScriptSolutionItemProvider
    {
        public SmartScriptGameobjectProvider(IUnityContainer container)
            : base("Gameobject Script", "Create script for object, including transports.", "SmartScriptGameobjectIcon", SmartScriptType.GameObject, container)
        {
        }

        public override ISolutionItem CreateSolutionItem()
        {
            IGameobjectEntryProviderService service = Container.Resolve<IGameobjectEntryProviderService>();
            uint? entry = service.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int)entry.Value, SmartScriptType.GameObject, Container);
        }
    }

    public class SmartScriptQuestProvider : SmartScriptSolutionItemProvider
    {
        public SmartScriptQuestProvider(IUnityContainer container)
            : base("Quest Script", "Write a script for quest: on accept, on reward.", "SmartScriptQuestIcon", SmartScriptType.Quest, container)
        {
        }

        public override ISolutionItem CreateSolutionItem()
        {
            IQuestEntryProviderService service = Container.Resolve<IQuestEntryProviderService>();
            uint? entry = service.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int)entry.Value, SmartScriptType.Quest, Container);
        }
    }

    public class SmartScriptAuraProvider : SmartScriptSolutionItemProvider
    {
        public SmartScriptAuraProvider(IUnityContainer container)
            : base("Aura Script", "Auras can have scripted several events: on apply, on remove, on periodic tick.", "SmartScriptAuraIcon", SmartScriptType.Aura, container)
        {
        }

        public override ISolutionItem CreateSolutionItem()
        {
            ISpellEntryProviderService service = Container.Resolve<ISpellEntryProviderService>();
            uint? entry = service.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int)entry.Value, SmartScriptType.Spell, Container);
        }
    }

    public class SmartScriptSpellProvider : SmartScriptSolutionItemProvider
    {
        public SmartScriptSpellProvider(IUnityContainer container)
            : base("Spell Script", "Create a new script for spell: this includes script for any existing effect in spell.", "SmartScriptSpellIcon", SmartScriptType.Spell, container)
        {
        }

        public override ISolutionItem CreateSolutionItem()
        {
            ISpellEntryProviderService service = Container.Resolve<ISpellEntryProviderService>();
            uint? entry = service.GetEntryFromService();
            if (!entry.HasValue)
                return null;
            return new SmartScriptSolutionItem((int)entry.Value, SmartScriptType.Spell, Container);
        }
    }

    public class SmartScriptTimedActionListProvider : SmartScriptSolutionItemProvider
    {
        public SmartScriptTimedActionListProvider(IUnityContainer container)
            : base("Timed action list", "Timed action list contains list of actions played in time, this can be used to create RP events, cameras, etc.", "SmartScriptTimedActionListIcon", SmartScriptType.Timed, container)
        {
        }
    }
}
