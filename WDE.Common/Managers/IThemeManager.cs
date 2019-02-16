using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common.Managers
{
    public interface IThemeManager
    {
        Theme CurrentTheme { get; }
        void SetTheme(Theme theme);
        IEnumerable<Theme> Themes { get; }
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
