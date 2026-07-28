using System.Linq;
using NUnit.Framework;
using WDE.DbScriptsEditor.Models;

namespace WDE.DbScriptsEditor.Test
{
    // The leaf tile table must mirror the core's buddy taxonomy exactly: every engine-reachable
    // locator is a leaf, every leaf round-trips through the codec, and per-command-kind filtering
    // offers no impossible combination.
    public class BuddyLeafTableTests
    {
        [Test]
        public void EveryLeaf_RoundTripsThroughCodec()
        {
            // "Both" is a context in which every leaf is valid, so it exercises the full table.
            const DbScriptBuddyCapability cap = DbScriptBuddyCapability.Both;
            foreach (var leaf in DbScriptBuddyLeaves.All)
            {
                var descriptor = leaf.ToDescriptor(entry: 100, searchValue: 5);
                var (bits, entry, radius) = descriptor.Encode(cap);
                // Decode needs the direction bits too; a self direction keeps the buddy dangling so
                // Provided stays true regardless of slot routing.
                var decoded = DbScriptFlagsCodec.Decode(bits | 0x004, entry, radius, cap);
                Assert.IsTrue(decoded.Buddy.Provided, $"{leaf.Id}: buddy not provided after round-trip");
                var matched = DbScriptBuddyLeaves.Match(decoded.Buddy);
                Assert.AreEqual(leaf.Id, matched?.Id, $"{leaf.Id}: matched back to {matched?.Id ?? "null"}");
            }
        }

        [Test]
        public void ValidForCapability_HasExpectedCounts()
        {
            int Count(DbScriptBuddyCapability cap) => DbScriptBuddyLeaves.All.Count(l => l.ValidFor(cap));
            // creature: everything except the three GO-only leaves
            Assert.AreEqual(16, Count(DbScriptBuddyCapability.Creature));
            // gameobject: the GO leaves + kind-agnostic spawn-group / string-id
            Assert.AreEqual(9, Count(DbScriptBuddyCapability.GameObject));
            Assert.AreEqual(DbScriptBuddyLeaves.All.Count, Count(DbScriptBuddyCapability.Both));
        }

        [Test]
        public void CreatureOnlyCommand_NeverOffersGameObjectLeaves()
        {
            var offered = DbScriptBuddyLeaves.All.Where(l => l.ValidFor(DbScriptBuddyCapability.Creature)).ToList();
            Assert.IsFalse(offered.Any(l => l.Mode == BuddyFindMode.NearestByEntry && l.IsGameObject));
            Assert.IsFalse(offered.Any(l => l.Mode == BuddyFindMode.ByGuid && l.IsGameObject));
        }

        [Test]
        public void GameObjectOnlyCommand_NeverOffersPoolOrPet()
        {
            var offered = DbScriptBuddyLeaves.All.Where(l => l.ValidFor(DbScriptBuddyCapability.GameObject)).ToList();
            Assert.IsFalse(offered.Any(l => l.Mode == BuddyFindMode.ByPool));
            Assert.IsFalse(offered.Any(l => l.Mode == BuddyFindMode.Pet));
        }

        [Test]
        public void Match_NeverThrows_OnAdversarialRawRows()
        {
            // 0x400 on a creature command, combo-4 self, dangling buddy, high bits set, etc.
            uint[] rawFlags = { 0x000, 0x004, 0x404, 0x040 | 0x004, 0x800 | 0x200, 0x010, 0x080 };
            foreach (var flags in rawFlags)
            foreach (var cap in new[] { DbScriptBuddyCapability.Creature, DbScriptBuddyCapability.GameObject, DbScriptBuddyCapability.Both })
            {
                var decoded = DbScriptFlagsCodec.Decode(flags, 1234, 10, cap);
                Assert.DoesNotThrow(() => DbScriptBuddyLeaves.Match(decoded.Buddy));
                if (decoded.Buddy.Provided)
                    Assert.IsNotNull(DbScriptBuddyLeaves.Match(decoded.Buddy),
                        $"flags=0x{flags:X3} cap={cap}: a provided buddy must match some leaf");
            }
        }

        [Test]
        public void LeafIds_AreUnique()
        {
            var ids = DbScriptBuddyLeaves.All.Select(l => l.Id).ToList();
            CollectionAssert.AllItemsAreUnique(ids);
        }
    }
}
