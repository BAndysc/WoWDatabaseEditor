using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WDE.Common;
using WDE.Module.Attributes;
using WDE.PacketViewer.PacketParserIntegration;
using WDE.PacketViewer.Processing.Processors;
using WDE.PacketViewer.Processing.Processors.ActionReaction;
using WDE.PacketViewer.Utils;
using WDE.PacketViewer.ViewModels;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.IntegrationTests
{
    public class RelatedPacketsTestCaseGroup
    {
        public string SniffFilePath { get; init; } = "";
        public List<RelatedPacketsTestCase> TestCases { get; init; } = new();
    }
    
    public class RelatedPacketsTestCase
    {
        public string SearchTextStartPacket { get; init; } = "";
        public List<string> MustIncludeGuid { get; init; } = new();
        public List<string> MightIncludeGuid { get; init; } = new();
        public List<string> CurrentlyIncludedGuidButShouldNotBe { get; init; } = new();
        public List<string> CurrentlyNotIncludedGuidButShouldBe { get; init; } = new();
        public int? MustStartPacketId { get; init; }
        public int? MustEndPacketId { get; init; }
        public string TestName { get; init; } = "";
    }
    
    [AutoRegister]
    public class RelatedPacketsTester
    {
        private readonly IRelatedPacketsFinder relatedPacketsFinder;
        private readonly ISniffLoader sniffLoader;
        private readonly PacketViewModelFactory viewModelFactory;

        private List<RelatedPacketsTestCaseGroup>? testCaseGroup;
        
        public RelatedPacketsTester(IRelatedPacketsFinder relatedPacketsFinder,
            ISniffLoader sniffLoader,
            PacketViewModelFactory viewModelFactory)
        {
            this.relatedPacketsFinder = relatedPacketsFinder;
            this.sniffLoader = sniffLoader;
            this.viewModelFactory = viewModelFactory;
        }

        public async Task RunTests(string filePath)
        {
            if (!File.Exists(filePath))
                throw new Exception("Tests: test case file doesn't exist");
            try
            {
                testCaseGroup =
                    JsonConvert.DeserializeObject<List<RelatedPacketsTestCaseGroup>>(
                        await File.ReadAllTextAsync(filePath));
            }
            catch (Exception e)
            {
                throw new Exception("Tests: error while deserializing test case file", e);
            }
            
            if (testCaseGroup != null)
                foreach (var test in testCaseGroup)
                    await RunSingleTestCase(Path.GetDirectoryName(filePath)!, test);
        }
        
        public async Task RunSingleTestCase(string basePath, RelatedPacketsTestCaseGroup testCaseGroup)
        {
            var sniffPath = Path.Combine(basePath, testCaseGroup.SniffFilePath);
            using var allocator = new RefCountedArena();
            var sniff = await sniffLoader.LoadSniff(allocator, sniffPath, null, CancellationToken.None, true, new Progress<float>());
            var store = new PacketViewModelStore(sniffPath);
            List<PacketViewModel> split = new();

            unsafe void SynchronousWork()
            {
                var splitter = new SplitUpdateProcessor(new GuidExtractorProcessor(), 0, allocator);
                foreach (ref var packet in sniff.Packets_.AsSpan())
                {
                    IEnumerable<(Pointer<PacketHolder>, int)>? output;
                    fixed (PacketHolder* ptr = &packet) // safe, because sniff.Packets_ is allocated from RefCountedAllocator
                    {
                        output = splitter.Process(ptr);
                    }

                    if (output != null)
                    {
                        foreach (var p in output)
                        {
                            if (viewModelFactory.Process(p.Item1, p.Item2) is PacketViewModel pvm)
                            {
                                split.Add(pvm);
                            }
                        }
                    }
                }

                var finalized = splitter.Finalize();
                if (finalized != null)
                {
                    foreach (var p in finalized)
                    {
                        if (viewModelFactory.Process(p.Item1, p.Item2) is PacketViewModel pvm)
                        {
                            split.Add(pvm);
                        }
                    }
                }

            }
            SynchronousWork();

            foreach (var group in testCaseGroup.TestCases)
            {   
                try
                {
                    TestSingle(sniff.GameVersion, store, @group, split);
                }
                catch (Exception e)
                {
                    LOG.LogError("FAILED: " + e.Message);
                }
                finally
                {
                    LOG.LogInformation("");
                }
            }
        }

        private void TestSingle(ulong gameBuild, PacketViewModelStore store, RelatedPacketsTestCase @group, List<PacketViewModel> split)
        {
            LOG.LogInformation("\n\n" + @group.TestName);
            var startPackets = FindPacket(store, split, @group.SearchTextStartPacket);
            LOG.LogInformation($"Text: '{@group.SearchTextStartPacket}' found in packet id {startPackets!.Id}");
            var result = relatedPacketsFinder.Find(gameBuild, split, split, startPackets!.Id, CancellationToken.None);

            var mustIncludeGuids = @group.MustIncludeGuid.StringToGuids().ToList();
            var mayIncludeGuids = @group.MightIncludeGuid.StringToGuids().ToList();
            var includesButShouldNot = @group.CurrentlyIncludedGuidButShouldNotBe.StringToGuids().ToList();
            var notIncludesButShould = @group.CurrentlyNotIncludedGuidButShouldBe.StringToGuids().ToList();

            if (@group.MustStartPacketId.HasValue && (!result.MinPacketNumber.HasValue ||
                                                     @group.MustStartPacketId.Value != result.MinPacketNumber.Value))
                throw new Exception("Invalid start packet");

            if (@group.MustEndPacketId.HasValue && (!result.MaxPacketNumber.HasValue ||
                                                   @group.MustEndPacketId.Value != result.MaxPacketNumber.Value))
                throw new Exception("Invalid end packet");

            var notIncluded = mustIncludeGuids.Where(guid =>
                !(result.IncludedGuids ?? new HashSet<UniversalGuid>()).Contains(guid)).ToList();
            if (notIncluded.Count > 0)
                throw new Exception("Some guid is not present: " +
                                    string.Join("\n", notIncluded.Select(s => s.ToWowParserString())));

            if (result.IncludedGuids != null &&
                result.IncludedGuids.Any(guid => !mustIncludeGuids.Contains(guid) &&
                                                 !mayIncludeGuids.Contains(guid) &&
                                                 !includesButShouldNot.Contains(guid) &&
                                                 !notIncludesButShould.Contains(guid)))
                throw new Exception("Too many guids are present!");

            foreach (var unnecessary in includesButShouldNot)
            {
                if (result.IncludedGuids != null && !result.IncludedGuids.Contains(unnecessary))
                    LOG.LogInformation(unnecessary.ToWowParserString() + " is no longer included, that's a success!");
            }

            foreach (var necessary in notIncludesButShould)
            {
                if (result.IncludedGuids != null && result.IncludedGuids.Contains(necessary))
                    LOG.LogInformation(necessary.ToWowParserString() + " is now included, that's a success!");
            }
        }

        private PacketViewModel? FindPacket(PacketViewModelStore store, ICollection<PacketViewModel> packets, string text)
        {
            foreach (var p in packets)
            {
                if (store.GetText(p).Contains(text, StringComparison.InvariantCultureIgnoreCase))
                    return p;
            }

            return null;
        }
    }
}