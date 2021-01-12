using System.IO;

namespace WDBXEditor.Reader.FileTypes
{
    public class WDBC : DBHeader
    {
        public override void ReadHeader(ref BinaryReader dbReader, string signature)
        {
            base.ReadHeader(ref dbReader, signature);
        }
    }
}