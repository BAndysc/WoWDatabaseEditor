using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using WDE.Common.Utils;
using WDE.MVVM.Observable;

namespace WDE.PacketViewer.Avalonia.Views
{
    public class SearchControl : TemplatedControl
    {
        private string searchText = "";
        public static readonly DirectProperty<SearchControl, string> SearchTextProperty = AvaloniaProperty.RegisterDirect<SearchControl, string>("SearchText", o => o.SearchText, (o, v) => o.SearchText = v);

        public string SearchText
        {
            get => searchText;
            set => SetAndRaise(SearchTextProperty, ref searchText, value);
        }
        
        private ICommand? findPreviousCommand;
        public static readonly DirectProperty<SearchControl, ICommand?> FindPreviousCommandProperty = AvaloniaProperty.RegisterDirect<SearchControl, ICommand?>("FindPreviousCommand", o => o.FindPreviousCommand, (o, v) => o.FindPreviousCommand = v);

        public ICommand? FindPreviousCommand
        {
            get => findPreviousCommand;
            set => SetAndRaise(FindPreviousCommandProperty, ref findPreviousCommand, value);
        }
        
        private ICommand? findNextCommand;
        public static readonly DirectProperty<SearchControl, ICommand?> FindNextCommandProperty = AvaloniaProperty.RegisterDirect<SearchControl, ICommand?>("FindNextCommand", o => o.FindNextCommand, (o, v) => o.FindNextCommand = v);

        public ICommand? FindNextCommand
        {
            get => findNextCommand;
            set => SetAndRaise(FindNextCommandProperty, ref findNextCommand, value);
        }
        
        
        private ICommand? closeCommand;
        public static readonly DirectProperty<SearchControl, ICommand?> CloseCommandProperty = AvaloniaProperty.RegisterDirect<SearchControl, ICommand?>("CloseCommand", o => o.CloseCommand, (o, v) => o.CloseCommand = v);

        public ICommand? CloseCommand
        {
            get => closeCommand;
            set => SetAndRaise(CloseCommandProperty, ref closeCommand, value);
        }

        private System.IDisposable? sub;
        private TextBox? searchBox;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            searchBox = e.NameScope.Get<TextBox>("SearchText");
            searchBox.Focus();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            
            sub = this.GetObservable(IsVisibleProperty).SubscribeAction(@is =>
            {
                if (@is && searchBox != null)
                {
                    DispatcherTimer.RunOnce(() =>
                    {
                        searchBox.Focus();
                    }, TimeSpan.FromMilliseconds(1));
                }
            });
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            sub?.Dispose();
            sub = null;
        }
    }
}
