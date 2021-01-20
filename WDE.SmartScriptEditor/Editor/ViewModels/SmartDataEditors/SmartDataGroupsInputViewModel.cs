using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.Commands;
using WDE.Common.Managers;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class SmartDataGroupsInputViewModel: BindableBase, IDialog
    {
        public SmartDataGroupsInputViewModel(string Name)
        {
            this.Name = Name;
            Save = new DelegateCommand(() => CloseOk?.Invoke());
        }

        public string Name { get; set; }
        
        public DelegateCommand Save { get; }

        public int DesiredWidth { get; } = 340;
        public int DesiredHeight { get; } = 180;
        public string Title { get; } = "";
        public bool Resizeable { get; } = false;
        public event Action CloseCancel;
        public event Action CloseOk;
    }
}
