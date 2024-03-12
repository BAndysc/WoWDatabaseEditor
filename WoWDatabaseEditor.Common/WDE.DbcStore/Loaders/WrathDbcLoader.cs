using WDE.Common.DBC;
using WDE.Common.DBC.Structs;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DbcStore.Providers;
using WDE.DbcStore.Spells.Wrath;
using WDE.DbcStore.Structs;
using WDE.Module.Attributes;

namespace WDE.DbcStore.Loaders;

[AutoRegister]
internal class WrathDbcLoader : BaseDbcLoader
{
    public WrathDbcLoader(IDbcSettingsProvider dbcSettingsProvider, 
        IDatabaseClientFileOpener opener,
        DBCD.DBCD dbcd) : base(dbcSettingsProvider, opener, dbcd)
    {
    }
    
    public override DBCVersions Version => DBCVersions.WOTLK_12340;
    
    public override int StepsCount => 43;

    protected override void LoadDbcCore(DbcData data, ITaskProgress progress)
    {
        Load("AreaTrigger.dbc", row => data.AreaTriggerStore.Add(row.GetInt(0), $"Area trigger"));
        Load("SkillLine.dbc", 0, 3, data.SkillStore, true);
        Load("Faction.dbc", row =>
        {
            var faction = new Faction(row.GetUShort(0), row.GetString(23 + LocaleOffset));
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
        Load("Spell.dbc", 0, 136, data.SpellStore, true);
        Load("Movie.dbc", 0, 1, data.MovieStore);
        Load("Map.dbc", row =>
        {
            var map = new MapEntry()
            {
                Id = row.GetUInt(0),
                Name = row.GetString(5 + LocaleOffset),
                Directory = row.GetString(1),
                Type = (InstanceType)row.GetUInt(2),
            };
            data.Maps.Add(map);
        });
        Load("Achievement.dbc", 0, 4, data.AchievementStore, true);
        Load("AreaTable.dbc", row =>
        {
            var entry = new AreaEntry()
            {
                Id = row.GetUInt(0),
                MapId = row.GetUInt(1),
                ParentAreaId = row.GetUInt(2),
                Flags1 = row.GetUInt(4),
                Name = row.GetString(11 + LocaleOffset)
            };
            data.Areas.Add(entry);
        });
        FillMapAreas(data);
        Load("chrClasses.dbc", 0, 4, data.ClassStore, true);
        Load("chrRaces.dbc", 0, 14, data.RaceStore, true);
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
        Load("SoundEntries.dbc", 0, 2, data.SoundStore);
        Load("SpellFocusObject.dbc", 0, 1, data.SpellFocusObjectStore, true);
        Load("QuestInfo.dbc", 0, 1, data.QuestInfoStore, true);
        Load("CharTitles.dbc", 0, 2, data.CharTitleStore, true);
        Load("CreatureModelData.dbc", 0, 2, data.CreatureModelDataStore);
        Load("CreatureDisplayInfo.dbc", 0, 1, data.CreatureDisplayInfoStore);
        Load("GameObjectDisplayInfo.dbc", 0, 1, data.GameObjectDisplayInfoStore);
        Load("Languages.dbc", 0, 1, data.LanguageStore, true);
        Load("QuestSort.dbc", 0, 1, data.QuestSortStore, true);
        Load("ItemExtendedCost.dbc", row => data.ExtendedCostStore.Add(row.GetInt(0), GenerateCostDescription(row.GetInt(1), row.GetInt(2), row.GetInt(4))));
        Load("TaxiNodes.dbc", 0, 5, data.TaxiNodeStore, true);
        Load("TaxiPath.dbc",  row => data.TaxiPathsStore.Add(row.GetUInt(0), (row.GetInt(1), row.GetInt(2))));
        Load("SpellItemEnchantment.dbc", 0, 14, data.SpellItemEnchantmentStore, true);
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
            var subject = row.GetString(1 + LocaleOffset);
            var body = row.GetString(18 + LocaleOffset);
            var name = string.IsNullOrEmpty(subject) ? body.TrimToLength(50) : subject;
            data.MailTemplateStore.Add(row.GetUInt(0), name.Replace("\n", ""));
        });
        Load("LFGDungeons.dbc", 0, 1, data.LFGDungeonStore, true);
        Load("ItemSet.dbc", 0, 1, data.ItemSetStore, true);
        Load("DungeonEncounter.dbc", 0, 5, data.DungeonEncounterStore, true);
        Load("HolidayNames.dbc", 0, 1, data.HolidayNamesStore, true);
        Load("Holidays.dbc", row =>
        {
            var id = row.GetUInt(0);
            var nameId = row.GetUInt(49);
            if (data.HolidayNamesStore.TryGetValue(nameId, out var name))
                data.HolidaysStore[id] = name;
            else
                data.HolidaysStore[id] = "Holiday " + id;
        });
        Load("WorldSafeLocs.dbc", 0, 5, data.WorldSafeLocsStore, true);
        Load("BattlemasterList.dbc", 0, 11, data.BattlegroundStore, true);
        Load("Achievement_Criteria.dbc", 0, 9, data.AchievementCriteriaStore, true);
        Load("Item.dbc", row =>
        {
            data.Items.Add(new DbcItemEntry()
            {
                Id = row.GetUInt(0),
                DisplayInfoId = row.GetUInt(5)
            });
        });
        Load("LockType.dbc", 0, 1, data.LockTypeStore, true);
        LoadAndRegister(data, "SpellCastTimes.dbc", "SpellCastTimeParameter", 0, row => GetCastTimeDescription(row.GetInt(1), row.GetInt(2), row.GetInt(3)));
        LoadAndRegister(data, "SpellDuration.dbc", "SpellDurationParameter", 0, row => GetDurationTimeDescription(row.GetInt(1), row.GetInt(2), row.GetInt(3)));
        LoadAndRegister(data, "SpellRange.dbc", "SpellRangeParameter", 0, row => GetRangeDescription(row.GetFloat(1), row.GetFloat(3), row.GetString(6 + localeIndex), row.GetFloat(2), row.GetFloat(4)));
        LoadAndRegister(data, "SpellRadius.dbc", "SpellRadiusParameter", 0, row => GetRadiusDescription(row.GetFloat(1), row.GetFloat(2), row.GetFloat(3)));
        Load("Vehicle.dbc", data.Vehicles, row =>
        {
            var veh = new Vehicle()
            {
                Id = (uint)row.GetUInt(0),
            };
            for (int i = 0; i < IVehicle.MaxSeats; ++i)
                veh.Seats[i] = row.GetUShort(6 + i);
            return veh;
        });
    }
}