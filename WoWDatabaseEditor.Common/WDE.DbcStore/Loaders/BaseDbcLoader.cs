using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DBCD;
using WDE.Common.DBC;
using WDE.Common.Game;
using WDE.Common.Parameters;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DbcStore.Providers;

namespace WDE.DbcStore.Loaders;

internal abstract class BaseDbcLoader : IDbcLoader
{
    private readonly IDbcSettingsProvider dbcSettingsProvider;
    private readonly IDatabaseClientFileOpener opener;
    private readonly DBCD.DBCD dbcd;
    protected string DbcPath { get; }
    public abstract DBCVersions Version { get; }
    public abstract int StepsCount { get; }

    private int currentStep;

    private ITaskProgress progress;

    protected int localeIndex;

    public BaseDbcLoader(IDbcSettingsProvider dbcSettingsProvider, 
        IDatabaseClientFileOpener opener,
        DBCD.DBCD dbcd)
    {
        DbcPath = dbcSettingsProvider.GetSettings().Path;
        this.dbcSettingsProvider = dbcSettingsProvider;
        this.opener = opener;
        this.dbcd = dbcd;
        progress = null!;
    }

    protected int LocaleOffset => (int) dbcSettingsProvider.GetSettings().DBCLocale;

    protected abstract void LoadDbcCore(DbcData data, ITaskProgress progress);
    
    public void LoadDbc(DbcData data, int localeIndex, ITaskProgress progress)
    {
        this.progress = progress;
        this.localeIndex = localeIndex;
        LoadDbcCore(data, progress);
    }

    protected void LoadAndRegister(DbcData data, string filename, string parameter, int keyIndex, Func<IDbcIterator, string> getString)
    {
        Dictionary<long, SelectOption> dict = new();
        Load(filename, row =>
        {
            var id = row.GetUInt(keyIndex);
            dict[id] = new SelectOption(getString(row));
        });
        data.parametersToRegister.Add((parameter, dict));
    }

    protected void LoadAndRegister(DbcData data, string filename, string parameter, int keyIndex, int nameIndex, bool withLocale = false)
    {
        int locale = (int) DBCLocales.LANG_enUS;

        if (withLocale)
            locale = (int) dbcSettingsProvider.GetSettings().DBCLocale;
        LoadAndRegister(data, filename, parameter, keyIndex, r => r.GetString(nameIndex + locale));
    }

    protected void Load(string filename, Action<IDbcIterator> foreachRow)
    {
        progress?.Report(currentStep++, StepsCount, $"Loading {filename}");
        var path = $"{dbcSettingsProvider.GetSettings().Path}/{filename}";

        if (!File.Exists(path))
            return;

        var dbc = opener.Open(path);

        foreach (var entry in dbc)
            foreachRow(entry);
    }
    
    protected void Load<T>(string filename, List<T> outputList, Func<IDbcIterator, T> creator)
    {
        progress?.Report(currentStep++, StepsCount, $"Loading {filename}");
        var path = $"{dbcSettingsProvider.GetSettings().Path}/{filename}";

        if (!File.Exists(path))
            return;

        var dbc = opener.Open(path);

        outputList.Capacity = (int)dbc.RecordCount;
        
        foreach (var entry in dbc)
            outputList.Add(creator(entry));
    }
    
    protected void Load(string filename, int id, int nameIndex, Dictionary<long, string> dictionary, bool useLocale = false)
    {
        int locale = (int) DBCLocales.LANG_enUS;

        if (useLocale)
            locale = LocaleOffset;

        Load(filename, row => dictionary.Add(row.GetInt(id), row.GetString(nameIndex + locale)));
    }
    
    protected void Load(string filename, int id, int nameIndex, Dictionary<long, long> dictionary)
    {
        Load(filename, row => dictionary.Add(row.GetInt(id), row.GetInt(nameIndex)));
    }

