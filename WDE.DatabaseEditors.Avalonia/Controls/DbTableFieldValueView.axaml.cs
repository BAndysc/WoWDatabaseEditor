using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace WDE.DatabaseEditors.Avalonia.Controls
{
    public class DbTableFieldValueView : TemplatedControl
    {
        private bool isModified;
        public static readonly DirectProperty<DbTableFieldValueView, bool> IsModifiedProperty = AvaloniaProperty.RegisterDirect<DbTableFieldValueView, bool>("IsModified", o => o.IsModified, (o, v) => o.IsModified = v);

        private bool showModifiedIcon;
        public static readonly DirectProperty<DbTableFieldValueView, bool> ShowModifiedIconProperty = AvaloniaProperty.RegisterDirect<DbTableFieldValueView, bool>("ShowModifiedIcon", o => o.ShowModifiedIcon, (o, v) => o.ShowModifiedIcon = v);

        private ICommand? chooseParameterCommand;
        public static readonly DirectProperty<DbTableFieldValueView, ICommand?> ChooseParameterCommandProperty = AvaloniaProperty.RegisterDirect<DbTableFieldValueView, ICommand?>("ChooseParameterCommand", o => o.ChooseParameterCommand, (o, v) => o.ChooseParameterCommand = v);

        private bool isReadOnly;
        public static readonly DirectProperty<DbTableFieldValueView, bool> IsReadOnlyProperty = AvaloniaProperty.RegisterDirect<DbTableFieldValueView, bool>("IsReadOnly", o => o.IsReadOnly, (o, v) => o.IsReadOnly = v);

        public ICommand? ChooseParameterCommand
        {
            get => chooseParameterCommand;
            set => SetAndRaise(ChooseParameterCommandProperty, ref chooseParameterCommand, value);
        }

        public bool IsModified
        {
            get => isModified;
            set => SetAndRaise(IsModifiedProperty, ref isModified, value);
        }

        public bool IsReadOnly
        {
            get => isReadOnly;
            set => SetAndRaise(IsReadOnlyProperty, ref isReadOnly, value);
        }
        
        public bool ShowModifiedIcon
        {
            get => showModifiedIcon;
            set => SetAndRaise(ShowModifiedIconProperty, ref showModifiedIcon, value);
        }
    }
}