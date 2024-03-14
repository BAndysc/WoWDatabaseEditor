using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using WDE.Common.Game;
using WDE.Common.Services.HeadConnector;

namespace WDE.WoWHeadConnector.Test
{
    public class HeadFetchServiceIntegrationTest
    {
        private IHeadFetchService headService = null!;
        
        [SetUp]
        public void Setup()
        {
            headService = new HeadFetchService(new UriLocator(), new WwwClient(), new HeadParser());
        }

        [Test]
        public async Task TestMurlocMaster()
        {
            var abilities = await headService.FetchNpcAbilities(HeadSourceType.Master, 46);
            CollectionAssert.AreEquivalent(new uint[]{3368}, abilities.Select(a => a.SpellId));
        }

        [Test]
        public async Task TestMurlocClassic()
        {
            var abilities = await headService.FetchNpcAbilities(HeadSourceType.Classic, 46);
            CollectionAssert.AreEquivalent(new uint[]{3368}, abilities.Select(a => a.SpellId));
        }

        [Test]
        public async Task TestMurlocTbc()
        {
            var abilities = await headService.FetchNpcAbilities(HeadSourceType.Tbc, 46);
            CollectionAssert.AreEquivalent(new uint[]{3368}, abilities.Select(a => a.SpellId));
        }
        
        [Test]
        public async Task TestHexthralledSoldierMaster()
        {
            var abilities = await headService.FetchNpcAbilities(HeadSourceType.Master, 137134);
            CollectionAssert.AreEquivalent(new uint[]{261827, 261828}, abilities.Select(a => a.SpellId));
        }

        [Test]
        public async Task TestHexthralledSoldierTbc()
        {
            var abilities = await headService.FetchNpcAbilities(HeadSourceType.Tbc, 137134);
            Assert.AreEqual(0, abilities.Count);
        }

        [Test]
        public async Task TestModes()
        {
            var abilities = await headService.FetchNpcAbilities(HeadSourceType.Master, 26554);
            var bySpell = abilities.GroupBy(a => a.SpellId).ToDictionary(a => a.Key, a => a.SelectMany(m => m.Modes ?? Array.Empty<MapDifficulty>()).ToList());
            
            CollectionAssert.AreEquivalent(new MapDifficulty[]
            {
                MapDifficulty.DungeonNormal
            }, bySpell[48698]);
            CollectionAssert.AreEquivalent(new MapDifficulty[]
            {
                MapDifficulty.DungeonNormal
            }, bySpell[48699]);
            CollectionAssert.AreEquivalent(new MapDifficulty[]
            {
                MapDifficulty.DungeonNormal
            }, bySpell[48700]);
            CollectionAssert.AreEquivalent(new MapDifficulty[]
            {
                MapDifficulty.DungeonHeroic
            }, bySpell[59081]);
            CollectionAssert.AreEquivalent(new MapDifficulty[]
            {
                MapDifficulty.DungeonHeroic
            }, bySpell[59082]);
        }
    }
}