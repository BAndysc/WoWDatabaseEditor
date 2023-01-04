using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Metadata;
using AvaloniaStyles.Utils;

namespace AvaloniaStyles.Controls
{
    public class WizardPanel : TemplatedControl
    {
        private ICommand? nextStepCommand;
        public static readonly DirectProperty<WizardPanel, ICommand?> NextStepCommandProperty = AvaloniaProperty.RegisterDirect<WizardPanel, ICommand?>("NextStepCommand", o => o.NextStepCommand, (o, v) => o.NextStepCommand = v);
        
        private ICommand? previousStepCommand;
        public static readonly DirectProperty<WizardPanel, ICommand?> PreviousStepCommandProperty = AvaloniaProperty.RegisterDirect<WizardPanel, ICommand?>("PreviousStepCommand", o => o.PreviousStepCommand, (o, v) => o.PreviousStepCommand = v);
        
        private uint currentStepIndex;
        public static readonly DirectProperty<WizardPanel, uint> CurrentStepIndexProperty = AvaloniaProperty.RegisterDirect<WizardPanel, uint>("CurrentStepIndex", o => o.CurrentStepIndex, (o, v) => o.CurrentStepIndex = v);

        public static readonly DirectProperty<WizardPanel, uint> StepsCountProperty =
            AvaloniaProperty.RegisterDirect<WizardPanel, uint>("StepsCount", o => o.StepsCount);
        
        private bool isLoading;
        public static readonly DirectProperty<WizardPanel, bool> IsLoadingProperty = AvaloniaProperty.RegisterDirect<WizardPanel, bool>("IsLoading", o => o.IsLoading, (o, v) => o.IsLoading = v);
        
        public uint StepsCount => (uint)Children.Count;
        
        public static readonly StyledProperty<DataTemplate> TitleTemplateProperty =
            AvaloniaProperty.Register<WizardPanel, DataTemplate>(nameof(TitleTemplate));
        public DataTemplate TitleTemplate
        {
            get => GetValue(TitleTemplateProperty);
            set => SetValue(TitleTemplateProperty, value);
        }
        
        public ICommand? NextStepCommand
        {
            get => nextStepCommand;
            set => SetAndRaise(NextStepCommandProperty, ref nextStepCommand, value);
        }

        public ICommand? PreviousStepCommand
        {
            get => previousStepCommand;
            set => SetAndRaise(PreviousStepCommandProperty, ref previousStepCommand, value);
        }

        public uint CurrentStepIndex
        {
            get => currentStepIndex;
            set => SetAndRaise(CurrentStepIndexProperty, ref currentStepIndex, value);
        }

        [Content]
        public Avalonia.Controls.Controls Children { get; } = new();

        public bool IsLoading
        {
            get => isLoading;
            set => SetAndRaise(IsLoadingProperty, ref isLoading, value);
        }

        static WizardPanel()
        {
            CurrentStepIndexProperty.Changed.AddClassHandler<WizardPanel>((o, e) => o.UpdateVisibility());
        }

        public WizardPanel()
        {
            NextStepCommand = new ActionCommand(() => currentStepIndex++, () => true);
            PreviousStepCommand = new ActionCommand(() => currentStepIndex--, () => true);
            Children.CollectionChanged += ChildrenChanged;
        }

        private void UpdateVisibility()
        {
            foreach (var logic in Children)
                logic.IsVisible = false;

            if (currentStepIndex < Children.Count)
            {
                var newChild = Children[(int)currentStepIndex];
                newChild.IsVisible = true;

                var newTitle = GetStepTitle(newChild);
                SetStepTitle(this, $"{currentStepIndex+1}. {newTitle}");
            }
        }
        
        protected virtual void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            List<Control> controls;
            if (partPanelContainer == null)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    
                    controls = e.NewItems!.OfType<Control>().ToList();
                    partPanelContainer!.Children.InsertRange(e.NewStartingIndex, controls);
                    
                    /*LogicalChildren.InsertRange(e.NewStartingIndex, controls);
                    VisualChildren.InsertRange(e.NewStartingIndex, e.NewItems!.OfType<Visual>());*/
                    break;

                case NotifyCollectionChangedAction.Move:
                    partPanelContainer!.Children.MoveRange(e.OldStartingIndex, e.OldItems!.Count, e.NewStartingIndex);
                    
                    //LogicalChildren.MoveRange(e.OldStartingIndex, e.OldItems!.Count, e.NewStartingIndex);
                    //VisualChildren.MoveRange(e.OldStartingIndex, e.OldItems!.Count, e.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    controls = e.OldItems!.OfType<Control>().ToList();
                    partPanelContainer!.Children.RemoveAll(controls);
                    
                    //LogicalChildren.RemoveAll(controls);
                    //VisualChildren.RemoveAll(e.OldItems!.OfType<Visual>());
                    break;

                case NotifyCollectionChangedAction.Replace:
                    for (var i = 0; i < e.OldItems!.Count; ++i)
                    {
                        var index = i + e.OldStartingIndex;
                        var child = (Control)e.NewItems![i]!;

                        partPanelContainer!.Children[index] = child;
                        //LogicalChildren[index] = child;
                        //VisualChildren[index] = child;
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException();
            }
            
            RaisePropertyChanged(StepsCountProperty, (uint)0, (uint)Children.Count);
            InvalidateMeasure();
            UpdateVisibility();
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            partPanelContainer = e.NameScope.Get<Panel>("PART_PanelContainer");

            partPanelContainer!.Children.InsertRange(0, Children);
            
            UpdateVisibility();
        }

        private Panel? partPanelContainer;

        public static readonly AttachedProperty<string> StepTitleProperty = AvaloniaProperty.RegisterAttached<AvaloniaObject, string>("StepTitle", typeof(WizardPanel));

        public static string? GetStepTitle(AvaloniaObject obj) => (string?)obj?.GetValue(StepTitleProperty) ?? "(null)";

        public static void SetStepTitle(AvaloniaObject obj, string value)
        {
            obj.SetValue(StepTitleProperty, value);
        }
    }
}