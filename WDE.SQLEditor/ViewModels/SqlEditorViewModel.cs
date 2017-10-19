using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WDE.SQLEditor.ViewModels
{
    public class SqlEditorViewModel : BindableBase
    {
        public string Code { get; set; }

        public SqlEditorViewModel(string sql)
        {
            Code = sql;
        }
    }
}
