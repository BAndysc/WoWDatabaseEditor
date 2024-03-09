
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;

public abstract class JsonBaseCreatureAddon : IBaseCreatureAddon
{
    public abstract uint PathId { get; set; }
    
    
    public uint Mount { get; set; }

    public abstract uint MountCreatureId { get; set; }
    public abstract byte StandState { get; }
    public abstract byte VisFlags { get; }
    public abstract byte AnimTier { get; }
    public abstract byte Sheath { get; }
    public abstract byte PvP { get; }

    
    public uint Emote { get; set; }

    
    public uint VisibilityDistanceType { get; set; }
    
    
    public string? Auras { get; set; }
}

public abstract class JsonBaseCreatureAddonTrinity : JsonBaseCreatureAddon
{
    public override byte Sheath => sheath;
    public override byte PvP => pvP;
    public override byte StandState => standState;
    public override byte VisFlags => visFlags;
    public override byte AnimTier => animTier;

    
    public byte standState { get; set; }
    
    
    public byte animTier { get; set; }
    
    
    public byte visFlags { get; set; }
    
    
    public byte sheath { get; set; }

    
    public byte pvP { get; set; }
}


public class JsonCreatureAddonWrath : JsonBaseCreatureAddonTrinity, ICreatureAddon
{
    
    
    
    public uint Guid { get; set; }
    
    
    public override uint PathId { get; set; }
    
    
    public override uint MountCreatureId { get; set; }
}


public class JsonCreatureTemplateAddon : JsonBaseCreatureAddonTrinity, ICreatureTemplateAddon
{
    
    
    
    public uint Entry { get; set; }
    
    
    public override uint PathId { get; set; }

    
    public override uint MountCreatureId { get; set; }
}


public class JsonCreatureAddonCata : JsonBaseCreatureAddonTrinity, ICreatureAddon
{
    
    
    
    public uint Guid { get; set; }
    
    
    public override uint PathId { get; set; }

    public override uint MountCreatureId { get; set; }
}


public class JsonCreatureTemplateAddonCata : JsonBaseCreatureAddonTrinity, ICreatureTemplateAddon
{
    
    
    
    public uint Entry { get; set; }
    
    
    public override uint PathId { get; set; }

    public override uint MountCreatureId { get; set; }
}


public class JsonCreatureAddonMaster : JsonBaseCreatureAddonTrinity, ICreatureAddon
{
    
    
    
    public uint Guid { get; set; }
    
    
    public override uint PathId { get; set; }

    
    public override uint MountCreatureId { get; set; }
}


public class JsonCreatureTemplateAddonMaster : JsonBaseCreatureAddonTrinity, ICreatureTemplateAddon
{
    
    
    
    public uint Entry { get; set; }
    
    
    public override uint PathId { get; set; }

    
    public override uint MountCreatureId { get; set; }
}


public class JsonCreatureAddonAC: JsonBaseCreatureAddon, ICreatureAddon
{
    
    
    
    public uint Guid { get; set; }
    
    
    public override uint PathId { get; set; }

    public override uint MountCreatureId { get; set; }
    public override byte StandState => (byte)(bytes1 & 0xFF);
    public override byte VisFlags => (byte)((bytes1 >> 16) & 0xFF);
    public override byte AnimTier => (byte)((bytes1 >> 24) & 0xFF);
    public override byte Sheath => (byte)(bytes2 & 0xFF);
    public override byte PvP => (byte)((bytes2 >> 8) & 0xFF);

    
    public uint bytes1 { get; set; }
    
    
    public uint bytes2 { get; set; }
}


public class JsonCreatureTemplateAddonAC : JsonBaseCreatureAddon, ICreatureTemplateAddon
{
    
    
    
    public uint Entry { get; set; }
    
    
    public override uint PathId { get; set; }

    public override uint MountCreatureId { get; set; }
    
    public override byte StandState => (byte)(bytes1 & 0xFF);
    public override byte VisFlags => (byte)((bytes1 >> 16) & 0xFF);
    public override byte AnimTier => (byte)((bytes1 >> 24) & 0xFF);
    public override byte Sheath => (byte)(bytes2 & 0xFF);
    public override byte PvP => (byte)((bytes2 >> 8) & 0xFF);

    
    public uint bytes1 { get; set; }
    
    
    public uint bytes2 { get; set; }
}