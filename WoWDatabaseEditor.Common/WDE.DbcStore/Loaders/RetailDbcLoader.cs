using WDE.Common.DBC;
using WDE.Common.DBC.Structs;
using WDE.Common.Parameters;
using WDE.Common.Tasks;
using WDE.DbcStore.Providers;
using WDE.DbcStore.Structs;
using WDE.Module.Attributes;

namespace WDE.DbcStore.Loaders;

[AutoRegister]
internal class RetailDbcLoader : BaseDbcLoader
{
    public RetailDbcLoader(IDbcSettingsProvider dbcSettingsProvider,
        IDatabaseClientFileOpener opener,
        DBCD.DBCD dbcd) : base(dbcSettingsProvider, opener, dbcd)
    {
    }

    public override DBCVersions Version => DBCVersions.RETAIL;
    
    public override int StepsCount => 19;
    
    protected override void LoadDbcCore(DbcData data, ITaskProgress progress)
    {
        Load("AreaTrigger.db2", string.Empty, data.AreaTriggerStore);
        Load("SpellName.db2", "Name_lang", data.SpellStore);
        Load("Achievement.db2", "Title_lang", data.AchievementStore);
        LoadDB2("AreaTable.db2", row =>
        {
            var flags = row.FieldAs<int[]>("Flags");
            var entry = new AreaEntry()
            {
                Id = (uint)row.ID,
                MapId = row.FieldAs<ushort>("ContinentID"),
                ParentAreaId = row.FieldAs<ushort>("ParentAreaID"),
                Flags1 = (uint)flags[0],
                Flags2 = (uint)flags[1],
                Name = row.FieldAs<string>("AreaName_lang")
            };
            data.Areas.Add(entry);
        });
        LoadDB2("Map.db2", row =>
        {
            var map = new MapEntry()
            {
                Id = (uint)row.ID,
                Name = row.FieldAs<string>("MapName_lang"),
                Directory = row.FieldAs<string>("Directory"),
                Type = (InstanceType)row.FieldAs<sbyte>("InstanceType"),
            };
            data.Maps.Add(map);
        });
        FillMapAreas(data);
        Load("ChrClasses.db2", "Name_lang", data.ClassStore);
        Load("ChrRaces.db2", "Name_lang", data.RaceStore);
        LoadDB2("Emotes.db2", row =>
        {
            var proc = row.FieldAs<uint>("EmoteSpecProc");
            var name = row.FieldAs<string>("EmoteSlashCommand");
            if (proc == 0)
                data.EmoteOneShotStore.Add(row.ID, name);
            else if (proc == 2)
                data.EmoteStateStore.Add(row.ID, name);
            data.EmoteStore.Add(row.ID, name);
        });
        LoadDB2("ItemSparse.db2", row =>
        {

            var item = new ItemSparse()
            {
                Id = row.FieldAs<int>("ID"),
                Name = row.FieldAs<string>("Display_lang"),
                RandomSelect = 0,
                ItemRandomSuffixGroupId = 0,
            };
            data.ItemSparses.Add(item);
        });
        Load("EmotesText.db2", "Name", data.TextEmoteStore);
        Load("Languages.db2", "Name_lang", data.LanguageStore);
        LoadDB2("Faction.db2", row =>
        {
            var faction = new Faction((ushort)row.ID, row.FieldAs<string>("Name_lang"));
            data.Factions.Add(faction);
            data.FactionStore[faction.FactionId] = faction.Name;
        });
        LoadDB2("FactionTemplate.db2", row =>
        {
            var template = new FactionTemplate()
            {
                TemplateId = (ushort)row.ID,
                Faction = row.FieldAs<ushort>("Faction"),
                Flags = (ushort)row.FieldAs<int>("Flags"),
                FactionGroup = (FactionGroupMask)row.FieldAs<byte>("FactionGroup"),
                FriendGroup = (FactionGroupMask)row.FieldAs<byte>("FriendGroup"),
                EnemyGroup = (FactionGroupMask)row.FieldAs<byte>("EnemyGroup")
            };
            data.FactionTemplates.Add(template);
            data.FactionTemplateStore.Add(template.TemplateId, template.Faction);
        });
        Load("SceneScriptPackage.db2", "Name", data.SceneStore);
        Load("SpellFocusObject.db2", "Name_lang", data.SpellFocusObjectStore);
        Load("QuestInfo.db2", "InfoName_lang", data.QuestInfoStore);
        Load("CharTitles.db2", "Name_lang", data.CharTitleStore);
        Load("QuestSort.db2", "SortName_lang", data.QuestSortStore);
        Load("TaxiNodes.db2",  "Name_lang", data.TaxiNodeStore);
        LoadDB2("TaxiPath.db2",  row => data.TaxiPathsStore.Add(row.ID, (row.Field<int>("FromTaxiNode"), row.Field<int>("ToTaxiNode"))));
        LoadDB2("PhaseXPhaseGroup.db2", row => data.PhaseXPhaseGroup.Add(new (row.ID, row.Field<ushort>("PhaseID"), row.Field<int>("PhaseGroupID"))));
        LoadDB2("Phase.db2", row => data.PhaseStore[row.ID] = $"Phase {row.ID}");
        LoadDB2("Vehicle.db2", row =>
        {
            var veh = new Vehicle()
            {
                Id = (uint)row.ID,
            };
            var seats = row.Field<ushort[]>("SeatID");
            for (int i = 0; i < IVehicle.MaxSeats; ++i)
                veh.Seats[i] = seats[i];
            data.Vehicles.Add(veh);
        });
    }
}