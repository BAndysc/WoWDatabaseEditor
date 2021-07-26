using System;
using WDE.Module.Attributes;

namespace WDE.Common.Types
{
    [UniqueProvider]
    public interface INativeTextDocument
    {
        IObservable<int> Length { get; }
        string ToString();
        void FromString(string str);

        void Append(string str);
    }
}