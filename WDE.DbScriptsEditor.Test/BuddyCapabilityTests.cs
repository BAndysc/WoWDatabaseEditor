using System.Collections.Generic;
using NUnit.Framework;
using WDE.DbScriptsEditor.Models;

namespace WDE.DbScriptsEditor.Test
{
    public class BuddyCapabilityTests
    {
        [Test]
        public void Parse_MapsKnownValues()
        {
            Assert.AreEqual(DbScriptBuddyCapability.Creature, DbScriptBuddyCapabilities.Parse("creature"));
            Assert.AreEqual(DbScriptBuddyCapability.GameObject, DbScriptBuddyCapabilities.Parse("gameobject"));
            Assert.AreEqual(DbScriptBuddyCapability.Both, DbScriptBuddyCapabilities.Parse("both"));
        }

        [Test]
        public void Parse_IsCaseAndWhitespaceTolerant()
        {
            Assert.AreEqual(DbScriptBuddyCapability.GameObject, DbScriptBuddyCapabilities.Parse("  GameObject "));
            Assert.AreEqual(DbScriptBuddyCapability.Both, DbScriptBuddyCapabilities.Parse("BOTH"));
        }

        [Test]
        public void Parse_EmptyOrUnknown_FallsBackToCreature()
        {
            Assert.AreEqual(DbScriptBuddyCapability.Creature, DbScriptBuddyCapabilities.Parse(null));
            Assert.AreEqual(DbScriptBuddyCapability.Creature, DbScriptBuddyCapabilities.Parse(""));
            Assert.AreEqual(DbScriptBuddyCapability.Creature, DbScriptBuddyCapabilities.Parse("creture"));
        }

        [Test]
        public void AllowsHelpers_MatchCapability()
        {
            Assert.IsTrue(DbScriptBuddyCapability.Creature.AllowsCreature());
            Assert.IsFalse(DbScriptBuddyCapability.Creature.AllowsGameObject());

            Assert.IsFalse(DbScriptBuddyCapability.GameObject.AllowsCreature());
            Assert.IsTrue(DbScriptBuddyCapability.GameObject.AllowsGameObject());

            Assert.IsTrue(DbScriptBuddyCapability.Both.AllowsCreature());
            Assert.IsTrue(DbScriptBuddyCapability.Both.AllowsGameObject());
        }

        private static DbScriptCommandDefinition Command(DbScriptBuddyCapability buddy) =>
            new()
            {
                Id = 1,
                Name = "TEST",
                NameReadable = "Test",
                Buddy = buddy,
                Parameters = new List<DbScriptCommandParameter>(),
                Description = "",
            };

        private static BuddyDescriptor Buddy(bool isGameObject) =>
            new(BuddyFindMode.NearestByEntry, isGameObject, entry: 100, searchValue: 5, includeDespawned: false, allEligible: false);

        private static readonly ScriptDirection PlainDirection =
            new(SourceTargetKind.OriginalSource, SourceTargetKind.OriginalTarget);

        [Test]
        public void Validate_CreatureCommand_WarnsOnGameObjectBuddy()
        {
            var warnings = DbScriptStructuralValidator.Validate(PlainDirection, Buddy(isGameObject: true),
                Command(DbScriptBuddyCapability.Creature));
            CollectionAssert.Contains(warnings, "This command's buddy must be a creature, not a gameobject.");
        }

        [Test]
        public void Validate_GameObjectCommand_WarnsOnCreatureBuddy()
        {
            var warnings = DbScriptStructuralValidator.Validate(PlainDirection, Buddy(isGameObject: false),
                Command(DbScriptBuddyCapability.GameObject));
            CollectionAssert.Contains(warnings, "This command's buddy must be a gameobject, not a creature.");
        }

        [Test]
        public void Validate_BothCommand_NeverWarnsOnBuddyKind()
        {
            var creatureWarnings = DbScriptStructuralValidator.Validate(PlainDirection, Buddy(isGameObject: false),
                Command(DbScriptBuddyCapability.Both));
            var goWarnings = DbScriptStructuralValidator.Validate(PlainDirection, Buddy(isGameObject: true),
                Command(DbScriptBuddyCapability.Both));
            CollectionAssert.IsEmpty(creatureWarnings);
            CollectionAssert.IsEmpty(goWarnings);
        }
    }
}
