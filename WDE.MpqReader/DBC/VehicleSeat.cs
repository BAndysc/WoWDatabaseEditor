using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public ref struct VehicleSeat
{
    public readonly int Id;
    public readonly Vector3 AttachmentOffset;
    public readonly int RideAnimationLoop;

    public VehicleSeat(IDbcIterator dbcIterator, GameFilesVersion version)
    {
        Id = dbcIterator.GetInt(0);
        AttachmentOffset = new Vector3(dbcIterator.GetFloat(3), dbcIterator.GetFloat(4), dbcIterator.GetFloat(5));
        RideAnimationLoop = dbcIterator.GetInt(16);
    }

    public VehicleSeat(IWdcIterator dbcIterator, GameFilesVersion version)
    {
        Id = dbcIterator.Id;
        AttachmentOffset = new Vector3(dbcIterator.GetFloat("AttachmentOffset", 0),
            dbcIterator.GetFloat("AttachmentOffset", 1),
            dbcIterator.GetFloat("AttachmentOffset", 2));
        RideAnimationLoop = dbcIterator.GetShort("RideAnimLoop");
    }

    public VehicleSeat()
    {
        Id = -1;
    }

    public static VehicleSeat Empty => new VehicleSeat();
}