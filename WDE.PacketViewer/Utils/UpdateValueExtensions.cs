using System.Collections.Generic;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.Utils;

public static unsafe class UpdateValueExtensions
{
    private static int? Unpack(Google.Protobuf.WellKnownTypes.Int32Value* ptr) => ptr != null ? ptr->Value : null;
    private static long? Unpack(Google.Protobuf.WellKnownTypes.Int64Value* ptr) => ptr != null ? ptr->Value : null;
    private static uint? Unpack(Google.Protobuf.WellKnownTypes.UInt32Value* ptr) => ptr != null ? ptr->Value : null;
    private static ulong? Unpack(Google.Protobuf.WellKnownTypes.UInt64Value* ptr) => ptr != null ? ptr->Value : null;
    private static float? Unpack(Google.Protobuf.WellKnownTypes.FloatValue* ptr) => ptr != null ? ptr->Value : null;
    private static double? Unpack(Google.Protobuf.WellKnownTypes.DoubleValue* ptr) => ptr != null ? ptr->Value : null;
    private static bool? Unpack(Google.Protobuf.WellKnownTypes.BoolValue* ptr) => ptr != null ? ptr->Value : null;
    private static UniversalGuid? Unpack(UniversalGuid* ptr) => ptr != null ? *ptr : null;

    private static long? GetInt(this UpdateValuesObjectDataFields fields, string field)
    {
        if (field == "OBJECT_FIELD_ENTRY")
            return Unpack(fields.EntryID);
        
        if (field == "OBJECT_DYNAMIC_FLAGS")
            return Unpack(fields.DynamicFlags);
        
        if (fields.Unit != null)
        {
            foreach (var getter in intGetters)
            {
                var pair = getter(ref *fields.Unit);
                if (pair.Item1 == field)
                    return pair.Item2;
            }
        }

        return null;
    }
    
    private static float? GetFloat(this UpdateValuesObjectDataFields fields, string field)
    {
        if (field == "OBJECT_FIELD_SCALE_X")
            return Unpack(fields.Scale);
        
        if (fields.Unit != null)
        {
            foreach (var getter in floatGetters)
            {
                var pair = getter(ref *fields.Unit);
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
                var pair = getter(ref *fields.Unit);
                if (pair.Item1 == field)
                    return pair.Item2;
            }
        }

        return null;
    }

