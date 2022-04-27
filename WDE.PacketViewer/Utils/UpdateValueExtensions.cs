using System.Collections.Generic;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.Utils;

public static class UpdateValueExtensions
{
    private static long? GetInt(this UpdateValuesObjectDataFields fields, string field)
    {
        if (field == "OBJECT_FIELD_ENTRY")
            return fields.EntryID;
        
        if (field == "OBJECT_DYNAMIC_FLAGS")
            return fields.DynamicFlags;
        
        if (fields.Unit != null)
        {
            foreach (var getter in intGetters)
            {
                var pair = getter(fields.Unit);
                if (pair.Item1 == field)
                    return pair.Item2;
            }
        }

        return null;
    }

    private static UniversalGuid? GetGuid(this UpdateValuesObjectDataFields fields, string field)
    {
        if (fields.Unit != null)
        {
            foreach (var getter in guidGetters)
            {
                var pair = getter(fields.Unit);
                if (pair.Item1 == field)
                    return pair.Item2;
            }
        }

        return null;
    }

    public static bool TryGetInt(this UpdateValues update, string field, out long value)
    {
        if (update.ValuesCase == UpdateValues.ValuesOneofCase.Legacy)
            return update.Legacy.Ints.TryGetValue(field, out value);

        var val = GetInt(update.Fields, field);
        value = val ?? 0;
        return val.HasValue;
    }

    public static long GetIntOrDefault(this UpdateValues update, string field)
    {
        if (update.TryGetInt(field, out var v))
            return v;
        return 0;
    }

    public static bool TryGetGuid(this UpdateValues update, string field, out UniversalGuid value)
    {
        if (update.ValuesCase == UpdateValues.ValuesOneofCase.Legacy)
            return update.Legacy.Guids.TryGetValue(field, out value);
        
        var val = GetGuid(update.Fields, field);
        value = val ?? null!;
        return val != null;
    }
    
    private static IEnumerable<System.Func<UpdateValuesObjectDataFields, (string, long?)>> objectIntGetters = new System.Func<UpdateValuesObjectDataFields, (string, long?)>[]
    {
        fields => ("OBJECT_DYNAMIC_FLAGS", fields.DynamicFlags),
        fields => ("OBJECT_FIELD_ENTRY", fields.EntryID),
    };
    
    private static IEnumerable<System.Func<UpdateValuesUnitDataFields, (string, long?)>> intGetters = new System.Func<UpdateValuesUnitDataFields, (string, long?)>[]
    {
        fields =>
        {
            var race = fields.Race.HasValue ? (byte)fields.Race : (byte)0;
            var @class = fields.ClassId.HasValue ? (byte)fields.ClassId : (byte)0;
            var gender = fields.Sex.HasValue ? (byte)fields.Sex : (byte)0;
            var total = (race | (@class << 8) | (gender << 16));
            return ("UNIT_FIELD_BYTES_0", fields.Race.HasValue | fields.ClassId.HasValue | fields.Sex.HasValue ? total : null);
        },
        fields =>
        {
            var standState = fields.StandState.HasValue ? (byte)fields.StandState : (byte)0;
            var petTalents = fields.PetTalentPoints.HasValue ? (byte)fields.PetTalentPoints : (byte)0;
            var visFlags = fields.VisFlags.HasValue ? (byte)fields.VisFlags : (byte)0;
            var animTier = fields.AnimTier.HasValue ? (byte)fields.AnimTier : (byte)0;
            var total = (standState | (petTalents << 8) | (visFlags << 16) | (animTier << 24));
            return ("UNIT_FIELD_BYTES_1", fields.StandState.HasValue | fields.PetTalentPoints.HasValue | fields.VisFlags.HasValue | fields.AnimTier.HasValue ? total : null);
        },
        fields =>
        {
            var sheathState = fields.SheatheState.HasValue ? (byte)fields.SheatheState : (byte)0;
            var pvpFlags = fields.PvpFlags.HasValue ? (byte)fields.PvpFlags : (byte)0;
            var petFlags = fields.PetFlags.HasValue ? (byte)fields.PetFlags : (byte)0;
            var shapeshiftForm = fields.ShapeshiftForm.HasValue ? (byte)fields.ShapeshiftForm : (byte)0;
            var total = (sheathState | (pvpFlags << 8) | (petFlags << 16) | (shapeshiftForm << 24));
            return ("UNIT_FIELD_BYTES_2", fields.SheatheState.HasValue | fields.PvpFlags.HasValue | fields.PetTalentPoints.HasValue | fields.ShapeshiftForm.HasValue ? total : null);
        },
        fields => ("UNIT_FIELD_DISPLAY_POWER", fields.DisplayPower),
        fields => ("UNIT_FIELD_HEALTH", fields.Health),
        fields => ("UNIT_FIELD_POWER", fields.Power.Count > 0 ? fields.Power[0].Value : null),
        fields => ("UNIT_FIELD_POWER + 1", fields.Power.Count > 1 ? fields.Power[1].Value : null),
        fields => ("UNIT_FIELD_MAXHEALTH", fields.MaxHealth),
        fields => ("UNIT_FIELD_MAXPOWER", fields.MaxPower.Count > 0 ? fields.MaxPower[0].Value : null),
        fields => ("UNIT_FIELD_MAXPOWER + 1", fields.MaxPower.Count > 1 ? fields.MaxPower[1].Value : null),
        fields => ("UNIT_FIELD_LEVEL", fields.Level),
        fields => ("UNIT_FIELD_FACTIONTEMPLATE", fields.FactionTemplate),
        fields => ("UNIT_VIRTUAL_ITEM_SLOT_ID", fields.VirtualItems.Count > 0 ? fields.VirtualItems[0]?.ItemID : null),
        fields => ("UNIT_VIRTUAL_ITEM_SLOT_ID + 1", fields.VirtualItems.Count > 1 ? fields.VirtualItems[1]?.ItemID : null),
        fields => ("UNIT_VIRTUAL_ITEM_SLOT_ID + 2", fields.VirtualItems.Count > 2 ? fields.VirtualItems[2]?.ItemID : null),
        fields => ("UNIT_FIELD_FLAGS", fields.Flags),
        fields => ("UNIT_FIELD_FLAGS_2", fields.Flags2),
        fields => ("UNIT_FIELD_FLAGS_3", fields.Flags3),
        fields => ("UNIT_FIELD_BASEATTACKTIME", fields.AttackRoundBaseTime.Count > 0 ? fields.AttackRoundBaseTime[0].Value : null),
        fields => ("UNIT_FIELD_RANGEDATTACKTIME", fields.RangedAttackRoundBaseTime),
        fields => ("UNIT_FIELD_DISPLAYID", fields.DisplayID),
        fields => ("UNIT_FIELD_NATIVEDISPLAYID", fields.NativeDisplayID),
        fields => ("UNIT_FIELD_MOUNTDISPLAYID", fields.MountDisplayID),
        // UNIT_FIELD_MINDAMAGE
        // UNIT_FIELD_MAXDAMAGE
        // UNIT_FIELD_MINOFFHANDDAMAGE
        // UNIT_FIELD_MAXOFFHANDDAMAGE
        fields => ("UNIT_CREATED_BY_SPELL", fields.CreatedBySpell),
        fields => ("UNIT_NPC_FLAGS", fields.NpcFlags.Count > 0 ? fields.NpcFlags[0].Value : null),
        fields => ("UNIT_NPC_FLAGS + 1", fields.NpcFlags.Count > 1 ? fields.NpcFlags[1].Value : null),
        fields => ("UNIT_NPC_EMOTESTATE", fields.EmoteState),
        fields => ("UNIT_FIELD_BASE_MANA", fields.BaseMana),
        fields => ("UNIT_FIELD_BASE_HEALTH", fields.BaseHealth)
    };
    
