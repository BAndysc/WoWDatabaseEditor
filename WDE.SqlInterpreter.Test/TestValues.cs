using System.Linq;
using NUnit.Framework;
using WDE.Common.Database;

namespace WDE.SqlInterpreter.Test
{
    public class TestValues
    {
        private BaseQueryEvaluator evaluator = null!;
    
        [SetUp]
        public void Setup()
        {
            evaluator = new("world", "hotfix", DataDatabaseType.World);
        }

        [Test]
        public void InvalidQuery()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `abc` (`a`, `b`) VALUES (5);").ToList();
            Assert.AreEqual(0, result.Count);
        }
    
        [Test]
        public void Comment()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `abc` (`a`, `b`) VALUES (5, 6),\n -- (6)\n(7, 8);").ToList();
            Assert.AreEqual(2, result[0].Inserts.Count);
            CollectionAssert.AreEqual(new object[]{5L, 6L}, result[0].Inserts[0]);
            CollectionAssert.AreEqual(new object[]{7L, 8L}, result[0].Inserts[1]);
        }
    
        [Test]
        public void DifferentTypes()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `abc` (`a`, `b`, `c`, `d`, `e`) VALUES (5, -6, 5.5, 'abc', NULL);").ToList();
            Assert.AreEqual(1, result[0].Inserts.Count);
            CollectionAssert.AreEqual(new object?[]{5L, -6L, 5.5f, "abc", null}, result[0].Inserts[0]);
        }
    
    
        [Test]
        public void MultipleInserts()
        {
            var result = evaluator.ExtractInserts("INSERT INTO `abc` (`a`) VALUES (5);INSERT INTO `def` (`b`) VALUES (6);").ToList();
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(DatabaseTable.WorldTable("abc"), result[0].TableName);
            Assert.AreEqual(DatabaseTable.WorldTable("def"), result[1].TableName);
            CollectionAssert.AreEqual(new object[]{5L}, result[0].Inserts[0]);
            CollectionAssert.AreEqual(new object[]{6L}, result[1].Inserts[0]);
        }

        [Test]
        public void DeleteInsert()
        {
            var result = evaluator.ExtractInserts(
                "DELETE FROM `npc_text` WHERE `ID` IN (4);\nINSERT INTO `npc_text` (`ID`, `text0_0`, `text0_1`, `BroadcastTextID0`, `lang0`, `Probability0`, `EmoteDelay0_0`, `Emote0_0`, `EmoteDelay0_1`, `Emote0_1`, `EmoteDelay0_2`, `Emote0_2`, `text1_0`, `text1_1`, `BroadcastTextID1`, `lang1`, `Probability1`, `EmoteDelay1_0`, `Emote1_0`, `EmoteDelay1_1`, `Emote1_1`, `EmoteDelay1_2`, `Emote1_2`, `text2_0`, `text2_1`, `BroadcastTextID2`, `lang2`, `Probability2`, `EmoteDelay2_0`, `Emote2_0`, `EmoteDelay2_1`, `Emote2_1`, `EmoteDelay2_2`, `Emote2_2`, `text3_0`, `text3_1`, `BroadcastTextID3`, `lang3`, `Probability3`, `EmoteDelay3_0`, `Emote3_0`, `EmoteDelay3_1`, `Emote3_1`, `EmoteDelay3_2`, `Emote3_2`, `text4_0`, `text4_1`, `BroadcastTextID4`, `lang4`, `Probability4`, `EmoteDelay4_0`, `Emote4_0`, `EmoteDelay4_1`, `Emote4_1`, `EmoteDelay4_2`, `Emote4_2`, `text5_0`, `text5_1`, `BroadcastTextID5`, `lang5`, `Probability5`, `EmoteDelay5_0`, `Emote5_0`, `EmoteDelay5_1`, `Emote5_1`, `EmoteDelay5_2`, `Emote5_2`, `text6_0`, `text6_1`, `BroadcastTextID6`, `lang6`, `Probability6`, `EmoteDelay6_0`, `Emote6_0`, `EmoteDelay6_1`, `Emote6_1`, `EmoteDelay6_2`, `Emote6_2`, `text7_0`, `text7_1`, `BroadcastTextID7`, `lang7`, `Probability7`, `EmoteDelay7_0`, `Emote7_0`, `EmoteDelay7_1`, `Emote7_1`, `EmoteDelay7_2`, `Emote7_2`) VALUES\n(15773, \"Patience, friend. Allow me to assist in preparing another attempt.\", \"\", 39996, 0, 1, 0, 0, 0, 0, 0, 0, NULL, NULL, 0, 0, 0, 0, 0, 0, 0, 0, 0, NULL, NULL, 0, 0, 0, 0, 0, 0, 0, 0, 0, NULL, NULL, 0, 0, 0, 0, 0, 0, 0, 0, 0, NULL, NULL, 0, 0, 0, 0, 0, 0, 0, 0, 0, NULL, NULL, 0, 0, 0, 0, 0, 0, 0, 0, 0, NULL, NULL, 0, 0, 0, 0, 0, 0, 0, 0, 0, NULL, NULL, 0, 0, 0, 0, 0, 0, 0, 0, 0);\n").ToList();
            //evaluator.ExtractInserts("DELETE FROM `abc` WHERE `a` = 4; INSERT INTO `abc` (`a`) VALUES (5);").ToList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(DatabaseTable.WorldTable("npc_text"), result[0].TableName);
        }
    }
}