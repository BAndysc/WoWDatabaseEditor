using System.Collections.Generic;

namespace WDE.Common.Services.MessageBox
{
    public interface IMessageBox<T>
    {
        string Title { get; }
        MessageBoxIcon Icon { get; }
        string MainInstruction { get; }
        string Content { get; }
        string ExpandedInformation { get; }
        string Footer { get; }
        MessageBoxIcon FooterIcon { get; }
        IList<IMessageBoxButton<T>> Buttons { get; }
    }
}