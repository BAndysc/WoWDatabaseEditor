﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Annotations;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.SmartScriptEditor.Data;

namespace WDE.SmartScriptEditor.Editor.ViewModels.SmartDataEditors
{
    class SmartDataActionsEditorViewModel: BindableBase, ISmartDataEditorModel, IDialog
    {
        private readonly IParameterFactory parameterFactory;
        private readonly IWindowManager windowManager;
        public SmartDataActionsEditorViewModel(IParameterFactory parameterFactory, IWindowManager windowManager, in SmartGenericJsonData source, bool isCreatingItem)
        {
            Source = new SmartDataActionsEditorData(in source);
            this.parameterFactory = parameterFactory; ;
            this.windowManager = windowManager;
            SaveItem = new DelegateCommand((() => CloseOk?.Invoke()));
            DeleteItem = new DelegateCommand(DeleteSelectedParam);
            AddParameter = new AsyncAutoCommand(OpenCreateParameterWindow);
            EditParameter = new AsyncAutoCommand<SmartParameterJsonData?>(OpenEditParameterWindow);
            InsertOnSave = isCreatingItem;
            selectedParamIndex = -1;
        }

        public SmartDataActionsEditorData Source { get; private set; }
        public bool InsertOnSave { get; }
        private int selectedParamIndex;

        public int SelectedParamIndex
        {
            get => selectedParamIndex;
            set => selectedParamIndex = value;
        }

        public DelegateCommand SaveItem { get; }
        public DelegateCommand DeleteItem { get; }
        public AsyncAutoCommand AddParameter { get; }
        public AsyncAutoCommand<SmartParameterJsonData?> EditParameter { get; }

        private void DeleteSelectedParam()
        {
            if (SelectedParamIndex >= 0)
                Source.Parameters.RemoveAt(selectedParamIndex);
        }

        private async System.Threading.Tasks.Task OpenCreateParameterWindow()
        {
            var tempParam = new SmartParameterJsonData();
            await OpenParameterEditor(tempParam, true);
        }

        private async System.Threading.Tasks.Task OpenEditParameterWindow(SmartParameterJsonData? param)
        {
            if (param.HasValue)
                await OpenParameterEditor(param.Value, false);
        }

        private async System.Threading.Tasks.Task OpenParameterEditor(SmartParameterJsonData item, bool insertOnSave)
        {
            var vm = new SmartDataParameterEditorViewModel(parameterFactory, in item);
            if (await windowManager.ShowDialog(vm) && !vm.Source.IsEmpty())
            {
                if (insertOnSave)
                    Source.Parameters.Add(vm.Source.ToSmartParameterJsonData());
                else
                {
                    var newItem = vm.Source.ToSmartParameterJsonData();
                    var index = Source.Parameters.IndexOf(item);
                    if (index != -1)
                        Source.Parameters[index] = newItem;
                }
            }
        }

        public SmartGenericJsonData GetSource() => Source.ToSmartGenericJsonData();
        public bool IsSourceEmpty() => Source.IsEmpty();
        
        public int DesiredWidth { get; } = 473;
        public int DesiredHeight { get; } = 666;
        public string Title { get; } = "Action Editor";
        public bool Resizeable { get; } = false;
        public event Action CloseCancel;
        public event Action CloseOk;
    }

    class SmartDataActionsEditorData : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameReadable { get; set; }
        public string Help { get; set; }
        public string Description { get; set; }
        public ObservableCollection<string> TargetTypes { get; set; }
        public bool TargetIsSource { get; set; }
        private bool isImplicitSource;
        public bool IsImplicitSource
        {
            get => isImplicitSource;
            set
            {
                isImplicitSource = value;
                OnPropertyChanged();
            }
        }
        public string ImplicitSource { get; set; }
        public bool UsesTargetPosition { get; set; }
        public ObservableCollection<SmartParameterJsonData> Parameters { get; set; }

        internal SmartDataActionsEditorData(in SmartGenericJsonData source)
        {
            Id = source.Id;
            Name = source.Name;
            if (string.IsNullOrEmpty(Name))
                Name = "SMART_ACTION_";
            NameReadable = source.NameReadable;
            Help = source.Help;
            Description = source.Description;
            TargetTypes = source.TargetTypes == null ? new ObservableCollection<string>() : new ObservableCollection<string>(source.TargetTypes);
            TargetIsSource = source.TargetIsSource;
            IsImplicitSource = source.ImplicitSource != null;
            ImplicitSource = source.ImplicitSource;
            UsesTargetPosition = source.UsesTargetPosition;
            if (source.Parameters != null)
                Parameters = new ObservableCollection<SmartParameterJsonData>(source.Parameters);
            else
                Parameters = new ObservableCollection<SmartParameterJsonData>();
        }

        public SmartGenericJsonData ToSmartGenericJsonData()
        {
            var obj = new SmartGenericJsonData();
            obj.Id = Id;
            obj.Name = Name;
            obj.NameReadable = NameReadable;
            obj.Help = Help;
            obj.Description = Description;
            obj.TargetTypes = TargetTypes.Count == 0 ? null : TargetTypes.ToList();
            obj.TargetIsSource = TargetIsSource;
            obj.ImplicitSource = IsImplicitSource ? ImplicitSource : null;
            obj.UsesTargetPosition = UsesTargetPosition;
            if (Parameters.Count > 0)
                obj.Parameters = Parameters.ToList();
            return obj;
        }

        public bool IsEmpty() => string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(NameReadable);
        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName]
            string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
