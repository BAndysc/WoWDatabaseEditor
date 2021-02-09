using WDE.Module.Attributes;

namespace WDE.Common.Types
{
    [UniqueProvider]
    public interface INativeTextDocument
    {
        string ToString();
        void FromString(string str);

        void Append(string str);
    }
}