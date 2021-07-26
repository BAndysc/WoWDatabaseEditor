using System;
using AvaloniaEdit.Document;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WDE.Common.Avalonia.Types
{
    [AutoRegister]
    public class NativeTextDocument : INativeTextDocument
    {
        public TextDocument Native { get; }

        public IObservable<int> Length => Native.ToObservable(n => n.TextLength);

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