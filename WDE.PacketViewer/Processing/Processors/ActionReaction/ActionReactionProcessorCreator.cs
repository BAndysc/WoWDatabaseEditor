using System;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Module.Attributes;

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
        private readonly ISpellService spellService;
        private readonly Func<IUnitPositionFollower> unitFollower;
        private readonly Func<IChatEmoteSoundProcessor> chatEmote;
        private readonly Func<IWaypointProcessor> waypointProcessor;
        private readonly Func<IUpdateObjectFollower> update;
        private readonly Func<IPlayerGuidFollower> player;
        private readonly Func<IAuraSlotTracker> auraSlotTracker;
        private readonly Func<IDatabaseProvider> databaseProvider;

        public ActionReactionProcessorCreator(
            ISpellService spellService,
            Func<IUnitPositionFollower> unitFollower,
            Func<IChatEmoteSoundProcessor> chatEmote,
            Func<IWaypointProcessor> waypointProcessor,
            Func<IUpdateObjectFollower> update,
            Func<IPlayerGuidFollower> player,
            Func<IAuraSlotTracker> auraSlotTracker,
            Func<IDatabaseProvider> databaseProvider)
        {
            this.spellService = spellService;
            this.unitFollower = unitFollower;
            this.chatEmote = chatEmote;
            this.waypointProcessor = waypointProcessor;
            this.update = update;
            this.player = player;
            this.auraSlotTracker = auraSlotTracker;
            this.databaseProvider = databaseProvider;
        }
        
        public ActionReactionProcessor Create()
        {
            var unitFollower = this.unitFollower();
            var chatEmote = this.chatEmote();
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
            var chatEmote = this.chatEmote();
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