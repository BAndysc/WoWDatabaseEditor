using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.PacketViewer.Services;
using WDE.PacketViewer.ViewModels;

namespace WDE.PacketViewer.Filtering
{
    [UniqueProvider]
    public interface IPacketFilteringService
    {
        Task<ObservableCollection<PacketViewModel>?> Filter(IList<PacketViewModel> all, 
            string filter, 
            IReadOnlyFilterData? filterData,
            CancellationToken cancellationToken,
            IProgress<float> progress);
    }
}