    protected void LoadDB2(string filename, Action<DBCDRow> doAction)
    {
        var storage = dbcd.Load($"{dbcSettingsProvider.GetSettings().Path}/{filename}");
        foreach (DBCDRow item in storage.Values)
            doAction(item);
    }
    
    protected void Load(string filename, string fieldName, Dictionary<long, string> dictionary)
    {
        var storage = dbcd.Load($"{dbcSettingsProvider.GetSettings().Path}/{filename}");

        if (fieldName == String.Empty)
        {
            foreach (DBCDRow item in storage.Values)
                dictionary.Add(item.ID, String.Empty);
        }
        else
        {
            foreach (DBCDRow item in storage.Values)
            {
                if (item[fieldName] == null)
                    return;

                dictionary.Add(item.ID, item[fieldName].ToString()!);
            }
        }
    }

    protected void Load(string filename, string fieldName, Dictionary<long, long> dictionary)
    {
        var storage = dbcd.Load($"{dbcSettingsProvider.GetSettings().Path}/{filename}");

        if (fieldName == String.Empty)
        {
            foreach (DBCDRow item in storage.Values)
                dictionary.Add(item.ID, 0);
        }
        else
        {
            foreach (DBCDRow item in storage.Values)
            {
                if (item[fieldName] == null)
                    return;

                dictionary.Add(item.ID, Convert.ToInt64(item[fieldName]));
            }
        }
    }
    
    // Helpers
    protected void FillMapAreas(DbcData data)
    {
        var mapById = data.Maps.ToDictionary(x => x.Id, x => x);
        var areasById = data.Areas.ToDictionary(x => x.Id, x => x);
        foreach (var area in data.Areas)
        {
            area.Map = mapById.TryGetValue(area.MapId, out var map) ? map : null;
            if (area.ParentAreaId != 0)
                area.ParentArea = areasById.TryGetValue(area.ParentAreaId, out var parentArea) ? parentArea : null;
            
            data.AreaStore.Add(area.Id, area.Name);
        }

        foreach (var map in data.Maps)
        {
            data.MapStore.Add(map.Id, map.Name);
            data.MapDirectoryStore.Add(map.Id, map.Directory);
        }
    }

    protected void LoadLegionAreaGroup(DbcData data, Dictionary<long, string> areaGroupStore)
    {
        Dictionary<ushort, List<ushort>> areaGroupToArea = new Dictionary<ushort, List<ushort>>();
        Load("AreaGroupMember.db2", row =>
        {
            var areaId = row.GetUShort(1);
            var areaGroup = row.GetUShort(2);
            if (!areaGroupToArea.TryGetValue(areaGroup, out var list))
                list = areaGroupToArea[areaGroup] = new();
            list.Add(areaId);
        });
        foreach (var (group, list) in areaGroupToArea)
            areaGroupStore.Add(group, BuildAreaGroupName(data, list));
    }

    protected string GetLockDescription(Func<int, long> getLockKeyType, Func<int, long> getLockProperty, Func<int, long> getSkill)
    {
        for (int i = 0; i < 8; ++i)
        {
            var type = (LockKeyType)getLockKeyType(i);
            
            if (type == LockKeyType.None)
                continue;
            
            var lockProperty = getLockProperty(i);
            var skill = getSkill(i);

            if (type == LockKeyType.Item)
            {
                
            }
            else if (type == LockKeyType.Skill)
            {
                
            }
            else if (type == LockKeyType.Spell)
            {
                
            }
        }

        return "";
    }
    
    private StringBuilder sb = new StringBuilder();
    
