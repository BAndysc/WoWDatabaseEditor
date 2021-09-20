// enable this define to enable Explain property to check where does the value come from
//#define TRACING
using System;
using System.Linq;
namespace WDE.PacketViewer.Processing.Processors.ActionReaction.Rating
{
    public interface IEvaluation
    {
        float Value { get; }
#if TRACING
        string Explain { get; }
#else
        string Explain => "";
#endif
    }
    
    public struct OneMinus : IEvaluation
    {
        public OneMinus(IEvaluation other)
        {
            Value = 1 - other.Value;
#if TRACING
            Explain = $"(1 - {other.Explain})";
#endif
        }

        public float Value { get; }
#if TRACING
        public string Explain { get; }
#endif
    }

    public struct Remap : IEvaluation
    {
        public Remap(IEvaluation value, float from, float to, float newFrom, float newTo)
        {
            float t = (Math.Clamp(value.Value, from, to) - from) / (to - from);
            Value = newFrom + (newTo - newFrom) * t;
#if TRACING
            if (newFrom == 0 && newTo == 1)
                Explain = $"remap01({value.Explain}, {from}, {to})";
            else
                Explain = $"remap({value.Explain}, {from}, {to}, {newFrom}, {newTo})";
#endif
        }
        
        public float Value { get; }
#if TRACING
        public string Explain { get; }
#endif
    }

    public struct Const : IEvaluation
    {
        public static Const One { get; } = new(1);
        public static Const Zero { get; } = new(0);
        public static Const Half { get; } = new(0.5f);
        
        public Const(float val)
        {
            Value = val;
#if TRACING
            Explain = $"{val:0.00}";
#endif
        }
        
        public float Value { get; }
#if TRACING
        public string Explain { get; }
#endif
    }
    
    public struct Power : IEvaluation
    {
        public Power(IEvaluation val, float exp)
        {
            Value = (float)Math.Pow(val.Value, exp);
#if TRACING
            Explain = $"{val.Explain}^{exp}";
#endif
        }
        
        public float Value { get; }
#if TRACING
        public string Explain { get; }
#endif
    }

    public struct Weighted : IEvaluation
    {
#if TRACING
        public Weighted(params (IEvaluation?, float)[] coefs)
        {
            Value = 0;
            foreach (var coef in coefs)
            {
                if (coef.Item1 == null)
                    continue;
                Value += coef.Item2 * coef.Item1.Value;
            }

            Explain = "(" + string.Join(" + ", coefs.Where(p => p.Item1 != null).Select(pair => $"{pair.Item1!.Explain} * {pair.Item2}")) + ")";
        }
#else
        // non alloc version
        public Weighted((IEvaluation?, float) a, (IEvaluation?, float) b, (IEvaluation?, float) c, (IEvaluation?, float) d)
        {
            Value = 0;
            if (a.Item1 != null)
                Value += a.Item2 * a.Item1.Value;
            if (b.Item1 != null)
                Value += b.Item2 * b.Item1.Value;
            if (c.Item1 != null)
                Value += c.Item2 * c.Item1.Value;
            if (d.Item1 != null)
                Value += d.Item2 * d.Item1.Value;
        }
#endif

        public float Value { get; }
#if TRACING
        public string Explain { get; }
#endif
    }

    public struct Multiply : IEvaluation
    {
        public Multiply(IEvaluation other, float mult)
        {
            Value = other.Value * mult;
#if TRACING
            Explain = other.Explain + $" * {mult:0.00}";
#endif
        }
        public float Value { get; }
#if TRACING
        public string Explain { get; }
#endif
    }
}