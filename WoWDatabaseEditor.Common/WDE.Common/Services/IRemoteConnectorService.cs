using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    public enum RemoteCommandType
    {
        Send,
        Receive,
        Debug
    }
    
    public class RemoteConnectorException : Exception
    {
        public RemoteConnectorException(string error, Exception? innerException = null) : base(error, innerException)
        {
        
        }
    }
    
    [UniqueProvider]
    public interface IRemoteConnectorService
    {
        event Action<string, TimeSpan, RemoteCommandType> OnLog;
        bool IsConnected { get; }
        bool HasValidSettings { get; }
        
        Task<string> ExecuteCommand(IRemoteCommand command);
        Task ExecuteCommands(IList<IRemoteCommand> commands);

        event Action<string> EditorCommandReceived;
        event Action EditorConnected;
        event Action EditorDisconnected;

        IList<IRemoteCommand> Merge(IList<IRemoteCommand> commands);
    }

    public class DebuggingFeaturesDisabledException : Exception
    {
        public DebuggingFeaturesDisabledException(string msg) : base(msg)
        {
        }
    }

    public class CouldNotConnectToRemoteServer : Exception
    {
        public CouldNotConnectToRemoteServer(Exception inner) : base("Couldn't connect to the remote server", inner)
        {
        }
    }
    
    public interface IRemoteCommand
    {
        RemoteCommandPriority Priority { get; }
        string GenerateCommand();
        bool TryMerge(IRemoteCommand other, out IRemoteCommand? mergedCommand);
    }

    public class AnonymousRemoteCommand : IRemoteCommand
    {
        private readonly RemoteCommandPriority priority;
        private readonly string command;

        public AnonymousRemoteCommand(string command, RemoteCommandPriority priority = RemoteCommandPriority.Middle)
        {
            this.priority = priority;
            this.command = command;
        }

        public RemoteCommandPriority Priority => priority;
        public string GenerateCommand() => command;

        public bool TryMerge(IRemoteCommand other, out IRemoteCommand? mergedCommand)
        {
            mergedCommand = null;
            return false;
        }
    }

    public enum RemoteCommandPriority
    {
        VeryFirst = 0,
        First,
        Middle,
        Last,
        VeryLast
    }
}