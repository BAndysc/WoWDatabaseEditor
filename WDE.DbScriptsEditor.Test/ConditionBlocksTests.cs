using System.Collections.Generic;
using NUnit.Framework;
using WDE.DbScriptsEditor.Models;

namespace WDE.DbScriptsEditor.Test
{
    public class ConditionBlocksTests
    {
        private static (bool, long) Step(long cond) => (true, cond);
        private static (bool, long) Neutral() => (false, 0);

        [Test]
        public void ConsecutiveSameCondition_FormOneBlock()
        {
            var blocks = DbScriptConditionBlocks.Compute(new List<(bool, long)>
            {
                Step(0), Step(5), Step(5), Step(5), Step(0),
            });
            Assert.AreEqual(1, blocks.Count);
            Assert.AreEqual(new DbScriptConditionBlock(1, 3, 5), blocks[0]);
        }

        [Test]
        public void NeutralRowsBetweenMembers_StayInside_TrailingOnesDont()
        {
            // step(5) wait step(5) wait step(0) → wait between members inside, trailing wait out
            var blocks = DbScriptConditionBlocks.Compute(new List<(bool, long)>
            {
                Step(5), Neutral(), Step(5), Neutral(), Step(0),
            });
            Assert.AreEqual(1, blocks.Count);
            Assert.AreEqual(new DbScriptConditionBlock(0, 2, 5), blocks[0]);
        }

        [Test]
        public void DifferentConditions_SplitIntoAdjacentBlocks()
        {
            var blocks = DbScriptConditionBlocks.Compute(new List<(bool, long)>
            {
                Step(5), Step(5), Step(7), Step(7),
            });
            Assert.AreEqual(2, blocks.Count);
            Assert.AreEqual(new DbScriptConditionBlock(0, 1, 5), blocks[0]);
            Assert.AreEqual(new DbScriptConditionBlock(2, 3, 7), blocks[1]);
        }

        [Test]
        public void SingleConditionedStep_IsASingletonBlock()
        {
            var blocks = DbScriptConditionBlocks.Compute(new List<(bool, long)>
            {
                Step(0), Step(9), Step(0),
            });
            Assert.AreEqual(1, blocks.Count);
            Assert.AreEqual(new DbScriptConditionBlock(1, 1, 9), blocks[0]);
        }

        [Test]
        public void SameConditionSeparatedByOtherStep_MakesTwoBlocks()
        {
            var blocks = DbScriptConditionBlocks.Compute(new List<(bool, long)>
            {
                Step(5), Step(0), Step(5),
            });
            Assert.AreEqual(2, blocks.Count);
            Assert.AreEqual(new DbScriptConditionBlock(0, 0, 5), blocks[0]);
            Assert.AreEqual(new DbScriptConditionBlock(2, 2, 5), blocks[1]);
        }

        [Test]
        public void NoConditions_NoBlocks()
        {
            var blocks = DbScriptConditionBlocks.Compute(new List<(bool, long)>
            {
                Step(0), Neutral(), Step(0),
            });
            Assert.IsEmpty(blocks);
        }
    }
}
