using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using WDE.Common.Services;
using WDE.RemoteSOAP.Helpers;

namespace WDE.RemoteSOAP.Test.Helpers
{
    public class RemoteCommandMergerTest
    {
        [Test]
        public void TestMerge()
        {
            var merged = RemoteCommandMerger.Merge(new List<IRemoteCommand>()
            {
                new MergableCommand("a"),
                new MergableCommand("b"),
                new MergableCommand("c"),
                new MergableCommand("d"),
            });
            
            Assert.AreEqual(1, merged.Count);
            Assert.AreEqual("abcd", merged[0].GenerateCommand());
        }

        [ExcludeFromCodeCoverage]
        private class MergableCommand : IRemoteCommand
        {
            private readonly string cmd;

            public MergableCommand(string cmd)
            {
                this.cmd = cmd;
            }

            public string GenerateCommand() => cmd;

            public bool TryMerge(IRemoteCommand other, out IRemoteCommand mergedCommand)
            {
                mergedCommand = null;
                if (other is MergableCommand mergableCommand)
                {
                    mergedCommand = new MergableCommand(cmd + mergableCommand.cmd);
                    return true;
                }

                return false;
            }
        }
    }
}