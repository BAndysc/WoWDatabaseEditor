using System;
using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.QuickAccess;


[UniqueProvider]
public interface IQuickAccessService
{
    Task Search(string text, Action<QuickAccessItem> produce, CancellationToken cancellationToken);
}