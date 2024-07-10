using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using Prism.Commands;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.PacketViewer.Processing.Processors;
using WDE.PacketViewer.Processing.Processors.Utils;
using WDE.PacketViewer.Utils;
using WDE.PacketViewer.Utils.IntervalTrees;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.ViewModels;

public class UpdateHistoryViewModel : ObservableBase
{
    public interface IUpdateItem
    {
        public string Key { get; }
        public string Value { get; }
        public int? PrevNumber { get; }
        public int? NextNumber { get; }
        public string? Prev { get; }
        public string? Next { get; }
        public string? Pretty { get; }
    }
    
    public class UpdateItem<T> : IUpdateItem where T : notnull
    {
        public UpdateItem(int time, string key, Interval<int, T> value, Interval<int, T>? prev, Interval<int, T>? next, IParameter<T>? param)
        {
            Key = key;
            Value = value.value.ToString()!;
            Pretty = param?.ToString(value.value);
            Prev = prev?.value.ToString();
            Next = next?.value.ToString();
            PrevNumber = prev?.start;
            NextNumber = next?.start;
            if (value.start == time && value.value is long l && prev is { value: long old } && param is FlagParameter flagParameter)
            {
                StringBuilder sb = new();
                for (int i = 0; i < 32; ++i)
                {
                    var flagIsSet = (l & (1 << i)) != 0;
                    var flagWasSet = (old & (1 << i)) != 0;

                    var name = flagParameter.ToString(1 << i);

                    if (flagIsSet && !flagWasSet)
                        sb.Append($"(+) {name}, ");
                    else if (flagIsSet && flagWasSet)
                        sb.Append($"{name}, ");
                }

                bool removedWritten = false;
                for (int i = 0; i < 32; ++i)
                {
                    var flagIsSet = (l & (1 << i)) != 0;
                    var flagWasSet = (old & (1 << i)) != 0;

                    if (!flagIsSet && flagWasSet)
                    {
                        if (!removedWritten)
                        {
                            sb.Append("   Removed flags: ");
                            removedWritten = true;
                        }
                        var name = flagParameter.ToString(1 << i);
                        sb.Append($"(-) {name}, ");
                    }
                }

                Pretty = sb.ToString();
            }
        }

        public string Key { get; }
        public string Value { get; }
        public int? PrevNumber { get; }
        public int? NextNumber { get; }
        public string? Prev { get; }
        public string? Next { get; }
        public string? Pretty { get; }
    }
    
    private readonly Func<IUpdateFieldsHistory> historyCreator;
    private IUpdateFieldsHistory? currentHistory;
    private UniversalGuid? currentGuid;

    public Func<IEnumerable, string, CancellationToken, Task<IEnumerable>> FindGuidAsyncPopulator { get; }
    public ObservableCollection<UniversalGuid> Guids { get; } = new();
    public ObservableCollection<IUpdateItem> CurrentValues { get; } = new ObservableCollection<IUpdateItem>();

    public UniversalGuid? CurrentGuid
    {
        get => currentGuid;
        set => SetProperty(ref currentGuid, value);
    }

    public bool LockCurrentGuid
    {
        get => lockCurrentGuid;
        set => SetProperty(ref lockCurrentGuid, value);
    }

    public DelegateCommand<int?> JumpToPacket { get; }

    private PacketViewModel? currentPacket;
    private bool lockCurrentGuid;

    public UpdateHistoryViewModel(Func<IUpdateFieldsHistory> historyCreator, 
        PrettyFlagParameter prettyFlagParameter,
        DelegateCommand<int?> goToPacket,
        IObservable<PacketViewModel?> selectedPacket)
    {
        this.historyCreator = historyCreator;

        JumpToPacket = goToPacket;
        CurrentGuid = currentGuid;

        AutoDispose(this.ToObservable(o => o.CurrentGuid).SubscribeAction(currentGuid =>
        {
            CurrentValues.Clear();
            if (currentHistory == null || currentGuid == null || currentPacket == null)
                return;
            
            var objectState = currentHistory.GetState(currentGuid.Value, currentPacket.Id);
            CurrentValues.Add(new UpdateItem<SniffObjectState>(currentPacket.Id, "STATE", objectState.current, objectState.previous, objectState.next, null));

            if (objectState.current.value is SniffObjectState.Spawned or SniffObjectState.InRange)
            {
                foreach (var intValue in currentHistory.IntValues(currentGuid.Value, currentPacket.Id))
                {
                    var parameter = prettyFlagParameter.GetPrettyParameter(intValue.key);
                    CurrentValues.Add(new UpdateItem<long>(currentPacket.Id, intValue.key, intValue.current, intValue.previous, intValue.next, parameter));
                }
            
                foreach (var floatValue in currentHistory.FloatValues(currentGuid.Value, currentPacket.Id))
                    CurrentValues.Add(new UpdateItem<float>(currentPacket.Id, floatValue.Item1, floatValue.Item2, floatValue.previous, floatValue.next, null));
            }

            CurrentValues.Sort((a, b) =>
            {
                if (a!.Key == "STATE")
                    return -1;
                if (b!.Key == "STATE")
                    return 1;
                return String.Compare(a!.Key, b!.Key, StringComparison.Ordinal);
            });
        }));
        
        AutoDispose(selectedPacket.SubscribeAction(p =>
        {
            currentPacket = p;

            if (!LockCurrentGuid)
                currentGuid = p?.MainActor;
            
            RaisePropertyChanged(nameof(CurrentGuid));
        }));

        FindGuidAsyncPopulator = (items, s, token) =>
        {
            return Task.Run(() =>
            {
                List<object> result = new();
                foreach (var guid in Guids)
                {
                    if (token.IsCancellationRequested)
                        return null!;
                    if (guid.ToWowParserString().Contains(s))
                        result.Add(guid);
                }
                return (IEnumerable)result;
            }, token);
        };
    }

    public void Invalidate(IEnumerable<PacketViewModel> packets)
    {
        currentHistory = historyCreator();
        foreach (var p in packets)
            currentHistory.Process(ref p.Packet);
        currentHistory.Finish();
        
        Guids.Clear();
        Guids.AddRange(currentHistory.AllGuids);
    }
}