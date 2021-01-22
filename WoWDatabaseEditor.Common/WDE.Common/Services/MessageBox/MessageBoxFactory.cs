using System.Collections.Generic;
using System.Linq;

namespace WDE.Common.Services.MessageBox
{
    public class MessageBoxFactory<T>
    {
        private string windowTitle;
        private MessageBoxIcon icon;
        private string mainInstruction;
        private string content;
        private string expandedInformation;
        private string footer;
        private MessageBoxIcon footerIcon;
        private List<IMessageBoxButton<T>> buttons = new();

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
            buttons.Add(new MessageBoxButton("Ok", returnValue));
            return this;
        }
        
        public MessageBoxFactory<T> WithYesButton(T returnValue)
        {
            buttons.Add(new MessageBoxButton("Yes", returnValue));
            return this;
        }
        
        public MessageBoxFactory<T> WithNoButton(T returnValue)
        {
            buttons.Add(new MessageBoxButton("No", returnValue));
            return this;
        }
        
        public MessageBoxFactory<T> WithCancelButton(T returnValue)
        {
            buttons.Add(new MessageBoxButton("Cancel", returnValue));
            return this;
        }
        
        public MessageBoxFactory<T> WithButton(string text, T returnValue)
        {
            buttons.Add(new MessageBoxButton(text, returnValue));
            return this;
        }
        
        public IMessageBox<T> Build()
        {
            return new MessageBox(windowTitle, icon, mainInstruction, content, expandedInformation, footer, footerIcon, buttons);
        }

        private class MessageBoxButton : IMessageBoxButton<T>
        {
            public MessageBoxButton(string name, T returnValue)
            {
                Name = name;
                ReturnValue = returnValue;
            }

            public string Name { get; }
            public T ReturnValue { get; }
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
                IList<IMessageBoxButton<T>> buttons)
            {
                Title = title;
                Icon = icon;
                MainInstruction = mainInstruction;
                Content = content;
                ExpandedInformation = expandedInformation;
                Footer = footer;
                FooterIcon = footerIcon;
                if ((buttons?.Count ?? 0) == 0)
                    Buttons = new List<IMessageBoxButton<T>>() {new MessageBoxButton("Ok", default)};
                else
                    Buttons = buttons.ToList();
            }

            public string Title { get; }
            public MessageBoxIcon Icon { get; }
            public string MainInstruction { get; }
            public string Content { get; }
            public string ExpandedInformation { get; }
            public string Footer { get; }
            public MessageBoxIcon FooterIcon { get; }
            public IList<IMessageBoxButton<T>> Buttons { get; }
        }
    }
}