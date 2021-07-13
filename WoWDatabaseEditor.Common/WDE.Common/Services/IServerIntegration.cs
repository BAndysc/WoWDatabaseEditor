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

    public readonly struct NearestGameObject
    {
        public uint Entry { get; init; }
        public float Distance { get; init; }
        public float X { get; init; }
        public float Y { get; init; }
        public float Z { get; init; }
    }
}