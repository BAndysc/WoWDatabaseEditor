using System.Collections.Generic;
using NUnit.Framework;
using TheEngine.ECS;

namespace TheEngine.Test.ECS
{
    public class EntityDataMangerTests
    {
        private struct ComponentA : IComponentData
        {
            public int a;
            public float b;
        }
        
        private struct ComponentB : IComponentData
        {
            public int x;
        }
        
        private IEntityManager entityManager = null!;
        private Archetype archetype;
        private Archetype archetypeOnlyA;
        private Archetype archetypeOnlyB;
        
        [SetUp]
        public void Setup()
        {
            entityManager = new EntityManager();
            archetype = entityManager
                .NewArchetype()
                .WithComponentData<ComponentA>().WithComponentData<ComponentB>();
            archetypeOnlyA = entityManager
                .NewArchetype()
                .WithComponentData<ComponentA>();
            archetypeOnlyB = entityManager
                .NewArchetype()
                .WithComponentData<ComponentB>();
        }

        [Test]
        public void NewEntityHasEmptyComponents()
        {
            var e = entityManager.CreateEntity(archetype);
            Assert.AreEqual(0, entityManager.GetComponent<ComponentA>(e).a);
            Assert.AreEqual(0, entityManager.GetComponent<ComponentA>(e).b);
            Assert.AreEqual(0, entityManager.GetComponent<ComponentB>(e).x);
        }
        
        [Test]
        public void NewEntityCanSetComponentData()
        {
            var e = entityManager.CreateEntity(archetype);
            entityManager.GetComponent<ComponentA>(e).a = 5;
            entityManager.GetComponent<ComponentA>(e).b = 4;
            entityManager.GetComponent<ComponentB>(e).x = int.MaxValue;
            Assert.AreEqual(5, entityManager.GetComponent<ComponentA>(e).a);
            Assert.AreEqual(4.0f, entityManager.GetComponent<ComponentA>(e).b);
            Assert.AreEqual(int.MaxValue, entityManager.GetComponent<ComponentB>(e).x);
        }
        
        [Test]
        public void ReusedEntityClearsData()
        {
            var e = entityManager.CreateEntity(archetype);
            entityManager.GetComponent<ComponentA>(e).a = 5;
            entityManager.GetComponent<ComponentA>(e).b = 4;
            entityManager.GetComponent<ComponentB>(e).x = int.MaxValue;
            entityManager.DestroyEntity(e);
            e = entityManager.CreateEntity(archetype);
            Assert.AreEqual(0, entityManager.GetComponent<ComponentA>(e).a);
            Assert.AreEqual(0, entityManager.GetComponent<ComponentA>(e).b);
            Assert.AreEqual(0, entityManager.GetComponent<ComponentB>(e).x);
        }

        [Test]
        public void RemovingEntityWorks()
        {
            var a = entityManager.CreateEntity(archetype);
            var b = entityManager.CreateEntity(archetype);
            var c = entityManager.CreateEntity(archetype);
            entityManager.GetComponent<ComponentA>(a).a = 1;
            entityManager.GetComponent<ComponentA>(b).a = 2;
            entityManager.GetComponent<ComponentA>(c).a = 3;
            
            Assert.AreEqual(1, entityManager.GetComponent<ComponentA>(a).a);
            Assert.AreEqual(2, entityManager.GetComponent<ComponentA>(b).a);
            Assert.AreEqual(3, entityManager.GetComponent<ComponentA>(c).a);
            
            entityManager.DestroyEntity(a);
            Assert.AreEqual(2, entityManager.GetComponent<ComponentA>(b).a);
            Assert.AreEqual(3, entityManager.GetComponent<ComponentA>(c).a);
        }

        [Test]
        public void IterateThroughArchetype()
        {
            Entity[] entities = new Entity[3];
            entities[0] = entityManager.CreateEntity(archetype);
            entities[1] = entityManager.CreateEntity(archetype);
            entities[2] = entityManager.CreateEntity(archetype);
            entityManager.GetComponent<ComponentA>(entities[0]).a = 0;
            entityManager.GetComponent<ComponentA>(entities[1]).a = 1;
            entityManager.GetComponent<ComponentA>(entities[2]).a = 2;
            entityManager.GetComponent<ComponentB>(entities[0]).x = 2;
            entityManager.GetComponent<ComponentB>(entities[1]).x = 1;
            entityManager.GetComponent<ComponentB>(entities[2]).x = 0;

            var iterator = entityManager.ArchetypeIterator(archetype);
            foreach (var itr in iterator)
            {
                var accessComponentA = itr.DataAccess<ComponentA>();
                var accessComponentB = itr.DataAccess<ComponentB>();
                for (int i = 0; i < itr.Length; ++i)
                {
                    Assert.AreEqual(entities[i], itr[i]);
                    Assert.AreEqual(i, accessComponentA[i].a);
                    Assert.AreEqual(2 - i, accessComponentB[i].x);
                }   
            }
        }

        [Test]
        public void IterateMultipleArchetypes()
        {
            Entity[] entities = new Entity[3];
            entities[0] = entityManager.CreateEntity(archetype);
            entities[1] = entityManager.CreateEntity(archetypeOnlyA);
            entities[2] = entityManager.CreateEntity(archetypeOnlyB);
            entityManager.GetComponent<ComponentA>(entities[0]).a = 1;
            entityManager.GetComponent<ComponentA>(entities[1]).a = 2;
            
            List<Entity> foundEntities = new();
            List<int> foundValues = new();
            archetypeOnlyA.ForEach<ComponentA>((itr, start, end, access) =>
            {
                for (int i = start; i < end; ++i)
                {
                    foundEntities.Add(itr[i]);
                    foundValues.Add(access[i].a);
                }   
            });
            CollectionAssert.AreEquivalent(new[]{entities[0], entities[1]}, foundEntities);
            CollectionAssert.AreEquivalent(new[]{1, 2}, foundValues);
        }
    }
}