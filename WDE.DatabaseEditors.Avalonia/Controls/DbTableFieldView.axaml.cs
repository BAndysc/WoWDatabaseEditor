using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace WDE.DatabaseEditors.Avalonia.Controls
{
    public class DbTableFieldView : TemplatedControl
    {
        private string title = "";
        public static readonly DirectProperty<DbTableFieldView, string> TitleProperty = AvaloniaProperty.RegisterDirect<DbTableFieldView, string>("Title", o => o.Title, (o, v) => o.Title = v);
        
        private bool isModified;
        public static readonly DirectProperty<DbTableFieldView, bool> IsModifiedProperty = AvaloniaProperty.RegisterDirect<DbTableFieldView, bool>("IsModified", o => o.IsModified, (o, v) => o.IsModified = v);

        private ICommand? chooseParameterCommand;
        public static readonly DirectProperty<DbTableFieldView, ICommand?> ChooseParameterCommandProperty = AvaloniaProperty.RegisterDirect<DbTableFieldView, ICommand?>("ChooseParameterCommand", o => o.ChooseParameterCommand, (o, v) => o.ChooseParameterCommand = v);
        
        private bool isReadOnly;
        public static readonly DirectProperty<DbTableFieldView, bool> IsReadOnlyProperty = AvaloniaProperty.RegisterDirect<DbTableFieldView, bool>("IsReadOnly", o => o.IsReadOnly, (o, v) => o.IsReadOnly = v);

        public ICommand? ChooseParameterCommand
        {
            get => chooseParameterCommand;
            set => SetAndRaise(ChooseParameterCommandProperty, ref chooseParameterCommand, value);
        }
        
        public string Title
        {
            get => title;
            set => SetAndRaise(TitleProperty, ref title, value);
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

        static DbTableFieldView()
        {
            IsModifiedProperty.Changed.AddClassHandler<DbTableFieldView>((owner, param) =>
            {
                owner.PseudoClasses.Set(":modified", (bool)param.NewValue!);
            });
        }
    }
}