using System;
using System.Collections.Generic;
using System.Windows.Input;
using WDE.Common.Managers;

namespace WDE.Common.Menu
{
    public interface IMenuItem
    {
        string ItemName { get; }
    }

    public interface IMenuSeparator : IMenuItem
    {
        
    }

    public interface IMenuDocumentItem : IMenuItem
    {
        IDocument EditorDocument { get; }
    }

    public interface IMenuCommandItem : IMenuItem
    {
        ICommand ItemCommand { get; }
    }

    public interface IMenuCategoryItem: IMenuItem
    {
        List<IMenuItem> CategoryItems { get; }
    }
}
