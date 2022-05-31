namespace WDE.MpqReader.DBC;

public struct Time : IComparable<Time>
{
    private int minutes;
        
    public Time(int hours, int minutes)
    {
        this.minutes = hours * 60 + minutes;
    }
        
    public int Hour => minutes / 60;
    public int Minute => minutes % 60;
    public int TotalMinutes => minutes;
    public int HalfHourInMinutes => (int)Math.Round((minutes / 30.0f)) * 30;

    public bool Equals(Time other)
    {
        return minutes == other.minutes;
    }

    public override bool Equals(object? obj)
    {
        return obj is Time other && Equals(other);
    }

    public override int GetHashCode()
    {
        return minutes;
    }

    public int CompareTo(Time other)
    {
        return minutes.CompareTo(other.minutes);
    }

    public static bool operator <(Time left, Time right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(Time left, Time right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(Time left, Time right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(Time left, Time right)
    {
        return left.CompareTo(right) >= 0;
    }

    public static bool operator ==(Time left, Time right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Time left, Time right)
    {
        return !left.Equals(right);
    }

    public static Time FromHalfMinutes(int minutes)
    {
        return new Time(0, minutes / 2);
    }
        
    public static Time FromMinutes(int minutes)
    {
        return new Time(0, minutes);
    }

    public static Time operator -(Time left, Time right)
    {
        var diff = left.minutes - right.minutes;
        if (diff < 0)
            diff += 1440;
        return new Time(0, diff);
    }
}