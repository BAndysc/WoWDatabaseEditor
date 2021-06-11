using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.PacketViewer.ViewModels;

namespace WDE.PacketViewer.Filtering
{
    [UniqueProvider]
    public interface IPacketFilteringService
    {
        Task<ObservableCollection<PacketViewModel>?> Filter(IList<PacketViewModel> all, 
            string filter, 
            CancellationToken cancellationToken,
            IProgress<float> progress);
    }
}