using WDE.Common.DBC;
using WDE.Common.Parameters;
using WDE.Common.Tasks;
using WDE.DbcStore.Providers;
using WDE.DbcStore.Structs;
using WDE.Module.Attributes;

namespace WDE.DbcStore.Loaders;

[AutoRegister]
internal class ShadowlandsDbcLoader : BaseDbcLoader
{
    public ShadowlandsDbcLoader(IDbcSettingsProvider dbcSettingsProvider, 
        IDatabaseClientFileOpener opener,
        DBCD.DBCD dbcd) : base(dbcSettingsProvider, opener, dbcd)
    {
    }

    public override DBCVersions Version => DBCVersions.SHADOWLANDS_41079;
    
    public override int StepsCount => 19;
    
    protected override void LoadDbcCore(DbcData data, ITaskProgress progress)
    {
        Load("AreaTrigger.db2", string.Empty, data.AreaTriggerStore);
        Load("SpellName.db2", "Name_lang", data.SpellStore);
        Load("Achievement.db2", "Title_lang", data.AchievementStore);
        Load("AreaTable.db2", "AreaName_lang", data.AreaStore);
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
                RandomSelect = row.FieldAs<ushort>("RandomSelect"),
                ItemRandomSuffixGroupId = row.FieldAs<ushort>("ItemRandomSuffixGroupID"),
            };
            data.ItemSparses.Add(item);
        });
        Load("ItemRandomProperties.db2", "Name_lang", data.ItemRandomPropertiesStore);
        Load("ItemRandomSuffix.db2", "Name_lang", data.ItemRandomSuffixStore);
        Load("EmotesText.db2", "Name", data.TextEmoteStore);
        Load("Languages.db2", "Name_lang", data.LanguageStore);
        Load("Map.db2", "Directory", data.MapDirectoryStore);
        Load("Map.db2", "MapName_lang", data.MapStore);
        Load("Faction.db2", "Name_lang", data.FactionStore);
        Load("FactionTemplate.db2", "Faction", data.FactionTemplateStore);
        Load("SceneScriptPackage.db2", "Name", data.SceneStore);
        Load("SpellFocusObject.db2", "Name_lang", data.SpellFocusObjectStore);
        Load("QuestInfo.db2", "InfoName_lang", data.QuestInfoStore);
        Load("CharTitles.db2", "Name_lang", data.CharTitleStore);
        Load("QuestSort.db2", "SortName_lang", data.QuestSortStore);
        Load("TaxiNodes.db2",  "Name_lang", data.TaxiNodeStore);
        LoadDB2("TaxiPath.db2",  row => data.TaxiPathsStore.Add(row.ID, (row.Field<int>("FromTaxiNode"), row.Field<int>("ToTaxiNode"))));
    }
}