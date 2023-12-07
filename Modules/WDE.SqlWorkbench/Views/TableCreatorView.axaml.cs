using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using WDE.Common.Avalonia.Components;
using WDE.Common.Avalonia.Utils;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.SqlWorkbench.Models.DataTypes;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Views;

public partial class TableCreatorView : UserControl
{
    public TableCreatorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private void EditableItemsTextBlock_OnOnNewItemRequest(object? sender, NewItemRequestArgs e)
    {
        var editableItemsTextBlock = sender as EditableItemsTextBlock;
        
        if (editableItemsTextBlock == null)
            return;
        
        if (MySqlType.TryParse(e.Text, out var type))
        {
            editableItemsTextBlock.SelectedItem = new DataTypeViewModel(type);
        }
        else
        {
            async Task AskToOverride()
            {
                if (await ViewBind.ResolveViewModel<IMessageBoxService>().ShowDialog(
                        new MessageBoxFactory<bool>()
                            .SetTitle("Error")
                            .SetMainInstruction("Cannot parse type")
                            .SetContent(
                                $"'{e.Text}' looks like an invalid MySQL type. Do you want to use it anyway?\n\nIf you think this is wrong, please report a bug.")
                            .WithYesButton(true)
                            .WithCancelButton(false)
                            .Build()))
                {
                    editableItemsTextBlock.SelectedItem = new DataTypeViewModel(e.Text);
                }
            }
            AskToOverride().ListenErrors();
        }
    }

    private void EditableItemsTextBlock_OnCharsetItemRequest(object? sender, NewItemRequestArgs e)
    {
        var editableItemsTextBlock = sender as EditableItemsTextBlock;
        
        if (editableItemsTextBlock == null)
            return;

        editableItemsTextBlock.SelectedItem = new CharsetViewModel(e.Text, false);
    }

    private void EditableItemsTextBlock_OnCollationItemRequest(object? sender, NewItemRequestArgs e)
    {
        var editableItemsTextBlock = sender as EditableItemsTextBlock;
        
        if (editableItemsTextBlock == null)
            return;

        editableItemsTextBlock.SelectedItem = new CollationViewModel(e.Text, false, false);
    }
}