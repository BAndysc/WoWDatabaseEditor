using System;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;
using WDE.DbScriptsEditor.Data;
using WDE.DbScriptsEditor.Models;

namespace WDE.DbScriptsEditor.Test
{
    // Verifies the SmartFormat-based description rendering end-to-end (choose conditionals + the
    // random-text "or" list), since these replaced the hand-rolled choose expander.
    public class SmartFormatRenderTests
    {
        private DbScriptDataManager manager = null!;
        private IParameterFactory factory = null!;

        [SetUp]
        public async Task Setup()
        {
            factory = Substitute.For<IParameterFactory>();
            factory.IsRegisteredLong(Arg.Any<string>()).Returns(false);
            factory.IsRegisteredFloat(Arg.Any<string>()).Returns(false);
            factory.Factory(Arg.Any<string>()).Returns(Parameter.Instance);
            factory.Factory((string?)null).Returns(Parameter.Instance);
            manager = new DbScriptDataManager(new DbScriptDataProvider(new TestRuntimeDataService()), factory,
                Substitute.For<IMangosConditionService>(), Substitute.For<IMangosDatabaseProvider>(),
                Substitute.For<ITableEditorPickerService>());
            await manager.Initialize();
        }

        private string Render(uint command, DbScriptType type, Action<AbstractDbScriptLine> setup)
        {
            var line = new AbstractDbScriptLine { Command = command };
            setup(line);
            var step = new DbScriptStep(line, manager.GetCommand(command), DbScriptTypes.GetInfo(type), factory);
            return step.Readable;
        }

        [Test]
        public void Talk_SingleText_UsesToPreposition()
        {
            var r = Render(0, DbScriptType.QuestEnd, l => l.DataInt = 100);
            StringAssert.Contains("Say 100 to", r);
            StringAssert.DoesNotContain("(", r); // no parenthesised target
        }

        [Test]
        public void Talk_MultipleTexts_JoinedWithOr()
        {
            var r = Render(0, DbScriptType.QuestEnd, l => { l.DataInt = 100; l.DataInt2 = 200; l.DataInt3 = 300; });
            StringAssert.Contains("100 or 200 or 300", r);
        }

        [Test]
        public void Talk_RandomTemplate_IgnoresFixedTexts()
        {
            var r = Render(0, DbScriptType.QuestEnd, l => { l.DataInt = 100; l.DataLong = 5; });
            StringAssert.Contains("Say 5 to", r);
            StringAssert.DoesNotContain("100", r); // the template path hides the fixed text
        }

        [Test]
        public void Emote_And_Cast_RandomPool_JoinedWithOr()
        {
            var emote = Render(1, DbScriptType.CreatureDeath, l => { l.DataLong = 10; l.DataInt = 20; l.DataInt2 = 30; });
            StringAssert.Contains("emote 10 or 20 or 30", emote);

            var cast = Render(15, DbScriptType.CreatureDeath, l => { l.DataLong = 100; l.DataInt = 200; });
            StringAssert.Contains("Cast 100 or 200 on", cast);
        }

        [Test]
        public void KillCredit_ResolvesCreatureAndGroupChoose()
        {
            // on-creature-death: player = "Killer" (target), creature = "Dying creature" (source).
            var cd = DbScriptTypes.GetInfo(DbScriptType.CreatureDeath);
            // solo credit for an explicit creature entry
            var solo = Render(8, DbScriptType.CreatureDeath, l => { l.DataLong = 30; l.DataLong2 = 0; });
            StringAssert.Contains("Gain solo kill credit for 30", solo);
            StringAssert.StartsWith(cd.TargetLabel, solo); // "Killer: ..."
            // group credit for the involved creature (datalong 0 variant)
            var group = Render(8, DbScriptType.CreatureDeath, l => { l.DataLong = 0; l.DataLong2 = 1; });
            StringAssert.Contains("group kill credit for " + cd.SourceLabel, group); // "... for Dying creature"
        }

