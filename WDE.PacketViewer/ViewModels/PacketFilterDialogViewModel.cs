using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using DynamicData;
using Newtonsoft.Json;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.PacketViewer.Services;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.ViewModels
{
    public class PacketFilterDialogViewModel : ObservableBase, IDialog
    {
        private readonly IClipboardService clipboardService;
        private bool hasMinPacketNumber;
        private bool hasMaxPacketNumber;
        private int minPacketNumber;
        private int maxPacketNumber;

        private struct SerializedClipboardData
        {
            public int? MinPacket { get; set; }
            public int? MaxPacket { get; set; }
            public List<uint>? ExcludedEntries { get; set; }
            public List<uint>? IncludedEntries { get; set; }
            public List<string>? ExcludedOpcodes { get; set; }
            public List<string>? IncludedOpcodes { get; set; }
            public List<string>? ExcludedGuids { get; set; }
            public List<string>? IncludedGuids { get; set; }
            public List<int>? ForceIncludedNumbers { get; set; }
        }
        
        public PacketFilterDialogViewModel(IClipboardService clipboardService,
            IReadOnlyFilterData? filterData)
        {
            this.clipboardService = clipboardService;
            Accept = new DelegateCommand(() => CloseOk?.Invoke());
            Cancel = new DelegateCommand(() => CloseCancel?.Invoke());

            ClearFiltersCommand = new AsyncAutoCommand(async () =>
            {
                ExcludedEntries = "";
                IncludedEntries = "";
                ExcludedOpcodes = "";
                IncludedOpcodes = "";
                ExcludedGuids.Clear();
                IncludedGuids.Clear();
                CommaSeparatedPackets = "";
                HasMaxPacketNumber = false;
                HasMinPacketNumber = false;
                MinPacketNumber = 0;
                MaxPacketNumber = 0;
            });
            CopyFiltersCommand = new AsyncAutoCommand(async () =>
            {
                var data = new SerializedClipboardData()
                {
                    ExcludedEntries = GenerateList(excludedEntries, s => uint.TryParse(s, out _), uint.Parse),
                    IncludedEntries = GenerateList(includedOpcodes, s => uint.TryParse(s, out _), uint.Parse),
                    ExcludedGuids = ExcludedGuids.Count == 0 ? null : ExcludedGuids.Select(s => s.ToHexWithTypeString()).ToList(),
                    IncludedGuids = IncludedGuids.Count == 0 ? null : IncludedGuids.Select(s => s.ToHexWithTypeString()).ToList(),
                    ExcludedOpcodes = GenerateList(excludedOpcodes, _ => true, s => s.ToUpper()),
                    IncludedOpcodes = GenerateList(includedOpcodes, _ => true, s => s.ToUpper()),
                    ForceIncludedNumbers = GenerateList(commaSeparatedPackets, s => int.TryParse(s, out var x) && x >= 0, int.Parse),
                    MinPacket = HasMinPacketNumber ? MinPacketNumber : null,
                    MaxPacket = HasMaxPacketNumber ? MaxPacketNumber : null,
                };
                var serialized = JsonConvert.SerializeObject(data, Formatting.Indented, new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                clipboardService.SetText(serialized);
            });
            PasteFiltersCommand = new AsyncAutoCommand(async () =>
            {
                var text = await clipboardService.GetText();
                if (string.IsNullOrEmpty(text))
                    return;

                SerializedClipboardData? serialized = null;
                try
                {
                    serialized = JsonConvert.DeserializeObject<SerializedClipboardData>(text);
                }
                catch (Exception)
                {
                    // ignored
                }

                if (!serialized.HasValue)
                    return;
                
                HasMinPacketNumber = serialized.Value.MinPacket.HasValue;
                HasMaxPacketNumber = serialized.Value.MaxPacket.HasValue;
                MinPacketNumber = serialized.Value.MinPacket ?? 0;
                MaxPacketNumber = serialized.Value.MaxPacket ?? 0;
                
                if (serialized.Value.ExcludedEntries != null)
                    ExcludedEntries = string.Join("\n", serialized.Value.ExcludedEntries);
                if (serialized.Value.IncludedEntries != null)
                    IncludedEntries = string.Join("\n", serialized.Value.IncludedEntries);
                if (serialized.Value.ExcludedGuids != null)
                    ExcludedGuids.AddRange(serialized.Value.ExcludedGuids.StringToGuids());
                if (serialized.Value.IncludedGuids != null)
                    IncludedGuids.AddRange(serialized.Value.IncludedGuids.StringToGuids());
                if (serialized.Value.ExcludedOpcodes != null)
                    ExcludedOpcodes = string.Join("\n", serialized.Value.ExcludedOpcodes);
                if (serialized.Value.IncludedOpcodes != null)
                    IncludedOpcodes = string.Join("\n", serialized.Value.IncludedOpcodes);
                if (serialized.Value.ForceIncludedNumbers != null)
                    CommaSeparatedPackets = string.Join("\n", serialized.Value.ForceIncludedNumbers);
            });
            
            if (filterData != null)
            {
                HasMinPacketNumber = filterData.MinPacketNumber.HasValue;
                HasMaxPacketNumber = filterData.MaxPacketNumber.HasValue;
                MinPacketNumber = filterData.MinPacketNumber ?? 0;
                MaxPacketNumber = filterData.MaxPacketNumber ?? 0;
                
                if (filterData.ExcludedEntries != null)
                    ExcludedEntries = string.Join("\n", filterData.ExcludedEntries);
                if (filterData.IncludedEntries != null)
                    IncludedEntries = string.Join("\n", filterData.IncludedEntries);
                if (filterData.ExcludedGuids != null)
                    ExcludedGuids.AddRange(filterData.ExcludedGuids);
                if (filterData.IncludedGuids != null)
                    IncludedGuids.AddRange(filterData.IncludedGuids);
                if (filterData.ExcludedOpcodes != null)
                    ExcludedOpcodes = string.Join("\n", filterData.ExcludedOpcodes);
                if (filterData.IncludedOpcodes != null)
                    IncludedOpcodes = string.Join("\n", filterData.IncludedOpcodes);
                if (filterData.ForceIncludePacketNumbers != null)
                    CommaSeparatedPackets = string.Join("\n", filterData.ForceIncludePacketNumbers);
            }

            DeleteIncludedGuid = new DelegateCommand<UniversalGuid?>(guid =>
            {
                if (guid != null)
                    IncludedGuids.Remove(guid.Value);
            });
            
            DeleteExcludedGuid = new DelegateCommand<UniversalGuid?>(guid =>
            {
                if (guid != null)
                    ExcludedGuids.Remove(guid.Value);
            });

            AutoDispose(this.ToObservable(o => o.ExcludedEntries).SubscribeAction(_ => RaisePropertyChanged(nameof(EntriesHeader))));
            AutoDispose(this.ToObservable(o => o.IncludedEntries).SubscribeAction(_ => RaisePropertyChanged(nameof(EntriesHeader))));
            AutoDispose(ExcludedGuids.ToCountChangedObservable().SubscribeAction(_ => RaisePropertyChanged(nameof(GuidsHeader))));
            AutoDispose(IncludedGuids.ToCountChangedObservable().SubscribeAction(_ => RaisePropertyChanged(nameof(GuidsHeader))));
            AutoDispose(this.ToObservable(o => o.ExcludedOpcodes).SubscribeAction(_ => RaisePropertyChanged(nameof(OpcodesHeader))));
            AutoDispose(this.ToObservable(o => o.IncludedOpcodes).SubscribeAction(_ => RaisePropertyChanged(nameof(OpcodesHeader))));
        }

        public IFilterData FilterData =>
            new FilterData(
                HasMinPacketNumber ? minPacketNumber : null,
                HasMaxPacketNumber ? maxPacketNumber : null,
                GenerateList(excludedEntries, s => uint.TryParse(s, out _), uint.Parse),
                GenerateList(includedEntries, s => uint.TryParse(s, out _), uint.Parse),
                ExcludedGuids.Count == 0 ? null : ExcludedGuids,
                IncludedGuids.Count == 0 ? null : IncludedGuids,
                GenerateList(excludedOpcodes, _ => true, s => s.ToUpper()),
                GenerateList(includedOpcodes, _ => true, s => s.ToUpper()),
                GenerateList(commaSeparatedPackets, s => int.TryParse(s, out var x) && x >= 0, int.Parse)
            );

        private List<T>? GenerateList<T>(string str, Func<string, bool> can, Func<string, T> convert)
        {
            if (string.IsNullOrEmpty(str))
                return null;

            var list = str.Split(',', '\n')
                .Select(s => s.Trim())
                .Where(can)
                .Select(convert)
                .ToList();
            if (list.Count == 0)
                return null;
            return list;
        }

        public ObservableCollection<UniversalGuid> ExcludedGuids { get; } = new();
        public ObservableCollection<UniversalGuid> IncludedGuids { get; } = new();
        
        private string excludedOpcodes = "";
        public string ExcludedOpcodes
        {
            get => excludedOpcodes;
            set => SetProperty(ref excludedOpcodes, value);
        }
        
        private string includedOpcodes = "";
        public string IncludedOpcodes
        {
            get => includedOpcodes;
            set => SetProperty(ref includedOpcodes, value);
        }
        
        private string excludedEntries = "";
        public string ExcludedEntries
        {
            get => excludedEntries;
            set => SetProperty(ref excludedEntries, value);
        }
        
        private string includedEntries = "";
        public string IncludedEntries
        {
            get => includedEntries;
            set => SetProperty(ref includedEntries, value);
        }

        private string commaSeparatedPackets = "";
        public string CommaSeparatedPackets
        {
            get => commaSeparatedPackets;
            set => SetProperty(ref commaSeparatedPackets, value);
        }

        public bool HasMinPacketNumber
        {
            get => hasMinPacketNumber;
            set => SetProperty(ref hasMinPacketNumber, value);
        }

        public bool HasMaxPacketNumber
        {
            get => hasMaxPacketNumber;
            set => SetProperty(ref hasMaxPacketNumber, value);
        }

        public int MinPacketNumber
        {
            get => minPacketNumber;
            set => SetProperty(ref minPacketNumber, value);
        }

        public int MaxPacketNumber
        {
            get => maxPacketNumber;
            set => SetProperty(ref maxPacketNumber, value);
        }

        public ICommand Accept { get; }
        public ICommand Cancel { get; }
        public AsyncAutoCommand ClearFiltersCommand { get; }
        public AsyncAutoCommand CopyFiltersCommand { get; }
        public AsyncAutoCommand PasteFiltersCommand { get; }
        
        public int DesiredWidth => 500;
        public int DesiredHeight => 700;
        public string Title => "Basic packets filtering";
        public bool Resizeable => true;
        
        public string EntriesHeader
        {
            get
            {
                var excluded = string.IsNullOrEmpty(excludedEntries) ? 0 : excludedEntries.Count(l => l == ',' || l == '\n') + 1;
                var included = string.IsNullOrEmpty(includedEntries) ? 0 : includedEntries.Count(l => l == ',' || l == '\n') + 1;
                if (excluded == 0 && included == 0)
                    return "Entries";
                if (included > 0)
                    return $"Entries: {included} included";
                return $"Entries: {excluded} excluded";
            }
        }

        public string OpcodesHeader
        {
            get
            {
                var excluded = string.IsNullOrEmpty(excludedOpcodes) ? 0 : excludedOpcodes.Count(l => l == ',' || l == '\n') + 1;
                var included = string.IsNullOrEmpty(includedOpcodes) ? 0 : includedOpcodes.Count(l => l == ',' || l == '\n') + 1;
                if (included + excluded == 0)
                    return "Opcodes";
                if (included > 0)
                    return $"Opcodes: {included} included";
                return $"Opcodes: {excluded} excluded";
            }
        }

        public string GuidsHeader
        {
            get
            {
                if (ExcludedGuids.Count == 0 && IncludedGuids.Count == 0)
                    return "GUIDs";
                if (IncludedGuids.Count > 0)
                    return $"GUIDs: {IncludedGuids.Count.ToString()} included";
                return $"GUIDs: {ExcludedGuids.Count.ToString()} excluded";
            }
        }

        public DelegateCommand<UniversalGuid?> DeleteIncludedGuid { get; }
        public DelegateCommand<UniversalGuid?> DeleteExcludedGuid { get; }
        
        public event Action? CloseCancel;
        public event Action? CloseOk;

    }
}