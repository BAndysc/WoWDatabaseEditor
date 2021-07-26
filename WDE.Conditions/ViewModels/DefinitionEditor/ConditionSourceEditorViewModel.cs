using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AsyncAwaitBestPractices.MVVM;
using Prism.Mvvm;
using Prism.Commands;
using WDE.Common.Annotations;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.Conditions.Data;

namespace WDE.Conditions.ViewModels
{
    public class ConditionSourceEditorViewModel: BindableBase, IDialog
    {
        private readonly IParameterFactory parameterFactory;
        private readonly IWindowManager windowManager;
        
        public ConditionSourceEditorViewModel(in ConditionSourcesJsonData source, IParameterFactory parameterFactory, IWindowManager windowManager)
        {
            this.parameterFactory = parameterFactory;
            this.windowManager = windowManager;
            
            Source = new ConditionSourceEditorData(in source);
            SelectedTargetIndex = -1;
            Save = new DelegateCommand(() => CloseOk?.Invoke());
            ClearEntryParam = new DelegateCommand(ClearSourceEntryParam);
            EditEntryParam = new AsyncCommand(EditSourceEntryParam);
            ClearGroupParam = new DelegateCommand(ClearSourceGroupParam);
            EditGroupParam = new AsyncCommand(EditSourceGroupParam);
            ClearSourceIdParam = new DelegateCommand(ClearSourceSourceIdParam);
            EditSourceIdParam = new AsyncCommand(EditSourceSourceIdParam);
            DeleteTarget = new DelegateCommand(DeleteSourceTarget);
            AddTarget = new AsyncCommand(AddTargetToSource);
            EditTarget = new AsyncCommand<ConditionSourceTargetJsonData?>(EditSourceTarget);
        }
        
        public ConditionSourceEditorData Source { get; }
        public int SelectedTargetIndex { get; set; }
        public DelegateCommand Save { get; }
        public DelegateCommand ClearEntryParam { get; }
        public AsyncCommand EditEntryParam { get; }
        public DelegateCommand ClearGroupParam { get; }
        public AsyncCommand EditGroupParam { get; }
        public DelegateCommand ClearSourceIdParam { get; }
        public AsyncCommand EditSourceIdParam { get; }
        public DelegateCommand DeleteTarget { get; }
        public AsyncCommand AddTarget { get; }
        public AsyncCommand<ConditionSourceTargetJsonData?> EditTarget { get; }

        private void ClearSourceEntryParam() => Source.UpdateDataParam(null, ConditionSourceParamUpdate.UpdateEntryParam);

        private void ClearSourceGroupParam() => Source.UpdateDataParam(null, ConditionSourceParamUpdate.UpdateGroupParam);

        private void ClearSourceSourceIdParam() => Source.UpdateDataParam(null, ConditionSourceParamUpdate.UpdateSourceIdParam);

        private Task EditSourceEntryParam() => OpenParamEditor(Source.EntryParam ?? new ConditionSourceParamsJsonData(), ConditionSourceParamUpdate.UpdateEntryParam);

        private Task EditSourceGroupParam() => OpenParamEditor(Source.GroupParam ?? new ConditionSourceParamsJsonData(), ConditionSourceParamUpdate.UpdateGroupParam);

        private Task EditSourceSourceIdParam() => OpenParamEditor(Source.SourceIdParam ?? new ConditionSourceParamsJsonData(), ConditionSourceParamUpdate.UpdateSourceIdParam);

        private async Task OpenParamEditor(ConditionSourceParamsJsonData item, ConditionSourceParamUpdate paramUpdate)
        {
            var vm = new ConditionsParameterEditorViewModel(in item, parameterFactory);
            if (await windowManager.ShowDialog(vm) && !vm.IsEmpty())
                Source.UpdateDataParam(vm.Source.ToConditionSourceParamsJsonData(), paramUpdate);
        }

        private void DeleteSourceTarget()
        {
            if (SelectedTargetIndex >= 0)
                Source.Targets.RemoveAt(SelectedTargetIndex);
        }

