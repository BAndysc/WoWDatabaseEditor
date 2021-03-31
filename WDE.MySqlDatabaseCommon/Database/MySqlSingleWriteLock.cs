using System;
using System.Threading;
using System.Threading.Tasks;

namespace WDE.MySqlDatabaseCommon.Database
{
    public class DatabaseLock
    {
        protected static SemaphoreSlim WriteMutex = new SemaphoreSlim(1);

        public static async Task<IDisposable> WriteLock()
        {
            await WriteMutex.WaitAsync();
            return new MySqlWriteLock();
        }

        private struct MySqlWriteLock : IDisposable
        {
            public void Dispose()
            {
                WriteMutex.Release();
            }
        }
    }

}