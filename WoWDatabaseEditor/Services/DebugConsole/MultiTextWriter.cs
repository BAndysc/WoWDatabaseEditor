using System;

namespace WoWDatabaseEditorCore.Services.DebugConsole
{
    public class MultiTextWriter
        : System.IO.TextWriter
    {
        protected readonly System.Text.Encoding MEncoding;
        protected readonly System.Collections.Generic.IEnumerable<System.IO.TextWriter> MWriters;
        public override System.Text.Encoding Encoding => MEncoding;

        public event Action? WrittenText;

        public MultiTextWriter(System.Collections.Generic.IEnumerable<System.IO.TextWriter> textWriters, System.Text.Encoding encoding)
        {
            MWriters = textWriters;
            MEncoding = encoding;
        }
        
        public MultiTextWriter(System.Collections.Generic.IEnumerable<System.IO.TextWriter> textWriters)
            : this(textWriters, textWriters.GetEnumerator().Current.Encoding)
        { }
        
        public MultiTextWriter(System.Text.Encoding enc, params System.IO.TextWriter[] textWriters)
            : this(textWriters, enc)
        { }

        public MultiTextWriter(params System.IO.TextWriter[] textWriters)
            : this((System.Collections.Generic.IEnumerable<System.IO.TextWriter>)textWriters)
        { }

        public override void Flush()
        {
            foreach (System.IO.TextWriter thisWriter in MWriters)
            {
                thisWriter.Flush();
            }
        }

        public override async System.Threading.Tasks.Task FlushAsync()
        {
            foreach (System.IO.TextWriter thisWriter in MWriters)
            {
                await thisWriter.FlushAsync();
            }
        }

        public override void Write(char[] buffer, int index, int count)
        {
            foreach (System.IO.TextWriter thisWriter in MWriters)
            {
                thisWriter.Write(buffer, index, count);
            }
            WrittenText?.Invoke();
        }

        public override void Write(System.ReadOnlySpan<char> buffer)
        {
            foreach (System.IO.TextWriter thisWriter in MWriters)
            {
                thisWriter.Write(buffer);
            }
            WrittenText?.Invoke();
        }

        public override async System.Threading.Tasks.Task WriteAsync(char[] buffer, int index, int count)
        {
            foreach (System.IO.TextWriter thisWriter in MWriters)
            {
                await thisWriter.WriteAsync(buffer, index, count);
            }
            WrittenText?.Invoke();
        }


        public override async System.Threading.Tasks.Task WriteAsync(System.ReadOnlyMemory<char> buffer, System.Threading.CancellationToken cancellationToken = default)
        {
            foreach (System.IO.TextWriter thisWriter in MWriters)
            {
                await thisWriter.WriteAsync(buffer, cancellationToken);
            }
            WrittenText?.Invoke();
        }

        protected override void Dispose(bool disposing)
        {
            foreach (System.IO.TextWriter thisWriter in MWriters)
            {
                thisWriter.Dispose();
            }
        }


        public override async System.Threading.Tasks.ValueTask DisposeAsync()
        {
            foreach (System.IO.TextWriter thisWriter in MWriters)
            {
                await thisWriter.DisposeAsync();
            }
        }

        public override void Close()
        {
            foreach (System.IO.TextWriter thisWriter in MWriters)
            {
                thisWriter.Close();
            }
        }
    }
}