using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using DynamicData.Binding;
using WDE.Module.Attributes;
using WDE.PacketViewer.Services;
using WDE.PacketViewer.Utils;
using WDE.PacketViewer.ViewModels;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.Filtering
{
    [AutoRegister]
    [SingleInstance]
    public class ParallelPacketFilteringService : IPacketFilteringService
    {
        private bool AcceptFilterData(PacketViewModel packet, IReadOnlyFilterData filterData)
        {
            if (filterData.ForceIncludePacketNumbers != null)
            {
                if (filterData.ForceIncludePacketNumbers.Contains(packet.Id))
                    return true;
            }
            
            if (filterData.MinPacketNumber.HasValue && packet.Id < filterData.MinPacketNumber.Value)
                return false;
            if (filterData.MaxPacketNumber.HasValue && packet.Id > filterData.MaxPacketNumber.Value)
                return false;
            
            if (filterData.IncludedEntries != null)
            {
                if (!filterData.IncludedEntries.Contains(packet.Entry))
                    return false;
            }
            else if (filterData.ExcludedEntries != null)
            {
                if (filterData.ExcludedEntries.Contains(packet.Entry))
                    return false;
            }
            
            if (filterData.IncludedOpcodes != null)
            {
                if (!filterData.IncludedOpcodes.Contains(packet.Opcode))
                    return false;
            }
            else if (filterData.ExcludedOpcodes != null)
            {
                if (filterData.ExcludedOpcodes.Contains(packet.Opcode))
                    return false;
            }

            if (filterData.IncludedOpcodesWildcards != null)
            {
                bool any = false;
                foreach (var prefix in filterData.IncludedOpcodesWildcards)
                {
                    if (packet.Opcode.StartsWith(prefix))
                    {
                        any = true;
                        break;
                    }
                }

                if (!any)
                    return false;
            }
            if (filterData.ExcludedOpcodesWildcards != null)
            {
                foreach (var prefix in filterData.ExcludedOpcodesWildcards)
                {
                    if (packet.Opcode.StartsWith(prefix))
                        return false;
                }
            }
            
            if (filterData.IncludedGuids != null)
            {
                foreach (var guid in filterData.IncludedGuids)
                {
                    if ((packet.MainActor?.Equals(guid) ?? false) || packet.Text.Contains(guid.ToHexString()))
                        return true;
                }

                return false;
            }
            else if (filterData.ExcludedGuids != null)
            {
                foreach (var guid in filterData.ExcludedGuids)
                {
                    if (packet.Text.Contains(guid.ToHexString()))
                        return false;
                }

                return true;
            }

            return true;
        }
        
        public async Task<ObservableCollection<PacketViewModel>?> Filter(IList<PacketViewModel> all, 
            string filter, 
            IReadOnlyFilterData? filterData,
            CancellationToken cancellationToken,
            IProgress<float> progress)
        {
            int count = all.Count;
            int threads = Environment.ProcessorCount;
            int packetsPerThread = count / threads;
            progress.Report(0);

            var filtered = new ObservableCollectionExtended<PacketViewModel>();
            if (string.IsNullOrEmpty(filter))
            {
                using (filtered.SuspendNotifications())
                {
                    // fast hot path
                    if (filterData == null)
                    {
                        filtered.AddRange(all);
                    }
                    else
                    {
                        foreach (var packet in all)
                        {
                            if (AcceptFilterData(packet, filterData))
                                filtered.Add(packet);
                        }
                    }
                }
            }
            else
            {
                PacketPlayerLogin? playerLogin = FindPlayerLogin(all);
                
                Task[] tasks = new Task[threads];
                List<PacketViewModel>[] partialResults = new List<PacketViewModel>[threads];
            
                for (int j = 0; j < threads; ++j)
                {
                    int CPU = j;
                    tasks[j] = Task.Run(() =>
                    {
                        var evaluator = new DatabaseExpressionEvaluator(filter, playerLogin?.PlayerGuid ?? new UniversalGuid());
                        var partialResult = new List<PacketViewModel>(packetsPerThread);
                        var upTo = CPU == threads - 1 ? count : (CPU + 1) * packetsPerThread;
                    
                        for (int index = CPU * packetsPerThread; index < upTo; ++index)
                        {
                            if (filterData == null || AcceptFilterData(all[index], filterData))
                            {
                                if (evaluator.Evaluate(all[index]) is true)
                                    partialResult.Add(all[index]);
                            }
                            
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            if (CPU == 0)
                            {
                                if (index % 100 == 0)
                                    progress.Report(1.0f * index / packetsPerThread);
                            }
                        }

                        partialResults[CPU] = cancellationToken.IsCancellationRequested ? null! : partialResult;
                    }, cancellationToken);
                }
            
                await Task.WhenAll(tasks).ConfigureAwait(true);
                
                progress.Report(1);

                if (cancellationToken.IsCancellationRequested)
                    return null;

                using (filtered.SuspendNotifications())
                    foreach (var partialResult in partialResults)
                        filtered.AddRange(partialResult);
            }
            return filtered;
        }

        private PacketPlayerLogin? FindPlayerLogin(IList<PacketViewModel> all)
        {
            // CMSG_PLAYER_LOGIN should always be in the beginning, no point in traversing whole sniff
            for (int i = 0; i < Math.Min(2000, all.Count); ++i)
                if (all[i].Packet.KindCase == PacketHolder.KindOneofCase.PlayerLogin)
                    return all[i].Packet.PlayerLogin;
            return null;
        }
    }
}