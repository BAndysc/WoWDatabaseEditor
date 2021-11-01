// Original code by https://github.com/Thenarden/nmpq 
// Apache-2.0 License
using WDE.Common.MPQ;

namespace WDE.MPQ
{
    class MpqUserDataHeader : IMpqUserDataHeader
    {
        public bool HasUserData { get; set; }
        public int ArchiveOffset { get; set; }
        public byte[]? UserData { get; set; }
        public int UserDataReservedSize { get; set; }
        public int UserDataSize { get; set; }
    }
}