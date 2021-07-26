using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using DynamicData.Binding;
using WDE.Module.Attributes;
using WDE.PacketViewer.ViewModels;
using WoWPacketParser.Proto;

namespace WDE.PacketViewer.Filtering
{
    [AutoRegister]
    [SingleInstance]
    public class ParallelPacketFilteringService : IPacketFilteringService
    {
        public async Task<ObservableCollection<PacketViewModel>?> Filter(IList<PacketViewModel> all, 
            string filter, 
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
                    filtered.AddRange(all);
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
                            if (evaluator.Evaluate(all[index]) is true)
                                partialResult.Add(all[index]);

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

            if (filtered.Count > 0)
            {
                DateTime prevTime = filtered[0].Time;
                foreach (var packet in filtered)
                {
                    packet.Diff = (int)packet.Time.Subtract(prevTime).TotalMilliseconds;
                    prevTime = packet.Time;
                }
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