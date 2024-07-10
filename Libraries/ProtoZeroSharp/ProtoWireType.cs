namespace ProtoZeroSharp;

internal enum ProtoWireType
{
    VarInt = 0,
    Fixed64 = 1,
    Length = 2,
    StartGroup = 3,
    EndGroup = 4,
    Fixed32 = 5
}