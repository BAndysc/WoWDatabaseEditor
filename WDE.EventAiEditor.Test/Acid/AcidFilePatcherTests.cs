using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WDE.Common.Database;
using WDE.MangosEventAiEditor.Acid;

namespace WDE.EventAiEditor.Test.Acid
{
    public class AcidFilePatcherTests
    {
        private const string Header = "INSERT INTO `creature_ai_scripts` (`id`,`creature_id`,`event_type`,`event_inverse_phase_mask`,`event_chance`,`event_flags`,`event_param1`,`event_param2`,`event_param3`,`event_param4`,`event_param5`,`event_param6`,`action1_type`,`action1_param1`,`action1_param2`,`action1_param3`,`action2_type`,`action2_param1`,`action2_param2`,`action2_param3`,`action3_type`,`action3_param1`,`action3_param2`,`action3_param3`,`comment`) VALUES";

        private static string Row(long id, long entry, string comment, string terminator) =>
            $"('{id}','{entry}','0','0','100','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','0','{comment}'){terminator}";

        private static IEventAiLine Line(uint id, int entry, string comment) => new AbstractEventAiLine
        {
            Id = id,
            CreatureIdOrGuid = entry,
            EventChance = 100,
            Comment = comment
        };

        private static readonly string File = string.Join("\n",
            "-- some copyright",
            "TRUNCATE creature_ai_scripts;",
            "",
            Header,
            "-- =====",
            "-- Zone A",
            "-- =====",
            "-- Mob One 100",
            Row(10001, 100, "Mob One - Foo", ","),
            Row(10002, 100, "Mob One - Bar", ","),
            "-- Mob Three 300",
            Row(30001, 300, "Mob Three - Foo", ";"),
            "",
            Header,
            "-- Mob Five 500",
            Row(50001, 500, "Mob Five - Foo", ";"),
            "",
            "INSERT INTO `creature_ai_summons` (`id`,`position_x`,`position_y`,`position_z`,`orientation`,`spawntimesecs`,`comment` ) VALUES",
            "('1','-10.5','20.1','30','1.5','60','summon spot');",
            "");

        private static string Patch(long entry, string? name, params IEventAiLine[] lines)
        {
            var result = AcidFilePatcher.UpdateCreatureScript(File, entry, name, lines, out var error);
            Assert.IsNull(error);
            Assert.IsNotNull(result);
            return result!;
        }

        [Test]
        public void ReplacesExistingBlockInTheMiddle()
        {
            var result = Patch(100, "Mob One", Line(10001, 100, "Mob One - New comment"));
            var lines = result.Split('\n');

            CollectionAssert.Contains(lines, Row(10001, 100, "Mob One - New comment", ","));
            CollectionAssert.DoesNotContain(lines, Row(10002, 100, "Mob One - Bar", ","));
            // header comment stays, terminator of the statement untouched
            CollectionAssert.Contains(lines, "-- Mob One 100");
            CollectionAssert.Contains(lines, Row(30001, 300, "Mob Three - Foo", ";"));
        }

        [Test]
        public void ReplacesBlockAtStatementEnd()
        {
            var result = Patch(300, "Mob Three",
                Line(30001, 300, "Mob Three - Foo"),
                Line(30002, 300, "Mob Three - Bar"));
            var lines = result.Split('\n');

            CollectionAssert.Contains(lines, Row(30001, 300, "Mob Three - Foo", ","));
            CollectionAssert.Contains(lines, Row(30002, 300, "Mob Three - Bar", ";"));
        }

        [Test]
        public void DeletesScriptIncludingItsHeaderComment()
        {
            var result = Patch(100, "Mob One");
            var lines = result.Split('\n');

            CollectionAssert.DoesNotContain(lines, Row(10001, 100, "Mob One - Foo", ","));
            CollectionAssert.DoesNotContain(lines, Row(10002, 100, "Mob One - Bar", ","));
            CollectionAssert.DoesNotContain(lines, "-- Mob One 100");
            // zone separators survive
            CollectionAssert.Contains(lines, "-- Zone A");
            CollectionAssert.Contains(lines, "-- =====");
        }

        [Test]
        public void DeletingLastBlockMovesTheSemicolon()
        {
            var result = Patch(300, "Mob Three");
            var lines = result.Split('\n');

            CollectionAssert.DoesNotContain(lines, "-- Mob Three 300");
            CollectionAssert.Contains(lines, Row(10002, 100, "Mob One - Bar", ";"));
        }

