using NUnit.Framework;
using WDE.DbScriptsEditor.Models;

namespace WDE.DbScriptsEditor.Test
{
    public class SourceTargetCompilerTests
    {
        private static BuddyDescriptor Creature => DbScriptBuddyLeaves.All[0].ToDescriptor(100, 5); // nearest alive
        private static BuddyDescriptor OtherCreature => DbScriptBuddyLeaves.All[2].ToDescriptor(200, 8); // all alive

        [Test]
        public void SetSlot_PutsBuddyInSourceSlot()
        {
            var start = DbScriptFlagsCodec.Decode(0, 0, 0, DbScriptBuddyCapability.Creature);
            var after = DbScriptSourceTargetCompiler.SetSlot(start, isSource: true, SourceTargetKind.Buddy, Creature);
            Assert.AreEqual(SourceTargetKind.Buddy, after.Direction.Source);
            Assert.AreEqual(SourceTargetKind.OriginalTarget, after.Direction.Target);
            Assert.IsTrue(after.Buddy.Provided);
            Assert.AreEqual(Creature, after.Buddy);
        }

        [Test]
        public void SetSlot_ClearsBuddy_WhenNoSlotUsesIt()
        {
            var withBuddy = DbScriptSourceTargetCompiler.SetSlot(
                DbScriptFlagsCodec.Decode(0, 0, 0, DbScriptBuddyCapability.Creature),
                isSource: true, SourceTargetKind.Buddy, Creature);
            var reverted = DbScriptSourceTargetCompiler.SetSlot(withBuddy, isSource: true, SourceTargetKind.OriginalSource, BuddyDescriptor.None);
            Assert.IsFalse(reverted.Buddy.Provided);
            Assert.IsFalse(reverted.Direction.UsesBuddy);
        }

        [Test]
        public void SetSlot_KeepsInertBits_WhenBuddyUnchanged_ClearsWhenChanged()
        {
            // 0x400 on a creature command is inert; a buddy is located by entry.
            var start = DbScriptFlagsCodec.Decode(DbScriptFlags.BuddyByGo, 100, 5, DbScriptBuddyCapability.Creature);
            Assume.That(start.InertBuddyBits, Is.EqualTo(DbScriptFlags.BuddyByGo));
            Assume.That(start.Buddy.Provided);

            // change the OTHER slot (target) — the buddy is untouched, inert bits survive
            var keptTargetOnBuddy = DbScriptSourceTargetCompiler.SetSlot(start, isSource: false, SourceTargetKind.OriginalSource, BuddyDescriptor.None);
            Assert.AreEqual(DbScriptFlags.BuddyByGo, keptTargetOnBuddy.InertBuddyBits, "inert bits must survive an unrelated slot edit");

            // change the buddy itself — a fresh choice drops the inert bits
            var changed = DbScriptSourceTargetCompiler.SetSlot(start, isSource: true, SourceTargetKind.Buddy, OtherCreature);
            Assert.AreEqual(0u, changed.InertBuddyBits, "a new buddy choice clears inert bits");
        }

        [Test]
        public void SameAsSource_MirrorsSourceIntoTarget()
        {
            // buddy source → target is the same buddy (B,B)
            var buddySource = DbScriptSourceTargetCompiler.SetSlot(
                DbScriptFlagsCodec.Decode(0, 0, 0, DbScriptBuddyCapability.Creature),
                isSource: true, SourceTargetKind.Buddy, Creature);
            var mirrored = DbScriptSourceTargetCompiler.SetSlot(buddySource, isSource: false,
                buddySource.Direction.Source, buddySource.Buddy);
            Assert.AreEqual(SourceTargetKind.Buddy, mirrored.Direction.Target);
            Assert.AreEqual(mirrored.Direction.Source, mirrored.Direction.Target);
            Assert.AreEqual(Creature, mirrored.Buddy);

            // original-source source → (S,S), no buddy
            var plain = DbScriptFlagsCodec.Decode(0, 0, 0, DbScriptBuddyCapability.Creature);
            var selfSource = DbScriptSourceTargetCompiler.SetSlot(plain, isSource: false,
                plain.Direction.Source, plain.Buddy);
            Assert.AreEqual(SourceTargetKind.OriginalSource, selfSource.Direction.Target);
            Assert.IsFalse(selfSource.Buddy.Provided);
        }

        [Test]
        public void SetConditionBuddy_MakesDanglingBuddy_WithSelfDirection()
        {
            var start = DbScriptFlagsCodec.Decode(0, 0, 0, DbScriptBuddyCapability.Both); // (S,T), no buddy
            var after = DbScriptSourceTargetCompiler.SetConditionBuddy(start, Creature);

            Assert.IsTrue(after.Buddy.Provided);
            Assert.IsFalse(after.Direction.UsesBuddy, "a condition buddy must occupy neither slot");
            Assert.AreEqual(after.Direction.Source, after.Direction.Target, "must be a self direction");

            // it must round-trip through the codec and decode back to a dangling buddy
            Assert.IsTrue(DbScriptFlagsCodec.TryEncode(after, out var flags, out var entry, out var radius));
            var reDecoded = DbScriptFlagsCodec.Decode(flags, entry, radius, DbScriptBuddyCapability.Both);
            Assert.IsTrue(reDecoded.Buddy.Provided);
            Assert.IsFalse(reDecoded.Direction.UsesBuddy);
        }

        [Test]
        public void SetConditionBuddy_None_ClearsTheBuddy()
        {
            var start = DbScriptFlagsCodec.Decode(0, 0, 0, DbScriptBuddyCapability.Both);
            var withCond = DbScriptSourceTargetCompiler.SetConditionBuddy(start, Creature);
            var cleared = DbScriptSourceTargetCompiler.SetConditionBuddy(withCond, BuddyDescriptor.None);
            Assert.IsFalse(cleared.Buddy.Provided);
            Assert.IsFalse(cleared.Direction.UsesBuddy);
        }
    }
}
