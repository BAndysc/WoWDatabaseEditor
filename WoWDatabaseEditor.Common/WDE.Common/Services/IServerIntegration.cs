using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    [UniqueProvider]
    public interface IServerIntegration
    {
        Task<uint?> GetSelectedEntry();

        Task<IList<NearestGameObject>?> GetNearestGameObjects();
    }

    public class ReverseRemoteCommandAttribute : Attribute
    {
        public ReverseRemoteCommandAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }

    [NonUniqueProvider]
    public interface IReverseRemoteCommand
    {
        Task Invoke(ICommandArguments arguments);
        bool BringEditorToFront { get; }
    }

    [NonUniqueProvider]
    public interface IRawReverseRemoteCommand
    {
        Task Invoke(string raw);
        bool BringEditorToFront { get; }
    }

    public interface ICommandArguments
    {
        int LeftArguments { get; }
        int TotalArguments { get; }
        string TakeRestArguments { get; }
        bool TryGetUint(out uint number);
        bool TryGetInt(out int number);
        bool TryGetFloat(out float number);
        bool TryGetString(out string word);
    }
    
    public readonly struct NearestGameObject
    {
        public uint Entry { get; init; }
        public float Distance { get; init; }
        public float X { get; init; }
        public float Y { get; init; }
        public float Z { get; init; }
    }
}