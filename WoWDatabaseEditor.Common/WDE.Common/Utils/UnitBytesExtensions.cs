namespace WDE.Common.Utils;

public static class UnitBytesExtensions
{
    public static Bytes0 SplitBytes0(this long l, ulong gameBuild)
    {
        if (gameBuild >= 17359)
            return new Bytes0((byte)(l & 0xFF),
                (byte)((l >> 8) & 0xFF),
                (byte)((l >> 24) & 0xFF), 0);
        else
            return new Bytes0((byte)(l & 0xFF),
                (byte)((l >> 8) & 0xFF),
                (byte)((l >> 16) & 0xFF),
                (byte)((l >> 24) & 0xFF));
    }
    
    public static Bytes1 SplitBytes1(this long l)
    {
        return new Bytes1((byte)(l & 0xFF),
            (byte)((l >> 8) & 0xFF),
            (byte)((l >> 16) & 0xFF),
            (byte)((l >> 24) & 0xFF));
    }
    
    public static Bytes2 SplitBytes2(this long l)
    {
        return new Bytes2((byte)(l & 0xFF),
            (byte)((l >> 8) & 0xFF),
            (byte)((l >> 16) & 0xFF),
            (byte)((l >> 24) & 0xFF));
    }

    public static uint ToUint(this Bytes0 bytes, ulong gameBuild)
    {
        if (gameBuild >= 17359)
            return (uint)((bytes.gender << 24) | (bytes.@class << 8) | bytes.race);
        else
            return (uint)((bytes.powerType << 24) | (bytes.gender << 16) | (bytes.@class << 8) | bytes.race);
    }

    public static uint ToUint(this Bytes1 bytes)
    {
        return (uint)((bytes.animTier << 24) | (bytes.visFlags << 16) | (bytes.petTalents << 8) | bytes.standState);
    }

    public static uint ToUint(this Bytes2 bytes)
    {
        return (uint)((bytes.shapeshiftForm << 24) | (bytes.petFlags << 16) | (bytes.pvpFlag << 8) | bytes.sheathState);
    }
}

public struct Bytes0
{
    public readonly byte race;
    public readonly byte @class;
    public readonly byte gender;
    public readonly byte powerType;

    public Bytes0(byte race, byte @class, byte gender, byte powerType)
    {
        this.race = race;
        this.@class = @class;
        this.gender = gender;
        this.powerType = powerType;
    }
}

public struct Bytes1
{
    public readonly byte standState;
    public readonly byte petTalents;
    public readonly byte visFlags;
    public readonly byte animTier;

    public Bytes1(byte standState, byte petTalents, byte visFlags, byte animTier)
    {
        this.standState = standState;
        this.petTalents = petTalents;
        this.visFlags = visFlags;
        this.animTier = animTier;
    }
}

public struct Bytes2
{
    public readonly byte sheathState;
    public readonly byte pvpFlag;
    public readonly byte petFlags;
    public readonly byte shapeshiftForm;

    public Bytes2(byte sheathState, byte pvpFlag, byte petFlags, byte shapeshiftForm)
    {
        this.sheathState = sheathState;
        this.pvpFlag = pvpFlag;
        this.petFlags = petFlags;
        this.shapeshiftForm = shapeshiftForm;
    }
}