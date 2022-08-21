using System.Collections.Specialized;
using Prism.Mvvm;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Editor.ViewModels
{
    public class EventAiTeachingTips : BindableBase, System.IDisposable
    {
        private const string TipMultipleActions = "EventAiEditor.MultipleActions";
        private const string TipControlToCopy = "EventAiEditor.ControlToCopy";
        private const string TipBetaWarning = "EventAiEditor.FirstTimeBetaWarning";
        private readonly ITeachingTipService teachingTipService;
        private readonly EventAiEditorViewModel vm;
        private readonly EventAiScript script;

        public bool MultipleActionsTipOpened { get; set; }
        public bool ControlToCopyOpened { get; set; }
        
        public EventAiTeachingTips(IMessageBoxService messageBoxService,
            ITeachingTipService teachingTipService,
            EventAiEditorViewModel vm, 
            EventAiScript script)
        {
            this.teachingTipService = teachingTipService;
            this.vm = vm;
            this.script = script;

            if (!teachingTipService.IsTipShown(TipBetaWarning))
            {
                teachingTipService.ShowTip(TipBetaWarning);
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Beta warning")
                    .SetMainInstruction("The Event AI editor is still in beta")
                    .SetContent("Hey, this is just a friendly reminder that the event AI editor is still in beta stage, so bugs can happen, including (but unlikely) data corruption (the editor will not save anything without your permission tho, don't worry).")
                    .WithOkButton(true)
                    .Build()).ListenErrors();
            }
        }

        private void OnPaste()
        {
            if (teachingTipService.ShowTip(TipControlToCopy))
            {
                ControlToCopyOpened = true;
                RaisePropertyChanged(nameof(ControlToCopyOpened));
            }
        }

        private void AllActionsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null)
                return;

            foreach (EventAiAction item in e.NewItems)
            {
                // MultipleActionsTipOpened
                if (item.Parent == null)
                    continue;

                if (item.Parent.Actions.Count != 1) // so user knows about multiple actions
                {
                    teachingTipService.ShowTip(TipMultipleActions);
                    continue;
                }
                
                var indexOf = script.Events.IndexOf(item.Parent);
                if (indexOf == -1 || indexOf == 0)
                    continue;

                var previousEvent = script.Events[indexOf - 1];

                if (previousEvent.Actions.Count >= 2) // so user knows about multiple actions
                {
                    teachingTipService.ShowTip(TipMultipleActions);
                    continue;
                }
                
                if (!AreEventsEqual(previousEvent, item.Parent))
                    continue;

                if (teachingTipService.ShowTip(TipMultipleActions))
                {
                    MultipleActionsTipOpened = true;
                    RaisePropertyChanged(nameof(MultipleActionsTipOpened));
                }
            }
        }

        private bool AreEventsEqual(EventAiEvent a, EventAiEvent b)
        {
            if (a.Id != b.Id)
                return false;

            for (int i = 0; i < a.ParametersCount; ++i)
            {
                if (a.GetParameter(i).Value != b.GetParameter(i).Value)
                    return false;
            }

            if (a.Chance.Value != b.Chance.Value)
                return false;

            if (a.Flags.Value != b.Flags.Value)
                return false;

            if (a.Phases.Value != b.Phases.Value)
                return false;

            return true;
        }

        public void Dispose()
        {
            vm.OnPaste -= OnPaste;
            script.AllActions.CollectionChanged -= AllActionsOnCollectionChanged;
        }

        public void Start()
        {
            // subscribe only if tips not shown yet
            if (AnyTipToShow())
            {
                vm.OnPaste += OnPaste;
                script.AllActions.CollectionChanged += AllActionsOnCollectionChanged;
            }
        }

        private bool AnyTipToShow()
        {
            return !teachingTipService.IsTipShown(TipMultipleActions) ||
                   !teachingTipService.IsTipShown(TipControlToCopy);
        }
    }
}