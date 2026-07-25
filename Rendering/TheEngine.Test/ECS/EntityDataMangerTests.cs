using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheEngine.ECS;
using TheEngine.Managers;

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


        [ArrayComponent]
        private struct ComponentD : IComponentData
        {
            public int a;
            public float b;
        }

        [ArrayComponent]
        private struct ComponentE : IComponentData
        {
            public int x;
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
        private Archetype archetypeOnlyD = null!;
        private Archetype archetypeOnlyE = null!;
        private Archetype archetypeOnlyManaged = null!;
        private Archetype archetypeAAndManaged = null!;
        
        [SetUp]
        public void Setup()
        {
            entityManager = new EntityManager(new StatsManager(), null!);
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
            archetypeOnlyD = entityManager
                .NewArchetype()
                .WithComponentData<ComponentD>();
            archetypeOnlyE = entityManager
                .NewArchetype()
                .WithComponentData<ComponentE>();
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
            archetypeOnlyA.ForEach<ComponentA>((itr, thread, start, end, access) =>
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
        }

        [Test]
        public void ArrayComponentTest()
        {
            var entity = entityManager.CreateEntity(archetypeOnlyD);

            // Initially, entity should have no array components
            var components = entityManager.GetArrayComponents<ComponentD>(entity);
            Assert.AreEqual(0, components.Length);

            // Add first component
            var component1 = new ComponentD { a = 10, b = 1.5f };
            entityManager.AddArrayComponent(entity, component1);

            components = entityManager.GetArrayComponents<ComponentD>(entity);
            Assert.AreEqual(1, components.Length);
            Assert.AreEqual(10, components[0].a);
            Assert.AreEqual(1.5f, components[0].b);

            // Add second component
            var component2 = new ComponentD { a = 20, b = 2.5f };
            entityManager.AddArrayComponent(entity, component2);

            components = entityManager.GetArrayComponents<ComponentD>(entity);
            Assert.AreEqual(2, components.Length);
            Assert.AreEqual(10, components[0].a);
            Assert.AreEqual(1.5f, components[0].b);
            Assert.AreEqual(20, components[1].a);
            Assert.AreEqual(2.5f, components[1].b);

            // Add third component
            var component3 = new ComponentD { a = 30, b = 3.5f };
            entityManager.AddArrayComponent(entity, component3);

            components = entityManager.GetArrayComponents<ComponentD>(entity);
            Assert.AreEqual(3, components.Length);
            Assert.AreEqual(30, components[2].a);
            Assert.AreEqual(3.5f, components[2].b);

            // Remove middle component (index 1)
            bool removed = entityManager.RemoveArrayComponent<ComponentD>(entity, 1);
            Assert.IsTrue(removed);

            components = entityManager.GetArrayComponents<ComponentD>(entity);
            Assert.AreEqual(2, components.Length);
            Assert.AreEqual(10, components[0].a); // First component unchanged
            Assert.AreEqual(30, components[1].a); // Third component moved to index 1

            // Remove first component (index 0)
            removed = entityManager.RemoveArrayComponent<ComponentD>(entity, 0);
            Assert.IsTrue(removed);

            components = entityManager.GetArrayComponents<ComponentD>(entity);
            Assert.AreEqual(1, components.Length);
            Assert.AreEqual(30, components[0].a); // Only third component remains

            // Remove last component
            removed = entityManager.RemoveArrayComponent<ComponentD>(entity, 0);
            Assert.IsTrue(removed);

            components = entityManager.GetArrayComponents<ComponentD>(entity);
            Assert.AreEqual(0, components.Length);

            // Try to remove from empty array - should return false
            removed = entityManager.RemoveArrayComponent<ComponentD>(entity, 0);
            Assert.IsFalse(removed);
        }

        [Test]
        public void MultipleEntitiesArrayComponentsTest()
        {
            var entity1 = entityManager.CreateEntity(archetypeOnlyE);
            var entity2 = entityManager.CreateEntity(archetypeOnlyE);

            // Add components to entity1
            entityManager.AddArrayComponent(entity1, new ComponentE { x = 100 });
            entityManager.AddArrayComponent(entity1, new ComponentE { x = 200 });

            // Add components to entity2
            entityManager.AddArrayComponent(entity2, new ComponentE { x = 300 });
            entityManager.AddArrayComponent(entity2, new ComponentE { x = 400 });
            entityManager.AddArrayComponent(entity2, new ComponentE { x = 500 });

            var dataAccess = entityManager.GetArrayDataAccessByEntity<ComponentE>(entity1);
            Assert.AreEqual(dataAccess[entity1, 0].x, 100);
            Assert.AreEqual(dataAccess[entity1, 1].x, 200);
            Assert.AreEqual(dataAccess[entity2, 0].x, 300);
            Assert.AreEqual(dataAccess[entity2, 1].x, 400);
            Assert.AreEqual(dataAccess[entity2, 2].x, 500);


            // Verify entity1 components
            var components1 = entityManager.GetArrayComponents<ComponentE>(entity1);
            Assert.AreEqual(2, components1.Length);
            Assert.AreEqual(100, components1[0].x);
            Assert.AreEqual(200, components1[1].x);

            // Verify entity2 components
            var components2 = entityManager.GetArrayComponents<ComponentE>(entity2);
            Assert.AreEqual(3, components2.Length);
            Assert.AreEqual(300, components2[0].x);
            Assert.AreEqual(400, components2[1].x);
            Assert.AreEqual(500, components2[2].x);

            // Remove component from entity1 should not affect entity2
            entityManager.RemoveArrayComponent<ComponentE>(entity1, 0);

            components1 = entityManager.GetArrayComponents<ComponentE>(entity1);
            components2 = entityManager.GetArrayComponents<ComponentE>(entity2);

            Assert.AreEqual(1, components1.Length);
            Assert.AreEqual(200, components1[0].x);

            Assert.AreEqual(3, components2.Length); // Entity2 should be unchanged
            Assert.AreEqual(300, components2[0].x);
            Assert.AreEqual(400, components2[1].x);
            Assert.AreEqual(500, components2[2].x);
        }

        [Test]
        public void EntityDestroyWithArrayComponentsTest()
        {
            var entity = entityManager.CreateEntity(archetypeOnlyD);

            // Add several components
            entityManager.AddArrayComponent(entity, new ComponentD { a = 1, b = 1.0f });
            entityManager.AddArrayComponent(entity, new ComponentD { a = 2, b = 2.0f });
            entityManager.AddArrayComponent(entity, new ComponentD { a = 3, b = 3.0f });

            var components = entityManager.GetArrayComponents<ComponentD>(entity);
            Assert.AreEqual(3, components.Length);

            // Destroy entity - this should clean up all array components
            entityManager.DestroyEntity(entity);

            // Entity should no longer exist
            Assert.IsFalse(entityManager.Exist(entity));
        }

        [Test]
        public void InterleavedArrayComponentsAcrossEntitiesTest()
        {
            var entity1 = entityManager.CreateEntity(archetypeOnlyD);
            var entity2 = entityManager.CreateEntity(archetypeOnlyD);

            // Interleaved additions across two entities
            entityManager.AddArrayComponent(entity1, new ComponentD { a = 1, b = 1.0f });
            entityManager.AddArrayComponent(entity2, new ComponentD { a = 100, b = 10.0f });
            entityManager.AddArrayComponent(entity1, new ComponentD { a = 2, b = 2.0f });
            entityManager.AddArrayComponent(entity1, new ComponentD { a = 3, b = 3.0f });
            entityManager.AddArrayComponent(entity2, new ComponentD { a = 200, b = 20.0f });
            entityManager.AddArrayComponent(entity1, new ComponentD { a = 4, b = 4.0f });
            entityManager.AddArrayComponent(entity2, new ComponentD { a = 300, b = 30.0f });

            var components1 = entityManager.GetArrayComponents<ComponentD>(entity1);
            var components2 = entityManager.GetArrayComponents<ComponentD>(entity2);
            Assert.AreEqual(4, components1.Length);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, components1.ToArray().Select(x => x.a).ToArray());
            Assert.AreEqual(3, components2.Length);
            CollectionAssert.AreEqual(new[] { 100, 200, 300 }, components2.ToArray().Select(x => x.a).ToArray());

            // Remove from middle of entity1 and verify entity2 is unaffected
            Assert.IsTrue(entityManager.RemoveArrayComponent<ComponentD>(entity1, 1)); // remove a = 2
            components1 = entityManager.GetArrayComponents<ComponentD>(entity1);
            components2 = entityManager.GetArrayComponents<ComponentD>(entity2);
            CollectionAssert.AreEqual(new[] { 1, 3, 4 }, components1.ToArray().Select(x => x.a).ToArray());
            CollectionAssert.AreEqual(new[] { 100, 200, 300 }, components2.ToArray().Select(x => x.a).ToArray());

            // Add again interleaved after removal
            entityManager.AddArrayComponent(entity2, new ComponentD { a = 400, b = 40.0f });
            entityManager.AddArrayComponent(entity1, new ComponentD { a = 5, b = 5.0f });

            components1 = entityManager.GetArrayComponents<ComponentD>(entity1);
            components2 = entityManager.GetArrayComponents<ComponentD>(entity2);
            CollectionAssert.AreEqual(new[] { 1, 3, 4, 5 }, components1.ToArray().Select(x => x.a).ToArray());
            CollectionAssert.AreEqual(new[] { 100, 200, 300, 400 }, components2.ToArray().Select(x => x.a).ToArray());

            // Validate via data access by entity indexer
            var access = entityManager.GetArrayDataAccessByEntity<ComponentD>(entity1);
            Assert.AreEqual(1, access[entity1, 0].a);
            Assert.AreEqual(3, access[entity1, 1].a);
            Assert.AreEqual(4, access[entity1, 2].a);
            Assert.AreEqual(5, access[entity1, 3].a);

            Assert.AreEqual(100, access[entity2, 0].a);
            Assert.AreEqual(200, access[entity2, 1].a);
            Assert.AreEqual(300, access[entity2, 2].a);
            Assert.AreEqual(400, access[entity2, 3].a);
        }

        [Test]
        public void RandomArrayComponentAndEntityOperationsStressTest()
        {
            var random = new Random(42); // Fixed seed for reproducibility
            var entities = new List<Entity>();
            var expectedData = new Dictionary<Entity, List<ComponentD>>();

            // Perform random operations
            for (int iteration = 0; iteration < 500; iteration++)
            {
                var operation = random.Next(0, 4); // 0=create entity, 1=destroy entity, 2=add component, 3=remove component

                switch (operation)
                {
                    case 0: // Create entity
                        if (entities.Count < 50) // Limit max entities
                        {
                            var entity = entityManager.CreateEntity(archetypeOnlyD);
                            entities.Add(entity);
                            expectedData[entity] = new List<ComponentD>();
                        }
                        break;

                    case 1: // Destroy entity
                        if (entities.Count > 0)
                        {
                            var entityIndex = random.Next(0, entities.Count);
                            var entity = entities[entityIndex];
                            entities.RemoveAt(entityIndex);
                            expectedData.Remove(entity);
                            entityManager.DestroyEntity(entity);
                        }
                        break;

                    case 2: // Add array component
                        if (entities.Count > 0)
                        {
                            var entityIndex = random.Next(0, entities.Count);
                            var entity = entities[entityIndex];
                            if (expectedData[entity].Count < 10) // Limit components per entity
                            {
                                var component = new ComponentD { a = random.Next(1, 1000), b = (float)random.NextDouble() * 100 };
                                expectedData[entity].Add(component);
                                entityManager.AddArrayComponent(entity, component);
                            }
                        }
                        break;

                    case 3: // Remove array component
                        if (entities.Count > 0)
                        {
                            var entityIndex = random.Next(0, entities.Count);
                            var entity = entities[entityIndex];
                            if (expectedData[entity].Count > 0)
                            {
                                var componentIndex = random.Next(0, expectedData[entity].Count);
                                expectedData[entity].RemoveAt(componentIndex);
                                entityManager.RemoveArrayComponent<ComponentD>(entity, componentIndex);
                            }
                        }
                        break;
                }

                // Validate state every 50 iterations
                if (iteration % 50 == 49)
                {
                    ValidateArrayComponentsState(entities, expectedData);
                }
            }

            // Final validation
            ValidateArrayComponentsState(entities, expectedData);
        }

        private void ValidateArrayComponentsState(List<Entity> entities, Dictionary<Entity, List<ComponentD>> expectedData)
        {
            // Verify all entities exist and have correct components
            foreach (var entity in entities)
            {
                Assert.IsTrue(entityManager.Exist(entity), $"Entity {entity.Id} should exist");

                var actualComponents = entityManager.GetArrayComponents<ComponentD>(entity);
                var expectedComponents = expectedData[entity];

                Assert.AreEqual(expectedComponents.Count, actualComponents.Length,
                    $"Entity {entity.Id} should have {expectedComponents.Count} components but has {actualComponents.Length}");

                for (int i = 0; i < expectedComponents.Count; i++)
                {
                    Assert.AreEqual(expectedComponents[i].a, actualComponents[i].a,
                        $"Entity {entity.Id} component {i} field 'a' mismatch");
                    Assert.AreEqual(expectedComponents[i].b, actualComponents[i].b, 0.0001f,
                        $"Entity {entity.Id} component {i} field 'b' mismatch");
                }
            }

            // Verify data access by entity works correctly
            if (entities.Count > 0)
            {
                var dataAccess = entityManager.GetArrayDataAccessByEntity<ComponentD>(entities[0]);
                foreach (var entity in entities)
                {
                    var expectedComponents = expectedData[entity];
                    for (int i = 0; i < expectedComponents.Count; i++)
                    {
                        Assert.AreEqual(expectedComponents[i].a, dataAccess[entity, i].a,
                            $"Data access: Entity {entity.Id} component {i} field 'a' mismatch");
                        Assert.AreEqual(expectedComponents[i].b, dataAccess[entity, i].b, 0.0001f,
                            $"Data access: Entity {entity.Id} component {i} field 'b' mismatch");
                    }
                }
            }
        }

        [Test]
        public void ArrayComponentsWithEntitySwappingEdgeCases()
        {
            // Create entities with different numbers of components
            var entity1 = entityManager.CreateEntity(archetypeOnlyE); // Will have 3 components
            var entity2 = entityManager.CreateEntity(archetypeOnlyE); // Will have 1 component
            var entity3 = entityManager.CreateEntity(archetypeOnlyE); // Will have 0 components
            var entity4 = entityManager.CreateEntity(archetypeOnlyE); // Will have 2 components

            // Add components
            entityManager.AddArrayComponent(entity1, new ComponentE { x = 10 });
            entityManager.AddArrayComponent(entity1, new ComponentE { x = 11 });
            entityManager.AddArrayComponent(entity1, new ComponentE { x = 12 });

            entityManager.AddArrayComponent(entity2, new ComponentE { x = 20 });

            // entity3 has no components

            entityManager.AddArrayComponent(entity4, new ComponentE { x = 40 });
            entityManager.AddArrayComponent(entity4, new ComponentE { x = 41 });

            // Test case 1: Remove entity with more components than the swapped entity
            // Remove entity1 (3 components), entity4 (2 components) should be swapped into its place
            entityManager.DestroyEntity(entity1);

            // Verify entity4 is now at entity1's position and has correct data
            var entity4Components = entityManager.GetArrayComponents<ComponentE>(entity4);
            Assert.AreEqual(2, entity4Components.Length);
            Assert.AreEqual(40, entity4Components[0].x);
            Assert.AreEqual(41, entity4Components[1].x);

            // Verify other entities are unaffected
            var entity2Components = entityManager.GetArrayComponents<ComponentE>(entity2);
            Assert.AreEqual(1, entity2Components.Length);
            Assert.AreEqual(20, entity2Components[0].x);

            var entity3Components = entityManager.GetArrayComponents<ComponentB>(entity3);
            Assert.AreEqual(0, entity3Components.Length);

            // Test case 2: Remove entity with fewer components than the swapped entity
            var entity5 = entityManager.CreateEntity(archetypeOnlyE);
            entityManager.AddArrayComponent(entity5, new ComponentE { x = 50 });
            entityManager.AddArrayComponent(entity5, new ComponentE { x = 51 });
            entityManager.AddArrayComponent(entity5, new ComponentE { x = 52 });
            entityManager.AddArrayComponent(entity5, new ComponentE { x = 53 });

            // Remove entity2 (1 component), entity5 (4 components) should be swapped into its place
            entityManager.DestroyEntity(entity2);

            // Verify entity5 has all its components
            var entity5Components = entityManager.GetArrayComponents<ComponentE>(entity5);
            Assert.AreEqual(4, entity5Components.Length);
            Assert.AreEqual(50, entity5Components[0].x);
            Assert.AreEqual(51, entity5Components[1].x);
            Assert.AreEqual(52, entity5Components[2].x);
            Assert.AreEqual(53, entity5Components[3].x);

            // Test case 3: Remove entity with no components
            var entity6 = entityManager.CreateEntity(archetypeOnlyE);
            entityManager.AddArrayComponent(entity6, new ComponentE { x = 60 });

            // Remove entity3 (0 components), entity6 (1 component) should be swapped into its place
            entityManager.DestroyEntity(entity3);

            // Verify entity6 has its component
            var entity6Components = entityManager.GetArrayComponents<ComponentE>(entity6);
            Assert.AreEqual(1, entity6Components.Length);
            Assert.AreEqual(60, entity6Components[0].x);
        }

        [Test]
        public void DebugRemovingEntityIssue()
        {
            // This test helps debug the RemovingEntityWorks issue
            var a = entityManager.CreateEntity(archetype);
            var b = entityManager.CreateEntity(archetype);
            var c = entityManager.CreateEntity(archetype);

            // Set component values
            entityManager.GetComponent<ComponentA>(a).a = 1;
            entityManager.GetComponent<ComponentA>(b).a = 2;
            entityManager.GetComponent<ComponentA>(c).a = 3;

            // Verify initial values
            Assert.AreEqual(1, entityManager.GetComponent<ComponentA>(a).a);
            Assert.AreEqual(2, entityManager.GetComponent<ComponentA>(b).a);
            Assert.AreEqual(3, entityManager.GetComponent<ComponentA>(c).a);

            // Check if ComponentA is being treated as array component in this archetype
            // This should be FALSE since we used .WithComponentData<> not .WithComponentData<>
            Console.WriteLine($"ComponentA IsArray in archetype: {archetype.Components.First(x => x.DataType == typeof(ComponentA)).IsArray}");

            // Destroy entity a - entity c should be swapped into a's position
            entityManager.DestroyEntity(a);

            // After destroying a, c should still have value 3
            Assert.AreEqual(2, entityManager.GetComponent<ComponentA>(b).a, "Entity b should still have value 2");
            Assert.AreEqual(3, entityManager.GetComponent<ComponentA>(c).a, "Entity c should still have value 3 after swap");
        }
    }
}

