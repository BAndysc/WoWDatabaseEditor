using System;
using System.Collections.Generic;
using System.Globalization;
using ProtoZeroSharp;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.Utils
{
    public static class GuidExtensions
    {
        public static bool IsEmpty(this ref readonly UniversalGuid guid)
        {
            if (guid.KindCase == UniversalGuid.KindOneofCase.Guid64)
                return guid.Guid64.Low == 0 && guid.Guid64.High == 0;

            if (guid.KindCase == UniversalGuid.KindOneofCase.Guid128)
                return guid.Guid128.Low == 0 && guid.Guid128.High == 0;

            return true;
        }

        public static bool IsEmpty(this ref readonly Optional<UniversalGuid> guid)
        {
            if (!guid.HasValue)
                return true;

            return guid.Value.IsEmpty();
        }
        
        public static string ToHexString(this ref readonly UniversalGuid guid)
        {
            if (guid.KindCase == UniversalGuid.KindOneofCase.Guid64)
                return $"0x" + guid.Guid64.High.ToString("X8") + guid.Guid64.Low.ToString("X8");
            
            if (guid.KindCase == UniversalGuid.KindOneofCase.Guid128)
                return $"0x" + guid.Guid128.High.ToString("X16") + guid.Guid128.Low.ToString("X16");
            
            return "0x0";
        }

        public static string ToHexWithTypeString(this ref readonly UniversalGuid guid)
        {
            return $"{guid.ToHexString()}/{guid.Entry}/{guid.Type}";
        }

        public static bool TryParseGuid(this string str, uint entry, UniversalHighGuid type, out UniversalGuid? guid)
        {
            guid = null;
            if (!str.StartsWith("0x"))
                return false;

            if (str.Length == 34)
            {
                var high = str.Substring(2, 16);
                var low = str.Substring(18, 16);

                if (ulong.TryParse(low, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var lowNum) &&
                    ulong.TryParse(high, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var highNum))
                {
                    guid = new UniversalGuid()
                    {
                        Entry = entry,
                        Type = type,
                        Guid128 = new UniversalGuid128() { Low = lowNum, High = highNum }
                    };
                    return true;
                }
            }
            else if (str.Length == 18)
            {
                var high = str.Substring(2, 8);
                var low = str.Substring(10, 8);

                if (ulong.TryParse(low, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var lowNum) &&
                    ulong.TryParse(high, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var highNum))
                {
                    guid = new UniversalGuid()
                    {
                        Entry = entry,
                        Type = type,
                        Guid64 = new UniversalGuid64() { Low = lowNum, High = highNum }
                    };
                    return true;
                }
            }

            return false;
        }
        
        public static IEnumerable<UniversalGuid> StringToGuids(this IEnumerable<string> strings)
        {
            foreach (var s in strings)
            {
                var parts = s.Split("/");
                if (parts.Length != 3)
                    continue;

                if (!uint.TryParse(parts[1], out var entry))
                    continue;

                if (!Enum.TryParse<UniversalHighGuid>(parts[2], out var type))
                    continue;

                if (!parts[0].TryParseGuid(entry, type, out var guid))
                    continue;
                
                yield return guid!.Value;
            }
        }

        public static uint GetLow(this ref readonly UniversalGuid guid)
        {
            if (guid.KindCase == UniversalGuid.KindOneofCase.Guid128)
                return (uint)(guid.Guid128.Low & 0xFFFFFFFFFF);
            else if (guid.KindCase == UniversalGuid.KindOneofCase.Guid64)
            {
                switch (guid.Type)
                {
                    case UniversalHighGuid.Player:
                    case UniversalHighGuid.DynamicObject:
                    case UniversalHighGuid.RaidGroup:
                    case UniversalHighGuid.Item:
                        return (uint)(guid.Guid64.Low & 0x000FFFFFFFFFFFFF);
                    case UniversalHighGuid.GameObject:
                    case UniversalHighGuid.Transport:
                    //case HighGuidType.MOTransport: ??
                    case UniversalHighGuid.Vehicle:
                    case UniversalHighGuid.Creature:
                    case UniversalHighGuid.Pet:
                        return (uint)(guid.Guid64.Low & 0x00000000FFFFFFFFul);
                }

                return (uint)(guid.Guid64.Low & 0x00000000FFFFFFFFul);
            }

            return 0;
        }

        public static uint GetEntry(this ref readonly UniversalGuid128 guid)
        {
            return (uint)((guid.High >> 6) & 0x7FFFFF);
        }

        public static byte GetSubType(this ref readonly UniversalGuid128 guid) => (byte)(guid.High & 0x3F);

        public static ushort GetRealmId(this ref readonly UniversalGuid128 guid) => (ushort)((guid.High >> 42) & 0x1FFF);

        public static uint GetServerId(this ref readonly UniversalGuid128 guid) => (uint)((guid.Low >> 40) & 0xFFFFFF);

        public static ushort GetMapId(this ref readonly UniversalGuid128 guid) => (ushort)((guid.High >> 29) & 0x1FFF);

        public static bool HasEntry(this ref readonly UniversalGuid guid)
        {
            switch (guid.Type)
            {
                case UniversalHighGuid.Creature:
                case UniversalHighGuid.GameObject:
                case UniversalHighGuid.Pet:
                case UniversalHighGuid.Vehicle:
                case UniversalHighGuid.AreaTrigger:
                    return true;
                default:
                    return false;
            }
        }

        public static string ToWowParserString(this in UniversalGuid guid)
        {
            if (guid.KindCase == UniversalGuid.KindOneofCase.Guid128 && (guid.Guid128.High != 0 || guid.Guid128.Low != 0))
            {
                if (guid.HasEntry())
                {
                    // ReSharper disable once UseStringInterpolation
                    return string.Format("Full: 0x{0:X16}{1:X16} {2}/{3} R{4}/S{5} Map: {6} Entry: {7} Low: {8}", guid.Guid128.High, guid.Guid128.Low,
                        guid.Type, guid.Guid128.GetSubType(), guid.Guid128.GetRealmId(), guid.Guid128.GetServerId(), guid.Guid128.GetMapId(),
                        guid.Entry, guid.GetLow());
                }

                // ReSharper disable once UseStringInterpolation
                return string.Format("Full: 0x{0:X16}{1:X16} {2}/{3} R{4}/S{5} Map: {6} Low: {7}", guid.Guid128.High, guid.Guid128.Low,
                    guid.Type, guid.Guid128.GetSubType(), guid.Guid128.GetRealmId(), guid.Guid128.GetServerId(), guid.Guid128.GetMapId(),
                    guid.GetLow());
            }
            else if (guid.KindCase == UniversalGuid.KindOneofCase.Guid64 && (guid.Guid64.High != 0 || guid.Guid64.Low != 0))
            {
                if (guid.HasEntry())
                {
                    return string.Format("Full: 0x{0:X8} Type: {1} Entry: {2} Low: {0}", guid.GetLow(), guid.Type, guid.Entry);
                }
                return string.Format("Full: 0x{0:X8} Type: {1} Low: {0}", guid.GetLow(), guid.Type);
            }

            return "Full: 0x0";
        }

        public static string ToWowParserString(this ref readonly Optional<UniversalGuid> guid)
        {
            if (!guid.HasValue)
                return "0x0 (null)";

            return guid.Value.ToWowParserString();
        }

        public static string ToWowParserString(this UniversalGuid? guid)
        {
            if (!guid.HasValue)
                return "0x0 (null)";

            var copy = guid.Value;

            return copy.ToWowParserString();
        }

        public static ushort GetMapId(this ref readonly UniversalGuid guid)
        {
            if (guid.KindCase == UniversalGuid.KindOneofCase.Guid128)
                return guid.Guid128.GetMapId();
            throw new Exception("Can't get Map from guid " + guid.ToWowParserString() + " because it's not a 128bit guid");
        }

        public static ushort TryGetMapId(this ref readonly UniversalGuid guid, ushort defaultValue)
        {
            if (guid.KindCase == UniversalGuid.KindOneofCase.Guid128)
                return guid.Guid128.GetMapId();
            return defaultValue;
        }
    }
}