    private static IEnumerable<System.Func<UpdateValuesUnitDataFields, (string, float?)>> floatGetters = new System.Func<UpdateValuesUnitDataFields, (string, float?)>[]
    {
        fields => ("UNIT_FIELD_COMBATREACH", fields.CombatReach),
        fields => ("UNIT_FIELD_BOUNDINGRADIUS", fields.BoundingRadius),
    };

    private static IEnumerable<System.Func<UpdateValuesUnitDataFields, (string, UniversalGuid?)>> guidGetters = new System.Func<UpdateValuesUnitDataFields, (string, UniversalGuid?)>[]
    {
        fields => ("UNIT_FIELD_CHARM", fields.Charm),
        fields => ("UNIT_FIELD_SUMMON", fields.Summon),
        fields => ("UNIT_FIELD_CRITTER", fields.Critter),
        fields => ("UNIT_FIELD_CHARMEDBY", fields.CharmedBy),
        fields => ("UNIT_FIELD_SUMMONEDBY", fields.SummonedBy),
        fields => ("UNIT_FIELD_CREATEDBY", fields.CreatedBy),
        fields => ("UNIT_FIELD_DEMON_CREATOR", fields.DemonCreator),
        fields => ("UNIT_FIELD_TARGET", fields.Target),
    };
    
    public static IEnumerable<KeyValuePair<string, long>> Ints(this UpdateValues update)
    {
        if (update.ValuesCase == UpdateValues.ValuesOneofCase.Legacy)
        {
            foreach (var pair in update.Legacy.Ints)
                yield return pair;
            yield break;
        }

        var fields = update.Fields;
        
        foreach (var getter in objectIntGetters)
        {
            var pair = getter(fields);
            if (pair.Item2.HasValue)
                yield return new KeyValuePair<string, long>(pair.Item1, pair.Item2.Value);
        }
        
        if (fields.Unit != null)
        {
            foreach (var getter in intGetters)
            {
                var pair = getter(fields.Unit);
                if (pair.Item2.HasValue)
                    yield return new KeyValuePair<string, long>(pair.Item1, pair.Item2.Value);
            }
        }
    }
    
    public static IEnumerable<KeyValuePair<string, float>> Floats(this UpdateValues update)
    {
        if (update.ValuesCase == UpdateValues.ValuesOneofCase.Legacy)
        {
            foreach (var pair in update.Legacy.Floats)
                yield return pair;
            yield break;
        }

        var fields = update.Fields;
        
        if (fields.Scale.HasValue)
            yield return new KeyValuePair<string, float>("OBJECT_FIELD_SCALE_X", fields.Scale.Value);
        
        if (fields.Unit != null)
        {
            foreach (var getter in floatGetters)
            {
                var pair = getter(fields.Unit);
                if (pair.Item2.HasValue)
                    yield return new KeyValuePair<string, float>(pair.Item1, pair.Item2.Value);
            }
        }
    }
    
    public static IEnumerable<KeyValuePair<string, UniversalGuid>> Guids(this UpdateValues update)
    {
        if (update.ValuesCase == UpdateValues.ValuesOneofCase.Legacy)
        {
            foreach (var pair in update.Legacy.Guids)
                yield return pair;
            yield break;
        }

        var fields = update.Fields;

        if (fields.Unit != null)
        {
            foreach (var getter in guidGetters)
            {
                var pair = getter(fields.Unit);
                if (pair.Item2 != null)
                    yield return new KeyValuePair<string, UniversalGuid>(pair.Item1, pair.Item2);
            }
        }
    }
}