    protected string GetRangeDescription(float minHostile, float maxHostile, string name, float? minFriendly = null, float? maxFriendly = null)
    {
        sb.Clear();
        if ((!minFriendly.HasValue || minHostile == minFriendly.Value) && (!maxFriendly.HasValue || maxHostile == maxFriendly.Value))
        {
            if (minHostile == 0)
                return $"{name} ({maxHostile} yd)";
            
            return $"{name} ({minHostile}-{maxHostile} yd)";
        }

        sb.Append(name);
        sb.Append(' ');
        sb.Append('(');

        sb.Append("hostile: ");
        if (minHostile == 0)
            sb.Append(maxHostile);
        else
        {
            sb.Append(minHostile);
            sb.Append(" - ");
            sb.Append(maxHostile);
        }
        sb.Append("yd, friendly: ");
        
        if (minFriendly == 0)
            sb.Append(maxFriendly);
        else
        {
            sb.Append(minFriendly);
            sb.Append(" - ");
            sb.Append(maxFriendly);
        }
        sb.Append("yd");
        
        sb.Append(')');
        return sb.ToString();
    }
    
    protected string GetRadiusDescription(float @base, float perLevel, float max)
    {
        if (perLevel == 0)
            return @base + " yd";
        if (max == 0)
            return $"{@base} yd + {perLevel} yd/level";
        return $"min({max} yd, {@base} yd + {perLevel} yd/level)";
    }

    protected string GetCastTimeDescription(int @base, int perLevel, int min)
    {
        if (@base == 0 && perLevel == 0 && min == 0)
            return "Instant";
        if (perLevel == 0)
            return @base.ToPrettyDuration();
        if (min == 0)
            return $"{@base.ToPrettyDuration()} + {perLevel.ToPrettyDuration()}/level";
        return $"max({min.ToPrettyDuration()}, {@base.ToPrettyDuration()} + {perLevel.ToPrettyDuration()}/level)";
    }

    protected string GetDurationTimeDescription(int @base, int perLevel, int max)
    {
        if (@base == -1)
            return "Infinite";
        if (perLevel == 0)
            return @base.ToPrettyDuration();
        if (max == 0)
            return $"{@base.ToPrettyDuration()} + {perLevel.ToPrettyDuration()}/level";
        return $"min({max.ToPrettyDuration()}, {@base.ToPrettyDuration()} + {perLevel.ToPrettyDuration()}/level)";
    }
    
    protected string BuildAreaGroupName(DbcData data, IReadOnlyList<ushort> areas)
    {
        if (areas.Count == 1)
        {
            if (data.AreaStore.TryGetValue(areas[0], out var name))
                return string.Format("{0} [{1}]", name, areas[0]);
            return "Area " + areas[0];
        }
        
        return string.Join(", ", areas.Select(area => data.AreaStore.TryGetValue(area, out var name) ? string.Format("{0} [{1}]", name, area) : "Area " + area));
    }
    
    protected string BuildAreaGroupName(DbcData data, IDbcIterator row, int start, int count)
    {
        for (int i = start; i < start + count; ++i)
        {
            var id = row.GetUInt(i);
            if (id == 0)
            {
                count = i - start;
                break;
            }
        }

        if (count == 1)
        {
            if (data.AreaStore.TryGetValue(row.GetUInt(start), out var name))
                return name;
            return "Area " + row.GetUInt(start);
        }
        
        StringBuilder sb = new();
        for (int i = start; i < start + count; ++i)
        {
            if (data.AreaStore.TryGetValue(row.GetUInt(start), out var name))
                sb.Append(name);
            else
                sb.Append("Area " + row.GetUInt(i));
            
            if (i != start + count - 1)
                sb.Append(", ");
        }

        return sb.ToString();
    }

    protected string GenerateCostDescription(int honor, int arenaPoints, int item)
    {
        if (honor != 0 && arenaPoints == 0 && item == 0)
            return honor + " honor";
        if (honor == 0 && arenaPoints != 0 && item == 0)
            return arenaPoints + " arena points";
        if (honor == 0 && arenaPoints == 0 && item != 0)
            return item + " item";
        if (item == 0)
            return honor + " honor, " + arenaPoints + " arena points";
        return honor + " honor, " + arenaPoints + " arena points, " + item + " item";
    }

}