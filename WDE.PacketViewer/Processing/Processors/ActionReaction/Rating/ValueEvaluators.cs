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
        public static OneMinus From<T>(in T other) where T : IEvaluation
        {
            return new OneMinus()
            {
                Value = other.Value,
#if TRACING
                Explain = $"(1 - {other.Explain})"
#endif
            };
        }
        
        public float Value { get; private init; }
#if TRACING
        public string Explain { get; private init;}
#else
        public string Explain => "";
#endif
    }

    public struct Remap : IEvaluation
    {
        public static Remap From<T>(T value, float from, float to, float newFrom, float newTo) where T : IEvaluation
        {
            float t = (Math.Clamp(value.Value, from, to) - from) / (to - from);
#if TRACING
            string explain = null!;
            if (newFrom == 0 && newTo == 1)
                explain = $"remap01({value.Explain}, {from}, {to})";
            else
                explain = $"remap({value.Explain}, {from}, {to}, {newFrom}, {newTo})";
#endif
            return new Remap()
            {
                Value = newFrom + (newTo - newFrom) * t,
#if TRACING
                Explain = explain
#endif
            };
        }
        
        public float Value { get; private init; }
#if TRACING
        public string Explain { get; private init; }
#else
        public string Explain => "";
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
#else
        public string Explain => "";
#endif
    }
    
    public struct Power : IEvaluation
    {
        public static Power From<T>(T val, float exp) where T : IEvaluation
        {
            return new Power()
            {
                Value = (float)Math.Pow(val.Value, exp),
#if TRACING
                Explain = $"{val.Explain}^{exp}"
#endif
            };
        }
        
        public float Value { get; private init; }
#if TRACING
        public string Explain { get; private init; }
#else
        public string Explain => "";
#endif
    }

    public struct Weighted : IEvaluation 
    {
        public static Weighted From<T1, T2, T3, T4>((T1, float) c1, (T2, float) c2, (T3, float) c3, (T4, float) c4) where T1 : IEvaluation where T2 : IEvaluation where T3 : IEvaluation where T4 : IEvaluation
        {
            return new Weighted()
            {
                Value = c1.Item1.Value * c1.Item2 + c2.Item1.Value * c2.Item2 + c3.Item1.Value * c3.Item2 +
                        c4.Item1.Value * c4.Item2,
#if TRACING
                Explain = "(" + $"{c1.Item1!.Explain} * {c1.Item2}" + " + " + $"{c2.Item1!.Explain} * {c2.Item2}" +
                          " + " + $"{c3.Item1!.Explain} * {c3.Item2}" + " + " + $"{c4.Item1!.Explain} * {c4.Item2}" + ")"
#endif
            };
        }

        public float Value { get; private init; }
#if TRACING
        public string Explain { get; private init; }
#else
        public string Explain => "";
#endif
    }

    public struct Multiply : IEvaluation
    {
        public static Multiply From<T1>(in T1 other, float mult)  where T1 : IEvaluation
        {
            return new Multiply()
            {
                Value = other.Value * mult,
#if TRACING
                Explain = other.Explain + $" * {mult:0.00}"
#endif
            };
        }
        public float Value { get; private init; }
#if TRACING
        public string Explain { get; private init; }
#else
        public string Explain => "";
#endif
    }

    public struct FinalEvaluation : IEvaluation
    {
        public float Value { get; init; }
#if TRACING
        public string Explain { get; init; }
#else
        public string Explain => "";
#endif
        
        public static FinalEvaluation From<T>(in T t) where T : IEvaluation
        {
            return new FinalEvaluation()
            {
                Value = t.Value,
#if TRACING
                Explain = t.Explain,
#endif
            };
        }
    }
}