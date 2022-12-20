using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC
{
    public class AreaTrigger
    {
        public readonly int Id;
        public readonly int ContinentId;
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly uint PhaseUseFlags;
        public readonly uint PhaseId;
        public readonly uint PhaseGroup;
        public readonly float Radius;
        public readonly float BoxLength;
        public readonly float BoxWidth;
        public readonly float BoxHeight;
        public readonly float BoxYaw;

        public AreaTriggerShape Shape => Radius > 0 ? AreaTriggerShape.Sphere : AreaTriggerShape.Box;
        
        public AreaTrigger(IDbcIterator dbcIterator, GameFilesVersion version)
        {
            Id = dbcIterator.GetInt(0);
            ContinentId = dbcIterator.GetInt(1);
            X = dbcIterator.GetFloat(2);
            Y = dbcIterator.GetFloat(3);
            Z = dbcIterator.GetFloat(4);
            int offset = version == GameFilesVersion.Cataclysm_4_3_4 ? 3 : 0;
            if (version == GameFilesVersion.Cataclysm_4_3_4)
            {
                PhaseUseFlags = dbcIterator.GetUInt(5);
                PhaseId = dbcIterator.GetUInt(6);
                PhaseGroup = dbcIterator.GetUInt(7);
            }
            Radius = dbcIterator.GetFloat(5 + offset);
            BoxLength = dbcIterator.GetFloat(6 + offset);
            BoxWidth = dbcIterator.GetFloat(7 + offset);
            BoxHeight = dbcIterator.GetFloat(8 + offset);
            BoxYaw = dbcIterator.GetFloat(9 + offset);
        }

        public AreaTrigger(IWdcIterator dbcIterator)
        {
            Id = dbcIterator.Id;
            ContinentId = dbcIterator.GetShort("ContinentID");
            X = dbcIterator.GetFloat("Pos", 0);
            Y = dbcIterator.GetFloat("Pos", 1);
            Z = dbcIterator.GetFloat("Pos", 2);
            PhaseUseFlags = dbcIterator.GetByte("PhaseUseFlags");
            PhaseId = (uint)dbcIterator.GetShort("PhaseID");
            PhaseGroup = (uint)dbcIterator.GetShort("PhaseGroupID");
            Radius = dbcIterator.GetFloat("Radius");
            BoxLength = dbcIterator.GetFloat("Box_length");
            BoxWidth = dbcIterator.GetFloat("Box_width");
            BoxHeight = dbcIterator.GetFloat("Box_height");
            BoxYaw = dbcIterator.GetFloat("Box_yaw");
        }
        
        private AreaTrigger()
        {
            Id = -1;
            ContinentId = -1;
            X = 0;
            Y = 0;
            Z = 0;
            Radius = 0;
            BoxLength = 0;
            BoxWidth = 0;
            BoxHeight = 0;
            BoxYaw = 0;
        }

        public static AreaTrigger Empty => new AreaTrigger();
    }
}
