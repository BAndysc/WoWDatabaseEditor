using System;
using System.Collections.Generic;
using Ookii.Dialogs.Wpf;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.WPF.Managers
{
    [SingleInstance]
    [AutoRegister]
    public class MessageBoxService : IMessageBoxService
    {
        public T ShowDialog<T>(IMessageBox<T> messageBox)
        {
            using (TaskDialog dialog = new TaskDialog())
            {
                dialog.WindowTitle = messageBox.Title;
                dialog.MainInstruction = messageBox.MainInstruction;
                dialog.Content = messageBox.Content;
                dialog.ExpandedInformation = messageBox.ExpandedInformation;
                dialog.Footer = messageBox.Footer;
                dialog.FooterIcon = ToDialogIcon(messageBox.FooterIcon);
                dialog.MainIcon = ToDialogIcon(messageBox.Icon);

                Dictionary<TaskDialogButton, T> buttonToValue = new();
                foreach (var buttonDefinition in messageBox.Buttons)
                {
                    TaskDialogButton button = GenerateButton(buttonDefinition.Name);
                    buttonToValue[button] = buttonDefinition.ReturnValue;
                    dialog.Buttons.Add(button);
                }

                TaskDialogButton returned = dialog.ShowDialog();

                return buttonToValue[returned];
            }
        }

        private TaskDialogButton GenerateButton(string name)
        {
            switch (name)
            {
                case "Ok":
                    return new TaskDialogButton(ButtonType.Ok);
                case "Yes":
                    return new TaskDialogButton(ButtonType.Yes);
                case "No":
                    return new TaskDialogButton(ButtonType.No);
                case "Cancel":
                    return new TaskDialogButton(ButtonType.Cancel);
                default:
                    return new TaskDialogButton(name);
            }
        }

        private TaskDialogIcon ToDialogIcon(MessageBoxIcon icon)
        {
            switch (icon)
            {
                case MessageBoxIcon.NoIcon:
                    return TaskDialogIcon.Custom;
                case MessageBoxIcon.Warning:
                    return TaskDialogIcon.Warning;
                case MessageBoxIcon.Error:
                    return TaskDialogIcon.Error;
                case MessageBoxIcon.Information:
                    return TaskDialogIcon.Information;
                case MessageBoxIcon.Shield:
                    return TaskDialogIcon.Shield;
                default:
                    throw new ArgumentOutOfRangeException(nameof(icon), icon, null);
            }
        }
    }
}