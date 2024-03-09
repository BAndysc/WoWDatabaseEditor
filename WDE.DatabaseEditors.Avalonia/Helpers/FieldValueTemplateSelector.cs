using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.ViewModels;
using WDE.DatabaseEditors.ViewModels.Template;
using WDE.Parameters;

namespace WDE.DatabaseEditors.Avalonia.Helpers
{
    public class FieldValueTemplateSelector : IDataTemplate
    {
        public DataTemplate? FlagsTemplate { get; set; }
        public DataTemplate? ItemsTemplate { get; set; }
        public DataTemplate? GenericTemplate { get; set; }
        public DataTemplate? BoolTemplate { get; set; }
        public DataTemplate? CommandTemplate { get; set; }

        public Control? Build(object? param)
        {
            if (param is ViewModels.MultiRow.DatabaseCellViewModel { ActionCommand: { } } or 
                ViewModels.SingleRow.SingleRecordDatabaseCellViewModel { ActionCommand: { } } or 
                ViewModels.Template.DatabaseCellViewModel { ActionCommand: { } } or 
                ViewModels.OneToOneForeignKey.SingleRecordDatabaseCellViewModel { ActionCommand: { } })
                return CommandTemplate!.Build(param);
            if ((param is DatabaseCellViewModel vm && vm.ParameterValue is IParameterValue<long> holder && holder.Parameter is BoolParameter) ||
                (param is ViewModels.MultiRow.DatabaseCellViewModel vm2 && vm2.ParameterValue is IParameterValue<long> holder2 && holder2.Parameter is BoolParameter) ||
                (param is ViewModels.SingleRow.SingleRecordDatabaseCellViewModel vm3 && vm3.ParameterValue is IParameterValue<long> holder3 && holder3.Parameter is BoolParameter) ||
                (param is ViewModels.OneToOneForeignKey.SingleRecordDatabaseCellViewModel vm6 && vm6.ParameterValue is IParameterValue<long> holder4 && holder4.Parameter is BoolParameter))
                return BoolTemplate!.Build(param);
            if (param is BaseDatabaseCellViewModel vm5 && vm5.UseFlagsPicker)
                return FlagsTemplate!.Build(param);
            if (param is BaseDatabaseCellViewModel vm4 && vm4.UseItemPicker)
                return ItemsTemplate!.Build(param);
            return GenericTemplate!.Build(param);
        }

        public bool Match(object? data)
        {
            return data is DatabaseCellViewModel or
                ViewModels.MultiRow.DatabaseCellViewModel or 
                ViewModels.SingleRow.SingleRecordDatabaseCellViewModel or 
                ViewModels.OneToOneForeignKey.SingleRecordDatabaseCellViewModel;
        }
    }
}