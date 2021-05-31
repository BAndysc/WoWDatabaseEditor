using System;
using System.Collections.Specialized;
using Prism.Mvvm;
using WDE.Common.Services;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class SmartTeachingTips : BindableBase, System.IDisposable
    {
        private const string TipWaitAction = "SmartScriptEditor.WaitActionTipOpened";
        private const string TipMultipleActions = "SmartScriptEditor.MultipleActions";
        private const string TipYouCanNameStoredTargets = "SmartScriptEditor.YouCanNameStoredTargets";
        private readonly ITeachingTipService teachingTipService;
        private readonly SmartScript script;

        public bool MultipleActionsTipOpened { get; set; }
        public bool WaitActionTipOpened { get; set; }
        public bool YouCanNameStoredTargetsTipOpened { get; set; }
        
        public SmartTeachingTips(ITeachingTipService teachingTipService, SmartScript script)
        {
            this.teachingTipService = teachingTipService;
            this.script = script;
        }

        private void AllActionsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null)
                return;
            
            foreach (SmartAction item in e.NewItems)
            {
                // WaitActionTipOpened
                {
                    if (item.Id == SmartConstants.ActionCallTimedActionList)
                    {
                        if (teachingTipService.ShowTip(TipWaitAction))
                        {
                            WaitActionTipOpened = true;
                            RaisePropertyChanged(nameof(WaitActionTipOpened));
                        }
                    }
                }
                
                // YouCanNameStoredTargetsTipOpened
                {
                    if (item.Source.Id == SmartConstants.SourceStoredObject || item.Target.Id == SmartConstants.SourceStoredObject)
                    {
                        if (teachingTipService.ShowTip(TipYouCanNameStoredTargets))
                        {
                            YouCanNameStoredTargetsTipOpened = true;
                            RaisePropertyChanged(nameof(YouCanNameStoredTargetsTipOpened));
                        }
                    }
                }
                
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

        private bool AreEventsEqual(SmartEvent a, SmartEvent b)
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
            script.AllActions.CollectionChanged -= AllActionsOnCollectionChanged;
        }

        public void Start()
        {
            // subscribe only if tips not shown yet
            if (AnyTipToShow())
                script.AllActions.CollectionChanged += AllActionsOnCollectionChanged;
        }

        private bool AnyTipToShow()
        {
            return !teachingTipService.IsTipShown(TipMultipleActions) ||
                   !teachingTipService.IsTipShown(TipWaitAction) || 
                   !teachingTipService.IsTipShown(TipYouCanNameStoredTargets);
        }
    }
}