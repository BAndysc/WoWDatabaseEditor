using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDE.ThemeChanger.Data
{
    public struct ThemeSettings
    {
        public string Name { get; }

        public ThemeSettings(string name)
        {
            Name = name;
        }
    }
}
