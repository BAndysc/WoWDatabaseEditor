using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Events;
using TheMaths;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Tasks;
using WDE.MapSpawns.Models.Solution;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Generators.AreaTriggers;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Models.AreaTriggers;

/// <summary>One editable <c>areatrigger_teleport</c> row - the destination (and requirements) of a
/// teleport fired by entering the DBC-defined trigger shape. Keyed by trigger id.</summary>
public sealed class AreaTriggerTeleportData
{
    public uint Id { get; init; }
    public string? Name;
    public uint RequiredLevel;
    public uint RequiredItem;
    public uint RequiredItem2;
    public uint HeroicKey;
    public uint HeroicKey2;
    public uint RequiredQuestDone;
    public uint RequiredQuestDoneHeroic;
    public uint Map;
    public Vector3 Position;
    public float Orientation;
    public uint ConditionId;
    public uint Status;
    public string? StatusFailedText;
}

[UniqueProvider]
public interface IAreaTriggerEditorService
{
    bool IsSupported { get; }
    bool SupportsTavern { get; }
    bool SupportsQuestRelation { get; }
    bool SupportsScript { get; }
    /// <summary>Which optional teleport columns the active core has - the inspector hides the rest.</summary>
    AreaTriggerTeleportColumns TeleportColumns { get; }
    bool HasData { get; }
    int LoadedMap { get; }
    bool AnyDirty { get; }
    int Revision { get; }
    IReadOnlyDictionary<uint, AreaTriggerTeleportData> Teleports { get; }
    /// <summary>Trigger id -> tavern name; presence of the key = the trigger is a tavern.</summary>
    IReadOnlyDictionary<uint, string?> Taverns { get; }
    /// <summary>Trigger id -> exploration quest completed by entering the trigger.</summary>
    IReadOnlyDictionary<uint, uint> QuestRelations { get; }
    /// <summary>Trigger id -> script-library script name.</summary>
    IReadOnlyDictionary<uint, string> ScriptNames { get; }
    Task LoadForMap(int mapId);
    void PumpPendingLoads();
    /// <summary>Quest id -> name, snapshotted from the app-side quest store at load time
    /// (the game thread must not call app services directly).</summary>
    string? GetQuestName(uint questId);
    /// <summary>Creates (or returns the existing) teleport row - one destination per trigger.</summary>
    AreaTriggerTeleportData CreateTeleport(uint triggerId, uint map, Vector3 position, float orientation);
    void NotifyTeleportChanged(uint triggerId);
    void DeleteTeleport(uint triggerId);
    void SetTavern(uint triggerId, bool isTavern, string? name);
    void SetQuestRelation(uint triggerId, uint? quest);
    /// <summary>Null or empty script name removes the row.</summary>
    void SetScriptName(uint triggerId, string? scriptName);
    /// <summary>The exact SQL <see cref="Save"/> would execute right now (null = nothing dirty).
    /// Pure: no execution, no state change.</summary>
    IQuery? BuildSaveQuery();
    Task Save();
}

