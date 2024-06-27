namespace WowPacketParser.Proto.Processing
{
    public interface IPacketProcessor<T>
    {
        void Initialize(ulong gameBuild) { }
        T? Process(ref readonly PacketHolder packet);
    }
}
