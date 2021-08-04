using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using WDE.Common.Avalonia.Utils;

namespace WDE.Common.Avalonia.Components
{
    public class MultiViewModelContentPresenter : TemplatedControl
    {
        private Dictionary<object, IControl> viewModelsToViews = new();
        
        public static readonly DirectProperty<MultiViewModelContentPresenter, IEnumerable> ViewModelsProperty =
            AvaloniaProperty.RegisterDirect<MultiViewModelContentPresenter, IEnumerable>(nameof(ViewModels), o => o.ViewModels, (o, v) => o.ViewModels = v);

        private IEnumerable items = new AvaloniaList<object>();
        public IEnumerable ViewModels
        {
            get => items;
            set => SetAndRaise(ViewModelsProperty, ref items, value);
        }
        
        public static readonly DirectProperty<MultiViewModelContentPresenter, IControl?> SelectedViewProperty =
            AvaloniaProperty.RegisterDirect<MultiViewModelContentPresenter, IControl?>(nameof(SelectedView), o => o.SelectedView);
        private IControl? selectedView;
        public IControl? SelectedView
        {
            get => selectedView;
            private set => SetAndRaise(SelectedViewProperty, ref selectedView, value);
        }
        
        public static readonly DirectProperty<MultiViewModelContentPresenter, object?> SelectedViewModelProperty =
            AvaloniaProperty.RegisterDirect<MultiViewModelContentPresenter, object?>(nameof(SelectedViewModel), o => o.SelectedViewModel, (o, v) => o.SelectedViewModel = v);

        private object? selectedViewModel;
        public object? SelectedViewModel
        {
            get => selectedViewModel;
            set
            {
                SetAndRaise(SelectedViewModelProperty, ref selectedViewModel, value);
                if (value != null && viewModelsToViews.TryGetValue(value, out var view))
                    SelectedView = view;
                else
                    SelectedView = new Panel();
            }
        }
        
        static MultiViewModelContentPresenter()
        {
            ViewModelsProperty.Changed.AddClassHandler<MultiViewModelContentPresenter>((x, e) => x.ItemsChanged(e));
        }
        
        protected virtual void ItemsChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var oldValue = e.OldValue as IEnumerable;
            var newValue = e.NewValue as IEnumerable;

            if (oldValue is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged -= NewInccOnCollectionChanged;
            }
            viewModelsToViews.Clear();
            LogicalChildren.Clear();

            if (newValue is INotifyCollectionChanged newIncc)
            {
                newIncc.CollectionChanged += NewInccOnCollectionChanged;
            }

            if (newValue is IList newList)
                AddItems(newList);
        }

        private void AddItems(IList newItems)
        {
            foreach (var newViewModel in newItems)
            {
                if (ViewBind.TryResolve(newViewModel, out var view))
                {
                    if (view is not IControl controlView)
                        continue;
                    viewModelsToViews[newViewModel] = controlView;
                    if (selectedViewModel == newViewModel)
                        SelectedView = view as IControl;
                    if (view is ILogical logical)
                        LogicalChildren.Add(logical);
                }
            }
        }

        private void NewInccOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                AddItems(e.NewItems!);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var oldViewModel in e.OldItems!)
                {
                    if (viewModelsToViews.TryGetValue(oldViewModel, out var view) && view is ILogical logical)
                        LogicalChildren.Remove(logical);
                    viewModelsToViews.Remove(oldViewModel);
                }
            }
        }
    }
}