// using Avalonia.Controls;
// using Avalonia.Controls.Templates;
// using Avalonia.Markup.Xaml.Templates;
// using WDE.DatabaseEditors.ViewModels;
//
// namespace WDE.DatabaseEditors.Avalonia.Helpers
// {
//     public class FirstColumnDataTemplate : IDataTemplate
//     {
//         public DataTemplate? FirstColumn { get; set; }
//         public DataTemplate? Column { get; set; }
//         
//         public IControl Build(object param)
//         {
//             if (param is DatabaseCellViewModel cell)
//             {
//                 if (cell.ParentEntity.IsFirstEntity)
//                     return FirstColumn!.Build(param);
//             }
//             return Column!.Build(param);
//         }
//
//         public bool Match(object data)
//         {
//             return data is DatabaseCellViewModel;
//         }
//     }
// }