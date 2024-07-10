using System.Collections.Generic;
using WDE.Common.DBC;
using WDE.Common.DBC.Structs;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DbcStore.Providers;
using WDE.DbcStore.Spells.Cataclysm;
using WDE.DbcStore.Structs;
using WDE.Module.Attributes;

namespace WDE.DbcStore.Loaders;

[AutoRegister]
internal class MopDbcLoader : BaseDbcLoader
{
    public MopDbcLoader(IDbcSettingsProvider dbcSettingsProvider, 
        IDatabaseClientFileOpener opener,
        DBCD.DBCD dbcd) : base(dbcSettingsProvider, opener, dbcd)
    {
    }

    public override DBCVersions Version => DBCVersions.MOP_18414;

    public override int StepsCount => 49;
    
    protected override void LoadDbcCore(DbcData data, ITaskProgress progress)
    {
        var fileData = new Dictionary<long, string>();
        Load("Achievement_Criteria.dbc", 0, 10, data.AchievementCriteriaStore);
        Load("FileData.dbc", 0, 1, fileData);
        Load("AreaTrigger.dbc", row => data.AreaTriggerStore.Add(row.GetInt(0), $"Area trigger"));
        Load("BattlemasterList.dbc", 0, 19, data.BattlegroundStore);
        Load("SkillLine.dbc", 0, 2, data.SkillStore);
        Load("Faction.dbc", row =>
        {
            var faction = new Faction(row.GetUShort(0), row.GetString(23));
            data.Factions.Add(faction);
            data.FactionStore[faction.FactionId] = faction.Name;
        });
        Load("FactionTemplate.dbc", row =>
        {
            var template = new FactionTemplate()
            {
                TemplateId = row.GetUInt(0),
                Faction = row.GetUShort(1),
                Flags = row.GetUShort(2),
                FactionGroup = (FactionGroupMask)row.GetUShort(3),
                FriendGroup = (FactionGroupMask)row.GetUShort(4),
                EnemyGroup = (FactionGroupMask)row.GetUShort(5)
            };
            data.FactionTemplates.Add(template);
            data.FactionTemplateStore[row.GetUInt(0)] = row.GetUInt(1);
        });
        Load("CurrencyTypes.dbc", 0, 2, data.CurrencyTypeStore);
        Load("CurrencyTypes.dbc", data.CurrencyTypes, row => 
            new CurrencyType()
            {
                Id = row.GetUInt(0),
                CategoryId = (byte)row.GetUShort(1),
                Name = row.GetString(2),
                InventoryIconPath = row.GetString(3),
                SpellWeight = row.GetUInt(5),
                SpellCategory = (byte)row.GetUShort(6),
                MaxQuantity = row.GetUInt(7),
                MaxEarnablePerWeek = row.GetUInt(8),
                Flags = (CurrencyTypesFlags)row.GetUInt(9),
                Quality = (byte)row.GetUShort(10),
                Description = row.GetString(11)
            });
        Load("Spell.dbc", 0, 1, data.SpellStore);
        Load("Movie.dbc", row => data.MovieStore.Add(row.GetInt(0), fileData.GetValueOrDefault(row.GetInt(3)) ?? "Unknown movie"));
        Load("Map.dbc", row =>
        {
            var map = new MapEntry()
            {
                Id = row.GetUInt(0),
                Name = row.GetString(5),
                Directory = row.GetString(1),
                Type = (InstanceType)row.GetUInt(2),
            };
            data.Maps.Add(map);
        });
        Load("Achievement.dbc", 0, 4, data.AchievementStore);
        Load("AreaTable.dbc", row =>
        {
            var entry = new AreaEntry()
            {
                Id = row.GetUInt(0),
                MapId = row.GetUInt(1),
                ParentAreaId = row.GetUInt(2),
                Flags1 = row.GetUInt(4),
                Flags2 = row.GetUInt(5),
                Name = row.GetString(13)
            };
            data.Areas.Add(entry);
        });
        FillMapAreas(data);
        Load("ChrClasses.dbc", 0, 3, data.ClassStore);
        Load("ChrRaces.dbc", 0, 14, data.RaceStore);
        Load("Difficulty.dbc", 0, 11, data.DifficultyStore);
        Load("Emotes.dbc", row =>
        {
            var proc = row.GetUInt(4);
            if (proc == 0)
                data.EmoteOneShotStore.Add(row.GetUInt(0), row.GetString(1));
            else if (proc == 2)
                data.EmoteStateStore.Add(row.GetUInt(0), row.GetString(1));
            data.EmoteStore.Add(row.GetUInt(0), row.GetString(1));
        });
        Load("EmotesText.dbc", 0, 1, data.TextEmoteStore);
        Load("Item-sparse.db2", row =>
        {
            var item = new ItemSparse()
            {
                Id = row.GetInt(0),
                Name = row.GetString(100),
                RandomSelect = row.GetUShort(112),
                ItemRandomSuffixGroupId = row.GetUShort(113),
            };
            data.ItemSparses.Add(item);
        });
        Load("ItemRandomProperties.dbc", 0, 7, data.ItemRandomPropertiesStore);
        Load("ItemRandomSuffix.dbc", 0, 1, data.ItemRandomSuffixStore);
        Load("Phase.dbc", 0, 1, data.PhaseStore);
        Load("SoundEntries.dbc", 0, 2, data.SoundStore);
        Load("SpellFocusObject.dbc", 0, 1, data.SpellFocusObjectStore);
        Load("QuestInfo.dbc", 0, 1, data.QuestInfoStore);
        Load("CharTitles.dbc", 0, 2, data.CharTitleStore);
        Load("CreatureModelData.dbc", 0, 2, data.CreatureModelDataStore);
        Load("CreatureDisplayInfo.dbc", 0, 1, data.CreatureDisplayInfoStore);
        Load("GameObjectDisplayInfo.dbc", 0, 1, data.GameObjectDisplayInfoStore);
        Load("Languages.dbc", 0, 1, data.LanguageStore);
        Load("QuestSort.dbc", 0, 1, data.QuestSortStore);
        Load("ItemExtendedCost.dbc", row => data.ExtendedCostStore.Add(row.GetInt(0), GenerateCostDescription(row.GetInt(1), row.GetInt(2), row.GetInt(4))));
        Load("TaxiNodes.dbc", 0, 5, data.TaxiNodeStore);
        Load("TaxiPath.dbc",  row => data.TaxiPathsStore.Add(row.GetUInt(0), (row.GetInt(1), row.GetInt(2))));
        Load("SpellItemEnchantment.dbc", 0, 11, data.SpellItemEnchantmentStore);
        Load("AreaGroup.dbc",  row => data.AreaGroupStore.Add(row.GetUInt(0), BuildAreaGroupName(data, row, 1, 6)));
        Load("ItemDisplayInfo.dbc", row =>
        {
            data.ItemDisplayInfos.Add(new ItemDisplayInfoEntry()
            {
                Id = row.GetUInt(0),
                InventoryIconPath = row.GetString(5)
            });
        });
        Load("MailTemplate.dbc", row =>
        {
            var subject = row.GetString(1);
            var body = row.GetString(2);
            var name = string.IsNullOrEmpty(subject) ? body.TrimToLength(50) : subject;
            data.MailTemplateStore.Add(row.GetUInt(0), name.Replace("\n", ""));
        });
        Load("LFGDungeons.dbc", 0, 1, data.LFGDungeonStore);
        Load("ItemSet.dbc", 0, 1, data.ItemSetStore);
        Load("DungeonEncounter.dbc", 0, 5, data.DungeonEncounterStore);
        Load("HolidayNames.dbc", 0, 1, data.HolidayNamesStore);
        Load("Holidays.dbc", row =>
        {
            var id = row.GetUInt(0);
            var nameId = row.GetUInt(49);
            if (data.HolidayNamesStore.TryGetValue(nameId, out var name))
                data.HolidaysStore[id] = name;
            else
                data.HolidaysStore[id] = "Holiday " + id;
        });
        Load("WorldSafeLocs.dbc", 0, 6, data.WorldSafeLocsStore);
        Load("Item.dbc", row =>
        {
            data.Items.Add(new DbcItemEntry()
            {
                Id = row.GetUInt(0),
                DisplayInfoId = row.GetUInt(5)
            });
        });
        Load("LockType.dbc", 0, 1, data.LockTypeStore);
        Load("Vignette.dbc", 0, 1, data.VignetteStore);
        LoadAndRegister(data,"SpellCastTimes.dbc", "SpellCastTimeParameter", 0, row => GetCastTimeDescription(row.GetInt(1), row.GetInt(2), row.GetInt(3)));
        LoadAndRegister(data,"SpellDuration.dbc", "SpellDurationParameter", 0, row => GetDurationTimeDescription(row.GetInt(1), row.GetInt(2), row.GetInt(3)));
        LoadAndRegister(data, "SpellRange.dbc", "SpellRangeParameter", 0, row => GetRangeDescription(row.GetFloat(1), row.GetFloat(3), row.GetString(6), row.GetFloat(2), row.GetFloat(4)));
        LoadAndRegister(data,"SpellRadius.dbc", "SpellRadiusParameter", 0, row => GetRadiusDescription(row.GetFloat(1), row.GetFloat(2), row.GetFloat(4)));
        Load("Vehicle.dbc", data.Vehicles, row =>
        {
            var veh = new Vehicle()
            {
                Id = (uint)row.GetUInt(0),
            };
            for (int i = 0; i < IVehicle.MaxSeats; ++i)
                veh.Seats[i] = row.GetUShort(7 + i);
            return veh;
        });
        Load("PlayerCondition.dbc", row =>
        {
            data.PlayerConditions.Add(new MoPPlayerconditionEntry()
            {
                Id = row.GetInt(0),
                Flags = row.GetByte(1),
                MinLevel = row.GetUShort(2),
                MaxLevel = row.GetUShort(3),
                RaceMask = row.GetLong(4),
                ClassMask = row.GetInt(5),
                Gender = row.GetSbyte(6),
                NativeGender = row.GetSbyte(7),
                SkillID0 = row.GetUShort(8),
                SkillID1 = row.GetUShort(9),
                SkillID2 = row.GetUShort(10),
                SkillID3 = row.GetUShort(11),
                MinSkill0 = row.GetUShort(12),
                MinSkill1 = row.GetUShort(13),
                MinSkill2 = row.GetUShort(14),
                MinSkill3 = row.GetUShort(15),
                MaxSkill0 = row.GetUShort(16),
                MaxSkill1 = row.GetUShort(17),
                MaxSkill2 = row.GetUShort(18),
                MaxSkill3 = row.GetUShort(19),
                SkillLogic = row.GetUInt(20),
                LanguageID = row.GetByte(21),
                MinLanguage = row.GetByte(22),
                MaxLanguage = row.GetInt(23),
                MinFactionID0 = row.GetUInt(24),
                MinFactionID1 = row.GetUInt(25),
                MinFactionID2 = row.GetUInt(26),
                MaxFactionID = row.GetUShort(27),
                MinReputation0 = row.GetByte(28),
                MinReputation1 = row.GetByte(29),
                MinReputation2 = row.GetByte(30),
                MaxReputation = row.GetByte(31),
                ReputationLogic = row.GetUInt(32),
                MinPVPRank = row.GetByte(33),
                MaxPVPRank = row.GetByte(34),
                PvpMedal = row.GetByte(35),
                PrevQuestLogic = row.GetUInt(36),
                PrevQuestID0 = row.GetUShort(37),
                PrevQuestID1 = row.GetUShort(38),
                PrevQuestID2 = row.GetUShort(39),
                PrevQuestID3 = row.GetUShort(40),
                CurrQuestLogic = row.GetUInt(41),
                CurrQuestID0 = row.GetUShort(42),
                CurrQuestID1 = row.GetUShort(43),
                CurrQuestID2 = row.GetUShort(44),
                CurrQuestID3 = row.GetUShort(45),
                CurrentCompletedQuestLogic = row.GetUInt(46),
                CurrentCompletedQuestID0 = row.GetUShort(47),
                CurrentCompletedQuestID1 = row.GetUShort(48),
                CurrentCompletedQuestID2 = row.GetUShort(49),
                CurrentCompletedQuestID3 = row.GetUShort(50),
                SpellLogic = row.GetUInt(51),
                SpellID0 = row.GetInt(52),
                SpellID1 = row.GetInt(53),
                SpellID2 = row.GetInt(54),
                SpellID3 = row.GetInt(55),
                ItemLogic = row.GetUInt(56),
                ItemID0 = row.GetInt(57),
                ItemID1 = row.GetInt(58),
                ItemID2 = row.GetInt(59),
                ItemID3 = row.GetInt(60),
                ItemCount0 = row.GetUInt(61),
                ItemCount1 = row.GetUInt(62),
                ItemCount2 = row.GetUInt(63),
                ItemCount3 = row.GetUInt(64),
                ItemFlags = row.GetByte(65),
                Explored0 = row.GetUShort(66),
                Explored1 = row.GetUShort(67),
                Time0 = row.GetUInt(68),
                Time1 = row.GetUInt(69),
                AuraSpellLogic = row.GetUInt(70),
                AuraSpellID0 = row.GetInt(71),
                AuraSpellID1 = row.GetInt(72),
                AuraSpellID2 = row.GetInt(73),
                AuraSpellID3 = row.GetInt(74),
                WorldStateExpressionID = row.GetUShort(75),
                WeatherID = row.GetByte(76),
                PartyStatus = row.GetByte(77),
                LifetimeMaxPVPRank = row.GetByte(78),
                AchievementLogic = row.GetUInt(79),
                Achievement0 = row.GetUShort(80),
                Achievement1 = row.GetUShort(81),
                Achievement2 = row.GetUShort(82),
                Achievement3 = row.GetUShort(83),
                LfgLogic = row.GetUInt(84),
                LfgStatus0 = row.GetByte(85),
                LfgStatus1 = row.GetByte(86),
                LfgStatus2 = row.GetByte(87),
                LfgStatus3 = row.GetByte(88),
                LfgCompare0 = row.GetByte(89),
                LfgCompare1 = row.GetByte(90),
                LfgCompare2 = row.GetByte(91),
                LfgCompare3 = row.GetByte(92),
                LfgValue0 = row.GetUInt(93),
                LfgValue1 = row.GetUInt(94),
                LfgValue2 = row.GetUInt(95),
                LfgValue3 = row.GetUInt(96),
                AreaLogic = row.GetUInt(97),
                AreaID0 = row.GetUShort(98),
                AreaID1 = row.GetUShort(99),
                AreaID2 = row.GetUShort(100),
                AreaID3 = row.GetUShort(101),
                CurrencyLogic = row.GetUInt(102),
                CurrencyID0 = row.GetUInt(103),
                CurrencyID1 = row.GetUInt(104),
                CurrencyID2 = row.GetUInt(105),
                CurrencyID3 = row.GetUInt(106),
                CurrencyCount0 = row.GetUInt(107),
                CurrencyCount1 = row.GetUInt(108),
                CurrencyCount2 = row.GetUInt(109),
                CurrencyCount3 = row.GetUInt(110),
                QuestKillID = row.GetUShort(111),
                QuestKillLogic = row.GetUInt(112),
                QuestKillMonster0 = row.GetUInt(113),
                QuestKillMonster1 = row.GetUInt(114),
                QuestKillMonster2 = row.GetUInt(115),
                QuestKillMonster3 = row.GetUInt(116),
                MinExpansionLevel = row.GetSbyte(117),
                MaxExpansionLevel = row.GetSbyte(118),
                MinExpansionTier = row.GetSbyte(119),
                MaxExpansionTier = row.GetSbyte(120),
                MinGuildLevel = row.GetByte(121),
                MaxGuildLevel = row.GetByte(122),
                PhaseUseFlags = row.GetByte(123),
                PhaseID = row.GetUShort(124),
                PhaseGroupID = row.GetUInt(125),
                MinAvgItemLevel = row.GetInt(126),
                MaxAvgItemLevel = row.GetInt(127),
                MinAvgEquippedItemLevel = row.GetUShort(128),
                MaxAvgEquippedItemLevel = row.GetUShort(129),
                ChrSpecializationIndex = row.GetSbyte(130),
                ChrSpecializationRole = row.GetSbyte(131),
                Failure_description_lang = row.GetString(132),
                PowerType = row.GetSbyte(133),
                PowerTypeComp = row.GetByte(134),
                PowerTypeValue = row.GetByte(135),
            });
        });
    }
}