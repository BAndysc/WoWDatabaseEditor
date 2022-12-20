using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class ChrRaces
{
    public readonly uint Id;
    // public readonly int Flags;
    // public readonly int FactionID;
    // public readonly int ExplorationSoundID;
    // public readonly int MaleDisplayId;
    // public readonly int FemaleDisplayId;
    public readonly string ClientPrefix;
    // public readonly int BaseLanguage;
    // public readonly int CreatureType;
    // public readonly int ResSicknessSpellID;
    // public readonly int SplashSoundID;
    // public readonly string ClientFilestring;
    // public readonly int CinematicSequenceID;
    // public readonly int Alliance;
    // public readonly string Name_Lang;
    // public readonly string Name_Female_Lang;
    // public readonly string Name_Male_Lang;
    // public readonly string FacialHairCustomization1;
    // public readonly string FacialHairCustomization2;
    // public readonly string HairCustomization;
    // public readonly int Required_Expansion;

    public ChrRaces(IDbcIterator dbcIterator)
    {
        Id = dbcIterator.GetUInt(0);
        // Flags = dbcIterator.GetInt(1);
        // FactionID = dbcIterator.GetInt(2);
        // ExplorationSoundID = dbcIterator.GetInt(3);
        // MaleDisplayId = dbcIterator.GetInt(4);
        // FemaleDisplayId = dbcIterator.GetInt(5);
        ClientPrefix = dbcIterator.GetString(6);
        // BaseLanguage = dbcIterator.GetInt(7);
        // CreatureType = dbcIterator.GetInt(8);
        // ResSicknessSpellID = dbcIterator.GetInt(9);
        // SplashSoundID = dbcIterator.GetInt(10);
        // ClientFilestring = dbcIterator.GetString(11);
        // CinematicSequenceID = dbcIterator.GetInt(12);
        // Alliance = dbcIterator.GetInt(13);
        // TODO : Name_Lang = dbcIterator.GetString(1);
        // TODO : Name_Female_Lang = dbcIterator.GetString(1);
        // TODO : Name_Male_Lang = dbcIterator.GetString(1);
        // TODO : FacialHairCustomization1 = dbcIterator.GetString(1);
        // TODO : FacialHairCustomization2 = dbcIterator.GetString(1);
        // TODO : HairCustomization = dbcIterator.GetString(1);
        // TODO : Required_Expansion = dbcIterator.GetInt(1);
    }
    
    public ChrRaces(IWdcIterator dbcIterator)
    {
        Id = (uint)dbcIterator.Id;
        ClientPrefix = dbcIterator.GetString("ClientPrefix");
    }

    private ChrRaces()
    {
        Id = 0;
        // Flags = 0;
        // FactionID = 0;
        // ExplorationSoundID = 0;
        // MaleDisplayId = 0;
        // FemaleDisplayId = 0;
        ClientPrefix = "";
        // BaseLanguage = 0;
        // CreatureType = 0;
        // ResSicknessSpellID = 0;
        // SplashSoundID = 0;
        // ClientFilestring = "";
        // CinematicSequenceID = 0;
        // Alliance = 0;
        // Name_Lang = "";
        // Name_Female_Lang = "";
        // Name_Male_Lang = "";
        // FacialHairCustomization1 = "";
        // FacialHairCustomization2 = "";
        // HairCustomization = "";
        // Required_Expansion = 0;
    }

    public static ChrRaces Empty => new ChrRaces();
}