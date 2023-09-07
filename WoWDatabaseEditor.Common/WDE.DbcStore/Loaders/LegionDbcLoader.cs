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
        Load("ItemSparse.db2", 0, 2, data.ItemStore);
        Load("ItemExtendedCost.db2", row =>
        {
            var id = row.GetUInt(0);
            StringBuilder desc = new StringBuilder();
            for (int i = 0; i < 5; ++i)
            {
                var count = row.GetUInt(2, i);
                var currency = row.GetUInt(5, i);
                var item = row.GetUInt(1, i);
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
                    if (!data.ItemStore.TryGetValue(item, out var itemName))
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
        LoadAndRegister(data, "SpellRange.db2", "SpellRangeParameter", 0, 1);
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
        Load("GarrMission.db2", 19, 0, data.GarrisonMissionStore);
        Load("GarrTalent.db2", 7, 0, data.GarrisonTalentStore);
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
    }
}