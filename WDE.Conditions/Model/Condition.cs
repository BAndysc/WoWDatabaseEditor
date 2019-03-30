using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Conditions.Model
{
    public class Condition
    {
        public int Type { get; set; }

        public int Target { get; set; }

        public int Value1 { get; set; }

        public int Value2 { get; set; }

        public int Value3 { get; set; }

        public bool Negative { get; set; }
    }
}
