using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public abstract class LightParam<T>
{
    public readonly uint Id;
    public readonly uint Count;
    public readonly Time[] Times;
    public readonly T[] Values;

    public T GetAtTime(Time time)
    {
        if (Count == 1)
            return Values[0];
        if (Count == 0)
            return default;

        int higherThan = -1;
        int lowerThan = -1;

        for (int i = 0; i < Count; ++i)
        {
            if (Times[i] == time)
                return Values[i];
            if (higherThan == -1 && Times[i] > time)
                higherThan = i;
            if (lowerThan == -1 && Times[Count - i - 1] < time)
                lowerThan = (int)Count - i - 1;
        }
        if (lowerThan == -1)
            lowerThan = (int)Count - 1;
        if (higherThan == -1)
            higherThan = 0;
            
        var diff = Times[higherThan] - Times[lowerThan];
        var timeToLower = time - Times[lowerThan];
        var timeToHigher = Times[higherThan] - time;

        var colorHigher = Values[higherThan];
        var colorLower = Values[lowerThan];

        return Lerp(colorLower, colorHigher, (float)timeToLower.TotalMinutes / diff.TotalMinutes);
    }

    protected abstract T Lerp(T colorLower, T colorHigher, float t);

    public LightParam(IDbcIterator dbcIterator, Func<IDbcIterator, int, T> reader)
    {
        int i = 0;
        Id = dbcIterator.GetUInt(i++);
        Count = dbcIterator.GetUInt(i++);
        Times = new Time[Count];
        Values = new T[Count];
        for (int j = 0; j < Count; j++)
            Times[j] = Time.FromHalfMinutes((int)dbcIterator.GetUInt(i++));
        i += (int)(16 - Count);
        for (int j = 0; j < Count; j++)
            Values[j] = reader(dbcIterator, i++);
    }

    protected LightParam()
    {
        Count = 0;
    }
    
    protected LightParam(T[] values, Time[] times)
    {
        Count = (uint)values.Length;
        Values = values;
        Times = times;
    }
}