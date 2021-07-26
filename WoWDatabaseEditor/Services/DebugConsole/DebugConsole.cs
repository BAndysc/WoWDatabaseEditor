using System;
using System.IO;
using System.Text;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.DebugConsole
{
    [AutoRegister]
    [SingleInstance]
    public class DebugConsole : IDebugConsole
    {
        private ConsoleOutputMultiplexer multiplexer;
        private readonly StringBuilder stringBuilder;
        private readonly StringWriter textWriter;
        
        public DebugConsole()
        {
            stringBuilder = new();
            textWriter = new(stringBuilder);
            multiplexer = new ConsoleOutputMultiplexer(textWriter);
            multiplexer.WrittenText += () => WrittenText?.Invoke();
        }

        public event Action? WrittenText;
        public string Log => stringBuilder.ToString();
    }

    [UniqueProvider]
    public interface IDebugConsole
    {
        event Action? WrittenText;
        string Log { get; }
    }
    
    public class ConsoleOutputMultiplexer : System.IDisposable
    {
        protected TextWriter? oldOut;
        protected MultiTextWriter? multiPlexer;
        public event Action? WrittenText;

        public ConsoleOutputMultiplexer(StringWriter stringWriter)
        {
            oldOut = System.Console.Out;

            try
            {
                multiPlexer = new MultiTextWriter(oldOut.Encoding, oldOut, stringWriter);
                multiPlexer.WrittenText += () => WrittenText?.Invoke();

                System.Console.SetOut(multiPlexer);
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine("Cannot open Redirect.txt for writing");
                System.Console.WriteLine(e.Message);
            }
        }

        void System.IDisposable.Dispose()
        {
            if (oldOut != null)
                System.Console.SetOut(oldOut);

            if (multiPlexer != null)
            {
                multiPlexer.Flush();
                multiPlexer.Close();
            }
        }
    }
}