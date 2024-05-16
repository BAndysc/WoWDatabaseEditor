using System;
using System.Collections.Generic;
using System.Text;
using WDE.Common.DBC;
using WDE.Common.DBC.Structs;
using WDE.Common.Parameters;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DbcStore.Providers;
using WDE.DbcStore.Spells.Legion;
using WDE.DbcStore.Structs;
using WDE.Module.Attributes;

namespace WDE.DbcStore.Loaders;

[AutoRegister]
internal class LegionDbcLoader : BaseDbcLoader
{
    public LegionDbcLoader(IDbcSettingsProvider dbcSettingsProvider, 
        IDatabaseClientFileOpener opener,
        DBCD.DBCD dbcd) : base(dbcSettingsProvider, opener, dbcd)
    {
    }

    public override DBCVersions Version => DBCVersions.LEGION_26972;

    public override int StepsCount => 42;

    protected override void LoadDbcCore(DbcData data, ITaskProgress progress)
    {
        Load("CriteriaTree.db2", 0, 1, data.AchievementCriteriaStore);
        Load("AreaTrigger.db2", row => data.AreaTriggerStore.Add(row.GetInt(14), $"Area trigger"));
        Load("AreaTable.db2", row =>
        {
            var entry = new AreaEntry()
            {
                Id = row.GetUInt(0),
                MapId = row.GetUInt(5),
                ParentAreaId = row.GetUInt(6),
                Flags1 = row.GetUInt(3, 0),
                Flags2 = row.GetUInt(3, 1),
                Name = row.GetString(2)
            };
            data.Areas.Add(entry);
        });

        Load("ConversationLine.db2", row =>
        {
            uint Id = row.GetUInt(0);
            uint BroadcastTextId = row.GetUInt(1);
            // var broadcastText = await database.GetBroadcastTextByIdAsync(BroadcastTextId); TODO
            data.ConversationLineStore.Add(Id, BroadcastTextId.ToString());
        });

        Load("Map.db2", row =>
        {
            var map = new MapEntry()
            {
                Id = row.GetUInt(0),
                Name = row.GetString(2),
                Directory = row.GetString(1),
                Type = (InstanceType)row.GetUInt(17),
            };
            data.Maps.Add(map);
        });
        FillMapAreas(data);
        LoadLegionAreaGroup(data, data.AreaGroupStore);
        Load("BattlemasterList.db2", 0, 1, data.BattlegroundStore);
        Load("CurrencyTypes.db2", 0, 1, data.CurrencyTypeStore);
        Load("DungeonEncounter.db2", 6, 0, data.DungeonEncounterStore);
        Load("Difficulty.db2", 0, 1, data.DifficultyStore);
        Dictionary<int, string> temporaryItemNames = new();
        Load("ItemSparse.db2", row =>
        {
            var item = new ItemSparse()
            {
                Id = row.GetInt(0),
                Name = row.GetString(2),
                RandomSelect = row.GetUShort(34),
                ItemRandomSuffixGroupId = row.GetUShort(35),
            };
            temporaryItemNames[item.Id] = item.Name;
            data.ItemSparses.Add(item);
        });
        Load("ItemExtendedCost.db2", row =>
        {
            var id = row.GetUInt(0);
            StringBuilder desc = new StringBuilder();
            for (int i = 0; i < 5; ++i)
            {
                var count = row.GetUInt(2, i);
                var currency = row.GetUInt(5, i);
                var item = row.GetInt(1, i);
                var itemsCount = row.GetUShort(3, i);

                if (currency != 0 && count != 0)
                {
                    if (data.CurrencyTypeStore.TryGetValue(currency, out var currencyName))
                        desc.Append($"{count} x {currencyName}, ");
                    else
                        desc.Append($"{count} x Currency {currency}, ");
                }
                if (itemsCount != 0 && item != 0)
                {
                    if (!temporaryItemNames.TryGetValue(item, out var itemName))
                        itemName = "item " + item;

                    if (itemsCount == 1)
                        desc.Append($"{itemName}, ");
                    else
                        desc.Append($"{itemsCount} x {itemName}, ");
                }
            }
            var arenaRating = row.GetUShort(4);
            if (arenaRating != 0)
            {
                desc.Append($"min arena rating {arenaRating}");
            }
            data.ExtendedCostStore.Add(id, desc.ToString());
        });
        Load("ItemRandomProperties.db2", 0, 1, data.ItemRandomPropertiesStore);
        Load("ItemRandomSuffix.db2", 0, 1, data.ItemRandomSuffixStore);
        Load("ItemSet.db2", 0, 1, data.ItemSetStore);
        Load("LFGDungeons.db2", 0, 1, data.LFGDungeonStore);
        Load("chrRaces.db2", 30, 2, data.RaceStore);
        Load("achievement.db2", row => data.AchievementStore.Add(row.GetInt(12), row.GetString(0)));
        Load("spell.db2", row => data.SpellStore.Add(row.Key, row.GetString(1)));
        Load("chrClasses.db2", 19, 1, data.ClassStore);

        Load("Emotes.db2", row =>
        {
            var id = row.Key;
            var name = row.GetString(2);
            var proc = row.GetInt(6);
            if (proc == 0)
                data.EmoteOneShotStore.Add(id, name);
            else if (proc == 2)
                data.EmoteStateStore.Add(id, name);
            data.EmoteStore.Add(id, name);
        });

        Load("EmotesText.db2", 0, 1, data.TextEmoteStore);
        Load("HolidayNames.db2", 0, 1, data.HolidayNamesStore);
        Load("Holidays.db2", row =>
        {
            var id = row.GetUInt(0);
            var nameId = row.GetUInt(9);
            if (data.HolidayNamesStore.TryGetValue(nameId, out var name))
                data.HolidaysStore[id] = name;
            else
                data.HolidaysStore[id] = "Holiday " + id;
        });
        Load("Languages.db2", 1, 0, data.LanguageStore);
        Load("MailTemplate.DB2", row =>
        {
            var body = row.GetString(1);
            var name = body.TrimToLength(50);
            data.MailTemplateStore.Add(row.GetUInt(0), name.Replace("\n", ""));
        });
        Load("Faction.db2", row =>
        {
            var faction = new Faction(row.GetUShort(3), row.GetString(1));
            data.Factions.Add(faction);
            data.FactionStore[faction.FactionId] = faction.Name;
        });
        Load("FactionTemplate.db2", row =>
        {
            var template = new FactionTemplate()
            {
                TemplateId = row.Key,
                Faction = row.GetUShort(1),
                Flags = row.GetUShort(2),
                FactionGroup = (FactionGroupMask)row.GetUShort(5),
                FriendGroup = (FactionGroupMask)row.GetUShort(6),
                EnemyGroup = (FactionGroupMask)row.GetUShort(7)
            };
            data.FactionTemplates.Add(template);
            data.FactionTemplateStore.Add(row.GetInt(0), row.GetUShort(1));
        });
        // Load("Phase.db2", 1, 0, PhaseStore); // no names in legion :(
        Load("SoundKitName.db2", 0, 1, data.SoundStore);
        Load("SpellFocusObject.db2", 0, 1, data.SpellFocusObjectStore);
        Load("QuestInfo.db2", 0, 1, data.QuestInfoStore);
        Load("QuestSort.db2", 0, 1, data.QuestSortStore);
        Load("CharTitles.db2", 0, 1, data.CharTitleStore);
        Load("SkillLine.db2", 0, 1, data.SkillStore);
        Load("LockType.db2", 4, 0, data.LockTypeStore);
        Load("CreatureDisplayInfo.db2", row => data.CreatureDisplayInfoStore.Add(row.GetInt(0), row.GetUShort(2)));
        LoadAndRegister(data, "SpellCastTimes.db2", "SpellCastTimeParameter", 0, row => GetCastTimeDescription(row.GetInt(1), row.GetInt(3), row.GetInt(2)));
        LoadAndRegister(data, "SpellDuration.db2", "SpellDurationParameter", 0, row => GetDurationTimeDescription(row.GetInt(1), row.GetInt(3), row.GetInt(2)));
        LoadAndRegister(data, "SpellRange.db2", "SpellRangeParameter", 0, row => GetRangeDescription(row.GetFloat(3, 0), row.GetFloat(4, 0), row.GetString(1), row.GetFloat(3, 1), row.GetFloat(3, 2)));
        LoadAndRegister(data, "SpellRadius.db2", "SpellRadiusParameter", 0, row => GetRadiusDescription(row.GetFloat(1), row.GetFloat(2), row.GetFloat(4)));
        Load("SpellItemEnchantment.db2", 0, 1, data.SpellItemEnchantmentStore);
        Load("TaxiNodes.db2", 0, 1, data.TaxiNodeStore);
        Load("TaxiPath.db2", row => data.TaxiPathsStore.Add(row.GetInt(2), (row.GetUShort(0), row.GetUShort(1))));
        Load("SceneScriptPackage.db2", 0, 1, data.SceneStore);
        Load("BattlePetSpecies.db2", 8, 2, data.BattlePetSpeciesIdStore);
        Load("BattlePetAbility.db2", 0, 1, data.BattlePetAbilityStore);
        Load("Scenario.db2", 0, 1, data.ScenarioStore);
        Load("Vignette.db2", 0, 1, data.VignetteStore);
        Load("GarrClassSpec.db2", 7, 0, data.GarrisonClassSpecStore);
        Load("GarrMission.db2", data.Missions, row =>
            new GarrMission()
            {
                Name = row.GetString(0),
                Description = row.GetString(1),
                Location = row.GetString(2),
                MissionDuration = row.GetInt(3),
                OfferDuration = row.GetUInt(4),
                MapPos0 = row.GetFloat(5, 0),
                MapPos1 = row.GetFloat(5, 1),
                WorldPos0 = row.GetFloat(6, 0),
                WorldPos1 = row.GetFloat(6, 1),
                TargetItemLevel = row.GetUShort(7),
                UiTextureKitId = row.GetUShort(8),
                MissionCostCurrencyTypeId = row.GetUShort(9),
                TargetLevel = (byte)row.GetUInt(10),
                EnvGarrMechanicTypeId = (sbyte)row.GetInt(11),
                MaxFollowers = (byte)row.GetUInt(12),
                OfferedGarrMissionTextureId = (byte)row.GetUInt(13),
                GarrMissionTypeId = (byte)row.GetUInt(14),
                GarrFollowerTypeId = (byte)row.GetUInt(15),
                BaseCompletionChance = (byte)row.GetUInt(16),
                FollowerDeathChance = (byte)row.GetUInt(17),
                GarrTypeId = (byte)row.GetUInt(18),
                Id = row.GetInt(19),
                TravelDuration = row.GetInt(20),
                PlayerConditionId = row.GetUInt(21),
                MissionCost = row.GetUInt(22),
                Flags = row.GetUInt(23),
                BaseFollowerXp = row.GetUInt(24),
                AreaId = row.GetUInt(25),
                OvermaxRewardPackId = row.GetUInt(26),
                EnvGarrMechanicId = row.GetUInt(27),
                GarrMissionSetId = row.GetUInt(28),
            });
        Load("ConversationLine.db2", data.ConversationLines, row =>
            new ConversationLine()
            {
                Id = row.GetUInt(0),
                BroadcastTextId = row.GetUInt(1),
                NextLineId = row.GetUInt(4)
            });
        Load("UiTextureKit.db2", data.UiTextureKits, row =>
            new UiTextureKit()
            {
                Id = (int)row.Key,
                KitPrefix = row.GetString(1)
            });
        Load("GarrMissionType.db2", data.GarrMissionTypes, row => 
            new GarrMissionType()
            {
                Id = (byte)row.Key,
                Name = row.GetString(1),
                UiTextureAtlasMemberId = row.GetInt(2),
                UiTextureKitId = row.GetInt(3)
            });
        Load("CurrencyCategory.db2", data.CurrencyCategories, row => 
            new CurrencyCategory()
            {
                Id = (int)row.Key,
                Name = row.GetString(1)
            });
        Load("CurrencyTypes.db2", data.CurrencyTypes, row => 
            new CurrencyType()
            {
                Id = row.GetUInt(0),
                Name = row.GetString(1),
                Description = row.GetString(2),
                MaxQuantity = row.GetUInt(3),
                MaxEarnablePerWeek = row.GetUInt(4),
                Flags = (CurrencyTypesFlags)row.GetUInt(5),
                CategoryId = (byte)row.GetUShort(6),
                SpellCategory = (byte)row.GetUShort(7),
                Quality = (byte)row.GetUShort(8),
                InventoryIconFileId = row.GetUInt(9),
                SpellWeight = row.GetUInt(10),
            });
        Load("GarrTalent.db2", 7, 0, data.GarrisonTalentStore);
        Load("WorldSafeLocs.db2", 0, 1, data.WorldSafeLocsStore);
        Load("GarrBuilding.db2", row =>
        {
            var id = row.Key;
            var allyName = row.GetString(1);
            var hordeName = row.GetString(2);
            if (allyName == hordeName)
                data.GarrisonBuildingStore[id] = allyName;
            else
                data.GarrisonBuildingStore[id] = $"{allyName} / {hordeName}";
        });
        Load("ScenarioStep.db2", row =>
        {
            var stepId = row.Key;
            var description = row.GetString(1);
            var name = row.GetString(2);
            var scenarioId = row.GetUInt(3);
            var stepIndex = row.GetUInt(6);
            data.ScenarioStepStore[stepId] = name;
            if (!data.ScenarioToStepStore.TryGetValue(scenarioId, out var scenarioSteps))
                scenarioSteps = data.ScenarioToStepStore[scenarioId] = new();
            scenarioSteps[stepIndex] = stepId;
        });
        Load("ChrSpecialization.db2", row =>
        {
            var specId = row.Key;
            var name = row.GetString(0);
            var classId = row.GetUInt(4);
            if (data.ClassStore.TryGetValue(classId, out var className))
                data.CharSpecializationStore.Add(specId, $"{className} - {name}");
            else
                data.CharSpecializationStore.Add(specId, $"{name}");
        });
        Load("AdventureJournal.db2", 0, 1, data.AdventureJournalStore);
        Load("WorldMapArea.db2", 15, 0, data.WorldMapAreaStore);
        Load("CharShipmentContainer.db2", row =>
        {
            var shipment = new CharShipmentContainerEntry()
            {
                Id = row.Key,
                Name = row.GetString(1),
                Description = row.GetString(2),
            };
            string name = shipment.Name;
            if (name == "")
                name = shipment.Description != "" ? shipment.Description : "- no name -";

            data.CharShipmentContainerStore[row.Key] = name;
            data.CharShipmentContainers.Add(shipment);
        });
        Load("CharShipment.db2", row =>
        {
            data.CharShipments.Add(new CharShipmentEntry()
            {
                Id = row.Key,
                TreasureId = row.GetUInt(1),
                SpellId = row.GetUInt(3),
                DummyItemId = row.GetUInt(4),
                OnCompleteSpellId = row.GetUInt(5),
                ContainerId = row.GetUInt(6),
                GarrisonFollowerId = row.GetUInt(7)
            });
        });
        Load("GarrFollower.db2", row =>
        {
            data.GarrisonFollowers.Add(new GarrisonFollowerEntry
            {
                Id = row.Key,
                HordeCreatureEntry = row.GetUInt(3),
                AllianceCreatureEntry = row.GetUInt(4)
            });
        });
        Load("ItemDisplayInfo.db2", row =>
        {
            data.ItemDisplayInfos.Add(new ItemDisplayInfoEntry()
            {
                Id = row.GetUInt(0)
            });
        });
        Load("Item.db2", row =>
        {
            data.Items.Add(new DbcItemEntry()
            {
                Id = row.GetUInt(0),
                InventoryIconFileDataId = row.GetUInt(1)
            });
        });
        Load("ItemAppearance.db2", row =>
        {
            data.ItemAppearances.Add(new ItemAppearanceEntry()
            {
                Id = row.GetUInt(0),
                InventoryIconFileDataId = row.GetUInt(2)
            });
        });
        Load("ItemModifiedAppearance.db2", row =>
        {
            data.ItemModifiedAppearances.Add(new ItemModifiedAppearanceEntry()
            {
                ItemId = row.GetUInt(0),
                ItemAppearanceId = row.GetUInt(3)
            });
        });
        Load("Vehicle.db2", data.Vehicles, row =>
        {
            var veh = new Vehicle()
            {
                Id = (uint)row.Key,
            };
            for (int i = 0; i < IVehicle.MaxSeats; ++i)
                veh.Seats[i] = row.GetUShort(13, i);
            return veh;
        });
        Load("PlayerCondition.db2", row =>
        {
            data.PlayerConditions.Add(new LegionPlayerConditionEntry()
            {
                RaceMask = row.GetLong(0),
                Failure_description_lang = row.GetString(1),
                Id = row.GetInt(2),
                Flags = row.GetByte(3),
                MinLevel = row.GetUShort(4),
                MaxLevel = row.GetUShort(5),
                ClassMask = row.GetInt(6),
                Gender = row.GetSbyte(7),
                NativeGender = row.GetSbyte(8),
                SkillLogic = row.GetUInt(9),
                LanguageID = row.GetByte(10),
                MinLanguage = row.GetByte(11),
                MaxLanguage = row.GetInt(12),
                MaxFactionID = row.GetUShort(13),
                MaxReputation = row.GetByte(14),
                ReputationLogic = row.GetUInt(15),
                CurrentPvpFaction = row.GetSbyte(16),
                MinPVPRank = row.GetByte(17),
                MaxPVPRank = row.GetByte(18),
                PvpMedal = row.GetByte(19),
                PrevQuestLogic = row.GetUInt(20),
                CurrQuestLogic = row.GetUInt(21),
                CurrentCompletedQuestLogic = row.GetUInt(22),
                SpellLogic = row.GetUInt(23),
                ItemLogic = row.GetUInt(24),
                ItemFlags = row.GetByte(25),
                AuraSpellLogic = row.GetUInt(26),
                WorldStateExpressionID = row.GetUShort(27),
                WeatherID = row.GetByte(28),
                PartyStatus = row.GetByte(29),
                LifetimeMaxPVPRank = row.GetByte(30),
                AchievementLogic = row.GetUInt(31),
                LfgLogic = row.GetUInt(32),
                AreaLogic = row.GetUInt(33),
                CurrencyLogic = row.GetUInt(34),
                QuestKillID = row.GetUShort(35),
                QuestKillLogic = row.GetUInt(36),
                MinExpansionLevel = row.GetSbyte(37),
                MaxExpansionLevel = row.GetSbyte(38),
                MinExpansionTier = row.GetSbyte(39),
                MaxExpansionTier = row.GetSbyte(40),
                MinGuildLevel = row.GetByte(41),
                MaxGuildLevel = row.GetByte(42),
                PhaseUseFlags = row.GetByte(43),
                PhaseID = row.GetUShort(44),
                PhaseGroupID = row.GetUInt(45),
                MinAvgItemLevel = row.GetInt(46),
                MaxAvgItemLevel = row.GetInt(47),
                MinAvgEquippedItemLevel = row.GetUShort(48),
                MaxAvgEquippedItemLevel = row.GetUShort(49),
                ChrSpecializationIndex = row.GetSbyte(50),
                ChrSpecializationRole = row.GetSbyte(51),
                PowerType = row.GetSbyte(52),
                PowerTypeComp = row.GetByte(53),
                PowerTypeValue = row.GetByte(54),
                ModifierTreeID = row.GetUInt(55),
                WeaponSubclassMask = row.GetInt(56),
                SkillID0 = row.GetUShort(57, 0),
                SkillID1 = row.GetUShort(57, 1),
                SkillID2 = row.GetUShort(57, 2),
                SkillID3 = row.GetUShort(57, 3),
                MinSkill0 = row.GetUShort(58, 0),
                MinSkill1 = row.GetUShort(58, 1),
                MinSkill2 = row.GetUShort(58, 2),
                MinSkill3 = row.GetUShort(58, 3),
                MaxSkill0 = row.GetUShort(59, 0),
                MaxSkill1 = row.GetUShort(59, 1),
                MaxSkill2 = row.GetUShort(59, 2),
                MaxSkill3 = row.GetUShort(59, 3),
                MinFactionID0 = row.GetUInt(60, 0),
                MinFactionID1 = row.GetUInt(60, 1),
                MinFactionID2 = row.GetUInt(60, 2),
                MinReputation0 = row.GetByte(61, 0),
                MinReputation1 = row.GetByte(61, 1),
                MinReputation2 = row.GetByte(61, 2),
                PrevQuestID0 = row.GetUShort(62, 0),
                PrevQuestID1 = row.GetUShort(62, 1),
                PrevQuestID2 = row.GetUShort(62, 2),
                PrevQuestID3 = row.GetUShort(62, 3),
                CurrQuestID0 = row.GetUShort(63, 0),
                CurrQuestID1 = row.GetUShort(63, 1),
                CurrQuestID2 = row.GetUShort(63, 2),
                CurrQuestID3 = row.GetUShort(63, 3),
                CurrentCompletedQuestID0 = row.GetUShort(64, 0),
                CurrentCompletedQuestID1 = row.GetUShort(64, 1),
                CurrentCompletedQuestID2 = row.GetUShort(64, 2),
                CurrentCompletedQuestID3 = row.GetUShort(64, 3),
                SpellID0 = row.GetInt(65, 0),
                SpellID1 = row.GetInt(65, 1),
                SpellID2 = row.GetInt(65, 2),
                SpellID3 = row.GetInt(65, 3),
                ItemID0 = row.GetInt(66, 0),
                ItemID1 = row.GetInt(66, 1),
                ItemID2 = row.GetInt(66, 2),
                ItemID3 = row.GetInt(66, 3),
                ItemCount0 = row.GetUInt(67, 0),
                ItemCount1 = row.GetUInt(67, 1),
                ItemCount2 = row.GetUInt(67, 2),
                ItemCount3 = row.GetUInt(67, 3),
                Explored0 = row.GetUShort(68, 0),
                Explored1 = row.GetUShort(68, 1),
                Time0 = row.GetUInt(69, 0),
                Time1 = row.GetUInt(69, 1),
                AuraSpellID0 = row.GetInt(70, 0),
                AuraSpellID1 = row.GetInt(70, 1),
                AuraSpellID2 = row.GetInt(70, 2),
                AuraSpellID3 = row.GetInt(70, 3),
                AuraStacks0 = row.GetByte(71, 0),
                AuraStacks1 = row.GetByte(71, 1),
                AuraStacks2 = row.GetByte(71, 2),
                AuraStacks3 = row.GetByte(71, 3),
                Achievement0 = row.GetUShort(72, 0),
                Achievement1 = row.GetUShort(72, 1),
                Achievement2 = row.GetUShort(72, 2),
                Achievement3 = row.GetUShort(72, 3),
                LfgStatus0 = row.GetByte(73, 0),
                LfgStatus1 = row.GetByte(73, 1),
                LfgStatus2 = row.GetByte(73, 2),
                LfgStatus3 = row.GetByte(73, 3),
                LfgCompare0 = row.GetByte(74, 0),
                LfgCompare1 = row.GetByte(74, 1),
                LfgCompare2 = row.GetByte(74, 2),
                LfgCompare3 = row.GetByte(74, 3),
                LfgValue0 = row.GetUInt(75, 0),
                LfgValue1 = row.GetUInt(75, 1),
                LfgValue2 = row.GetUInt(75, 2),
                LfgValue3 = row.GetUInt(75, 3),
                AreaID0 = row.GetUShort(76, 0),
                AreaID1 = row.GetUShort(76, 1),
                AreaID2 = row.GetUShort(76, 2),
                AreaID3 = row.GetUShort(76, 3),
                CurrencyID0 = row.GetUInt(77, 0),
                CurrencyID1 = row.GetUInt(77, 1),
                CurrencyID2 = row.GetUInt(77, 2),
                CurrencyID3 = row.GetUInt(77, 3),
                CurrencyCount0 = row.GetUInt(78, 0),
                CurrencyCount1 = row.GetUInt(78, 1),
                CurrencyCount2 = row.GetUInt(78, 2),
                CurrencyCount3 = row.GetUInt(78, 3),
                QuestKillMonster0 = row.GetUInt(79, 0),
                QuestKillMonster1 = row.GetUInt(79, 1),
                QuestKillMonster2 = row.GetUInt(79, 2),
                QuestKillMonster3 = row.GetUInt(79, 3),
                QuestKillMonster4 = row.GetUInt(79, 4),
                QuestKillMonster5 = row.GetUInt(79, 5),
                MovementFlags0 = row.GetInt(80, 0),
                MovementFlags1 = row.GetInt(80, 1),
            });
        });
    }
}