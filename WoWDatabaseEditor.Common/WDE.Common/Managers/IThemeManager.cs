using System.Collections.Generic;

namespace WDE.Common.Managers
{
    public interface IThemeManager
    {
        Theme CurrentTheme { get; }
        IEnumerable<Theme> Themes { get; }
        void SetTheme(Theme theme);
        void UpdateCustomScaling(double? value);
    }

    public struct Theme
    {
        public string Name { get; }

        public Theme(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}