using System;
using System.IO;
using NUnit.Framework;
using WDE.MangosEventAiEditor.Acid;

namespace WDE.EventAiEditor.Test.Acid
{
    public class AcidFileLocatorTests
    {
        private string root = null!;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "wde_acid_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "wotlk-db", "ACID"));
            File.WriteAllText(Path.Combine(root, "wotlk-db", "ACID", "acid_wotlk.sql"), "-- acid");
            Directory.CreateDirectory(Path.Combine(root, "empty-repo", "ACID"));
            Directory.CreateDirectory(Path.Combine(root, "no-acid"));
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(root, true);
        }

        [Test]
        public void AcceptsTheRepositoryRoot()
        {
            var repo = Path.Combine(root, "wotlk-db");
            Assert.AreEqual(repo, AcidFileLocator.NormalizeRepositoryPath(repo));
        }

        [Test]
        public void AcceptsTheAcidSubfolderAndReturnsTheParent()
        {
            var repo = Path.Combine(root, "wotlk-db");
            Assert.AreEqual(repo, AcidFileLocator.NormalizeRepositoryPath(Path.Combine(repo, "ACID")));
            Assert.AreEqual(repo, AcidFileLocator.NormalizeRepositoryPath(Path.Combine(repo, "ACID") + Path.DirectorySeparatorChar));
        }

        [Test]
        public void RejectsFoldersWithoutAKnownAcidFile()
        {
            Assert.IsNull(AcidFileLocator.NormalizeRepositoryPath(Path.Combine(root, "no-acid")));
            Assert.IsNull(AcidFileLocator.NormalizeRepositoryPath(Path.Combine(root, "empty-repo")));
            Assert.IsNull(AcidFileLocator.NormalizeRepositoryPath(Path.Combine(root, "empty-repo", "ACID")));
            Assert.IsNull(AcidFileLocator.NormalizeRepositoryPath(null));
            Assert.IsNull(AcidFileLocator.NormalizeRepositoryPath("  "));
        }
    }
}
