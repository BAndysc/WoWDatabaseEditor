using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        private bool AcceptFilterData(IPacketViewModelStore store, PacketViewModel packet, IReadOnlyFilterData filterData)
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
            
            if (filterData.IncludedOpcodes != null || filterData.IncludedOpcodesWildcards != null)
            {
                bool any = false;
                if (filterData.IncludedOpcodes != null)
                {
                    foreach (var opcode in filterData.IncludedOpcodes)
                        if (opcode == packet.Opcode)
                        {
                            any = true;
                            break;
                        }
                }
                
                if (filterData.IncludedOpcodesWildcards != null)
                {
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
            }
            else if (filterData.ExcludedOpcodes != null)
            {
                if (filterData.ExcludedOpcodes.Contains(packet.Opcode))
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
                    if ((packet.MainActor?.Equals(guid) ?? false) || store.GetText(packet).Contains(guid.ToHexString()))
                        return true;
                }

                return false;
            }
            else if (filterData.ExcludedGuids != null)
            {
                foreach (var guid in filterData.ExcludedGuids)
                {
                    if (store.GetText(packet).Contains(guid.ToHexString()))
                        return false;
                }

                return true;
            }

            return true;
        }

        public bool IsMatched(PacketViewModel packet, IPacketViewModelStore store, string filter, IReadOnlyFilterData? filterData)
        {
            if (string.IsNullOrEmpty(filter))
            {
                if (filterData == null)
                {
                    return true;
                }
                else
                {
                    return AcceptFilterData(store, packet, filterData);
                }
            }
            else
            {
                var playerGuid = new UniversalGuid(); // playerLogin?.PlayerGuid ?? new UniversalGuid()
                var evaluator = new DatabaseExpressionEvaluator(filter, playerGuid, store);
                if (filterData == null || AcceptFilterData(store, packet, filterData))
                {
                    try
                    {
                        return evaluator.Evaluate(packet).Boolean is true;
                    }
                    catch (Exception)
                    {
                        return true; // i.e. when the fitler is invalid
                    }
                }
            }

            return false;
        }

        public async Task<IReadOnlyList<PacketViewModel>?> Filter(IReadOnlyList<PacketViewModel> all,
            IPacketViewModelStore store,
            string filter, 
            IReadOnlyFilterData? filterData,
            CancellationToken cancellationToken,
            IProgress<float> progress)
        {
            int count = all.Count;
            int threads = Environment.ProcessorCount;
            int packetsPerThread = count / threads;
            progress.Report(0);

            if (string.IsNullOrEmpty(filter))
            {
                // fast hot path
                if (filterData == null)
                {
                    return all;
                }
                else
                {
                    var filtered = new List<PacketViewModel>(all.Count);
                    foreach (var packet in all)
                    {
                        if (AcceptFilterData(store, packet, filterData))
                            filtered.Add(packet);
                    }

                    return filtered;
                }
            }
            else
            {
                PacketPlayerLogin? playerLogin = FindPlayerLogin(all);
                
                Task[] tasks = new Task[threads];
                PacketViewModel[]?[] partialResults = new PacketViewModel[]?[threads];
                int[] partialResultsCount = new int[threads];
            
                for (int j = 0; j < threads; ++j)
                {
                    int CPU = j;
                    tasks[j] = Task.Run(() =>
                    {
                        var evaluator = new DatabaseExpressionEvaluator(filter, playerLogin?.PlayerGuid ?? new UniversalGuid(), store);
                        var partialResult = ArrayPool<PacketViewModel>.Shared.Rent(packetsPerThread);
                        var partialResultIndex = 0;
                        var upTo = CPU == threads - 1 ? count : (CPU + 1) * packetsPerThread;
                    
                        for (int index = CPU * packetsPerThread; index < upTo; ++index)
                        {
                            if (filterData == null || AcceptFilterData(store, all[index], filterData))
                            {
                                if (evaluator.Evaluate(all[index]).Boolean is true)
                                    partialResult[partialResultIndex++] = all[index];
                            }
                            
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            if (CPU == 0)
                            {
                                if (index % 10000 == 0)
                                    progress.Report(1.0f * index / packetsPerThread);
                            }
                        }

                        if (cancellationToken.IsCancellationRequested)
                        {
                            partialResults[CPU] = null;
                            ArrayPool<PacketViewModel>.Shared.Return(partialResult);
                            return;
                        }

                        partialResultsCount[CPU] = partialResultIndex;
                        partialResults[CPU] = partialResult;
                    }, cancellationToken);
                }
            
                await Task.WhenAll(tasks).ConfigureAwait(true);
                
                progress.Report(1);

                if (cancellationToken.IsCancellationRequested)
                    return null;

                var filtered = new List<PacketViewModel>(partialResultsCount.Sum());
                for (int thread = 0; thread < threads; ++thread)
                {
                    if (partialResults[thread] != null)
                    {
                        for (int i = 0; i < partialResultsCount[thread]; ++i)
                        {
                            filtered.Add(partialResults[thread]![i]);
                        }
                        ArrayPool<PacketViewModel>.Shared.Return(partialResults[thread]!);
                    }
                }
                return filtered;
            }
        }

        private PacketPlayerLogin? FindPlayerLogin(IReadOnlyList<PacketViewModel> all)
        {
            // CMSG_PLAYER_LOGIN should always be in the beginning, no point in traversing whole sniff
            for (int i = 0; i < Math.Min(2000, all.Count); ++i)
                if (all[i].Packet.KindCase == PacketHolder.KindOneofCase.PlayerLogin)
                    return all[i].Packet.PlayerLogin;
            return null;
        }
    }
}