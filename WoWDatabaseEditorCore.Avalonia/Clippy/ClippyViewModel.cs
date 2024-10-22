using System;
using System.Collections.Generic;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Managers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Avalonia.Clippy;

public partial class ClippyViewModel : ObservableBase, IWindowViewModel
{
    private readonly IMainThread mainThread;
    private readonly IMessageBoxService messageBoxService;
    public int DesiredWidth { get; set; }
    public int DesiredHeight { get; set; }
    public string Title => "";
    public bool Resizeable => false;
    public ImageUri? Icon => null;

    [Notify] private ClippyQuestion? question;

    public ClippyViewModel(IMainThread mainThread, IMessageBoxService messageBoxService)
    {
        this.mainThread = mainThread;
        this.messageBoxService = messageBoxService;
    }

    public void CloseQuestion()
    {
        Question = null;
    }

    public void Activated()
    {
        mainThread.Delay(() =>
        {
            // Question = new ClippyQuestion("Which expansion do you want to script today?", new List<string>()
            // {
            //     "Vanilla!",
            //     "The Burning Crusade",
            //     "Wrath of the Lich King",
            //     "Cataclysm",
            //     "Mists of Pandaria",
            //     "Warlords of Draenor",
            //     "Legion",
            //     "Battle for Azeroth",
            //     "Shadowlands",
            //     "Dragonflight",
            //     "The War Within"
            // });
            Question = new ClippyQuestion("Welcome to 1998! How do you like the latest WoW Database Editor '97?", new List<string>()
            {
                "I love it!",
                "It's okay",
                "I hate it"
            }, option =>
            {
                CloseQuestion();
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Clippy")
                    .SetMainInstruction("April Fools' Day!")
                    .SetContent(option == "I hate it" ? "At least appreciate the effort!" : "I will continue to improve the editor and make it better!")
                    .WithOkButton(true).Build());
            });
        }, TimeSpan.FromSeconds(2));
    }
}

public class ClippyQuestion
{
    public ClippyQuestion(string question, List<string> answers, Action<string>? onAnswer = null)
    {
        Question = question;
        Answers = answers;
        SelectOption = new DelegateCommand<string>(onAnswer ?? (s => { }));
    }

    public string Question { get; }
    public IReadOnlyList<string> Answers { get; }
    public DelegateCommand<string> SelectOption { get; }
}