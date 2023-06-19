using System;
using WDE.Common.Services;

namespace WDE.DbcStore.Spells;

public struct FixedUintArray2
{
    public uint A { get; set; }
    public uint B { get; set; }
    public uint this[int i]
    {
        get
        {
            if (i < 0 || i >= 2)
                throw new IndexOutOfRangeException();
            return i == 0 ? A : B;
        }
        set
        {
            if (i < 0 || i >= 2)
                throw new IndexOutOfRangeException();
            if (i == 0)
                A = value;
            else
                B = value;
        }
    }
}

public struct FixedUintArray3
{
    public uint A { get; set; }
    public uint B { get; set; }
    public uint C { get; set; }
    public uint this[int i]
    {
        get
        {
            if (i < 0 || i >= 3)
                throw new IndexOutOfRangeException();
            return i == 0 ? A : (i == 1 ? B : C);
        }
        set
        {
            if (i < 0 || i >= 3)
                throw new IndexOutOfRangeException();
            if (i == 0)
                A = value;
            else if (i == 1)
                B = value;
            else
                C = value;
        }
    }
}


public struct FixedEffectTypeArray3
{
    public SpellEffectType A { get; set; }
    public SpellEffectType B { get; set; }
    public SpellEffectType C { get; set; }
    public SpellEffectType this[int i]
    {
        get
        {
            if (i < 0 || i >= 3)
                throw new IndexOutOfRangeException();
            return i == 0 ? A : (i == 1 ? B : C);
        }
        set
        {
            if (i < 0 || i >= 3)
                throw new IndexOutOfRangeException();
            if (i == 0)
                A = value;
            else if (i == 1)
                B = value;
            else
                C = value;
        }
    }
}

public struct FixedFloatArray3
{
    public float A { get; set; }
    public float B { get; set; }
    public float C { get; set; }
    public float this[int i]
    {
        get
        {
            if (i < 0 || i >= 3)
                throw new IndexOutOfRangeException();
            return i == 0 ? A : (i == 1 ? B : C);
        }
        set
        {
            if (i < 0 || i >= 3)
                throw new IndexOutOfRangeException();
            if (i == 0)
                A = value;
            else if (i == 1)
                B = value;
            else
                C = value;
        }
    }
}