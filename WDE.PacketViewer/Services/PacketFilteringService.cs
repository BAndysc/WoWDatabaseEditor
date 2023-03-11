using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Module.Attributes;
using WDE.PacketViewer.ViewModels;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.Services
{
    public interface IReadOnlyFilterData
    {
        public int? MinPacketNumber { get; }
        public int? MaxPacketNumber { get; }
        public IReadOnlySet<uint>? ExcludedEntries { get; }
        public IReadOnlySet<uint>? IncludedEntries { get; }
        public IReadOnlySet<UniversalGuid>? ExcludedGuids { get; }
        public IReadOnlySet<UniversalGuid>? IncludedGuids { get; }
        public IReadOnlySet<string>? ExcludedOpcodes { get; }
        public IReadOnlySet<string>? IncludedOpcodes { get; }
        public IReadOnlySet<int>? ForceIncludePacketNumbers { get; }

        public IEnumerable<string>? IncludedOpcodesWildcards { get; }
        public IEnumerable<string>? ExcludedOpcodesWildcards { get; }
    }
    
    public interface IFilterData : IReadOnlyFilterData
    {
        void ExcludeEntry(uint entry);
        void IncludeEntry(uint entry);
        void ExcludeOpcode(string opcode);
        void IncludeOpcode(string opcode);
        void ExcludeGuid(UniversalGuid guid);
        void IncludeGuid(UniversalGuid guid);
        void IncludePacketNumber(int packet);
        void SetMinMax(int? minPacket, int? maxPacket);
        bool IsEmpty { get; }
    }
    
    public class FilterData : IFilterData
    {
        private HashSet<uint>? excludedEntries;
        private HashSet<uint>? includedEntries;
        private HashSet<UniversalGuid>? excludedGuids;
        private HashSet<UniversalGuid>? includedGuids;
        private HashSet<string>? excludedOpcodes;
        private HashSet<string>? includedOpcodes;
        private HashSet<int>? includedPacketNumbers;
        private List<string>? includedOpcodesWildcards;
        private List<string>? excludedOpcodesWildcards;

        public int? MinPacketNumber { get; set; }
        public int? MaxPacketNumber { get; set; }
        public IReadOnlySet<uint>? ExcludedEntries => excludedEntries;
        public IReadOnlySet<uint>? IncludedEntries => includedEntries;
        public IReadOnlySet<UniversalGuid>? ExcludedGuids => excludedGuids;
        public IReadOnlySet<UniversalGuid>? IncludedGuids => includedGuids;
        public IReadOnlySet<string>? ExcludedOpcodes => excludedOpcodes;
        public IReadOnlySet<string>? IncludedOpcodes => includedOpcodes;
        public IReadOnlySet<int>? ForceIncludePacketNumbers => includedPacketNumbers;
        public IEnumerable<string>? IncludedOpcodesWildcards => includedOpcodesWildcards;
        public IEnumerable<string>? ExcludedOpcodesWildcards => excludedOpcodesWildcards;

        public FilterData()
        {
        }

        public FilterData(
            int? minPacketNumber, int? maxPacketNumber,
            ICollection<uint>? excludedEntries,
            ICollection<uint>? includedEntries,
            ICollection<UniversalGuid>? excludedGuids,
            ICollection<UniversalGuid>? includedGuids, 
            ICollection<string>? excludedOpcodes, 
            ICollection<string>? includedOpcodes, 
            ICollection<int>? includedPacketNumbers)
        {
            this.MinPacketNumber = minPacketNumber;
            this.MaxPacketNumber = maxPacketNumber;
            this.excludedEntries = excludedEntries == null ? null : new HashSet<uint>(excludedEntries);
            this.includedEntries = includedEntries == null ? null : new HashSet<uint>(includedEntries);
            this.excludedGuids = excludedGuids == null ? null : new HashSet<UniversalGuid>(excludedGuids);
            this.includedGuids = includedGuids == null ? null : new HashSet<UniversalGuid>(includedGuids);
            this.excludedOpcodes = excludedOpcodes == null ? null : new HashSet<string>(excludedOpcodes);
            this.includedOpcodes = includedOpcodes == null ? null : new HashSet<string>(includedOpcodes);
            this.includedPacketNumbers = includedPacketNumbers == null ? null : new HashSet<int>(includedPacketNumbers);
            includedOpcodesWildcards = includedOpcodes?.Where(c => c.EndsWith("*")).Select(c => c.Substring(0, c.Length - 1)).ToList();
            excludedOpcodesWildcards = excludedOpcodes?.Where(c => c.EndsWith("*")).Select(c => c.Substring(0, c.Length - 1)).ToList();
        }

        public void ExcludeEntry(uint entry)
        {
            includedEntries?.Remove(entry);
            excludedEntries ??= new HashSet<uint>();
            excludedEntries.Add(entry);
        }

        public void IncludeEntry(uint entry)
        {
            excludedEntries?.Remove(entry);
            includedEntries ??= new HashSet<uint>();
            includedEntries.Add(entry);
        }

        public void ExcludeOpcode(string opcode)
        {
            includedOpcodes?.Remove(opcode);
            excludedOpcodes ??= new HashSet<string>();
            if (excludedOpcodes.Add(opcode) && opcode.EndsWith("*"))
            {
                excludedOpcodesWildcards ??= new();
                excludedOpcodesWildcards.Add(opcode.Substring(0, opcode.Length - 1));
            }
        }

        public void IncludeOpcode(string opcode)
        {
            excludedOpcodes?.Remove(opcode);
            includedOpcodes ??= new HashSet<string>();
            if (includedOpcodes.Add(opcode) && opcode.EndsWith("*"))
            {
                includedOpcodesWildcards ??= new();
                includedOpcodesWildcards.Add(opcode.Substring(0, opcode.Length - 1));
            }
        }

        public void ExcludeGuid(UniversalGuid guid)
        {
            includedGuids?.Remove(guid);
            excludedGuids ??= new HashSet<UniversalGuid>();
            excludedGuids.Add(guid);
        }

        public void IncludeGuid(UniversalGuid guid)
        {
            excludedGuids?.Remove(guid);
            includedGuids ??= new HashSet<UniversalGuid>();
            includedGuids.Add(guid);
        }

        public void SetMinMax(int? minPacket, int? maxPacket)
        {
            MinPacketNumber = minPacket;
            MaxPacketNumber = maxPacket;
        }

        public bool IsEmpty => !MinPacketNumber.HasValue &&
                               !MaxPacketNumber.HasValue &&
                               (excludedEntries?.Count ?? 0) == 0 &&
                               (includedEntries?.Count ?? 0) == 0 &&
                               (excludedGuids?.Count ?? 0) == 0 &&
                               (includedGuids?.Count ?? 0) == 0 &&
                               (excludedOpcodes?.Count ?? 0) == 0 &&
                               (includedOpcodes?.Count ?? 0) == 0 &&
                               (includedOpcodesWildcards?.Count ?? 0) == 0 &&
                               (excludedOpcodesWildcards?.Count ?? 0) == 0;
        
        public void IncludePacketNumber(int packet)
        {
            includedPacketNumbers ??= new();
            includedPacketNumbers.Add(packet);
        }
    }
    
    [UniqueProvider]
    public interface IPacketFilterDialogService
    {
        Task<IFilterData?> OpenFilterDialog(IReadOnlyFilterData? currentFilterData);
    }
    
    [AutoRegister]
    [SingleInstance]
    public class PacketFilterDialogService : IPacketFilterDialogService
    {
        private readonly IContainerProvider containerProvider;
        private readonly IWindowManager windowManager;

        public PacketFilterDialogService(IContainerProvider containerProvider,
            IWindowManager windowManager)
        {
            this.containerProvider = containerProvider;
            this.windowManager = windowManager;
        }

        public async Task<IFilterData?> OpenFilterDialog(IReadOnlyFilterData? currentFilterData)
        {
            using var vm = containerProvider.Resolve<PacketFilterDialogViewModel>((typeof(IReadOnlyFilterData),
                currentFilterData));
            if (await windowManager.ShowDialog(vm))
                return vm.FilterData;
            return null;
        }
    }
}