using Avalonia.Media;

namespace WDE.Common.Avalonia.Utils.NiceColorGenerator
{
    public interface IColorGenerator
    {
        Color GetNext();
        void Reset();
    }
}