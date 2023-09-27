using System;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.PacketViewer.ViewModels;

namespace WDE.PacketViewer.Processing.Processors.ActionReaction
{
    public interface IActionReactionProcessorCreator
    {
        ActionReactionProcessor Create();
        ActionReactionToTextProcessor CreateTextProcessor();
    }
    
    [AutoRegister]
    [SingleInstance]
    public class ActionReactionProcessorCreator : IActionReactionProcessorCreator
    {
        private readonly IDbcSpellService spellService;
        private readonly IContainerProvider provider;
        private readonly Func<IUnitPositionFollower> unitFollower;
        private readonly Func<IWaypointProcessor> waypointProcessor;
        private readonly Func<IUpdateObjectFollower> update;
        private readonly Func<IPlayerGuidFollower> player;
        private readonly Func<IAuraSlotTracker> auraSlotTracker;
        private readonly Func<IDatabaseProvider> databaseProvider;
        private readonly IParsingSettings settings;

        public ActionReactionProcessorCreator(
            IDbcSpellService spellService,
            IContainerProvider provider,
            Func<IUnitPositionFollower> unitFollower,
            Func<IWaypointProcessor> waypointProcessor,
            Func<IUpdateObjectFollower> update,
            Func<IPlayerGuidFollower> player,
            Func<IAuraSlotTracker> auraSlotTracker,
            Func<IDatabaseProvider> databaseProvider,
            IParsingSettings settings)
        {
            this.spellService = spellService;
            this.provider = provider;
            this.unitFollower = unitFollower;
            this.waypointProcessor = waypointProcessor;
            this.update = update;
            this.player = player;
            this.auraSlotTracker = auraSlotTracker;
            this.databaseProvider = databaseProvider;
            this.settings = settings;
        }
        
        public ActionReactionProcessor Create()
        {
            var unitFollower = this.unitFollower();
            var chatEmote = provider.Resolve<IChatEmoteSoundProcessor>((typeof(IParsingSettings), settings));
            var waypointProcessor = this.waypointProcessor();
            var update = this.update();
            var player = this.player();
            var auraSlotTracker = this.auraSlotTracker();
            return new ActionReactionProcessor(
                new ActionGenerator(spellService, unitFollower, chatEmote, update, player, waypointProcessor, auraSlotTracker),
                new EventDetectorProcessor(spellService, unitFollower, waypointProcessor, chatEmote, waypointProcessor, update,
                    player, auraSlotTracker, databaseProvider()),
                chatEmote,
                waypointProcessor,
                unitFollower,
                update, player, auraSlotTracker);
        }
        
        public ActionReactionToTextProcessor CreateTextProcessor()
        {
            var unitFollower = this.unitFollower();
            var chatEmote = provider.Resolve<IChatEmoteSoundProcessor>((typeof(IParsingSettings), settings));
            var waypointProcessor = this.waypointProcessor();
            var update = this.update();
            var player = this.player();
            var auraSlotTracker = this.auraSlotTracker();
            return new ActionReactionToTextProcessor(
                new ActionGenerator(spellService, unitFollower, chatEmote, update, player, waypointProcessor, auraSlotTracker),
                new EventDetectorProcessor(spellService, unitFollower, waypointProcessor, chatEmote, waypointProcessor, update,
                    player, auraSlotTracker, databaseProvider()),
                chatEmote,
                waypointProcessor,
                unitFollower,
                update, player,
                auraSlotTracker);
        }
    }
}