using ICSharpCode.AvalonEdit.Document;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.Common.WPF.Types
{
    // no single instance!
    [AutoRegister]
    public class NativeTextDocument : INativeTextDocument
    {
        public TextDocument Native { get; }

        public NativeTextDocument() : this(new TextDocument()) { }

        public NativeTextDocument(TextDocument document)
        {
            Native = document;
        }
    
        public void FromString(string str)
        {
            Native.Text = str;
        }

        public void Append(string str)
        {
            Native.BeginUpdate();
            Native.Insert(Native.TextLength, str);
            Native.EndUpdate();
        }

        public override string ToString()
        {
            return Native.Text;
        }
    }
}