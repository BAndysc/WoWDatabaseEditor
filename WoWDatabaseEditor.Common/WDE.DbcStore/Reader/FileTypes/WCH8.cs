namespace WDBXEditor.Reader.FileTypes
{
    internal class WCH8 : WCH7
    {
        public WCH8()
        {
            StringTableOffset = 0x14;
            HeaderSize = 0x34;
        }

        public WCH8(string filename)
        {
            StringTableOffset = 0x14;
            HeaderSize = 0x34;
            FileName = filename;
        }
    }
}