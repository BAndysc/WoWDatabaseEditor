using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common;

namespace WDE.Blueprints
{
    public class BlueprintSolutionItem : ISolutionItem
    {
        public bool IsContainer => false;

        public ObservableCollection<ISolutionItem> Items => null;

        public string ExtraId => "";

        public bool IsExportable => false;
    }
}
