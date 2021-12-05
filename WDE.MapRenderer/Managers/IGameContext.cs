using System.Collections;
using WDE.MpqReader;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers
{
    public interface IGameFiles
    {
        Task<PooledArray<byte>?> ReadFile(string fileName);
        PooledArray<byte>? ReadFileSyncPool(string fileName);
        byte[]? ReadFileSync(string fileName);
        string Adt(string mapName, int x, int y);
        string Wdt(string mapName);
        bool Initialize();
    }
    
    public interface IGameContext
    {
        event Action<int>? ChangedMap;
        Map CurrentMap { get; }
        void SetMap(int id);
        void StartCoroutine(IEnumerator coroutine);
    }
}