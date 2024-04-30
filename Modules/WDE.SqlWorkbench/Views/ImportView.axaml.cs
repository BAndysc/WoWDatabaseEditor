using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Views;

public partial class ImportView : UserControl
{
    public ImportView()
    {
        InitializeComponent();
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragEnterEvent, DragEnter);
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files))
            return;

        var files = e.Data.GetFiles();
        if (files == null)
            return;

        if (DataContext is not ImportViewModel vm)
            return;

        foreach (var file in files.Select(x => x.TryGetLocalPath())
                     .Where(x => x != null))
        {
            if (Directory.Exists(file))
                vm.AddDirectory(file);
            else if (File.Exists(file))
                vm.AddFile(file);
        }
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void DragEnter(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.Data.Contains(DataFormats.Files) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void TableItemKeyPressed(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            bool? setToValue = null;
            foreach (var selected in FilesListBox.Selection.SelectedItems.Cast<ImportFileViewModel>())
            {
                if (setToValue.HasValue)
                {
                    selected.IsChecked = setToValue.Value;
                }
                else
                {
                    selected.IsChecked = !selected.IsChecked;
                    setToValue = selected.IsChecked;
                }
            }

            e.Handled = true;
        }
    }
}