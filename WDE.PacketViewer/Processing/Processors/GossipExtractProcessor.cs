using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.PacketViewer.Utils;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class GossipExtractProcessor : PacketProcessor<bool>, IPacketTextDumper
    {
        private readonly IDatabaseProvider databaseProvider;
        private readonly IQueryGenerator<IGossipMenuOption> gossipMenuOptionGenerator;
        private readonly IQueryGenerator<IGossipMenuLine> gossipMenuGenerator;
        private readonly IQueryGenerator<CreatureGossipUpdate> creatureTemplateGenerator;
        private readonly IQueryGenerator<IPointOfInterest> poiGenerator;
        private readonly IQueryGenerator<INpcTextFull> npcTextInsertGenerator;
        private readonly IQueryGenerator<INpcText> npcTextDeleteGenerator;
        private UniversalGuid? lastGossiper;
        private Dictionary<uint, uint> gossiperToFirstMenuId = new();
        private GossipMenu? currentGossipMenu;
        private Dictionary<uint, GossipMenu> menus = new();
        private Dictionary<uint, IBroadcastText> broadcastTexts = new();
        private List<(uint entry, List<(uint broadcastText, float probability)>)> npcTexts = new();
        private (uint menuId, uint optionIndex)? lastChosenOption;

        private HashSet<uint> templatesNotExistingInDb = new();
        private Dictionary<uint, uint> overridenDatabaseGossipMenu = new();
        private HashSet<uint> menusAlreadyInDb = new();
        private HashSet<uint> menuOptionsAlreadyInDb = new();
        private HashSet<uint> menuOptionsMissingInDb = new();
        private HashSet<uint> menuMissingInDb = new();

        public GossipExtractProcessor(IDatabaseProvider databaseProvider,
            IQueryGenerator<IGossipMenuOption> gossipMenuOptionGenerator,
            IQueryGenerator<IGossipMenuLine> gossipMenuGenerator,
            IQueryGenerator<CreatureGossipUpdate> creatureTemplateGenerator,
            IQueryGenerator<IPointOfInterest> poiGenerator,
            IQueryGenerator<INpcTextFull> npcTextInsertGenerator,
            IQueryGenerator<INpcText> npcTextDeleteGenerator)
        {
            this.databaseProvider = databaseProvider;
            this.gossipMenuOptionGenerator = gossipMenuOptionGenerator;
            this.gossipMenuGenerator = gossipMenuGenerator;
            this.creatureTemplateGenerator = creatureTemplateGenerator;
            this.poiGenerator = poiGenerator;
            this.npcTextInsertGenerator = npcTextInsertGenerator;
            this.npcTextDeleteGenerator = npcTextDeleteGenerator;
        }
        
        private class GossipMenu
        {
            public uint MenuId { get; }
            public HashSet<uint> TextsId { get; }
            public HashSet<uint> DatabaseOnlyTextsId { get; } = new();
            public List<GossipMenuOption> Options { get; } = new();
            public List<GossipMenuOption> DatabaseOptions { get; } = new();

            public GossipMenu(uint menuId, uint textId)
            {
                MenuId = menuId;
                TextsId = new() { textId };
            }

            public void AddTextId(uint textId)
            {
                TextsId.Add(textId);
            }

            public bool TryGetOption(uint index, out GossipMenuOption option)
            {
                var found = Options.FirstOrDefault(o => o.Index == index);
                option = found!;
                return found != null;
            }

            public bool AddDatabaseOption(GossipMenuOption option)
            {
                DatabaseOptions.Add(option);
                return true;
            }
            
            public bool AddOption(GossipMenuOption option)
            {
                var matching = Options.FirstOrDefault(o => o.Index == option.Index);
                if (matching != null)
                {
                    if (matching.Text != option.Text || matching.Icon != option.Icon)
                    {
                        throw new Exception($"Same menu id, same option index different options :( menuId: {MenuId} option index: {option.Index}");
                    }
                    return false;
                }

                Options.Add(option);
                return true;
            }
        }

        private class GossipMenuOption
        {
            public uint Index { get; }
            public string Text { get; }
            public uint BroadcastTextId { get; set; }
            public GossipOptionIcon Icon { get; }
            public GossipOption OptionType { get; }
            public GameDefines.NpcFlags NpcFlags { get; }
            public int LinkedMenuId { get; set; }
            public bool IsFromSniff { get; }
            public bool BoxCoded { get; }
            public string? BoxText { get; }
            public uint BoxTextBroadcastId { get; set; }
            public uint ActionPoiId { get; set; }
            public uint BoxMoney { get; }
            public bool IsFromDatabase => !IsFromSniff;
            public PacketGossipPoi? ActionPoi { get; set; }

            public GossipMenuOption(GossipMessageOption option)
            {
                Index = option.OptionIndex;
                Text = option.Text.ToString() ?? "";
                Icon = (GossipOptionIcon)option.OptionNpc;
                OptionType = GossipOption.Gossip;
                NpcFlags = GameDefines.NpcFlags.Gossip;
                BoxCoded = option.BoxCoded;
                BoxMoney = option.BoxCost;
                BoxText = option.BoxText.BytesLength == 0 ? null : option.BoxText.ToString();
                IsFromSniff = true;
                switch (Icon)
                {
                    case GossipOptionIcon.Vendor:
                        NpcFlags = GameDefines.NpcFlags.Vendor;
                        OptionType = GossipOption.Vendor;
                        break;
                    case GossipOptionIcon.Taxi:
                        NpcFlags = GameDefines.NpcFlags.FlightMaster;
                        OptionType = GossipOption.TaxiVendor;
                        break;
                    case GossipOptionIcon.Trainer:
                        NpcFlags = GameDefines.NpcFlags.Trainer;
                        OptionType = GossipOption.Trainer;
                        break;
                    case GossipOptionIcon.Interact1:
                        NpcFlags = GameDefines.NpcFlags.SpiritHealer;
                        OptionType = GossipOption.SpiritHealer;
                        break;
                    case GossipOptionIcon.Interact2:
                        NpcFlags = GameDefines.NpcFlags.Innkeeper;
                        OptionType = GossipOption.Innkeeper;
                        break;
                    case GossipOptionIcon.MoneyBag:
                        OptionType = Text.Contains("auction", StringComparison.InvariantCultureIgnoreCase) ? GossipOption.Auctioneer : GossipOption.Banker;
                        NpcFlags = OptionType == GossipOption.Auctioneer ? GameDefines.NpcFlags.Auctioneer : GameDefines.NpcFlags.Banker;
                        break;
                    case GossipOptionIcon.Talk:
                        NpcFlags = GameDefines.NpcFlags.Petitioner;
                        OptionType = GossipOption.Petitioner;
                        break;
                    case GossipOptionIcon.Tabard:
                        NpcFlags = GameDefines.NpcFlags.TabardDesigner;
                        OptionType = GossipOption.TabardDesigner;
                        break;
                    case GossipOptionIcon.Battle:
                        NpcFlags = GameDefines.NpcFlags.BattleMaster;
                        OptionType = GossipOption.Battlefield;
                        break;
                    case GossipOptionIcon.Chat12:
                        NpcFlags = GameDefines.NpcFlags.StableMaster;
                        OptionType = GossipOption.StablePet;
                        break;
                    case GossipOptionIcon.Chat19:
                        NpcFlags = GameDefines.NpcFlags.BattleMaster;
                        OptionType = GossipOption.Battlefield;
                        break;
                }
            }

            public GossipMenuOption(IGossipMenuOption db)
            {
                IsFromSniff = false;
                Index = db.OptionIndex;
                Text = db.Text ?? "";
                Icon = db.Icon;
                BroadcastTextId = (uint)db.BroadcastTextId;
                OptionType = db.OptionType;
                LinkedMenuId = db.ActionMenuId;
                NpcFlags = (GameDefines.NpcFlags)db.NpcFlag;
                ActionPoiId = db.ActionPoiId;
                BoxCoded = db.BoxCoded != 0;
                BoxMoney = db.BoxMoney;
                BoxText = string.IsNullOrEmpty(db.BoxText) ? null : db.BoxText;
                BoxTextBroadcastId = (uint)db.BoxBroadcastTextId;
            }
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketGossipHello packet)
        {
            lastGossiper = packet.GossipSource;
            currentGossipMenu = null;
            lastChosenOption = null;
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketGossipMessage packet)
        {
            if (!packet.GossipSource.Equals(lastGossiper))
                return false;
            
            if (currentGossipMenu == null)
            {
                gossiperToFirstMenuId[packet.GossipSource.Entry] = packet.MenuId;
            }
            else if (lastChosenOption.HasValue)
            {
                if (currentGossipMenu.TryGetOption(lastChosenOption.Value.optionIndex, out var chosenOption))
                {
                    chosenOption.LinkedMenuId = (int)packet.MenuId;
                }
                else
                {
                    throw new Exception("Last chosen option, but no such option?");
                }
            }

            if (!menus.TryGetValue(packet.MenuId, out var menu))
                menu = menus[packet.MenuId] = new GossipMenu(packet.MenuId, packet.TextId);

            currentGossipMenu = menu;
            menu.AddTextId(packet.TextId);

            foreach (ref readonly var option in packet.Options.AsSpan())
            {
                if (!menu.TryGetOption(option.OptionIndex, out var gossipMenuOption))
                {
                    gossipMenuOption = new GossipMenuOption(option);
                    menu.AddOption(gossipMenuOption);
                }
                
                if (gossipMenuOption.Text != option.Text.ToString() || (int)gossipMenuOption.Icon != option.OptionNpc)
                    throw new Exception($"Same menu id, same option index different options :( menuId: {menu.MenuId} option index: {option.OptionIndex}");
            }
            
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketGossipClose packet)
        {
            lastGossiper = null;
            currentGossipMenu = null;
            lastChosenOption = null;
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketNpcText packet)
        {
            List<(uint broadcastText, float probability)> texts = new();
            foreach (ref readonly var text in packet.Texts.AsSpan())
            {
                texts.Add((text.BroadcastTextId, text.Probability));
            }
            npcTexts.Add((packet.Entry, texts));
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketGossipPoi packet)
        {
            if (lastChosenOption.HasValue)
            {
                if (menus.TryGetValue(lastChosenOption.Value.menuId, out var menu))
                {
                    if (menu.TryGetOption(lastChosenOption.Value.optionIndex, out var option))
                    {
                        option.ActionPoi = packet;
                    }
                }
            }
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketDbReply packet)
        {
            if (packet.KindCase != PacketDbReply.KindOneofCase.BroadcastText)
                return false;

            var b = packet.BroadcastText!;
            broadcastTexts[b.Id] = new AbstractBroadcastText()
            { 
                Id = b.Id,
                Language = (uint)b.Language,
                Text = b.Text0.ToString(),
                Text1 = b.Text1.ToString(),
                EmoteId1 = b.Emotes[0].EmoteId,
                EmoteId2 = b.Emotes[1].EmoteId,
                EmoteId3 = b.Emotes[2].EmoteId,
                EmoteDelay1 = b.Emotes[0].Delay,
                EmoteDelay2 = b.Emotes[1].Delay,
                EmoteDelay3 = b.Emotes[2].Delay,
                SoundEntriesId = b.Sounds[0],
                EmotesId = b.EmotesId,
                Flags = b.Flags
            };
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketGossipSelect packet)
        {
            lastChosenOption = (packet.MenuId, packet.OptionId);
            return base.Process(in basePacket, in packet);
        }

        private async Task<IBroadcastText?> GetBroadcastText(uint entry)
        {
            if (entry == 0)
                return null;
            if (broadcastTexts.TryGetValue(entry, out var b))
                return b;
            return await databaseProvider.GetBroadcastTextByIdAsync(entry);
        }
        
        public async Task<string> Generate()
        {
            var multiQuery = Queries.BeginTransaction(DataDatabaseType.World);

            await DeleteExisting();

            foreach (var menu in menus.Values)
            {
                foreach (var option in menu.Options)
                {
                    var broadCastTextId = await databaseProvider.GetBroadcastTextByTextAsync(option.Text);
                    if (broadCastTextId != null)
                        option.BroadcastTextId = broadCastTextId.Id;
                    
                    if (option.BoxText != null)
                    {
                        broadCastTextId = await databaseProvider.GetBroadcastTextByTextAsync(option.BoxText);
                        if (broadCastTextId != null)
                            option.BoxTextBroadcastId = broadCastTextId.Id;
                    }
                }
            }

            if (gossiperToFirstMenuId.Count > 0)
            {
                multiQuery.Comment("");
                multiQuery.Comment(" [ Creature templates ] ");
                multiQuery.Comment("");   
            }

            foreach (var menu in gossiperToFirstMenuId
                .Where(entry => !templatesNotExistingInDb.Contains(entry.Key)))
            {
                var template = await databaseProvider.GetCreatureTemplate(menu.Key);
                string? comment = template?.Name;
                if (overridenDatabaseGossipMenu.TryGetValue(menu.Key, out var originalMenu))
                    comment = $"{template?.Name} (was {originalMenu} in db)";
                
                multiQuery.Add(creatureTemplateGenerator.Update(new CreatureGossipUpdate()
                {
                    Entry = menu.Key,
                    GossipMenuId = menu.Value,
                    __comment = comment
                }));
            }

            bool first = true;
            foreach (var menu in gossiperToFirstMenuId
                .Where(entry => templatesNotExistingInDb.Contains(entry.Key)))
            {
                if (first)
                {
                    multiQuery.BlankLine();
                    multiQuery.Comment("Entries not existing in creature_template:");
                    first = false;
                }
                multiQuery.Add(creatureTemplateGenerator.Update(new CreatureGossipUpdate()
                {
                    Entry = menu.Key,
                    GossipMenuId = menu.Value,
                }));
            }

            List<(uint entry, List<(uint broadcastText, float probability)>)> npcTextsToInsert = new();
            foreach (var npcText in npcTexts)
            {
                var model = await databaseProvider.GetNpcText(npcText.entry);
                if (model == null)
                    npcTextsToInsert.Add(npcText);
            }
            if (npcTextsToInsert.Count > 0)
            {
                multiQuery.Comment("");
                multiQuery.Comment(" [ NPC TEXT ]");
                multiQuery.Comment("");
                foreach (var npcText in npcTextsToInsert)
                    multiQuery.Add(npcTextDeleteGenerator.Delete(new AbstractNpcText()
                    {
                        Id = npcText.entry
                    }));

                var inserts = await npcTextsToInsert.SelectAsync(async npcText =>
                {
                    var bcast1 = await GetBroadcastText(npcText.Item2[0].broadcastText);
                    var bcast2 = await GetBroadcastText(npcText.Item2[1].broadcastText);
                    var bcast3 = await GetBroadcastText(npcText.Item2[2].broadcastText);
                    var bcast4 = await GetBroadcastText(npcText.Item2[3].broadcastText);
                    var bcast5 = await GetBroadcastText(npcText.Item2[4].broadcastText);
                    var bcast6 = await GetBroadcastText(npcText.Item2[5].broadcastText);
                    var bcast7 = await GetBroadcastText(npcText.Item2[6].broadcastText);
                    var bcast8 = await GetBroadcastText(npcText.Item2[7].broadcastText);
                    return new AbstractNpcTextFull()
                    {
                        Id = npcText.entry,
                        Text0_0 = bcast1?.Text,
                        Text0_1 = bcast1?.Text1,
                        BroadcastTextID0 = (int?)bcast1?.Id ?? 0,
                        Lang0 = (byte?)bcast1?.Language ?? 0,
                        Probability0 = npcText.Item2[0].probability,
                        EmoteDelay0_0 = bcast1?.EmoteDelay1 ?? 0,
                        Emote0_0 = bcast1?.EmoteId1 ?? 0,
                        EmoteDelay0_1 = bcast1?.EmoteDelay2 ?? 0,
                        Emote0_1 = bcast1?.EmoteId2 ?? 0,
                        EmoteDelay0_2 = bcast1?.EmoteDelay3 ?? 0,
                        Emote0_2 = bcast1?.EmoteId3 ?? 0,


                        Text1_0 = bcast2?.Text,
                        Text1_1 = bcast2?.Text1,
                        BroadcastTextID1 = (int?)bcast2?.Id ?? 0,
                        Lang1 = (byte?)bcast2?.Language ?? 0,
                        Probability1 = npcText.Item2[1].probability,
                        EmoteDelay1_0 = bcast2?.EmoteDelay1 ?? 0,
                        Emote1_0 = bcast2?.EmoteId1 ?? 0,
                        EmoteDelay1_1 = bcast2?.EmoteDelay2 ?? 0,
                        Emote1_1 = bcast2?.EmoteId2 ?? 0,
                        EmoteDelay1_2 = bcast2?.EmoteDelay3 ?? 0,
                        Emote1_2 = bcast2?.EmoteId3 ?? 0,

                        Text2_0 = bcast3?.Text,
                        Text2_1 = bcast3?.Text1,
                        BroadcastTextID2 = (int?)bcast3?.Id ?? 0,
                        Lang2 = (byte?)bcast3?.Language ?? 0,
                        Probability2 = npcText.Item2[2].probability,
                        EmoteDelay2_0 = bcast3?.EmoteDelay1 ?? 0,
                        Emote2_0 = bcast3?.EmoteId1 ?? 0,
                        EmoteDelay2_1 = bcast3?.EmoteDelay2 ?? 0,
                        Emote2_1 = bcast3?.EmoteId2 ?? 0,
                        EmoteDelay2_2 = bcast3?.EmoteDelay3 ?? 0,
                        Emote2_2 = bcast3?.EmoteId3 ?? 0,


                        Text3_0 = bcast4?.Text,
                        Text3_1 = bcast4?.Text1,
                        BroadcastTextID3 = (int?)bcast4?.Id ?? 0,
                        Lang3 = (byte?)bcast4?.Language ?? 0,
                        Probability3 = npcText.Item2[3].probability,
                        EmoteDelay3_0 = bcast4?.EmoteDelay1 ?? 0,
                        Emote3_0 = bcast4?.EmoteId1 ?? 0,
                        EmoteDelay3_1 = bcast4?.EmoteDelay2 ?? 0,
                        Emote3_1 = bcast4?.EmoteId2 ?? 0,
                        EmoteDelay3_2 = bcast4?.EmoteDelay3 ?? 0,
                        Emote3_2 = bcast4?.EmoteId3 ?? 0,

                        Text4_0 = bcast5?.Text,
                        Text4_1 = bcast5?.Text1,
                        BroadcastTextID4 = (int?)bcast5?.Id ?? 0,
                        Lang4 = (byte?)bcast5?.Language ?? 0,
                        Probability4 = npcText.Item2[3].probability,
                        EmoteDelay4_0 = bcast5?.EmoteDelay1 ?? 0,
                        Emote4_0 = bcast5?.EmoteId1 ?? 0,
                        EmoteDelay4_1 = bcast5?.EmoteDelay2 ?? 0,
                        Emote4_1 = bcast5?.EmoteId2 ?? 0,
                        EmoteDelay4_2 = bcast5?.EmoteDelay3 ?? 0,
                        Emote4_2 = bcast5?.EmoteId3 ?? 0,


                        Text5_0 = bcast6?.Text,
                        Text5_1 = bcast6?.Text1,
                        BroadcastTextID5 = (int?)bcast6?.Id ?? 0,
                        Lang5 = (byte?)bcast6?.Language ?? 0,
                        Probability5 = npcText.Item2[5].probability,
                        EmoteDelay5_0 = bcast6?.EmoteDelay1 ?? 0,
                        Emote5_0 = bcast6?.EmoteId1 ?? 0,
                        EmoteDelay5_1 = bcast6?.EmoteDelay2 ?? 0,
                        Emote5_1 = bcast6?.EmoteId2 ?? 0,
                        EmoteDelay5_2 = bcast6?.EmoteDelay3 ?? 0,
                        Emote5_2 = bcast6?.EmoteId3 ?? 0,


                        Text6_0 = bcast7?.Text,
                        Text6_1 = bcast7?.Text1,
                        BroadcastTextID6 = (int?)bcast7?.Id ?? 0,
                        Lang6 = (byte?)bcast7?.Language ?? 0,
                        Probability6 = npcText.Item2[6].probability,
                        EmoteDelay6_0 = bcast7?.EmoteDelay1 ?? 0,
                        Emote6_0 = bcast7?.EmoteId1 ?? 0,
                        EmoteDelay6_1 = bcast7?.EmoteDelay2 ?? 0,
                        Emote6_1 = bcast7?.EmoteId2 ?? 0,
                        EmoteDelay6_2 = bcast7?.EmoteDelay3 ?? 0,
                        Emote6_2 = bcast7?.EmoteId3 ?? 0,

                        Text7_0 = bcast8?.Text,
                        Text7_1 = bcast8?.Text1,
                        BroadcastTextID7 = (int?)bcast8?.Id ?? 0,
                        Lang7 = (byte?)bcast8?.Language ?? 0,
                        Probability7 = npcText.Item2[7].probability,
                        EmoteDelay7_0 = bcast8?.EmoteDelay1 ?? 0,
                        Emote7_0 = bcast8?.EmoteId1 ?? 0,
                        EmoteDelay7_1 = bcast8?.EmoteDelay2 ?? 0,
                        Emote7_1 = bcast8?.EmoteId2 ?? 0,
                        EmoteDelay7_2 = bcast8?.EmoteDelay3 ?? 0,
                        Emote7_2 = bcast8?.EmoteId3 ?? 0
                    };
                });
                
                multiQuery.Add(npcTextInsertGenerator.BulkInsert(inserts.ToList()));
            
                multiQuery.BlankLine();
                multiQuery.BlankLine();
            }
            
            multiQuery.Comment("");
            multiQuery.Comment(" [ MENUS ]");
            multiQuery.Comment("");
            DumpGossipMenu(multiQuery, false);

            var poiToAdd = menus.Values
                .SelectMany(menu => menu.Options)
                .Where(option => option.ActionPoi != null)
                .ToList();
            if (poiToAdd.Count > 0)
            {
                var dbPoi = await databaseProvider.GetPointsOfInterestsAsync();
                var freePoiNum = dbPoi.Select(poi => poi.Id).Max() + 1;
                
                multiQuery.Comment("");
                multiQuery.Comment(" [ POINTS OF INTEREST ]");
                multiQuery.Comment("");

                foreach (var freeId in Enumerable.Range((int)freePoiNum, poiToAdd.Count))
                    multiQuery.Add(poiGenerator.Delete(new AbstractPointOfInterest()
                    {
                        Id = (uint)freeId
                    }));

                var id = freePoiNum;
                multiQuery.Add(poiGenerator.BulkInsert(poiToAdd.Select(option => new AbstractPointOfInterest
                {
                    Id = id++,
                    PositionX = option.ActionPoi!.Value.Coordinates.X,
                    PositionY = option.ActionPoi.Value.Coordinates.Y,
                    Icon = option.ActionPoi.Value.Icon,
                    Flags = option.ActionPoi.Value.Flags,
                    Importance = option.ActionPoi.Value.Importance,
                    Name = option.ActionPoi.Value.Name.ToString() ?? "",
                    VerifiedBuild  = 0,
                }).ToList()));

                id = freePoiNum;
                foreach (var option in poiToAdd)
                {
                    option.ActionPoiId = id++;
                    option.ActionPoi = null;
                }
            }
            
            multiQuery.Comment("");
            multiQuery.Comment(" [ OPTIONS ]");
            multiQuery.Comment("");

            first = true;
            foreach (var menu in menus.Where(pair => menuOptionsMissingInDb.Contains(pair.Key)))
            {
                if (menu.Value.Options.Count == 0)
                    continue;
                
                if (first)
                {
                    multiQuery.BlankLine();
                    multiQuery.Comment("Missing from the database:");
                    first = false;
                }
                DumpGossipMenuOptions(multiQuery, menu);
            }

            first = true;
            foreach (var menu in menus.Where(pair => !menuOptionsAlreadyInDb.Contains(pair.Key) &&
                                                     !menuOptionsMissingInDb.Contains(pair.Key)))
            {
                if (menu.Value.Options.Count == 0)
                    continue;
                
                if (first)
                {
                    multiQuery.BlankLine();
                    multiQuery.Comment("Partially in the database:");
                    first = false;
                }
                DumpGossipMenuOptions(multiQuery, menu);
            }
            
            multiQuery.BlankLine();
            multiQuery.BlankLine();
            multiQuery.Comment(" [                ]");
            multiQuery.Comment(" [ ALREADY IN DB  ]");
            multiQuery.Comment(" [                ]");
            multiQuery.BlankLine();
            DumpGossipMenu(multiQuery, true);
            
            first = true;
            foreach (var menu in menus.Where(pair => menuOptionsAlreadyInDb.Contains(pair.Key)))
            {
                if (menu.Value.Options.Count == 0)
                    continue;

                if (first)
                {
                    multiQuery.BlankLine();
                    multiQuery.Comment("Existing in the database:");
                    first = false;
                }
                
                DumpGossipMenuOptions(multiQuery, menu);
            }

            return multiQuery.Close().QueryString;
        }

        private void DumpGossipMenu(IMultiQuery multiQuery, bool existing)
        {
            bool any = false;
            foreach (var menu in menus)
            {
                if (menusAlreadyInDb.Contains(menu.Key) != existing)
                    continue;

                any = true;
                multiQuery.Add(gossipMenuGenerator.Delete(new AbstractGossipMenuLine()
                {
                    MenuId = menu.Key
                }));
            }

            multiQuery.Add(gossipMenuGenerator.BulkInsert(menus
                .Where(menu => menusAlreadyInDb.Contains(menu.Key) == existing)
                .SelectMany(menu => menu.Value.TextsId.Select(text => new AbstractGossipMenuLine()
                {
                    MenuId = menu.Key,
                    TextId = text,
                    __comment = (string?)null
                }).Concat(menu.Value.DatabaseOnlyTextsId.Select(dbText => new AbstractGossipMenuLine()
                {
                    MenuId = menu.Key,
                    TextId = dbText,
                    __comment = (string?)"not in sniff"
                }))).ToList()));

            if (any)
            {
                multiQuery.BlankLine();
                multiQuery.BlankLine();
            }
        }

        private void DumpGossipMenuOptions(IMultiQuery multiQuery, KeyValuePair<uint, GossipMenu> menu)
        {
            multiQuery.Add(gossipMenuOptionGenerator.Delete(new AbstractGossipMenuOption(){MenuId = menu.Key}));
            var options = menu.Value.Options.Select(option => (option.Index, new AbstractGossipMenuOption
            {
                MenuId = menu.Key,
                OptionIndex = option.Index,
                Icon = option.Icon,
                Text = option.Text,
                BroadcastTextId = (int)option.BroadcastTextId,
                OptionType = option.OptionType,
                NpcFlag = (uint)option.NpcFlags,
                ActionMenuId = option.LinkedMenuId,
                ActionPoiId = option.ActionPoiId,
                BoxCoded = option.BoxCoded ? 1U : 0,
                BoxMoney = option.BoxMoney,
                BoxText = option.BoxText,
                BoxBroadcastTextId = (int)option.BoxTextBroadcastId,
                __comment = (string?)null,
                __ignored = false
            })).Concat(menu.Value.DatabaseOptions.Select(option =>
            {
                var isConflicting = menu.Value.Options.Any(o => o.Index == option.Index);
                return (option.Index, new AbstractGossipMenuOption
                {
                    MenuId = menu.Key,
                    OptionIndex = option.Index,
                    Icon = option.Icon,
                    Text = option.Text,
                    BroadcastTextId = (int)option.BroadcastTextId,
                    OptionType = option.OptionType,
                    NpcFlag = (uint)option.NpcFlags,
                    ActionMenuId = option.LinkedMenuId,
                    ActionPoiId = option.ActionPoiId,
                    BoxCoded = option.BoxCoded ? 1U : 0,
                    BoxMoney = option.BoxMoney,
                    BoxText = option.BoxText,
                    BoxBroadcastTextId = (int)option.BoxTextBroadcastId,
                    __comment = "not in sniff" + (isConflicting ? ", CONFLICTING ID" : ""),
                    __ignored = isConflicting
                });
            }));
            
            multiQuery.Add(gossipMenuOptionGenerator.BulkInsert(options
                .OrderBy(o => o.Index)
                .Select(o => o.Item2)
                .ToList()));
            multiQuery.BlankLine();
        }

        private async Task ResolvePointsOfInterest()
        {
            var dbPoi = await databaseProvider.GetPointsOfInterestsAsync();

            foreach (var menu in menus.Values)
            {
                foreach (var option in menu.Options)
                {
                    if (option.ActionPoi == null)
                        continue;

                    var close = dbPoi.FirstOrDefault(
                        poi => poi.Icon == option.ActionPoi.Value.Icon &&
                               poi.Flags == option.ActionPoi.Value.Flags &&
                               Math.Abs(poi.PositionX - option.ActionPoi.Value.Coordinates.X) < 1 &&
                               Math.Abs(poi.PositionY - option.ActionPoi.Value.Coordinates.Y) < 1 &&
                               poi.Name == option.ActionPoi.Value.Name.ToString());
                    if (close != null)
                    {
                        option.ActionPoiId = close.Id;
                        option.ActionPoi = null;
                    }
                }
            }
        }

        private async Task DeleteExisting()
        {
            await ResolvePointsOfInterest();
            
            foreach (var entry in gossiperToFirstMenuId.Keys.ToList())
            {
                var template = await databaseProvider.GetCreatureTemplate(entry);
                if (template != null)
                {
                    if (template.GossipMenuId == gossiperToFirstMenuId[entry])
                        gossiperToFirstMenuId.Remove(entry);
                    else if (template.GossipMenuId != 0)
                        overridenDatabaseGossipMenu.Add(entry, template.GossipMenuId);
                }
                else
                    templatesNotExistingInDb.Add(entry);
            }

            var databaseMenus = await databaseProvider.GetGossipMenusAsync();
            var databaseGossipMenus = databaseMenus.ToDictionary(o => o.MenuId);
            foreach (var menu in menus)
            {
                if (!databaseGossipMenus.TryGetValue(menu.Key, out var dbMenu))
                {
                    menuMissingInDb.Add(menu.Key);
                    continue;
                }

                foreach (var dbText in dbMenu.Text)
                {
                    if (!menu.Value.TextsId.Contains(dbText.Id))
                        menu.Value.DatabaseOnlyTextsId.Add(dbText.Id);
                }
                
                if (menu.Value.DatabaseOnlyTextsId.Count == 0 &&
                    menu.Value.TextsId.All(text => dbMenu.Text.Any(f => f.Id == text)))
                    menusAlreadyInDb.Add(menu.Key);
            }

            foreach (var menu in menus)
            {
                if (menu.Value.Options.Count == 0)
                    continue;
                
                var existingOptions = await databaseProvider.GetGossipMenuOptionsAsync(menu.Key);

                if (existingOptions.Count == 0)
                {
                    menuOptionsMissingInDb.Add(menu.Key);
                    continue;
                }
                
                bool allInDb = true;
                foreach (var option in menu.Value.Options)
                {
                    var isInDb = existingOptions.Any(db => IsSameOption(option, db));
                    if (!isInDb)
                        allInDb = false;
                }

                foreach (var option in existingOptions)
                {
                    var isInSniff = menu.Value.Options.Any(sniff => IsSameOption(option, sniff));
                    if (!isInSniff)
                    {
                        allInDb = false;
                        menu.Value.AddDatabaseOption(new GossipMenuOption(option));
                    }
                }

                if (allInDb)
                {
                    menuOptionsAlreadyInDb.Add(menu.Key);
                }
            }
        }

        private bool IsSameOption(GossipMenuOption sniff, IGossipMenuOption db)
        {
            if (sniff.Index != db.OptionIndex)
                return false;

            if (sniff.Icon != db.Icon)
                return false;

            if (sniff.LinkedMenuId != db.ActionMenuId)
                return false;

            if (db.HasOptionType && sniff.OptionType != db.OptionType)
                return false;

            if ((uint)sniff.NpcFlags != db.NpcFlag)
                return false;

            if (sniff.Text != db.Text)
                return false;
            return true;
        }

        private bool IsSameOption(IGossipMenuOption db, GossipMenuOption sniff) => IsSameOption(sniff, db);
    }
}