using System.Threading.Tasks;

namespace WDE.Common.Collections;

public interface IAsyncIndexedCollection<T>
{
    Task<int> GetCount();
    Task<T> GetRow(int index);
}