        private async Task AddTargetToSource()
        {
            await OpenTargetEditor(new ConditionSourceTargetJsonData(), true);
        }

        private async Task EditSourceTarget(ConditionSourceTargetJsonData? obj)
        {
            if (obj.HasValue)
                await OpenTargetEditor(obj.Value, false);
        }

        private async Task OpenTargetEditor(ConditionSourceTargetJsonData item, bool isCreating)
        {
            var vm = new ConditionSourceTargetInputViewModel(in item);
            if (await windowManager.ShowDialog(vm) && !vm.IsEmpty())
            {
                if (isCreating)
                    Source.Targets.Add(vm.Source.ToConditionSourceTargetJsonData());
                else
                {
                    if (SelectedTargetIndex >= 0)
                        Source.Targets[SelectedTargetIndex] = vm.Source.ToConditionSourceTargetJsonData();
                }
            }
        }
        
        public bool IsEmpty() => Source.IsEmpty();
        
        public int DesiredWidth { get; } = 473;
        public int DesiredHeight { get; } = 666;
        public string Title { get; } = "Condition Source Editor";
        public bool Resizeable { get; } = false;
        public event Action? CloseCancel;
        public event Action? CloseOk;
    }

    public enum ConditionSourceParamUpdate
    {
        UpdateGroupParam,
        UpdateEntryParam,
        UpdateSourceIdParam,
    }

    public class ConditionSourceEditorData: INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ConditionSourceParamsJsonData? GroupParam { get; private set; }
        public ConditionSourceParamsJsonData? EntryParam { get; private set; }
        public ConditionSourceParamsJsonData? SourceIdParam { get; private set; }
        public ObservableCollection<ConditionSourceTargetJsonData> Targets { get; }

        public ConditionSourceEditorData(in ConditionSourcesJsonData source)
        {
            Id = source.Id;
            Name = source.Name;
            Description = source.Description;
            if (!string.IsNullOrWhiteSpace(source.Group.Type))
                GroupParam = source.Group;
            if (!string.IsNullOrWhiteSpace(source.Entry.Type))
                EntryParam = source.Entry;
            if (!string.IsNullOrWhiteSpace(source.SourceId.Type))
                SourceIdParam = source.SourceId;
            Targets = new ObservableCollection<ConditionSourceTargetJsonData>();
            if (source.Targets != null)
            {
                foreach (var target in source.Targets)
                    Targets.Add(target.Value);
            }
        }

        public void UpdateDataParam(in ConditionSourceParamsJsonData? param, ConditionSourceParamUpdate paramUpdate)
        {
            switch (paramUpdate)
            {
                case ConditionSourceParamUpdate.UpdateGroupParam:
                    GroupParam = param;
                    OnPropertyChanged(nameof(GroupParam));
                    break;
                case ConditionSourceParamUpdate.UpdateEntryParam:
                    EntryParam = param;
                    OnPropertyChanged(nameof(EntryParam));
                    break;
                case ConditionSourceParamUpdate.UpdateSourceIdParam:
                    SourceIdParam = param;
                    OnPropertyChanged(nameof(SourceIdParam));
                    break;
            }
        }

        public ConditionSourcesJsonData ToConditionSourcesJsonData()
        {
            ConditionSourcesJsonData obj = new();
            obj.Id = Id;
            obj.Name = Name;
            obj.Description = Description;
            if (GroupParam.HasValue)
                obj.Group = GroupParam.Value;
            if (EntryParam.HasValue)
                obj.Entry = EntryParam.Value;
            if (SourceIdParam.HasValue)
                obj.SourceId = SourceIdParam.Value;
            obj.Targets = new Dictionary<int, ConditionSourceTargetJsonData>();
            for (int i = 0; i < Targets.Count; ++i)
                obj.Targets.Add(i, Targets[i]);
            return obj;
        }

        public bool IsEmpty() => string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Description);
        
        public event PropertyChangedEventHandler? PropertyChanged = delegate {};

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName]
            string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}