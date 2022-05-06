using System.Collections;
using TheMaths;
using WDE.MpqReader;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public interface IGameFiles
    {
        Task<PooledArray<byte>?> ReadFile(string fileName);
        PooledArray<byte>? ReadFileSyncPool(string fileName);
        byte[]? ReadFileSync(string fileName);
        byte[]? ReadFileSyncLocked(string fileName);
        string Adt(string mapName, int x, int y);
        string Wdt(string mapName);
        bool Initialize();
    }
    
    public interface IGameContext
    {
        float Delta { get; }
        event Action<int>? ChangedMap;
        Map CurrentMap { get; }
        void SetMap(int id, Vector3? position = null);
        void StartCoroutine(IEnumerator coroutine);
    }
}