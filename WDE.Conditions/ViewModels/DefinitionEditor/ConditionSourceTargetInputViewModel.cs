using System;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Managers;
using WDE.Conditions.Data;

namespace WDE.Conditions.ViewModels
{
    public class ConditionSourceTargetInputViewModel: BindableBase, IDialog
    {
        public ConditionSourceTargetInputViewModel(in ConditionSourceTargetJsonData source)
        {
            Source = new ConditionSourceTargetData(in source);
            Save = new DelegateCommand(() => CloseOk?.Invoke());
        }

        public ConditionSourceTargetData Source { get; }
        public DelegateCommand Save { get; }
        
        public bool IsEmpty() => Source.IsEmpty();

        public int DesiredWidth { get; } = 340;
        public int DesiredHeight { get; } = 300;
        public string Title { get; } = "Source Target Input";
        public bool Resizeable { get; } = false;
        public event Action? CloseCancel;
        public event Action? CloseOk;
    }

    public class ConditionSourceTargetData
    {
        public string Description { get; set; }
        public string Comment { get; set; }

        public ConditionSourceTargetData(in ConditionSourceTargetJsonData source)
        {
            Description = source.Description;
            Comment = source.Comment;
        }

        public ConditionSourceTargetJsonData ToConditionSourceTargetJsonData()
        {
            var obj = new ConditionSourceTargetJsonData();
            obj.Description = Description;
            obj.Comment = Comment;
            return obj;
        }

        public bool IsEmpty() => string.IsNullOrWhiteSpace(Description) || string.IsNullOrWhiteSpace(Comment);
    }
}