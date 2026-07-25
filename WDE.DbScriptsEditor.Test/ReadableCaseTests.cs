using NUnit.Framework;
using WDE.DbScriptsEditor.Models;

namespace WDE.DbScriptsEditor.Test
{
    public class ReadableCaseTests
    {
        [Test]
        public void CapitalizesActorAndActionAfterColon()
        {
            // the "Source: add flag" example
            Assert.AreEqual("Source: Add NPC flags 123",
                DbScriptReadableCase.SentenceCase("source: add NPC flags 123", hasActorColon: true));
        }

        [Test]
        public void CapitalizesBuddyActorAtSentenceStart()
        {
            // the "nearest creature Xyz" example, used as the leading actor
            Assert.AreEqual("Nearest creature Xyz: Set faction 5",
                DbScriptReadableCase.SentenceCase("nearest creature Xyz: Set faction 5", hasActorColon: true));
        }

        [Test]
        public void LeavesParentheticalTargetLowercase()
        {
            Assert.AreEqual("Source: Cast Fireball (nearest creature 5)",
                DbScriptReadableCase.SentenceCase("source: cast Fireball (nearest creature 5)", hasActorColon: true));
        }

        [Test]
        public void SkipsMarkupTagsWhenCapitalizing()
        {
            // actor and action live inside [s=N]…[/s] / [p=N]…[/p] spans
            Assert.AreEqual("[s=0]Source[/s]: [p=1]Add[/p] NPC flags",
                DbScriptReadableCase.SentenceCase("[s=0]source[/s]: [p=1]add[/p] NPC flags", hasActorColon: true));
        }

        [Test]
        public void NoActorColon_OnlyCapitalizesFirstWord()
        {
            Assert.AreEqual("Terminate script: do not touch this",
                DbScriptReadableCase.SentenceCase("terminate script: do not touch this", hasActorColon: false));
        }

        [Test]
        public void DoesNotCapitalizeLeadingNumber()
        {
            Assert.AreEqual("Source: 123 |= 456",
                DbScriptReadableCase.SentenceCase("source: 123 |= 456", hasActorColon: true));
        }

        [Test]
        public void StartsWithActorToken_DetectsActorPrefixedDescriptions()
        {
            Assert.IsTrue(DbScriptReadableCase.StartsWithActorToken("{source}: Set faction {Faction}"));
            Assert.IsTrue(DbScriptReadableCase.StartsWithActorToken("{player}: Teleport to {Map}"));
            Assert.IsTrue(DbScriptReadableCase.StartsWithActorToken("{target}: Despawn"));
            Assert.IsFalse(DbScriptReadableCase.StartsWithActorToken("Terminate script"));
            Assert.IsFalse(DbScriptReadableCase.StartsWithActorToken("Summon creature {Creature} at ({X}, {Y}, {Z})"));
        }
    }
}