public class AreaTriggerEditorService : IAreaTriggerEditorService
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly IMySqlExecutor mySqlExecutor;
    private readonly IMainThread mainThread;
    private readonly IEventAggregator eventAggregator;
    private readonly IParameterFactory parameterFactory;
    private readonly IAreaTriggerEditorConfig config;
    private readonly IQueryGenerator<IAreaTriggerTeleport> teleportGen;
    private readonly IQueryGenerator<IAreaTriggerTavern> tavernGen;
    private readonly IQueryGenerator<IAreaTriggerQuestRelation> questRelationGen;
    private readonly IQueryGenerator<IScriptedAreaTrigger> scriptGen;

    private sealed class Loaded
    {
        public required int MapId;
        public required Dictionary<uint, AreaTriggerTeleportData> Teleports;
        public required Dictionary<uint, string?> Taverns;
        public required Dictionary<uint, uint> QuestRelations;
        public required Dictionary<uint, string> ScriptNames;
        public required Dictionary<uint, string> QuestNames;
    }

    private volatile Loaded? pending;

    private readonly Dictionary<uint, AreaTriggerTeleportData> teleports = new();
    private readonly Dictionary<uint, string?> taverns = new();
    private readonly Dictionary<uint, uint> questRelations = new();
    private readonly Dictionary<uint, string> scriptNames = new();
    private readonly Dictionary<uint, string> questNames = new();

    // per-table dirty ids; "created" = created since load, so a create+delete emits no SQL at all.
    // A dirty id absent from its dictionary is a pending row deletion.
    private readonly HashSet<uint> dirtyTeleports = new(), createdTeleports = new();
    private readonly HashSet<uint> dirtyTaverns = new(), createdTaverns = new();
    private readonly HashSet<uint> dirtyQuestRelations = new(), createdQuestRelations = new();
    private readonly HashSet<uint> dirtyScripts = new(), createdScripts = new();

    public bool IsSupported => teleportGen.TableName != null;
    public bool SupportsTavern => tavernGen.TableName != null;
    public bool SupportsQuestRelation => questRelationGen.TableName != null;
    public bool SupportsScript => scriptGen.TableName != null;
    public AreaTriggerTeleportColumns TeleportColumns => config.TeleportColumns;
    public bool HasData { get; private set; }
    public int LoadedMap { get; private set; } = -1;
    public bool AnyDirty => dirtyTeleports.Count > 0 || dirtyTaverns.Count > 0 ||
                            dirtyQuestRelations.Count > 0 || dirtyScripts.Count > 0;
    public int Revision { get; private set; }
    public IReadOnlyDictionary<uint, AreaTriggerTeleportData> Teleports => teleports;
    public IReadOnlyDictionary<uint, string?> Taverns => taverns;
    public IReadOnlyDictionary<uint, uint> QuestRelations => questRelations;
    public IReadOnlyDictionary<uint, string> ScriptNames => scriptNames;

    public AreaTriggerEditorService(IDatabaseProvider databaseProvider,
        IMySqlExecutor mySqlExecutor,
        IMainThread mainThread,
        IEventAggregator eventAggregator,
        IParameterFactory parameterFactory,
        IAreaTriggerEditorConfig config,
        IQueryGenerator<IAreaTriggerTeleport> teleportGen,
        IQueryGenerator<IAreaTriggerTavern> tavernGen,
        IQueryGenerator<IAreaTriggerQuestRelation> questRelationGen,
        IQueryGenerator<IScriptedAreaTrigger> scriptGen)
    {
        this.databaseProvider = databaseProvider;
        this.mySqlExecutor = mySqlExecutor;
        this.mainThread = mainThread;
        this.eventAggregator = eventAggregator;
        this.parameterFactory = parameterFactory;
        this.config = config;
        this.teleportGen = teleportGen;
        this.tavernGen = tavernGen;
        this.questRelationGen = questRelationGen;
        this.scriptGen = scriptGen;
    }

    public async Task LoadForMap(int mapId)
    {
        // claim the map BEFORE the first await - see SpellTargetEditorService.LoadForMap
        LoadedMap = mapId;

        var teleportRows = await databaseProvider.GetAreaTriggerTeleportsAsync();
        var tavernRows = SupportsTavern ? await databaseProvider.GetAreaTriggerTavernsAsync() : null;
        var questRows = SupportsQuestRelation ? await databaseProvider.GetAreaTriggerQuestRelationsAsync() : null;
        var scriptRows = SupportsScript ? await databaseProvider.GetScriptedAreaTriggersAsync() : null;

        var teleportMap = new Dictionary<uint, AreaTriggerTeleportData>();
        if (teleportRows != null)
        {
            foreach (var r in teleportRows)
            {
                teleportMap[r.Id] = new AreaTriggerTeleportData
                {
                    Id = r.Id,
                    Name = r.Name,
                    RequiredLevel = r.RequiredLevel,
                    RequiredItem = r.RequiredItem,
                    RequiredItem2 = r.RequiredItem2,
                    HeroicKey = r.HeroicKey,
                    HeroicKey2 = r.HeroicKey2,
                    RequiredQuestDone = r.RequiredQuestDone,
                    RequiredQuestDoneHeroic = r.RequiredQuestDoneHeroic,
                    Map = r.Map,
                    Position = new Vector3(r.X, r.Y, r.Z),
                    Orientation = r.O,
                    ConditionId = r.ConditionId,
                    Status = r.Status,
                    StatusFailedText = r.StatusFailedText,
                };
            }
        }

        var tavernMap = new Dictionary<uint, string?>();
        if (tavernRows != null)
        {
            foreach (var r in tavernRows)
                tavernMap[r.Id] = r.Name;
        }

        var questMap = new Dictionary<uint, uint>();
        if (questRows != null)
        {
            foreach (var r in questRows)
                questMap[r.Id] = r.Quest;
        }

        var scriptMap = new Dictionary<uint, string>();
        if (scriptRows != null)
        {
            foreach (var r in scriptRows)
                scriptMap[r.Id] = r.ScriptName;
        }

        // quest names live in the app-side parameter store - snapshot them on the main thread
        var names = await mainThread.Schedule(() =>
        {
            var result = new Dictionary<uint, string>();
            var questParameter = parameterFactory.Factory("QuestParameter");
            if (questParameter.Items is { } items)
            {
                foreach (var (key, option) in items)
                {
                    if (key >= 0)
                        result[(uint)key] = option.Name;
                }
            }
            return result;
        });

        pending = new Loaded
        {
            MapId = mapId,
            Teleports = teleportMap,
            Taverns = tavernMap,
            QuestRelations = questMap,
            ScriptNames = scriptMap,
            QuestNames = names,
        };
    }

    public void PumpPendingLoads()
    {
        var p = pending;
        if (p == null)
            return;
        pending = null;

        teleports.Clear();
        taverns.Clear();
        questRelations.Clear();
        scriptNames.Clear();
        questNames.Clear();
        dirtyTeleports.Clear();
        createdTeleports.Clear();
        dirtyTaverns.Clear();
        createdTaverns.Clear();
        dirtyQuestRelations.Clear();
        createdQuestRelations.Clear();
        dirtyScripts.Clear();
        createdScripts.Clear();
        foreach (var (id, d) in p.Teleports)
            teleports[id] = d;
        foreach (var (id, name) in p.Taverns)
            taverns[id] = name;
        foreach (var (id, quest) in p.QuestRelations)
            questRelations[id] = quest;
        foreach (var (id, script) in p.ScriptNames)
            scriptNames[id] = script;
        foreach (var (id, name) in p.QuestNames)
            questNames[id] = name;
        LoadedMap = p.MapId;
        HasData = true;
        Revision++;
    }

    public string? GetQuestName(uint questId) =>
        questNames.TryGetValue(questId, out var name) ? name : null;

    public AreaTriggerTeleportData CreateTeleport(uint triggerId, uint map, Vector3 position, float orientation)
    {
        if (teleports.TryGetValue(triggerId, out var existing))
            return existing;

        var data = new AreaTriggerTeleportData
        {
            Id = triggerId,
            Map = map,
            Position = position,
            Orientation = orientation,
        };
        teleports[triggerId] = data;
        createdTeleports.Add(triggerId);
        dirtyTeleports.Add(triggerId);
        Revision++;
        return data;
    }

    public void NotifyTeleportChanged(uint triggerId)
    {
        if (!teleports.ContainsKey(triggerId))
            return;
        dirtyTeleports.Add(triggerId);
        Revision++;
    }

    public void DeleteTeleport(uint triggerId)
    {
        if (!teleports.Remove(triggerId))
            return;

        if (createdTeleports.Remove(triggerId))
            dirtyTeleports.Remove(triggerId); // never saved - nothing to delete in the database
        else
            dirtyTeleports.Add(triggerId);
        Revision++;
    }

    public void SetTavern(uint triggerId, bool isTavern, string? name)
    {
        if (isTavern)
        {
            bool existed = taverns.ContainsKey(triggerId);
            taverns[triggerId] = name;
            if (!existed)
                createdTaverns.Add(triggerId);
            dirtyTaverns.Add(triggerId);
        }
        else
        {
            if (!taverns.Remove(triggerId))
                return;
            if (createdTaverns.Remove(triggerId))
                dirtyTaverns.Remove(triggerId);
            else
                dirtyTaverns.Add(triggerId);
        }
        Revision++;
    }

    public void SetQuestRelation(uint triggerId, uint? quest)
    {
        if (quest is { } questId)
        {
            bool existed = questRelations.ContainsKey(triggerId);
            questRelations[triggerId] = questId;
            if (!existed)
                createdQuestRelations.Add(triggerId);
            dirtyQuestRelations.Add(triggerId);
        }
        else
        {
            if (!questRelations.Remove(triggerId))
                return;
            if (createdQuestRelations.Remove(triggerId))
                dirtyQuestRelations.Remove(triggerId);
            else
                dirtyQuestRelations.Add(triggerId);
        }
        Revision++;
    }

    public void SetScriptName(uint triggerId, string? scriptName)
    {
        if (!string.IsNullOrWhiteSpace(scriptName))
        {
            bool existed = scriptNames.ContainsKey(triggerId);
            scriptNames[triggerId] = scriptName;
            if (!existed)
                createdScripts.Add(triggerId);
            dirtyScripts.Add(triggerId);
        }
        else
        {
            if (!scriptNames.Remove(triggerId))
                return;
            if (createdScripts.Remove(triggerId))
                dirtyScripts.Remove(triggerId);
            else
                dirtyScripts.Add(triggerId);
        }
        Revision++;
    }

    public IQuery? BuildSaveQuery()
    {
        if (!AnyDirty)
            return null;

        IMultiQuery? multi = null;
        void Add(IQuery? q)
        {
            if (q == null)
                return;
            multi ??= Queries.BeginTransaction(q.Database);
            multi.Add(q);
        }

        // idempotent per-trigger rewrites; a deleted row is just the DELETE
        foreach (var id in dirtyTeleports)
        {
            Add(teleportGen.TryDelete(new AbstractAreaTriggerTeleport { Id = id }));
            if (!teleports.TryGetValue(id, out var d))
                continue;
            Add(teleportGen.TryInsert(new AbstractAreaTriggerTeleport
            {
                Id = d.Id,
                Name = d.Name,
                RequiredLevel = d.RequiredLevel,
                RequiredItem = d.RequiredItem,
                RequiredItem2 = d.RequiredItem2,
                HeroicKey = d.HeroicKey,
                HeroicKey2 = d.HeroicKey2,
                RequiredQuestDone = d.RequiredQuestDone,
                RequiredQuestDoneHeroic = d.RequiredQuestDoneHeroic,
                Map = d.Map,
                X = d.Position.X,
                Y = d.Position.Y,
                Z = d.Position.Z,
                O = d.Orientation,
                ConditionId = d.ConditionId,
                Status = d.Status,
                StatusFailedText = d.StatusFailedText,
            }));
        }

        foreach (var id in dirtyTaverns)
        {
            Add(tavernGen.TryDelete(new AbstractAreaTriggerTavern { Id = id }));
            if (taverns.TryGetValue(id, out var name))
                Add(tavernGen.TryInsert(new AbstractAreaTriggerTavern { Id = id, Name = name }));
        }

        foreach (var id in dirtyQuestRelations)
        {
            Add(questRelationGen.TryDelete(new AbstractAreaTriggerQuestRelation { Id = id }));
            if (questRelations.TryGetValue(id, out var quest))
                Add(questRelationGen.TryInsert(new AbstractAreaTriggerQuestRelation { Id = id, Quest = quest }));
        }

        foreach (var id in dirtyScripts)
        {
            Add(scriptGen.TryDelete(new AbstractScriptedAreaTrigger { Id = id }));
            if (scriptNames.TryGetValue(id, out var script))
                Add(scriptGen.TryInsert(new AbstractScriptedAreaTrigger { Id = id, ScriptName = script }));
        }

        return multi?.Close();
    }

    public async Task Save()
    {
        var query = BuildSaveQuery();
        if (query == null)
            return;

        // no dedicated solution item: the bridge parses this SQL against the generic table
        // definitions into plain table solution items. Parsing MUST see the pre-save database
        // (DELETE detection skips rows that don't exist), hence the parse-then-execute handshake.
        var save = new WorldEditQuerySave(query.QueryString);
        eventAggregator.GetEvent<WorldEditQuerySavingEvent>().Publish(save);
        if (save.Handled)
            await save.Parsed;

        try
        {
            await mainThread.Schedule(async () =>
            {
                await mySqlExecutor.ExecuteSql(query);
                return true;
            });
        }
        catch
        {
            save.NotifyExecutionFailed();
            throw;
        }
        save.NotifyExecuted(); // the bridge now pushes the parsed items into the session

        dirtyTeleports.Clear();
        createdTeleports.Clear();
        dirtyTaverns.Clear();
        createdTaverns.Clear();
        dirtyQuestRelations.Clear();
        createdQuestRelations.Clear();
        dirtyScripts.Clear();
        createdScripts.Clear();
        Revision++;
    }
}