    public static bool TryGetFloat(this UpdateValues update, string field, out float value)
    {
        if (update.ValuesCase == UpdateValues.ValuesOneofCase.Legacy)
            return update.Legacy.Floats.TryGetValue(field, out value);

        var val = GetFloat(update.Fields, field);
        value = val ?? 0;
        return val.HasValue;
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

    public static long? GetIntOrNull(this UpdateValues update, string field)
    {
        if (update.TryGetInt(field, out var v))
            return v;
        return null;
    }

    public static bool TryGetGuid(this UpdateValues update, string field, out UniversalGuid value)
    {
        if (update.ValuesCase == UpdateValues.ValuesOneofCase.Legacy)
            return update.Legacy.Guids.TryGetValue(field, out value);
        
        var val = GetGuid(update.Fields, field);
        value = val ?? default;
        return val != null;
    }
    
    private static IEnumerable<System.Func<UpdateValuesObjectDataFields, (string, long?)>> objectIntGetters = new System.Func<UpdateValuesObjectDataFields, (string, long?)>[]
    {
        fields => ("OBJECT_DYNAMIC_FLAGS", Unpack(fields.DynamicFlags)),
        fields => ("OBJECT_FIELD_ENTRY", Unpack(fields.EntryID))
    };

    public delegate (string, long?) IntGetterType(ref UpdateValuesUnitDataFields fields);
    public delegate (string, float?) FloatGetterType(ref UpdateValuesUnitDataFields fields);
    public delegate (string, UniversalGuid?) GuidGetterType(ref UpdateValuesUnitDataFields fields);

    private static IEnumerable<IntGetterType> intGetters = new IntGetterType[]
    {
        (ref UpdateValuesUnitDataFields fields) =>
        {
            var race = Unpack(fields.Race) ?? (byte)0;
            var @class = Unpack(fields.ClassId) ?? (byte)0;
            var gender = Unpack(fields.Sex) ?? (byte)0;
            var total = (race | (@class << 8) | (gender << 24));
            return ("UNIT_FIELD_BYTES_0", fields.Race != null || fields.ClassId  != null || fields.Sex != null ? total : null);
        },
        (ref UpdateValuesUnitDataFields fields) =>
        {
            var standState = Unpack(fields.StandState) ?? (byte)0;
            var petTalents = Unpack(fields.PetTalentPoints) ?? (byte)0;
            var visFlags = Unpack(fields.VisFlags) ?? (byte)0;
            var animTier = Unpack(fields.AnimTier) ?? (byte)0;
            var total = (standState | (petTalents << 8) | (visFlags << 16) | (animTier << 24));
            return ("UNIT_FIELD_BYTES_1", fields.StandState != null | fields.PetTalentPoints != null | fields.VisFlags != null | fields.AnimTier != null ? total : null);
        },
        (ref UpdateValuesUnitDataFields fields) =>
        {
            var sheathState = Unpack(fields.SheatheState) ?? (byte)0;
            var pvpFlags = Unpack(fields.PvpFlags) ?? (byte)0;
            var petFlags = Unpack(fields.PetFlags) ?? (byte)0;
            var shapeshiftForm = Unpack(fields.ShapeshiftForm) ?? (byte)0;
            var total = (sheathState | (pvpFlags << 8) | (petFlags << 16) | (shapeshiftForm << 24));
            return ("UNIT_FIELD_BYTES_2", fields.SheatheState != null | fields.PvpFlags != null | fields.PetTalentPoints != null | fields.ShapeshiftForm != null ? total : null);
        },
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_DISPLAY_POWER", Unpack(fields.DisplayPower)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_HEALTH", Unpack(fields.Health)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_POWER", fields.Power.Count > 0 ? fields.Power[0].Value.Value : null),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_POWER + 1", fields.Power.Count > 1 ? fields.Power[1].Value.Value : null),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_MAXHEALTH", Unpack(fields.MaxHealth)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_MAXPOWER", fields.MaxPower.Count > 0 ? fields.MaxPower[0].Value.Value : null),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_MAXPOWER + 1", fields.MaxPower.Count > 1 ? fields.MaxPower[1].Value.Value : null),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_LEVEL", Unpack(fields.Level)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_FACTIONTEMPLATE", Unpack(fields.FactionTemplate)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_VIRTUAL_ITEM_SLOT_ID", fields.VirtualItems.Count > 0 ? fields.VirtualItems[0].ItemID : null),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_VIRTUAL_ITEM_SLOT_ID + 1", fields.VirtualItems.Count > 1 ? fields.VirtualItems[1].ItemID : null),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_VIRTUAL_ITEM_SLOT_ID + 2", fields.VirtualItems.Count > 2 ? fields.VirtualItems[2].ItemID : null),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_FLAGS", Unpack(fields.Flags)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_FLAGS_2", Unpack(fields.Flags2)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_FLAGS_3", Unpack(fields.Flags3)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_BASEATTACKTIME", fields.AttackRoundBaseTime.Count > 0 ? fields.AttackRoundBaseTime[0].Value.Value : null),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_RANGEDATTACKTIME", Unpack(fields.RangedAttackRoundBaseTime)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_DISPLAYID", Unpack(fields.DisplayID)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_NATIVEDISPLAYID", Unpack(fields.NativeDisplayID)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_MOUNTDISPLAYID", Unpack(fields.MountDisplayID)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_INTERACT_SPELLID", Unpack(fields.InteractSpellID)),
        // UNIT_FIELD_MINDAMAGE
        // UNIT_FIELD_MAXDAMAGE
        // UNIT_FIELD_MINOFFHANDDAMAGE
        // UNIT_FIELD_MAXOFFHANDDAMAGE
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_CREATED_BY_SPELL", Unpack(fields.CreatedBySpell)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_NPC_FLAGS", fields.NpcFlags.Count > 0 ? fields.NpcFlags[0].Value.Value : null),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_NPC_FLAGS + 1", fields.NpcFlags.Count > 1 ? fields.NpcFlags[1].Value.Value : null),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_NPC_EMOTESTATE", Unpack(fields.EmoteState)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_BASE_MANA", Unpack(fields.BaseMana)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_BASE_HEALTH", Unpack(fields.BaseHealth))
    };
    
    private static IEnumerable<FloatGetterType> floatGetters = new FloatGetterType[]
    {
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_COMBATREACH", Unpack(fields.CombatReach)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_BOUNDINGRADIUS", Unpack(fields.BoundingRadius)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_DISPLAY_SCALE", Unpack(fields.DisplayScale))
    };

    private static IEnumerable<GuidGetterType> guidGetters = new GuidGetterType[]
    {
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_CHARM", Unpack(fields.Charm)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_SUMMON", Unpack(fields.Summon)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_CRITTER", Unpack(fields.Critter)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_CHARMEDBY", Unpack(fields.CharmedBy)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_SUMMONEDBY", Unpack(fields.SummonedBy)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_CREATEDBY", Unpack(fields.CreatedBy)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_DEMON_CREATOR", Unpack(fields.DemonCreator)),
        (ref UpdateValuesUnitDataFields fields) => ("UNIT_FIELD_TARGET", Unpack(fields.Target)),
    };
    
    public static IEnumerable<KeyValuePair<string, long>> Ints(this UpdateValues update)
    {
        List<KeyValuePair<string, long>> result = new();
        if (update.ValuesCase == UpdateValues.ValuesOneofCase.Legacy)
        {
            update.Legacy.Ints.GetUnderlyingArrays(out var keys, out var values);
            for (int i = 0; i < update.Legacy.Ints.Length; ++i)
                result.Add(new KeyValuePair<string, long>(keys[i].ToString() ?? "", values[i]));
            return result;
        }

        var fields = update.Fields;
        
        foreach (var getter in objectIntGetters)
        {
            var pair = getter(fields);
            if (pair.Item2.HasValue)
                result.Add(new KeyValuePair<string, long>(pair.Item1, pair.Item2.Value));
        }
        
        if (fields.Unit != null)
        {
            foreach (var getter in intGetters)
            {
                var pair = getter(ref *fields.Unit);
                if (pair.Item2.HasValue)
                    result.Add(new KeyValuePair<string, long>(pair.Item1, pair.Item2.Value));
            }
        }

        return result;
    }
    
    public static IEnumerable<KeyValuePair<string, float>> Floats(this UpdateValues update)
    {
        List<KeyValuePair<string, float>> result = new();
        if (update.ValuesCase == UpdateValues.ValuesOneofCase.Legacy)
        {
            update.Legacy.Floats.GetUnderlyingArrays(out var keys, out var values);
            for (int i = 0; i < update.Legacy.Floats.Length; ++i)
                result.Add(new KeyValuePair<string, float>(keys[i].ToString() ?? "", values[i]));
            return result;
        }

        var fields = update.Fields;
        
        if (fields.Scale != null)
            result.Add(new KeyValuePair<string, float>("OBJECT_FIELD_SCALE_X", fields.Scale->Value));
        
        if (fields.Unit != null)
        {
            foreach (var getter in floatGetters)
            {
                var pair = getter(ref *fields.Unit);
                if (pair.Item2.HasValue)
                    result.Add(new KeyValuePair<string, float>(pair.Item1, pair.Item2.Value));
            }
        }

        return result;
    }
    
    public static IEnumerable<KeyValuePair<string, UniversalGuid>> Guids(this UpdateValues update)
    {
        List<KeyValuePair<string, UniversalGuid>> result = new();
        if (update.ValuesCase == UpdateValues.ValuesOneofCase.Legacy)
        {
            update.Legacy.Guids.GetUnderlyingArrays(out var keys, out var values);
            for (int i = 0; i < update.Legacy.Guids.Length; ++i)
                result.Add(new KeyValuePair<string, UniversalGuid>(keys[i].ToString() ?? "", values[i]));
            return result;
        }

        var fields = update.Fields;

        if (fields.Unit != null)
        {
            foreach (var getter in guidGetters)
            {
                var pair = getter(ref *fields.Unit);
                if (pair.Item2 != null)
                    result.Add(new KeyValuePair<string, UniversalGuid>(pair.Item1, pair.Item2 ?? default));
            }
        }

        return result;
    }
}