        [Test]
        public void ChooseWords_RenderForEnumBools()
        {
            StringAssert.Contains("Add NPC flags", Render(29, DbScriptType.CreatureDeath, l => { l.DataLong = 4; l.DataLong2 = 1; }));
            StringAssert.Contains("Toggle NPC flags", Render(29, DbScriptType.CreatureDeath, l => { l.DataLong = 4; l.DataLong2 = 2; }));
            StringAssert.Contains("Switch to running", Render(25, DbScriptType.CreatureDeath, l => l.DataLong = 1));
            StringAssert.Contains("Switch to walking", Render(25, DbScriptType.CreatureDeath, l => l.DataLong = 0));
            StringAssert.Contains("Remove one stack of aura 5", Render(14, DbScriptType.CreatureDeath, l => { l.DataLong = 5; l.DataLong2 = 2; }));
        }

        [Test]
        public void TerminateScript_NestedChoose()
        {
            // no search -> plain terminate
            Assert.AreEqual("Terminate script", Render(31, DbScriptType.CreatureMovement, _ => { }));
            // search creature within distance
            var searched = Render(31, DbScriptType.CreatureMovement, l => { l.DataLong = 40; l.DataLong2 = 10; });
            StringAssert.Contains("unless 40 is alive within 10 yd", searched);
            // pool search
            StringAssert.Contains("pool 7", Render(31, DbScriptType.CreatureMovement, l => l.DataLong3 = 7));
        }

        [Test]
        public void EveryCommand_RendersWithoutRawTemplateLeftovers()
        {
            // A SmartFormat parse failure silently falls back to the raw template — which still
            // contains ":choose(" or "Value}". Render every command (with columns set so choose
            // branches are exercised) across a few script types and assert nothing leaked.
            foreach (var type in new[] { DbScriptType.CreatureDeath, DbScriptType.Gossip, DbScriptType.QuestEnd, DbScriptType.CreatureMovement })
            {
                var info = DbScriptTypes.GetInfo(type);
                for (uint id = 0; id <= 57; id++)
                {
                    if (!manager.Contains(id))
                        continue;
                    foreach (var v in new long[] { 0, 1, 2 })
                    {
                        var r = Render(id, type, l =>
                        {
                            l.DataLong = (uint)v; l.DataLong2 = (uint)v; l.DataLong3 = (uint)v;
                            l.DataInt = (int)v; l.DataInt2 = (int)v; l.DataInt3 = (int)v; l.DataInt4 = (int)v;
                            l.X = v; l.Y = v; l.Z = v; l.O = v; l.Speed = v; l.DataFloat = v;
                        });
                        StringAssert.DoesNotContain(":choose(", r, $"command {id} ({info.ReadableName}, v={v}): template not rendered");
                        StringAssert.DoesNotContain("Value}", r, $"command {id} ({info.ReadableName}, v={v}): raw Value token leaked");
                    }
                }
            }
        }

        [Test]
        public void QuestExplored_DistanceChoose()
        {
            var near = Render(7, DbScriptType.QuestEnd, l => { l.DataLong = 42; l.DataLong2 = 0; });
            StringAssert.DoesNotContain("within", near);
            var far = Render(7, DbScriptType.QuestEnd, l => { l.DataLong = 42; l.DataLong2 = 15; });
            StringAssert.Contains("within 15 yd", far);
        }

        [Test]
        public void StartRelay_TemplateChoose()
        {
            var direct = Render(45, DbScriptType.Gossip, l => l.DataLong = 7);
            StringAssert.Contains("Start relay script 7", direct);
            var random = Render(45, DbScriptType.Gossip, l => l.DataLong2 = 9);
            StringAssert.Contains("Start 9", random);
        }

        [Test]
        public void ValueZeroVariants_RenderContextually()
        {
            StringAssert.Contains("Reset faction", Render(22, DbScriptType.CreatureDeath, _ => { }));
            StringAssert.Contains("Demorph", Render(23, DbScriptType.CreatureDeath, _ => { }));
            StringAssert.Contains("Dismount", Render(24, DbScriptType.CreatureDeath, _ => { }));
            StringAssert.Contains("Set faction 5", Render(22, DbScriptType.CreatureDeath, l => l.DataLong = 5));
        }
    }
}
