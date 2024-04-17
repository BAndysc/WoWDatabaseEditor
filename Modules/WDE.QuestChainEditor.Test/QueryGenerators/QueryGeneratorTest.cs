using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using WDE.Common.Database;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.QueryGenerators;

namespace WDE.QuestChainEditor.Test.QueryGenerators;

public class QueryGeneratorTest
{
    private ushort questA = 1;
    private ushort questB = 2;
    private ushort questC = 3;
    private ushort questD = 4;
    private ushort questE = 5;
    private ushort questF = 6;
    private ushort questG = 7;
    private ushort questH = 8;
    private ushort questI = 9;
    private ushort questJ = 10;
    private ushort questX = 12;
    
    private IQuestTemplate CreateTemplate(uint entry, int prev, uint next, int exclusiveGroup)
    {
        var template = Substitute.For<IQuestTemplate>();
        template.Entry.Returns(entry);
        template.PrevQuestId.Returns(prev);
        template.ExclusiveGroup.Returns(exclusiveGroup);
        var nextId = (int)next;
        template.NextQuestId.Returns(nextId);
        return template;
    }
    
    private QuestStore CreateStore(List<(uint entry, int prev, uint next, int exclusiveGroup)> quests)
    {
        var source = Substitute.For<IQuestTemplateSource>();

        foreach (var q in quests)
        {
            var template = CreateTemplate(q.entry, q.prev, q.next, q.exclusiveGroup);
            source.GetTemplate(q.entry).Returns(template);
        }

        foreach (var grouped in quests.GroupBy(q => q.prev))
        {
            var byPrevious = grouped.Select(tuple => CreateTemplate(tuple.entry, tuple.prev, tuple.next, tuple.exclusiveGroup)).ToList();
            source.GetByPreviousQuestId((uint)Math.Abs(grouped.Key))
                .Returns(byPrevious);
        }
        
        foreach (var grouped in quests.GroupBy(q => q.next))
        {
            var byNext = grouped.Select(tuple => CreateTemplate(tuple.entry, tuple.prev, tuple.next, tuple.exclusiveGroup)).ToList();
            source.GetByNextQuestId(grouped.Key)
                .Returns(byNext);
        }

        var store = new QuestStore(source);
        return store;
    }

    private void VerifyQuery(List<ChainRawData> quests,
        uint loadQuest)
    {
        var store = CreateStore(quests.Select(q=>(q.Id, q.PrevQuestId, (uint)q.NextQuestId, q.ExclusiveGroup)).ToList());
        store.LoadQuest(loadQuest);
        var queryGenerator = new ChainGenerator();
        var generated = queryGenerator.Generate(store).ToList();
        CollectionAssert.AreEquivalent(quests, generated);
    }
    
    [Test]
    public void TestSimplePreQuest()
    {
        VerifyQuery(new()
        {
            new (1, 0, 0, 0, 0),
            new (2, 1, 0, 0, 0)
        }, 1);
    }
    
    [Test]
    public void TestSimplePreQuest2()
    {
        VerifyQuery(new()
        {
            new (1, 0, 0, 0),
            new (2, 1, 0, 0)
        }, 2);
    }
    
    [Test]
    public void TestChainOfQuests()
    {
        VerifyQuery(new()
        {
            new (1, 0, 0, 0),
            new (2, 1, 0, 0),
            new (3, 2, 0, 0),
            new (4, 3, 0, 0)
        }, 3);
    }
    
    [Test]
    public void TestManyLeadsToOneButOnlyOneRequired()
    {
        VerifyQuery(new()
        {
            new (1, 0, 4, 1),
            new (2, 0, 4, 1),
            new (3, 0, 4, 1),
            new (4, 0, 0, 0),
            new (5, 4, 0, 0)
        }, 3);
    }
    
    [Test]
    public void TestManyLeadsToOneAllRequired()
    {
        VerifyQuery(new()
        {
            new (1, 0, 4, -1),
            new (2, 0, 4, -1),
            new (3, 0, 4, -1),
            new (4, 0, 0, 0),
            new (5, 4, 0, 0)
        }, 5);
    }
    
    [Test]
    public void TestEitherAandBorC()
    {
        VerifyQuery(new()
        {
            new (1, 0, 4, -1), // A
            new (2, 0, 4, -1), // B
            new (3, 5, 4, 0), // C
            new (4, 0, 0, 0), // D
            new (5, 0, 0, 0)
        }, 5);
    }
    
    [Test]
    public void TestQuestWithSplitAndChildQuest()
    {
        VerifyQuery(new()
        {
            new (1, 0, 0, 0),
            new (2, 1, 5, -2),
            new (3, 1, 5, -2),
            new (4, -3, 0, 0),
            new (5, 0, 0, 0)
        }, 5);
    }
    
    /*
    *questA*    *questC*    *questE*
       |           |            |
    *questB*    *questD*    *questF*
       \           |           /
         ------ *questG* -----
     */
    [Test]
    public void TestMultipleQuestChainsLeadingToOneFinalQuest()
    {
        VerifyQuery(new()
        {
            //new (questX, 0, 0, 0),
            new (questA, 0, 0, 0),
            new (questB, questA, questG, -questB),
            new (questC, 0, 0, 0),
            new (questD, questC, questG, -questB),
            new (questE, 0, 0, 0),
            new (questF, questE, questG, -questB),
            new (questG, 0, 0, 0),
        }, 2);
    }
    
    /*
                *questA*
                   |
                *questB*
              /          \
          *questC*     *questF*
             |         *questG*
          *questD*     *questH*
             |            |
          *questE*     *questI*
             \           /
                *questJ*
     */
    [Test]
    public void TestComplicated()
    {
        VerifyQuery(new()
        {
            new (questA, 0, 0, 0),
            new (questB, questA, 0, 0),
            new (questC, questB, 0, 0),
            new (questD, questC, 0, 0),
            new (questE, questD, questJ, -questE),
            new (questF, questB, questI, -questF),
            new (questG, questB, questI, -questF),
            new (questH, questB, questI, -questF),
            new (questI, 0, questJ, -questE),
            new (questJ, 0, 0, 0),
        }, questG);
    }
}