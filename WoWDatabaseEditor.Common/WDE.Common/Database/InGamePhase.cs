using System;

namespace WDE.Common.Database;

[Flags]
public enum InGamePhase : uint
{
    Phase1 = 1,
    Phase2 = 2,
    Phase3 = 4,
    Phase4 = 8,
    Phase5 = 16,
    Phase6 = 32,
    Phase7 = 64,
    Phase8 = 128,
    Phase9 = 256,
    Phase10 = 512,
    Phase11 = 1024,
    Phase12 = 2048,
    Phase13 = 4096,
    Phase14 = 8192,
    Phase15 = 16384,
    Phase16 = 32768,
    Phase17 = 65536,
    Phase18 = 131072,
    Phase19 = 262144,
    Phase20 = 524288,
    Phase21 = 1048576,
    Phase22 = 2097152,
    Phase23 = 4194304,
    Phase24 = 8388608,
    Phase25 = 16777216,
    Phase26 = 33554432,
    Phase27 = 67108864,
    Phase28 = 134217728,
    Phase29 = 268435456,
    Phase30 = 536870912,
    Phase31 = 1073741824,
    Phase32 = 2147483648
}