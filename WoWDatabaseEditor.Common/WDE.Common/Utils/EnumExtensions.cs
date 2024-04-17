using System;
using System.Runtime.CompilerServices;
public static class EnumFlagExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasFlagFast<TEnum>(this TEnum lhs, TEnum rhs) where TEnum : unmanaged, Enum
    {
        unsafe
        {
            switch (sizeof(TEnum))
            {
                case 1:
                    return (*(byte*)(&lhs) & *(byte*)(&rhs)) > 0;
                case 2:
                    return (*(ushort*)(&lhs) & *(ushort*)(&rhs)) > 0;
                case 4:
                    return (*(uint*)(&lhs) & *(uint*)(&rhs)) > 0;
                case 8:
                    return (*(ulong*)(&lhs) & *(ulong*)(&rhs)) > 0;
                default:
                    throw new Exception("Size does not match a known Enum backing type.");
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasAllFlagsFast<TEnum>(this TEnum lhs, TEnum rhs) where TEnum : unmanaged, Enum
    {
        unsafe
        {
            switch (sizeof(TEnum))
            {
                case 1:
                    return (*(byte*)(&lhs) & *(byte*)(&rhs)) == *(byte*)(&rhs);
                case 2:
                    return (*(ushort*)(&lhs) & *(ushort*)(&rhs)) == *(ushort*)(&rhs);
                case 4:
                    return (*(uint*)(&lhs) & *(uint*)(&rhs)) == *(uint*)(&rhs);
                case 8:
                    return (*(ulong*)(&lhs) & *(ulong*)(&rhs)) == *(ulong*)(&rhs);
                default:
                    throw new Exception("Size does not match a known Enum backing type.");
            }
        }
    }
}