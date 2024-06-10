using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using WDE.Common.Avalonia;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Utils;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.Avalonia.Controls
{
    public abstract class OpenableFastCellViewBase : FastCellViewBase, ICustomCopyPaste
    {
        protected bool opened;
        protected Panel? partPanel;
        protected TextBlock? partText;

        private System.IDisposable? subscriptionsOnOpen;
        private System.IDisposable? focusDisposable = null;
        private AdornerLayer? adornerLayer;
        protected System.IDisposable? textBoxDisposable;
        private Control? editingControl;
        
        public bool DisableDoubleClick
        {
            get => GetValue(DisableDoubleClickProperty);
            set => SetValue(DisableDoubleClickProperty, value);
        }
        public static readonly StyledProperty<bool> DisableDoubleClickProperty = AvaloniaProperty.Register<OpenableFastCellViewBase, bool>("DisableDoubleClick", false);
        
        protected virtual bool DismissOnWindowFocusLost => false;
        
        static OpenableFastCellViewBase()
        {
            PointerPressedEvent.AddClassHandler(typeof(OpenableFastCellViewBase), (sender, args) =>
            {
                if (sender is not OpenableFastCellViewBase that || args is not PointerPressedEventArgs e)
                    return;
                
                if (that.isReadOnly || e.Handled)
                    return;

                if (!e.GetCurrentPoint(that).Properties.IsLeftButtonPressed)
                    return;
            
                if (!ReferenceEquals(e.Source, that) && !ReferenceEquals(e.Source, that.partPanel) && !ReferenceEquals(e.Source, that.partText))
                    return;

                if ((that.DisableDoubleClick || that.IsFocused) && !that.opened)
                {
                    that.OpenForEditing();
                    e.Handled = true;
                }
                else if (!that.IsFocused)
                {
                    that.Focus(NavigationMethod.Tab);
                    e.Handled = true;
                }
            }, RoutingStrategies.Tunnel);
        }

        protected abstract Control CreateEditingControl();
        
        public async Task DoPaste()
        {
            if (isReadOnly)
                return;

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            var text = clipboard == null ? null : await clipboard.GetTextAsync();

            if (string.IsNullOrEmpty(text))
                return;
            
            PasteImpl(text);
        }

        protected abstract void PasteImpl(string text);

        public abstract void DoCopy();

        protected override void OnKeyDown(KeyEventArgs e)
        {
            HandleMoveLeftRightUpBottom(e, true);
            base.OnKeyDown(e);
            if (KeyGestures.Copy?.Matches(e) ?? false)
            {
                DoCopy();
                e.Handled = true;
            }
            else if (KeyGestures.Paste?.Matches(e) ?? false)
            {
                DoPaste().ListenErrors();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                OpenForEditing();
                e.Handled = true;
            }
        }
        
        protected override void EndEditing(bool commit = true, bool focus = true)
        {
            if (partText != null)
                partText.IsVisible = true;
            textBoxDisposable?.Dispose();
            textBoxDisposable = null;
            
            if (editingControl != null && adornerLayer != null)
                adornerLayer.Children.Remove(editingControl);
            
            if (editingControl != null)
                EndEditingInternal(commit);

            subscriptionsOnOpen?.Dispose();
            subscriptionsOnOpen = null;
            focusDisposable?.Dispose();
            focusDisposable = null;
            adornerLayer = null;
            opened = false;
            editingControl = null;

            if (focus)
                Focus(NavigationMethod.Tab);
        }

        protected abstract void EndEditingInternal(bool commit);

        protected virtual bool OpenForEditing()
        {
            if (opened || isReadOnly)
                return false;

            opened = true;

            editingControl = CreateEditingControl();
            adornerLayer = AdornerLayer.GetAdornerLayer(this);
            if (adornerLayer == null)
            {
                EndEditing();
                return false;
            }
            
            adornerLayer.Children.Add(editingControl);
            AdornerLayer.SetAdornedElement(editingControl, this);
            editingControl.Focus();
            
            var toplevel = this.GetVisualRoot() as TopLevel;
            if (toplevel != null)
            {
                subscriptionsOnOpen = toplevel.AddDisposableHandler(PointerPressedEvent, (s, ev) =>
                {
                    bool hitTextbox = false;
                    ILogical? logical = ev.Source as ILogical;
                    while (logical != null)
                    {
                        if (ReferenceEquals(logical, editingControl))
                        {
                            hitTextbox = true;
                            break;
                        }
                        logical = logical.LogicalParent;
                    }
                    if (!hitTextbox)
                        EndEditing();
                }, RoutingStrategies.Tunnel);
                if (DismissOnWindowFocusLost && toplevel is Window w)
                {
                    focusDisposable = w.GetObservable(WindowBase.IsActiveProperty)
                        .SubscribeAction(@is =>
                        {
                            if (!@is)
                                EndEditing();
                        });
                }
            }

            return true;
        }
    }
}
