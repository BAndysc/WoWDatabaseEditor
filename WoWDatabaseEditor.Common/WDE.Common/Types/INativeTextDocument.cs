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

        void DisableUndo();
        
        void Append(string str);
    }
}