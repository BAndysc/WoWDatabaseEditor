using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using AvaloniaStyles.Utils;
using WDE.Common.Disposables;

namespace AvaloniaStyles.Controls
{
    public class ToolBar : Control
    {
        public static readonly StyledProperty<object> MiddleContentProperty =
            AvaloniaProperty.Register<ToolBar, object>(nameof(MiddleContent));
        
        public static readonly StyledProperty<object> LeftContentProperty =
            AvaloniaProperty.Register<ToolBar, object>(nameof(LeftContent));
        
        public static readonly StyledProperty<object> RightContentProperty =
            AvaloniaProperty.Register<ToolBar, object>(nameof(RightContent));
        
        public static readonly StyledProperty<object> TopContentProperty =
            AvaloniaProperty.Register<ToolBar, object>(nameof(TopContent));
        
        public static readonly StyledProperty<object> TopLeftContentProperty =
            AvaloniaProperty.Register<ToolBar, object>(nameof(TopLeftContent));

        public static readonly DirectProperty<ToolBar, bool> IsEmptyProperty =
            AvaloniaProperty.RegisterDirect<ToolBar, bool>(nameof(IsEmpty), o => o.IsEmpty);

        public object MiddleContent
        {
            get => GetValue(MiddleContentProperty);
            set => SetValue(MiddleContentProperty, value);
        }
        
        public object LeftContent
        {
            get => GetValue(LeftContentProperty);
            set => SetValue(LeftContentProperty, value);
        }
        
        public object TopContent
        {
            get => GetValue(TopContentProperty);
            set => SetValue(TopContentProperty, value);
        }
        
        public object TopLeftContent
        {
            get => GetValue(TopLeftContentProperty);
            set => SetValue(TopLeftContentProperty, value);
        }

        public object RightContent
        {
            get => GetValue(RightContentProperty);
            set => SetValue(RightContentProperty, value);
        }

        private bool _isEmpty;
        public bool IsEmpty
        {
            get => _isEmpty;
        }

        private bool IsContentEmpty(object? o)
        {
            if (o == null)
            {
                return true;
            }

            if (o is ContentControl cc)
            {
                if (cc.Content == null)
                    return true;
                if (cc.ContentTemplate is IExtendedDataTemplate ct)
                    return !ct.HasContent(cc.Content);
            }

            return false;
        }

        static ToolBar()
        {
            MiddleContentProperty.Changed.AddClassHandler<ToolBar>(Action);
            LeftContentProperty.Changed.AddClassHandler<ToolBar>(Action);
            RightContentProperty.Changed.AddClassHandler<ToolBar>(Action);
            TopContentProperty.Changed.AddClassHandler<ToolBar>(Action);
            TopLeftContentProperty.Changed.AddClassHandler<ToolBar>(Action);
        }

        private static void Action(ToolBar toolbar, AvaloniaPropertyChangedEventArgs args)
        {
            using var _ = toolbar.IsEmptyUpdateScope();
            if (args.OldValue is ILogical oldLogical)
            {
                toolbar.LogicalChildren.Remove(oldLogical);
                if (oldLogical is ContentControl cc)
                {
                    cc.PropertyChanged -= toolbar.OnContentPropertyChanged;
                }
            }

            if (args.NewValue is ILogical newLogical)
            {
                toolbar.LogicalChildren.Add(newLogical);
                if (newLogical is ContentControl cc)
                {
                    cc.PropertyChanged += toolbar.OnContentPropertyChanged;
                }
            }
        }

        private void OnContentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            using var _ = IsEmptyUpdateScope();
        }

        private IDisposable IsEmptyUpdateScope()
        {
            var wasEmpty = _isEmpty;
            return new ActionDisposable(() =>
            {
                _isEmpty = IsContentEmpty(MiddleContent) &&
                          IsContentEmpty(LeftContent) &&
                          IsContentEmpty(RightContent) &&
                          IsContentEmpty(TopContent) &&
                          IsContentEmpty(TopLeftContent);
                if (_isEmpty != wasEmpty)
                {
                    RaisePropertyChanged(IsEmptyProperty, wasEmpty, _isEmpty);
                }
            });
        }
    }
}