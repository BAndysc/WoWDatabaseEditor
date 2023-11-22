using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;

namespace WDE.LootEditor.Services;

[SingleInstance]
[AutoRegister]
internal class LootUserQuestionsService : ILootUserQuestionsService
{
    private readonly IMessageBoxService messageBoxService;

    public LootUserQuestionsService(IMessageBoxService messageBoxService)
    {
        this.messageBoxService = messageBoxService;
    }
    
    public async Task<SaveDialogResult> AskToSave(
        LootEditingMode lootEditingMode,
        LootSourceType sourceType,
        ICollection<LootEntry> referencesToBeUnloaded,
        ICollection<LootEntry> rootsToBeRemoved)
    {
        StringBuilder messsage = new StringBuilder();
        messsage.AppendLine("This change will cause the following loot:");
        if (referencesToBeUnloaded.Count > 0)
        {
            foreach (var lootEntry in referencesToBeUnloaded)
                messsage.AppendLine(" > loot reference " + lootEntry);
            messsage.Append("to be unloaded");
            
            if (rootsToBeRemoved.Count > 0)
                messsage.AppendLine(" and");
            else
                messsage.AppendLine(".");
        }

        if (rootsToBeRemoved.Count > 0)
        {
            foreach (var lootEntry in rootsToBeRemoved)
                messsage.AppendLine($" > loot {sourceType} {lootEntry}");
            if (lootEditingMode == LootEditingMode.PerLogicalEntity)
                messsage.AppendLine($"to be removed from the {sourceType} loot.");
            else
                messsage.AppendLine($"to be unloaded.");
        }
        
        messsage.AppendLine();
        messsage.Append("However, these loots are modified. Do you want to save all changes?");
            
        return await messageBoxService.ShowDialog<SaveDialogResult>(new MessageBoxFactory<SaveDialogResult>()
            .SetTitle("Save changes?")
            .SetMainInstruction("Do you want to save changes?")
            .SetContent(messsage.ToString())
            .WithYesButton(SaveDialogResult.Save)
            .WithNoButton(SaveDialogResult.DontSave)
            .WithCancelButton(SaveDialogResult.Cancel)
            .Build());
    }

    public async Task NotifyCouldNotApplySql(Exception error)
    {
        await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
            .SetTitle("Error")
            .SetMainInstruction("Couldn't apply SQL")
            .SetContent(error.Message)
            .WithOkButton(true)
            .Build());
    }
}