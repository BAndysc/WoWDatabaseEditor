using System;
using System.Collections.Generic;
using System.Linq;
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
        
        private struct ComponentC : IComponentData
        {
        }

        private class ManagedData : IManagedComponentData
        {
            public string? str;
        }
        
        private IEntityManager entityManager = null!;
        private Archetype archetype = null!;
        private Archetype archetypeOnlyA = null!;
        private Archetype archetypeOnlyB = null!;
        private Archetype archetypeOnlyC = null!;
        private Archetype archetypeOnlyManaged = null!;
        private Archetype archetypeAAndManaged = null!;
        
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
            archetypeOnlyC = entityManager
                .NewArchetype()
                .WithComponentData<ComponentC>();
            archetypeOnlyManaged = entityManager
                .NewArchetype()
                .WithManagedComponentData<ManagedData>();
            archetypeAAndManaged = entityManager
                .NewArchetype()
                .WithComponentData<ComponentA>()
                .WithManagedComponentData<ManagedData>();
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
        public void AllowEmptyComponentData()
        {
            var a = entityManager.CreateEntity(archetype);
            var b = entityManager.CreateEntity(archetypeOnlyC);
            var c = entityManager.CreateEntity(archetypeOnlyC);
            Assert.Throws<Exception>(() => entityManager.GetComponent<ComponentC>(a));
            entityManager.GetComponent<ComponentC>(b);
            entityManager.GetComponent<ComponentC>(c);
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
            while (iterator.MoveNext())
            {
                var current = iterator.Current;
                var accessComponentA = current.DataAccess<ComponentA>();
                var accessComponentB = current.DataAccess<ComponentB>();
                for (int i = 0; i < current.Length; ++i)
                {
                    Assert.AreEqual(entities[i], current[i]);
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
        
        [Test]
        public void BasicManagedData()
        {
            var entity = entityManager.CreateEntity(archetypeOnlyManaged);
            var data = new ManagedData() { str = "abc" };
            entityManager.SetManagedComponent<ManagedData>(entity, data);
            Assert.AreEqual("abc", entityManager.GetManagedComponent<ManagedData>(entity).str);
            Assert.AreSame(data, entityManager.GetManagedComponent<ManagedData>(entity));
        }
        
        [Test]
        public void MixedManagedAndNative()
        {
            var entityManaged = entityManager.CreateEntity(archetypeOnlyManaged);
            var entityNative = entityManager.CreateEntity(archetypeOnlyA);
            var entityMixed = entityManager.CreateEntity(archetypeAAndManaged);
            
            Assert.IsTrue(entityManager.Is(entityManaged, archetypeOnlyManaged));
            Assert.IsFalse(entityManager.Is(entityManaged, archetypeOnlyA));
            Assert.IsFalse(entityManager.Is(entityManaged, archetypeAAndManaged));
            
            Assert.IsFalse(entityManager.Is(entityNative, archetypeOnlyManaged));
            Assert.IsTrue(entityManager.Is(entityNative, archetypeOnlyA));
            Assert.IsFalse(entityManager.Is(entityNative, archetypeAAndManaged));
            
            Assert.IsTrue(entityManager.Is(entityMixed, archetypeOnlyManaged));
            Assert.IsTrue(entityManager.Is(entityMixed, archetypeOnlyA));
            Assert.IsTrue(entityManager.Is(entityMixed, archetypeAAndManaged));
        }
        
        [Test]
        public void AddComponentTest()
        {
            var entityA = entityManager.CreateEntity(archetypeOnlyA);
            
            Assert.IsTrue(entityManager.Is(entityA, archetypeOnlyA));
            Assert.IsFalse(entityManager.Is(entityA, archetypeOnlyB));
            Assert.IsFalse(entityManager.Is(entityA, archetype));
            
            entityManager.AddComponent(entityA, new ComponentB());
            
            Assert.IsTrue(entityManager.Is(entityA, archetypeOnlyA));
            Assert.IsTrue(entityManager.Is(entityA, archetypeOnlyB));
            Assert.IsTrue(entityManager.Is(entityA, archetype));

            int totalCount = 0;
            archetypeOnlyB.ForEach<ComponentB>((itr, start, end, componentsB) =>
            {
                totalCount += end - start;
            });
            Assert.AreEqual(1, totalCount);
            
            totalCount = 0;
            archetypeOnlyA.ForEach<ComponentA>((itr, start, end, componentsB) =>
            {
                totalCount += end - start;
            });
            Assert.AreEqual(1, totalCount);
        }
    }
}