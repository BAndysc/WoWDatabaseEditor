namespace WowPacketParser.PacketStructures
{
    public class StructureVersion
    {
        /*
         * Everytime you change structures.proto, please increment this version
         * so that user could know if one needs to reparse the sniff
         */
        public static readonly ulong MinProtobufStructureVersion = 20;
        public static readonly ulong MaxProtobufStructureVersion = 21;
    }
}
