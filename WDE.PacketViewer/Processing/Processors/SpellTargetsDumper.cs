using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors;

[AutoRegister]
public class SpellTargetsDumper : PacketProcessor<bool>, IPacketTextDumper, IUnfilteredTwoStepPacketBoolProcessor
{
    private const int ConditionSourceImplicitTarget = 13;
    private const int ConditionTypeEntryGuid = 31;
    private const int ConditionEntryGuidCreatureType = 3;

    private readonly ISpellService spellService;
    private long lastMapId = -1;

    private Dictionary<uint, (long map, Vec3 position, float? orientation)> targets = new();
    private Dictionary<uint, HashSet<uint>> hitEntries = new();

    public SpellTargetsDumper(IDbcSpellService spellService)
    {
        this.spellService = spellService;
    }

    public async Task<string> Generate()
    {
        List<int> validSpellEffects = new();
        var trans = Queries.BeginTransaction(DataDatabaseType.World);

        foreach (var (spell, dest) in targets)
        {
            validSpellEffects.Clear();

            if (!spellService.Exists(spell))
                continue;

            var spellEffects = spellService.GetSpellEffectsCount(spell);
            for (int i = 0; i < spellEffects; ++i)
            {
                var (targetA, targetB) = spellService.GetSpellEffectTargetType(spell, i);
                if (targetA == SpellTarget.DestDb || targetB == SpellTarget.DestDb)
                    validSpellEffects.Add(i);
            }

            if (validSpellEffects.Count > 0)
            {
                trans.Table(DatabaseTable.WorldTable("spell_target_position"))
                    .Where(row => row.Column<uint>("ID") == spell)
                    .Delete();

                trans.Table(DatabaseTable.WorldTable("spell_target_position"))
                    .BulkInsert(validSpellEffects.Select(x => new
                    {
                        ID = spell,
                        EffectIndex = x,
                        MapID = dest.map,
                        PositionX = dest.position.X,
                        PositionY = dest.position.Y,
                        PositionZ = dest.position.Z,
                        Orientation = dest.orientation,
                        __comment = spellService.Exists(spell) ? spellService.GetName(spell) : null
                    }));

                trans.BlankLine();
            }
        }

        trans.BlankLine();

        foreach (var (spell, entries) in hitEntries)
        {
            var effectsCount = spellService.GetSpellEffectsCount(spell);
            int entryEffectsMask = Enumerable.Range(0, effectsCount)
                .Select(idx => (idx, targets: spellService.GetSpellEffectTargetType(spell, idx)))
                .Where(pair => IsEntryTarget(pair.targets.a) || IsEntryTarget(pair.targets.b))
                .Select(pair => 1 << pair.idx)
                .Aggregate(0, (a, b) => a | b);

            if (entryEffectsMask == 0)
                continue;

            if (spellService.Exists(spell))
                trans.Comment($"{spellService.GetName(spell)} ({spell})");

            trans.Table(DatabaseTable.WorldTable("conditions"))
                .Where(row => row.Column<uint>("SourceTypeOrReferenceId") == ConditionSourceImplicitTarget && row.Column<uint>("SourceEntry") == spell)
                .Delete();

            trans.Table(DatabaseTable.WorldTable("conditions"))
                .BulkInsert(entries.Select((entry, index) =>
                new {
                    SourceTypeOrReferenceId = ConditionSourceImplicitTarget,
                    SourceGroup = entryEffectsMask,
                    SourceEntry = spell,
                    ElseGroup = index,
                    ConditionTypeOrReference = ConditionTypeEntryGuid,
                    ConditionValue1 = ConditionEntryGuidCreatureType,
                    ConditionValue2 = entry,
                    Comment = $"Target of spell is creature {entry}"
                }));
            trans.BlankLine();
        }

        return trans.Close().ToString() ?? "";
    }

    private bool IsEntryTarget(SpellTarget target)
    {
        return target switch
        {
            SpellTarget.DestNearbyEntry => true,
            SpellTarget.UnitSrcAreaEntry => true,
            SpellTarget.UnitDestAreaEntry => true,
            SpellTarget.UnitNearbyEntry => true,
            SpellTarget.GameobjectNearbyEntry => true,
            SpellTarget.DestRandomEntry => true,
            SpellTarget.UnitConeEntry => true,
            SpellTarget.DestAreaEntryExtra => true,
            SpellTarget.UnitConeEntry110 => true,
            SpellTarget.UnitConeEntry129 => true,
            SpellTarget.DestNearbyEntryOrSelfPos => true,
            _ => false
        };
    }

    protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketUpdateObject packet)
    {
        lastMapId = packet.MapId;
        return base.Process(in basePacket, in packet);
    }

    protected override unsafe bool Process(ref readonly PacketBase basePacket, ref readonly PacketSpellGo packet)
    {
        if (packet.Data == null)
            return default;

        var spell = packet.Data->Spell;
        if (packet.Data->DstLocation != null)
        {
            targets[spell] = (lastMapId, *packet.Data->DstLocation, packet.Data->DstOrientation);
        }

        foreach (var target in packet.Data->HitTargets.AsSpan())
        {
            if (target.Type is UniversalHighGuid.Creature or UniversalHighGuid.Vehicle && target.Entry > 0)
            {
                if (!hitEntries.TryGetValue(spell, out var entries))
                    entries = hitEntries[spell] = new();
                entries.Add(target.Entry);
            }
        }


        return base.Process(in basePacket, in packet);
    }

    public bool UnfilteredPreProcess(ref readonly PacketHolder packet)
    {
        if (packet.KindCase == PacketHolder.KindOneofCase.UpdateObject)
        {
            ref var updateObject = ref packet.UpdateObject;
            lastMapId = updateObject.MapId;
        }
        return true;
    }
}