        [Test]
        public void InsertsNewCreatureSortedByEntryWithHeaderComment()
        {
            var result = Patch(200, "Mob Two", Line(20001, 200, "Mob Two - Foo"));
            var lines = result.Split('\n').ToList();

            var headerIdx = lines.IndexOf("-- Mob Two 200");
            Assert.Greater(headerIdx, 0);
            Assert.AreEqual(Row(20001, 200, "Mob Two - Foo", ","), lines[headerIdx + 1]);
            // inserted after Mob One rows, before Mob Three's header comment
            Assert.AreEqual(Row(10002, 100, "Mob One - Bar", ","), lines[headerIdx - 1]);
            Assert.AreEqual("-- Mob Three 300", lines[headerIdx + 2]);
        }

        [Test]
        public void InsertsNewCreatureAtTheEndOfTheLastStatement()
        {
            var result = Patch(600, "Mob Six", Line(60001, 600, "Mob Six - Foo"));
            var lines = result.Split('\n').ToList();

            var headerIdx = lines.IndexOf("-- Mob Six 600");
            Assert.Greater(headerIdx, 0);
            // previous statement closer got demoted to a comma, our row closes the statement
            Assert.AreEqual(Row(50001, 500, "Mob Five - Foo", ","), lines[headerIdx - 1]);
            Assert.AreEqual(Row(60001, 600, "Mob Six - Foo", ";"), lines[headerIdx + 1]);
        }

        [Test]
        public void PrefersTheGapAfterTheClosestLowerEntryOverAFarAwaySuccessor()
        {
            // 350 must land after 300 (end of statement 1), not before 500 in the next
            // statement, because ordering restarts per section in the real ACID file
            var result = Patch(350, "Mob ThreeFifty", Line(35001, 350, "Mob ThreeFifty - Foo"));
            var lines = result.Split('\n').ToList();

            var headerIdx = lines.IndexOf("-- Mob ThreeFifty 350");
            Assert.Greater(headerIdx, 0);
            Assert.AreEqual(Row(30001, 300, "Mob Three - Foo", ","), lines[headerIdx - 1]);
            Assert.AreEqual(Row(35001, 350, "Mob ThreeFifty - Foo", ";"), lines[headerIdx + 1]);
        }

        [Test]
        public void EscapesQuotesInComments()
        {
            var result = Patch(200, "Mug'thol", Line(20001, 200, "Mug'thol - Cast Strike"));
            StringAssert.Contains("'Mug''thol - Cast Strike'", result);
            StringAssert.Contains("-- Mug'thol 200", result);
        }

        [Test]
        public void TrimsExporterCommentSuffixes()
        {
            var result = Patch(200, "Mob Two", Line(20001, 200, "Mob Two - Cast Foo, "));
            StringAssert.Contains("'Mob Two - Cast Foo'", result);
        }

        [Test]
        public void DoesNotTouchOtherTables()
        {
            var result = Patch(200, "Mob Two", Line(20001, 200, "Mob Two - Foo"));
            StringAssert.Contains("('1','-10.5','20.1','30','1.5','60','summon spot');", result);
            StringAssert.Contains("TRUNCATE creature_ai_scripts;", result);
        }

        [Test]
        public void UntouchedContentIsPreservedOnReplace()
        {
            // replacing a block with identical content must round-trip the whole file
            var result = Patch(300, "Mob Three", Line(30001, 300, "Mob Three - Foo"));
            Assert.AreEqual(File, result);
        }

        [Test]
        public void PreservesCrlfLineEndings()
        {
            var crlf = File.Replace("\n", "\r\n");
            var result = AcidFilePatcher.UpdateCreatureScript(crlf, 300, "Mob Three",
                new List<IEventAiLine> { Line(30001, 300, "Mob Three - Foo") }, out var error);
            Assert.IsNull(error);
            Assert.AreEqual(crlf, result);
        }

        [Test]
        public void FailsGracefullyWithoutAcidInsert()
        {
            var result = AcidFilePatcher.UpdateCreatureScript("-- just a comment\n", 100, null,
                new List<IEventAiLine> { Line(10001, 100, "x") }, out var error);
            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public void DeletingUnknownCreatureIsANoop()
        {
            var result = AcidFilePatcher.UpdateCreatureScript(File, 999, "Unknown",
                new List<IEventAiLine>(), out var error);
            Assert.IsNull(error);
            Assert.AreEqual(File, result);
        }
    }
}
