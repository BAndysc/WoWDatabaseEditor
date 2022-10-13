using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.PacketViewer.Utils;
using WDE.SqlQueryGenerator;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class GossipExtractProcessor : PacketProcessor<bool>, IPacketTextDumper
    {
        private readonly IDatabaseProvider databaseProvider;
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

        public GossipExtractProcessor(IDatabaseProvider databaseProvider)
        {
            this.databaseProvider = databaseProvider;
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
                Text = option.Text;
                Icon = (GossipOptionIcon)option.OptionIcon;
                OptionType = GossipOption.Gossip;
                NpcFlags = GameDefines.NpcFlags.Gossip;
                BoxCoded = option.BoxCoded;
                BoxMoney = option.BoxCost;
                BoxText = string.IsNullOrEmpty(option.BoxText) ? null : option.BoxText;
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

        protected override bool Process(PacketBase basePacket, PacketGossipHello packet)
        {
            lastGossiper = packet.GossipSource;
            currentGossipMenu = null;
            lastChosenOption = null;
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketGossipMessage packet)
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

            foreach (var option in packet.Options)
            {
                if (!menu.TryGetOption(option.OptionIndex, out var gossipMenuOption))
                {
                    gossipMenuOption = new GossipMenuOption(option);
                    menu.AddOption(gossipMenuOption);
                }
                
                if (gossipMenuOption.Text != option.Text || (int)gossipMenuOption.Icon != option.OptionIcon)
                    throw new Exception($"Same menu id, same option index different options :( menuId: {menu.MenuId} option index: {option.OptionIndex}");
            }
            
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketGossipClose packet)
        {
            lastGossiper = null;
            currentGossipMenu = null;
            lastChosenOption = null;
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketNpcText packet)
        {
            npcTexts.Add((packet.Entry, packet.Texts.Select(t => (t.BroadcastTextId, t.Probability)).ToList()));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketGossipPoi packet)
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
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketDbReply packet)
        {
            if (packet.KindCase != PacketDbReply.KindOneofCase.BroadcastText)
                return false;

            var b = packet.BroadcastText!;
            broadcastTexts[b.Id] = new AbstractBroadcastText()
            { 
                Id = b.Id,
                Language = (uint)b.Language,
                Text = b.Text0,
                Text1 = b.Text1,
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
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketGossipSelect packet)
        {
            lastChosenOption = (packet.MenuId, packet.OptionId);
            return base.Process(basePacket, packet);
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
            var multiQuery = Queries.BeginTransaction();

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
                var template = databaseProvider.GetCreatureTemplate(menu.Key);
                string? comment = template?.Name;
                if (overridenDatabaseGossipMenu.TryGetValue(menu.Key, out var originalMenu))
                    comment = $"{template?.Name} (was {originalMenu} in db)";
                
                multiQuery.Table("creature_template")
                    .Where(row => row.Column<uint>("entry") == menu.Key)
                    .Set("gossip_menu_id", menu.Value)
                    .Update(comment);
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
                multiQuery.Table("creature_template")
                    .Where(row => row.Column<uint>("entry") == menu.Key)
                    .Set("gossip_menu_id", menu.Value)
                    .Update();
            }

            var npcTextsToInsert =
                npcTexts.Where(npcText => databaseProvider.GetNpcText(npcText.entry) == null).ToList();
            if (npcTextsToInsert.Count > 0)
            {
                multiQuery.Comment("");
                multiQuery.Comment(" [ NPC TEXT ]");
                multiQuery.Comment("");
                multiQuery.Table("npc_text")
                    .WhereIn("ID", npcTextsToInsert.Select(npcText => npcText.entry))
                    .Delete();

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
                    return new
                    {
                        ID = npcText.entry,
                        text0_0 = bcast1?.Text,
                        text0_1 = bcast1?.Text1,
                        BroadcastTextID0 = bcast1?.Id ?? 0,
                        lang0 = bcast1?.Language ?? 0,
                        Probability0 = npcText.Item2[0].probability,
                        EmoteDelay0_0 = bcast1?.EmoteDelay1 ?? 0,
                        Emote0_0 = bcast1?.EmoteId1 ?? 0,
                        EmoteDelay0_1 = bcast1?.EmoteDelay2 ?? 0,
                        Emote0_1 = bcast1?.EmoteId2 ?? 0,
                        EmoteDelay0_2 = bcast1?.EmoteDelay3 ?? 0,
                        Emote0_2 = bcast1?.EmoteId3 ?? 0,


                        text1_0 = bcast2?.Text,
                        text1_1 = bcast2?.Text1,
                        BroadcastTextID1 = bcast2?.Id ?? 0,
                        lang1 = bcast2?.Language ?? 0,
                        Probability1 = npcText.Item2[1].probability,
                        EmoteDelay1_0 = bcast2?.EmoteDelay1 ?? 0,
                        Emote1_0 = bcast2?.EmoteId1 ?? 0,
                        EmoteDelay1_1 = bcast2?.EmoteDelay2 ?? 0,
                        Emote1_1 = bcast2?.EmoteId2 ?? 0,
                        EmoteDelay1_2 = bcast2?.EmoteDelay3 ?? 0,
                        Emote1_2 = bcast2?.EmoteId3 ?? 0,

                        text2_0 = bcast3?.Text,
                        text2_1 = bcast3?.Text1,
                        BroadcastTextID2 = bcast3?.Id ?? 0,
                        lang2 = bcast3?.Language ?? 0,
                        Probability2 = npcText.Item2[2].probability,
                        EmoteDelay2_0 = bcast3?.EmoteDelay1 ?? 0,
                        Emote2_0 = bcast3?.EmoteId1 ?? 0,
                        EmoteDelay2_1 = bcast3?.EmoteDelay2 ?? 0,
                        Emote2_1 = bcast3?.EmoteId2 ?? 0,
                        EmoteDelay2_2 = bcast3?.EmoteDelay3 ?? 0,
                        Emote2_2 = bcast3?.EmoteId3 ?? 0,


                        text3_0 = bcast4?.Text,
                        text3_1 = bcast4?.Text1,
                        BroadcastTextID3 = bcast4?.Id ?? 0,
                        lang3 = bcast4?.Language ?? 0,
                        Probability3 = npcText.Item2[3].probability,
                        EmoteDelay3_0 = bcast4?.EmoteDelay1 ?? 0,
                        Emote3_0 = bcast4?.EmoteId1 ?? 0,
                        EmoteDelay3_1 = bcast4?.EmoteDelay2 ?? 0,
                        Emote3_1 = bcast4?.EmoteId2 ?? 0,
                        EmoteDelay3_2 = bcast4?.EmoteDelay3 ?? 0,
                        Emote3_2 = bcast4?.EmoteId3 ?? 0,

                        text4_0 = bcast5?.Text,
                        text4_1 = bcast5?.Text1,
                        BroadcastTextID4 = bcast5?.Id ?? 0,
                        lang4 = bcast5?.Language ?? 0,
                        Probability4 = npcText.Item2[3].probability,
                        EmoteDelay4_0 = bcast5?.EmoteDelay1 ?? 0,
                        Emote4_0 = bcast5?.EmoteId1 ?? 0,
                        EmoteDelay4_1 = bcast5?.EmoteDelay2 ?? 0,
                        Emote4_1 = bcast5?.EmoteId2 ?? 0,
                        EmoteDelay4_2 = bcast5?.EmoteDelay3 ?? 0,
                        Emote4_2 = bcast5?.EmoteId3 ?? 0,


                        text5_0 = bcast6?.Text,
                        text5_1 = bcast6?.Text1,
                        BroadcastTextID5 = bcast6?.Id ?? 0,
                        lang5 = bcast6?.Language ?? 0,
                        Probability5 = npcText.Item2[5].probability,
                        EmoteDelay5_0 = bcast6?.EmoteDelay1 ?? 0,
                        Emote5_0 = bcast6?.EmoteId1 ?? 0,
                        EmoteDelay5_1 = bcast6?.EmoteDelay2 ?? 0,
                        Emote5_1 = bcast6?.EmoteId2 ?? 0,
                        EmoteDelay5_2 = bcast6?.EmoteDelay3 ?? 0,
                        Emote5_2 = bcast6?.EmoteId3 ?? 0,


                        text6_0 = bcast7?.Text,
                        text6_1 = bcast7?.Text1,
                        BroadcastTextID6 = bcast7?.Id ?? 0,
                        lang6 = bcast7?.Language ?? 0,
                        Probability6 = npcText.Item2[6].probability,
                        EmoteDelay6_0 = bcast7?.EmoteDelay1 ?? 0,
                        Emote6_0 = bcast7?.EmoteId1 ?? 0,
                        EmoteDelay6_1 = bcast7?.EmoteDelay2 ?? 0,
                        Emote6_1 = bcast7?.EmoteId2 ?? 0,
                        EmoteDelay6_2 = bcast7?.EmoteDelay3 ?? 0,
                        Emote6_2 = bcast7?.EmoteId3 ?? 0,

                        text7_0 = bcast8?.Text,
                        text7_1 = bcast8?.Text1,
                        BroadcastTextID7 = bcast8?.Id ?? 0,
                        lang7 = bcast8?.Language ?? 0,
                        Probability7 = npcText.Item2[7].probability,
                        EmoteDelay7_0 = bcast8?.EmoteDelay1 ?? 0,
                        Emote7_0 = bcast8?.EmoteId1 ?? 0,
                        EmoteDelay7_1 = bcast8?.EmoteDelay2 ?? 0,
                        Emote7_1 = bcast8?.EmoteId2 ?? 0,
                        EmoteDelay7_2 = bcast8?.EmoteDelay3 ?? 0,
                        Emote7_2 = bcast8?.EmoteId3 ?? 0
                    };
                });
                
                multiQuery.Table("npc_text")
                    .BulkInsert(inserts);
            
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

                multiQuery.Table("points_of_interest")
                    .WhereIn("ID", Enumerable.Range((int)freePoiNum, poiToAdd.Count))
                    .Delete();

                var id = freePoiNum;
                multiQuery.Table("points_of_interest")
                    .BulkInsert(poiToAdd.Select(option => new
                    {
                        ID = id++,
                        PositionX = option.ActionPoi!.Coordinates.X,
                        PositionY = option.ActionPoi.Coordinates.Y,
                        Icon = option.ActionPoi.Icon,
                        Flags = option.ActionPoi.Flags,
                        Importance = option.ActionPoi.Importance,
                        Name = option.ActionPoi.Name,
                        VerifiedBuild  = 0,
                    }));

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
                multiQuery.Table("gossip_menu")
                    .Where(row => row.Column<uint>("MenuID") == menu.Key)
                    .Delete();
            }

            multiQuery.Table("gossip_menu")
                .BulkInsert(menus
                    .Where(menu => menusAlreadyInDb.Contains(menu.Key) == existing)
                    .SelectMany(menu => menu.Value.TextsId.Select(text => new
                    {
                        MenuID = menu.Key,
                        TextID = text,
                        __comment = (string?)null
                    }).Concat(menu.Value.DatabaseOnlyTextsId.Select(dbText => new
                    {
                        MenuID = menu.Key,
                        TextID = dbText,
                        __comment = (string?)"not in sniff"
                    }))));

            if (any)
            {
                multiQuery.BlankLine();
                multiQuery.BlankLine();
            }
        }

        private static void DumpGossipMenuOptions(IMultiQuery multiQuery, KeyValuePair<uint, GossipMenu> menu)
        {
            multiQuery.Table("gossip_menu_option")
                .Where(row => row.Column<uint>("MenuID") == menu.Key)
                .Delete();
            var options = menu.Value.Options.Select(option => (option.Index, (object)new
            {
                MenuID = menu.Key,
                OptionID = option.Index,
                OptionIcon = (uint)option.Icon,
                OptionText = option.Text,
                OptionBroadcastTextID = option.BroadcastTextId,
                OptionType = (uint)option.OptionType,
                OptionNpcFlag = (uint)option.NpcFlags,
                ActionMenuID = option.LinkedMenuId,
                ActionPoiID = option.ActionPoiId,
                BoxCoded = option.BoxCoded ? 1 : 0,
                BoxMoney = option.BoxMoney,
                BoxText = option.BoxText,
                BoxBroadcastTextID = option.BoxTextBroadcastId,
                __comment = (string?)null,
                __ignored = false
            })).Concat(menu.Value.DatabaseOptions.Select(option =>
            {
                var isConflicting = menu.Value.Options.Any(o => o.Index == option.Index);
                return (option.Index, (object)new
                {
                    MenuID = menu.Key,
                    OptionID = option.Index,
                    OptionIcon = (uint)option.Icon,
                    OptionText = option.Text,
                    OptionBroadcastTextID = option.BroadcastTextId,
                    OptionType = (uint)option.OptionType,
                    OptionNpcFlag = (uint)option.NpcFlags,
                    ActionMenuID = option.LinkedMenuId,
                    ActionPoiID = option.ActionPoiId,
                    BoxCoded = option.BoxCoded ? 1 : 0,
                    BoxMoney = option.BoxMoney,
                    BoxText = option.BoxText,
                    BoxBroadcastTextID = option.BoxTextBroadcastId,
                    __comment = "not in sniff" + (isConflicting ? ", CONFLICTING ID" : ""),
                    __ignored = isConflicting
                });
            }));
            
            multiQuery.Table("gossip_menu_option")
                .BulkInsert(options
                    .OrderBy(o => o.Index)
                    .Select(o => o.Item2));
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
                        poi => poi.Icon == option.ActionPoi.Icon &&
                               poi.Flags == option.ActionPoi.Flags &&
                               Math.Abs(poi.PositionX - option.ActionPoi.Coordinates.X) < 1 &&
                               Math.Abs(poi.PositionY - option.ActionPoi.Coordinates.Y) < 1 &&
                               poi.Name == option.ActionPoi.Name);
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
                var template = databaseProvider.GetCreatureTemplate(entry);
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