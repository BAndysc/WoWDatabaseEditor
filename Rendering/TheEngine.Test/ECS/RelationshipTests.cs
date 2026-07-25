using System.Collections.Generic;
using NUnit.Framework;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Managers;

namespace TheEngine.Test.ECS
{
    public class RelationshipTests
    {
        private EntityManager em = null!;
        private Archetype arch = null!;

        [SetUp]
        public void Setup()
        {
            em = new EntityManager(new StatsManager(), null!);
            arch = em.NewArchetype();
        }

        [TearDown]
        public void TearDown() => em.Dispose();

        private Relationship Rel(Entity e) => em.GetComponent<Relationship>(e);

        private List<Entity> Children(Entity parent)
        {
            var list = new List<Entity>();
            for (var c = Rel(parent).FirstChild; c != Entity.Empty; c = Rel(c).NextSibling)
                list.Add(c);
            return list;
        }

        private List<Entity> Roots()
        {
            var list = new List<Entity>();
            for (var r = em.HierarchyFirstRoot; r != Entity.Empty; r = Rel(r).NextSibling)
                list.Add(r);
            return list;
        }

        [Test]
        public void NewEntityIsARoot()
        {
            var a = em.CreateEntity(arch);
            Assert.AreEqual(Entity.Empty, Rel(a).Parent);
            CollectionAssert.Contains(Roots(), a);
        }

        [Test]
        public void SetParentMovesOutOfRootListIntoChildList()
        {
            var parent = em.CreateEntity(arch);
            var child = em.CreateEntity(arch);

            em.SetParent(child, parent);

            Assert.AreEqual(parent, Rel(child).Parent);
            CollectionAssert.DoesNotContain(Roots(), child);
            CollectionAssert.Contains(Roots(), parent);
            CollectionAssert.AreEquivalent(new[] { child }, Children(parent));
            Assert.AreEqual(1, Rel(parent).ChildCount);
        }

        [Test]
        public void MultipleChildrenFormAList()
        {
            var parent = em.CreateEntity(arch);
            var c1 = em.CreateEntity(arch);
            var c2 = em.CreateEntity(arch);
            var c3 = em.CreateEntity(arch);

            em.SetParent(c1, parent);
            em.SetParent(c2, parent);
            em.SetParent(c3, parent);

            CollectionAssert.AreEquivalent(new[] { c1, c2, c3 }, Children(parent));
            Assert.AreEqual(3, Rel(parent).ChildCount);
        }

        [Test]
        public void ReparentMovesBetweenParents()
        {
            var p1 = em.CreateEntity(arch);
            var p2 = em.CreateEntity(arch);
            var child = em.CreateEntity(arch);

            em.SetParent(child, p1);
            em.SetParent(child, p2);

            CollectionAssert.DoesNotContain(Children(p1), child);
            CollectionAssert.Contains(Children(p2), child);
            Assert.AreEqual(0, Rel(p1).ChildCount);
            Assert.AreEqual(1, Rel(p2).ChildCount);
            Assert.AreEqual(p2, Rel(child).Parent);
        }

        [Test]
        public void SetParentEmptyMakesRootAgain()
        {
            var parent = em.CreateEntity(arch);
            var child = em.CreateEntity(arch);
            em.SetParent(child, parent);

            em.SetParent(child, Entity.Empty);

            Assert.AreEqual(Entity.Empty, Rel(child).Parent);
            CollectionAssert.Contains(Roots(), child);
            Assert.AreEqual(0, Rel(parent).ChildCount);
            CollectionAssert.IsEmpty(Children(parent));
        }

        [Test]
        public void RemovingMiddleChildKeepsListIntact()
        {
            var parent = em.CreateEntity(arch);
            var c1 = em.CreateEntity(arch);
            var c2 = em.CreateEntity(arch);
            var c3 = em.CreateEntity(arch);
            em.SetParent(c1, parent);
            em.SetParent(c2, parent);
            em.SetParent(c3, parent);

            em.SetParent(c2, Entity.Empty); // pull the middle one out (c3,c2,c1 order -> remove c2)

            CollectionAssert.AreEquivalent(new[] { c1, c3 }, Children(parent));
            Assert.AreEqual(2, Rel(parent).ChildCount);
            // walk both directions to confirm sibling links are consistent
            var forward = Children(parent);
            Assert.AreEqual(forward[0], Rel(forward[1]).PrevSibling);
            Assert.AreEqual(forward[1], Rel(forward[0]).NextSibling);
        }

        [Test]
        public void DestroyEntityCascadesToWholeSubtree()
        {
            var root = em.CreateEntity(arch);
            var a = em.CreateEntity(arch);
            var b = em.CreateEntity(arch);
            var grandchild = em.CreateEntity(arch);
            em.SetParent(a, root);
            em.SetParent(b, root);
            em.SetParent(grandchild, a);

            em.DestroyEntity(root);

            Assert.IsFalse(em.Exist(root));
            Assert.IsFalse(em.Exist(a));
            Assert.IsFalse(em.Exist(b));
            Assert.IsFalse(em.Exist(grandchild));
            CollectionAssert.DoesNotContain(Roots(), root);
        }

        [Test]
        public void DestroyingChildLeavesParentAndSiblingsIntact()
        {
            var parent = em.CreateEntity(arch);
            var c1 = em.CreateEntity(arch);
            var c2 = em.CreateEntity(arch);
            em.SetParent(c1, parent);
            em.SetParent(c2, parent);

            em.DestroyEntity(c1);

            Assert.IsTrue(em.Exist(parent));
            Assert.IsTrue(em.Exist(c2));
            CollectionAssert.AreEquivalent(new[] { c2 }, Children(parent));
            Assert.AreEqual(1, Rel(parent).ChildCount);
        }

        [Test]
        public void StructuralVersionChangesOnMutations()
        {
            var v0 = em.StructuralVersion;
            var a = em.CreateEntity(arch);
            var b = em.CreateEntity(arch);
            var afterCreate = em.StructuralVersion;
            Assert.Greater(afterCreate, v0);

            em.SetParent(b, a);
            Assert.Greater(em.StructuralVersion, afterCreate);
            var afterParent = em.StructuralVersion;

            em.DestroyEntity(a);
            Assert.Greater(em.StructuralVersion, afterParent);
        }

#if DEBUG
        [Test]
        public void ParentingToDescendantThrows()
        {
            var a = em.CreateEntity(arch);
            var b = em.CreateEntity(arch);
            em.SetParent(b, a);
            Assert.Throws<System.Exception>(() => em.SetParent(a, b));
        }

        [Test]
        public void ParentingToSelfThrows()
        {
            var a = em.CreateEntity(arch);
            Assert.Throws<System.Exception>(() => em.SetParent(a, a));
        }
#endif
    }
}
