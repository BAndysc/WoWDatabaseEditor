using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using NUnit.Framework;
using WDE.Common.Database;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Test.Models;

public class QuestStoreTests
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
        template.BreadcrumbForQuestId.Returns(0);
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
        
        source.GetByBreadCrumbQuestId(0).Returns(Enumerable.Empty<IQuestTemplate>());

        var store = new QuestStore(source);
        return store;
    }
    
    [Test]
    public void TestSimplePreQuest()
    {
        var store = CreateStore(new()
        {
            (1, 0, 0, 0),
            (2, 1, 0, 0)
        });
        
        store.LoadQuest(1);
        CollectionAssert.AreEquivalent(Array.Empty<string>(), store[1].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"1"}, store[2].MustBeCompleted.Select(r => r.ToString()));
    }
    
    [Test]
    public void TestSimplePreQuest2()
    {
        var store = CreateStore(new()
        {
            (1, 0, 0, 0),
            (2, 1, 0, 0)
        });
        
        store.LoadQuest(2);
        CollectionAssert.AreEquivalent(Array.Empty<string>(), store[1].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"1"}, store[2].MustBeCompleted.Select(r => r.ToString()));
    }
    
    [Test]
    public void TestChainOfQuests()
    {
        var store = CreateStore(new()
        {
            (1, 0, 0, 0),
            (2, 1, 0, 0),
            (3, 2, 0, 0),
            (4, 3, 0, 0)
        });
        
        store.LoadQuest(3);
        CollectionAssert.AreEquivalent(Array.Empty<string>(), store[1].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"1"}, store[2].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"2"}, store[3].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"3"}, store[4].MustBeCompleted.Select(r => r.ToString()));
    }
    
    [Test]
    public void TestManyLeadsToOneButOnlyOneRequired()
    {
        var store = CreateStore(new()
        {
            (1, 0, 4, 1),
            (2, 0, 4, 1),
            (3, 0, 4, 1),
            (4, 0, 0, 0),
            (5, 4, 0, 0)
        });
        
        store.LoadQuest(3);
        CollectionAssert.AreEquivalent(Array.Empty<string>(), store[1].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(Array.Empty<string>(), store[2].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(Array.Empty<string>(), store[3].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"1|2|3"}, store[4].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"4"}, store[5].MustBeCompleted.Select(r => r.ToString()));
    }
    
    [Test]
    public void TestManyLeadsToOneAllRequired()
    {
        var store = CreateStore(new()
        {
            (1, 0, 4, -1),
            (2, 0, 4, -1),
            (3, 0, 4, -1),
            (4, 0, 0, 0),
            (5, 4, 0, 0)
        });
        
        store.LoadQuest(5);
        CollectionAssert.AreEquivalent(Array.Empty<string>(), store[1].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(Array.Empty<string>(), store[2].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(Array.Empty<string>(), store[3].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"1&2&3"}, store[4].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"4"}, store[5].MustBeCompleted.Select(r => r.ToString()));
    }
    
    [Test]
    public void TestEitherAandBorC()
    {
        var store = CreateStore(new()
        {
            (1, 0, 4, -1), // A
            (2, 0, 4, -1), // B
            (3, 5, 4, 0), // C
            (4, 0, 0, 0), // D
            (5, 0, 0, 0)
        });
        
        store.LoadQuest(5);
        CollectionAssert.AreEquivalent(Array.Empty<string>(), store[1].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(Array.Empty<string>(), store[2].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"5"}, store[3].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"1&2", "3"}, store[4].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(Array.Empty<string>(), store[5].MustBeCompleted.Select(r => r.ToString()));
    }
    
    [Test]
    public void TestQuestWithSplitAndChildQuest()
    {
        var store = CreateStore(new()
        {
            (1, 0, 0, 0),
            (2, 1, 5, -2),
            (3, 1, 5, -2),
            (4, -3, 0, 0),
            (5, 0, 0, 0)
        });
        
        store.LoadQuest(5);
        CollectionAssert.AreEquivalent(Array.Empty<string>(), store[1].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"1"}, store[2].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"1"}, store[3].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"+3"}, store[4].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"2&3"}, store[5].MustBeCompleted.Select(r => r.ToString()));
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
        var store = CreateStore(new()
        {
            (questX, 0, 0, 0),
            (questA, 0, 0, 0),
            (questB, questA, questG, -questB),
            (questC, 0, 0, 0),
            (questD, questC, questG, -questB),
            (questE, 0, 0, 0),
            (questF, questE, questG, -questB),
            (questG, 0, 0, 0),
        });
        
        store.LoadQuest(2);
        //CollectionAssert.AreEquivalent(Array.Empty<string>(), store[8].Requirements.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(Array.Empty<string>(), store[questA].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"1"}, store[questB].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(Array.Empty<string>(), store[questC].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"3"}, store[questD].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(Array.Empty<string>(), store[questE].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"5"}, store[questF].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"2&4&6"}, store[questG].MustBeCompleted.Select(r => r.ToString()));
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
        var store = CreateStore(new()
        {
            (questA, 0, 0, 0),
            (questB, questA, 0, 0),

            (questC, questB, 0, 0),
            (questD, questC, 0, 0),
            (questE, questD, questJ, -questE),

            (questF, questB, questI, -questF),
            (questG, questB, questI, -questF),
            (questH, questB, questI, -questF),

            (questI, 0, questJ, -questE),

            (questJ, 0, 0, 0),
        });
        
        store.LoadQuest(questG);
        CollectionAssert.AreEquivalent(Array.Empty<string>(), store[questA].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"1"}, store[questB].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"2"}, store[questC].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"3"}, store[questD].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"4"}, store[questE].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"2"}, store[questF].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"2"}, store[questG].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"2"}, store[questH].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"6&7&8"}, store[questI].MustBeCompleted.Select(r => r.ToString()));
        CollectionAssert.AreEquivalent(new string[]{"5&9"}, store[questJ].MustBeCompleted.Select(r => r.ToString()));
    }
}
