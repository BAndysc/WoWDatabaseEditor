using System;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Module.Attributes;

namespace WDE.PacketViewer.MassParsing;

[AutoRegister]
public class MassParserMenu : IToolMenuItem
{
    public string ItemName => "Mass sniff parser";
    public ICommand ItemCommand { get; }
    public MenuShortcut? Shortcut => null;

    public MassParserMenu(Func<MassParserViewModel> creator,
        Lazy<IDocumentManager> documentManager)
    {
        ItemCommand = new DelegateCommand(() =>
        {
            documentManager.Value.OpenDocument(creator());
        });
    }
}