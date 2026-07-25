using System.Collections.Generic;
using Prism.Events;

namespace WDE.MapSpawns.Models.Solution;

// Published by the game-side editors right after their live SQL save succeeds, carrying the freshly
// saved per-key solution items (one per touched leader / group / path — items are keyed by their
// natural key). The full-app bridge upserts each into the session and refreshes its query — sessions
// are app-only, so in headless hosts (RenderingTester) nobody listens and the events are inert.

public class FormationsSavedEvent : PubSubEvent<IReadOnlyList<FormationsSolutionItem>> { }

public class SpawnGroupsSavedEvent : PubSubEvent<IReadOnlyList<SpawnGroupsSolutionItem>> { }

public class PoolsSavedEvent : PubSubEvent<IReadOnlyList<PoolsSolutionItem>> { }

public class WaypointsSavedEvent : PubSubEvent<WaypointsSolutionItem> { }

/// <summary>
/// Game -> app: a 3D editor without a dedicated solution item (graveyards, spell target positions)
/// is ABOUT to execute this SQL as its live save. The bridge runs it through
/// <c>IQueryParserService</c>, which resolves the statements against the GENERIC table definitions
/// and yields plain DatabaseTableSolutionItems for the session - the same items the table editors
/// themselves would produce.
///
/// The handshake exists because the parse must happen BEFORE the SQL executes: DELETE detection
/// only records rows that exist in the database at parse time (a delete of a row the same query
/// re-inserts is not a session deletion). The publisher therefore:
///  1. publishes this payload and, if <see cref="Handled"/> was set (synchronously, during
///     Publish), awaits <see cref="Parsed"/> before executing;
///  2. executes the SQL and calls <see cref="NotifyExecuted"/> (or
///     <see cref="NotifyExecutionFailed"/>) - only then does the bridge push the parsed items
///     into the session (the session query re-reads the just-written rows).
/// Inert in headless hosts: nobody sets Handled, the publisher skips the wait.
/// </summary>
public class WorldEditQuerySave
{
    public WorldEditQuerySave(string query) => Query = query;

    public string Query { get; }

    /// <summary>Set synchronously during Publish by the bridge (PublisherThread subscription);
    /// stays false when no full app is listening.</summary>
    public bool Handled { get; set; }

    private readonly System.Threading.Tasks.TaskCompletionSource parsed =
        new(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly System.Threading.Tasks.TaskCompletionSource executed =
        new(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completed by the bridge once the query was parsed (rows still in the database).</summary>
    public System.Threading.Tasks.Task Parsed => parsed.Task;

    /// <summary>Completed by the publisher after the SQL executed (canceled on failure).</summary>
    public System.Threading.Tasks.Task Executed => executed.Task;

    public void NotifyParsed() => parsed.TrySetResult();
    public void NotifyExecuted() => executed.TrySetResult();
    public void NotifyExecutionFailed() => executed.TrySetCanceled();
}

public class WorldEditQuerySavingEvent : PubSubEvent<WorldEditQuerySave> { }

/// <summary>Game -> app: open the generic spell_area table editor filtered to the given area and
/// its parent zone (the core matches spell_area.area on either). Handled by the bridge on the UI
/// thread; inert in headless hosts.</summary>
public class OpenSpellAreaEditorRequest
{
    public int AreaId { get; init; }
    public int ZoneId { get; init; }
}

public class OpenSpellAreaEditorEvent : PubSubEvent<OpenSpellAreaEditorRequest> { }

/// <summary>Game -> app: open the creature_template / gameobject_template editor for the entry
/// (a new document). Handled by the bridge on the UI thread; inert in headless hosts.</summary>
public class OpenTemplateEditorRequest
{
    public bool IsCreature { get; init; }
    public uint Entry { get; init; }
}

public class OpenTemplateEditorEvent : PubSubEvent<OpenTemplateEditorRequest> { }

/// <summary>Game -> app: open the gossip_menu editor for the given menu id. Handled by the bridge
/// on the UI thread; inert in headless hosts.</summary>
public class OpenGossipMenuEditorEvent : PubSubEvent<uint> { }

/// <summary>Game -> app: open the loot editor for the entry (creature/GO/skinning/pickpocket loot -
/// the loot service reconciles the per-core loot-id indirection). Handled by the bridge on the UI
/// thread; inert in headless hosts.</summary>
public class OpenLootEditorRequest
{
    public WDE.Common.Database.LootSourceType Type { get; init; }
    public uint Entry { get; init; }
}

public class OpenLootEditorEvent : PubSubEvent<OpenLootEditorRequest> { }

/// <summary>Which per-entry side table of a spawn to open. The game side stays table-name-agnostic -
/// the bridge resolves the semantic kind to the active core's table (cmangos and Trinity name the
/// quest relation tables differently).</summary>
public enum SpawnRelatedTable
{
    Vendor,
    Trainer,
    SpellClick,
    QuestStarter,
    QuestEnder,
}

/// <summary>Game -> app: open the generic editor of an entry-keyed side table (vendor items,
/// trainer spells, spellclick spells, quest starter/ender relations), filtered to the entry.
/// Handled by the bridge on the UI thread; inert in headless hosts.</summary>
public class OpenSpawnRelatedTableRequest
{
    public SpawnRelatedTable Table { get; init; }
    public bool IsCreature { get; init; }
    public uint Entry { get; init; }
}

public class OpenSpawnRelatedTableEvent : PubSubEvent<OpenSpawnRelatedTableRequest> { }
