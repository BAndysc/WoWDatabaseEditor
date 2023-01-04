using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaStyles.Controls;
using WoWDatabaseEditorCore.Services.ProblemsTool;

namespace WoWDatabaseEditorCore.Avalonia.Services.ProblemsTool
{
    public partial class ProblemsView : UserControl
    {
        public ProblemsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private GroupingListBox? listBox;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (DataContext is ProblemsViewModel vm)
                vm.RequestScrollTo += VmOnRequestScrollTo;
            listBox = this.FindControl<GroupingListBox>("ListBox");
        }

        private void VmOnRequestScrollTo(DocumentProblemsViewModel obj)
        {
            listBox?.ScrollToItem(obj);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            listBox = null;
            if (DataContext is ProblemsViewModel vm)
                vm.RequestScrollTo -= VmOnRequestScrollTo;
        }
    }
}