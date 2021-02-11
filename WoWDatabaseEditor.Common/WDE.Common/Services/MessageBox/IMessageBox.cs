using System.Collections.Generic;

namespace WDE.Common.Services.MessageBox
{
    public interface IMessageBox {}
    public interface IMessageBox<T> : IMessageBox
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