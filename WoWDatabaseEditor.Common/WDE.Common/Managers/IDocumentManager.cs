using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using WDE.Common.Windows;
using WDE.Module.Attributes;

namespace WDE.Common.Managers
{
    [UniqueProvider]
    public interface IDocumentManager : INotifyPropertyChanged
    {
        IDocument ActiveDocument { get; set; }
        IReadOnlyList<ITool> AllTools { get; }
        ObservableCollection<IDocument> OpenedDocuments { get; }
        ObservableCollection<ITool> OpenedTools { get; }
        void OpenDocument(IDocument editor);
        void OpenTool<T>() where T : ITool;
        void OpenTool(Type toolType);
    }
}