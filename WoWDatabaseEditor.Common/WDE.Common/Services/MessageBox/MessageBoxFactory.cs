using System.Collections.Generic;
using System.Linq;
using WDE.Common.Utils;

namespace WDE.Common.Services.MessageBox
{
    public struct EscapeToClose
    {
        public static EscapeToClose style;
    }
    
    public class MessageBoxFactory<T>
    {
        private string windowTitle = "";
        private MessageBoxIcon icon;
        private string mainInstruction = "";
        private string content = "";
        private string expandedInformation = "";
        private string footer = "";
        private MessageBoxIcon footerIcon;
        private List<IMessageBoxButton<T>> buttons = new();
        private IMessageBoxButton<T>? defaultButton = null;
        private IMessageBoxButton<T>? cancelButton = null;

        public MessageBoxFactory<T> SetTitle(string title)
        {
            windowTitle = title;
            return this;
        }
        
        public MessageBoxFactory<T> SetMainInstruction(string mainInstruction)
        {
            this.mainInstruction = mainInstruction;
            return this;
        }
        
        public MessageBoxFactory<T> SetContent(string content)
        {
            this.content = content;
            return this;
        }
        
        public MessageBoxFactory<T> SetExpandedInformation(string expandedInformation)
        {
            this.expandedInformation = expandedInformation;
            return this;
        }
        
        public MessageBoxFactory<T> SetFooter(string footer)
        {
            this.footer = footer;
            return this;
        }
        
        public MessageBoxFactory<T> SetFooterIcon(MessageBoxIcon footerIcon)
        {
            this.footerIcon = footerIcon;
            return this;
        }
        
        public MessageBoxFactory<T> SetIcon(MessageBoxIcon icon)
        {
            this.icon = icon;
            return this;
        }

        public MessageBoxFactory<T> WithOkButton(T returnValue)
        {
            return WithButton("Ok", returnValue, true, true);
        }
        
        public MessageBoxFactory<T> WithYesButton(T returnValue)
        {
            return WithButton("Yes", returnValue, true, false);
        }

        public MessageBoxFactory<T> WithNoButton(T returnValue, EscapeToClose? escapeToCancel = null)
        {
            return WithButton("No", returnValue, false, escapeToCancel.HasValue);
        }
        
        public MessageBoxFactory<T> WithCancelButton(T returnValue)
        {
            return WithButton("Cancel", returnValue, false, true);
        }
        
        public MessageBoxFactory<T> WithButton(string text, T returnValue, bool isDefault = false, bool isCancel = false)
        {
            buttons.Add(new MessageBoxButton(text, returnValue));
            if (isDefault)
                defaultButton = buttons[^1];
            if (isCancel)
                cancelButton = buttons[^1];
            return this;
        }
        
        public IMessageBox<T> Build()
        {
            return new MessageBox(windowTitle, icon, mainInstruction, content, expandedInformation, footer, footerIcon, buttons, defaultButton, cancelButton);
        }

        private class MessageBoxButton : IMessageBoxButton<T>
        {
            public MessageBoxButton(string name, T? returnValue)
            {
                Name = name;
                ReturnValue = returnValue;
            }

            public string Name { get; }
            public T? ReturnValue { get; }
        }
        
        private class MessageBox : IMessageBox<T>
        {
            public MessageBox(string title,
                MessageBoxIcon icon, 
                string mainInstruction,
                string content, 
                string expandedInformation,
                string footer,
                MessageBoxIcon footerIcon,
                IList<IMessageBoxButton<T>> buttons,
                IMessageBoxButton<T>? defaultButton,
                IMessageBoxButton<T>? cancelButton)
            {
                Title = title;
                Icon = icon;
                MainInstruction = mainInstruction;
                Content = content;
                ExpandedInformation = expandedInformation;
                Footer = footer;
                FooterIcon = footerIcon;
                DefaultButton = defaultButton;
                CancelButton = cancelButton;
                if ((buttons?.Count ?? 0) == 0)
                {
                    Buttons = new List<IMessageBoxButton<T>>() {new MessageBoxButton("Ok", default)};
                    DefaultButton = Buttons[0];
                    CancelButton = Buttons[0];
                }
                else
                {
                    Buttons = buttons!.ToList();
                    Buttons.Sort((a, b) => GetButtonOrder(a?.Name).CompareTo(GetButtonOrder(b?.Name)));
                }
            }

            private int GetButtonOrder(string? name)
            {
                switch (name)
                {
                    case "Yes":
                    case "Ok":
                        return 1;
                    case "No":
                        return 2;
                    case "Cancel":
                        return 3;
                    default:
                        return 0;
                }
            }

            public string Title { get; }
            public MessageBoxIcon Icon { get; }
            public string MainInstruction { get; }
            public string Content { get; }
            public string ExpandedInformation { get; }
            public string Footer { get; }
            public MessageBoxIcon FooterIcon { get; }
            public IList<IMessageBoxButton<T>> Buttons { get; }
            public IMessageBoxButton<T>? DefaultButton { get; }
            public IMessageBoxButton<T>? CancelButton { get; }
        }
    }
}