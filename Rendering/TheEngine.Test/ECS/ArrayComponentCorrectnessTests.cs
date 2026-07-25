using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using TheEngine.ECS;
using TheEngine.Managers;

namespace TheEngine.Test.ECS
{
    /// <summary>
    /// Correctness tests for the array component system, covering gaps not addressed by
    /// EntityDataMangerTests: ForEachArray iteration, archetype migration, mixed archetypes,
    /// multiple array component types, and ComponentArrayDataAccess edge cases.
    /// </summary>
    public class ArrayComponentCorrectnessTests
    {
        [ArrayComponent]
        private struct ArrA : IComponentData
        {
            public int value;
        }

        [ArrayComponent]
        private struct ArrB : IComponentData
        {
            public float value;
        }

        private struct RegA : IComponentData
        {
            public int value;
        }

        private struct RegB : IComponentData
        {
            public int value;
        }

        private IEntityManager em = null!;

        // archetypes
        private Archetype onlyArrA = null!;
        private Archetype onlyArrB = null!;
        private Archetype arrAAndArrB = null!;
        private Archetype arrAAndRegA = null!;
        private Archetype onlyRegA = null!;

        [SetUp]
        public void Setup()
        {
            em = new EntityManager(new StatsManager(), null!);
            onlyArrA      = em.NewArchetype().WithComponentData<ArrA>();
            onlyArrB      = em.NewArchetype().WithComponentData<ArrB>();
            arrAAndArrB   = em.NewArchetype().WithComponentData<ArrA>().WithComponentData<ArrB>();
            arrAAndRegA   = em.NewArchetype().WithComponentData<ArrA>().WithComponentData<RegA>();
            onlyRegA      = em.NewArchetype().WithComponentData<RegA>();
        }

        // -------------------------------------------------------------------------
        // ForEachArray – basic iteration
        // -------------------------------------------------------------------------

        [Test]
        public void ForEachArray_VisitsAllEntitiesAndSpans()
        {
            var e1 = em.CreateEntity(onlyArrA);
            var e2 = em.CreateEntity(onlyArrA);
            em.AddArrayComponent(e1, new ArrA { value = 1 });
            em.AddArrayComponent(e1, new ArrA { value = 2 });
            em.AddArrayComponent(e2, new ArrA { value = 10 });

            int sum = 0;
            int entityCount = 0;
            onlyArrA.ForEachArray<ArrA>((itr, thread, start, end, arrAs) =>
            {
                for (int i = start; i < end; i++)
                {
                    entityCount++;
                    var span = arrAs[i];
                    foreach (ref var c in span)
                        sum += c.value;
                }
            });

            Assert.AreEqual(2, entityCount);
            Assert.AreEqual(1 + 2 + 10, sum);
        }

        [Test]
        public void ForEachArray_EntityWithZeroComponents_ProducesEmptySpan()
        {
            var e1 = em.CreateEntity(onlyArrA);
            var e2 = em.CreateEntity(onlyArrA);
            em.AddArrayComponent(e1, new ArrA { value = 5 });
            // e2 intentionally has no array components

            int emptySpanCount = 0;
            int sumValues = 0;
            onlyArrA.ForEachArray<ArrA>((itr, thread, start, end, arrAs) =>
            {
                for (int i = start; i < end; i++)
                {
                    var span = arrAs[i];
                    if (span.Length == 0)
                        emptySpanCount++;
                    foreach (ref var c in span)
                        sumValues += c.value;
                }
            });

            Assert.AreEqual(1, emptySpanCount, "exactly one entity has no array components");
            Assert.AreEqual(5, sumValues);
        }

        [Test]
        public void ForEachArray_CanMutateComponentsThroughSpan()
        {
            var e = em.CreateEntity(onlyArrA);
            em.AddArrayComponent(e, new ArrA { value = 10 });
            em.AddArrayComponent(e, new ArrA { value = 20 });

            // Double each value via ForEachArray
            onlyArrA.ForEachArray<ArrA>((itr, thread, start, end, arrAs) =>
            {
                for (int i = start; i < end; i++)
                {
                    var span = arrAs[i];
                    for (int j = 0; j < span.Length; j++)
                        span[j].value *= 2;
                }
            });

            var components = em.GetArrayComponents<ArrA>(e);
            Assert.AreEqual(20, components[0].value);
            Assert.AreEqual(40, components[1].value);
        }

        // -------------------------------------------------------------------------
        // ForEachArray – mixed (array + regular) archetype
        // -------------------------------------------------------------------------

        [Test]
        public void ForEachArray_WithMixedArchetype_AccessesBothComponentTypes()
        {
            var e1 = em.CreateEntity(arrAAndRegA);
            var e2 = em.CreateEntity(arrAAndRegA);

            em.GetComponent<RegA>(e1).value = 100;
            em.GetComponent<RegA>(e2).value = 200;

            em.AddArrayComponent(e1, new ArrA { value = 1 });
            em.AddArrayComponent(e1, new ArrA { value = 2 });
            em.AddArrayComponent(e2, new ArrA { value = 3 });

            // Sum: each array value multiplied by the entity's RegA value
            int sum = 0;
            arrAAndRegA.ForEachArray<ArrA, RegA>((itr, thread, start, end, arrAs, regAs) =>
            {
                for (int i = start; i < end; i++)
                {
                    var span = arrAs[i];
                    int multiplier = regAs[i].value;
                    foreach (ref var c in span)
                        sum += c.value * multiplier;
                }
            });

            // e1: (1 + 2) * 100 = 300; e2: 3 * 200 = 600
            Assert.AreEqual(900, sum);
        }

        [Test]
        public void ForEachArray_WithMixedArchetype_CanMutateRegularComponent()
        {
            var e1 = em.CreateEntity(arrAAndRegA);
            var e2 = em.CreateEntity(arrAAndRegA);

            em.AddArrayComponent(e1, new ArrA { value = 2 });
            em.AddArrayComponent(e1, new ArrA { value = 3 });
            em.AddArrayComponent(e2, new ArrA { value = 7 });

            // Set RegA to the sum of that entity's array component values
            arrAAndRegA.ForEachArray<ArrA, RegA>((itr, thread, start, end, arrAs, regAs) =>
            {
                for (int i = start; i < end; i++)
                {
                    var span = arrAs[i];
                    int total = 0;
                    foreach (ref var c in span)
                        total += c.value;
                    regAs[i].value = total;
                }
            });

            Assert.AreEqual(5, em.GetComponent<RegA>(e1).value);
            Assert.AreEqual(7, em.GetComponent<RegA>(e2).value);
        }

        // -------------------------------------------------------------------------
        // ParallelForEachArray
        // -------------------------------------------------------------------------

        [Test]
        public void ParallelForEachArray_IteratesAllEntities()
        {
            const int entityCount = 500;
            const int componentsPerEntity = 4;

            for (int i = 0; i < entityCount; i++)
            {
                var e = em.CreateEntity(onlyArrA);
                for (int j = 0; j < componentsPerEntity; j++)
                    em.AddArrayComponent(e, new ArrA { value = 1 });
            }

            long total = 0;
            onlyArrA.ParallelForEachArray<ArrA>((itr, thread, start, end, arrAs) =>
            {
                long localSum = 0;
                for (int i = start; i < end; i++)
                {
                    var span = arrAs[i];
                    foreach (ref var c in span)
                        localSum += c.value;
                }
                Interlocked.Add(ref total, localSum);
            });

            Assert.AreEqual((long)entityCount * componentsPerEntity, total);
        }

        [Test]
        public void ParallelForEachArray_MixedArchetype_CorrectResults()
        {
            const int entityCount = 200;

            for (int i = 0; i < entityCount; i++)
            {
                var e = em.CreateEntity(arrAAndRegA);
                em.GetComponent<RegA>(e).value = i;
                em.AddArrayComponent(e, new ArrA { value = i });
                em.AddArrayComponent(e, new ArrA { value = i });
            }

            // For each entity: sum array values (both equal to i), store in RegA
            arrAAndRegA.ParallelForEachArray<ArrA, RegA>((itr, thread, start, end, arrAs, regAs) =>
            {
                for (int i = start; i < end; i++)
                {
                    var span = arrAs[i];
                    int s = 0;
                    foreach (ref var c in span) s += c.value;
                    regAs[i].value = s;
                }
            });

            // Verify via sequential read: every entity should have RegA = 2 * original_i
            // We can't easily look up entity by i here, so use ForEach to verify all are even
            bool allCorrect = true;
            arrAAndRegA.ForEachArray<ArrA, RegA>((itr, thread, start, end, arrAs, regAs) =>
            {
                for (int i = start; i < end; i++)
                {
                    var span = arrAs[i];
                    int expected = 0;
                    foreach (ref var c in span) expected += c.value;
                    if (regAs[i].value != expected)
                        allCorrect = false;
                }
            });

            Assert.IsTrue(allCorrect);
        }

        // -------------------------------------------------------------------------
        // Archetype migration preserves array components
        // -------------------------------------------------------------------------

        [Test]
        public void ArchetypeMigration_ArrayComponentsPreserved_WhenAddingRegularComponent()
        {
            // Start with an array-only archetype
            var e = em.CreateEntity(onlyArrA);
            em.AddArrayComponent(e, new ArrA { value = 42 });
            em.AddArrayComponent(e, new ArrA { value = 99 });

            // Migrate to a richer archetype by adding a regular component
            em.AddComponent(e, new RegA { value = 7 });

            // Array components must survive the migration
            var components = em.GetArrayComponents<ArrA>(e);
            Assert.AreEqual(2, components.Length);
            Assert.AreEqual(42, components[0].value);
            Assert.AreEqual(99, components[1].value);

            // Regular component should also be set
            Assert.AreEqual(7, em.GetComponent<RegA>(e).value);
        }

        [Test]
        public void ArchetypeMigration_MultipleEntities_AllArrayComponentsPreserved()
        {
            var entities = new Entity[10];
            for (int i = 0; i < entities.Length; i++)
            {
                entities[i] = em.CreateEntity(onlyArrA);
                em.AddArrayComponent(entities[i], new ArrA { value = i * 10 });
                em.AddArrayComponent(entities[i], new ArrA { value = i * 10 + 1 });
            }

            // Migrate all to mixed archetype
            for (int i = 0; i < entities.Length; i++)
                em.AddComponent(entities[i], new RegA { value = i });

            for (int i = 0; i < entities.Length; i++)
            {
                var components = em.GetArrayComponents<ArrA>(entities[i]);
                Assert.AreEqual(2, components.Length, $"entity {i} should still have 2 array components");
                Assert.AreEqual(i * 10,     components[0].value, $"entity {i} component[0]");
                Assert.AreEqual(i * 10 + 1, components[1].value, $"entity {i} component[1]");
            }
        }

        [Test]
        public void AddArrayComponent_MigratesArchetype_WhenComponentNotInArchetype()
        {
            var e = em.CreateEntity(onlyRegA);
            em.GetComponent<RegA>(e).value = 5;

            // ArrA is not part of onlyRegA - this should migrate the entity, like AddComponent does
            em.AddArrayComponent(e, new ArrA { value = 1 });
            em.AddArrayComponent(e, new ArrA { value = 2 });

            var components = em.GetArrayComponents<ArrA>(e);
            Assert.AreEqual(2, components.Length);
            Assert.AreEqual(1, components[0].value);
            Assert.AreEqual(2, components[1].value);

            // the regular component must survive the migration
            Assert.AreEqual(5, em.GetComponent<RegA>(e).value);
        }

        // -------------------------------------------------------------------------
        // Multiple array component types on the same archetype
        // -------------------------------------------------------------------------

        [Test]
        public void MultipleArrayTypes_StoredAndAccessedIndependently()
        {
            var e = em.CreateEntity(arrAAndArrB);
            em.AddArrayComponent(e, new ArrA { value = 1 });
            em.AddArrayComponent(e, new ArrA { value = 2 });
            em.AddArrayComponent(e, new ArrB { value = 10.0f });

            var arrAs = em.GetArrayComponents<ArrA>(e);
            var arrBs = em.GetArrayComponents<ArrB>(e);

            Assert.AreEqual(2, arrAs.Length);
            Assert.AreEqual(1, arrAs[0].value);
            Assert.AreEqual(2, arrAs[1].value);

            Assert.AreEqual(1, arrBs.Length);
            Assert.AreEqual(10.0f, arrBs[0].value, 0.001f);
        }

        [Test]
        public void MultipleArrayTypes_RemovingFromOneDoesNotAffectOther()
        {
            var e = em.CreateEntity(arrAAndArrB);
            em.AddArrayComponent(e, new ArrA { value = 1 });
            em.AddArrayComponent(e, new ArrA { value = 2 });
            em.AddArrayComponent(e, new ArrB { value = 3.0f });
            em.AddArrayComponent(e, new ArrB { value = 4.0f });

            em.RemoveArrayComponent<ArrA>(e, 0);

            var arrAs = em.GetArrayComponents<ArrA>(e);
            var arrBs = em.GetArrayComponents<ArrB>(e);

            Assert.AreEqual(1, arrAs.Length);
            Assert.AreEqual(2, arrAs[0].value);

            Assert.AreEqual(2, arrBs.Length);
            Assert.AreEqual(3.0f, arrBs[0].value, 0.001f);
            Assert.AreEqual(4.0f, arrBs[1].value, 0.001f);
        }

        // -------------------------------------------------------------------------
        // Entity reuse after destroy clears array state
        // -------------------------------------------------------------------------

        [Test]
        public void NewEntityAfterDestroy_HasEmptyArrayComponents()
        {
            var e1 = em.CreateEntity(onlyArrA);
            em.AddArrayComponent(e1, new ArrA { value = 999 });
            em.AddArrayComponent(e1, new ArrA { value = 888 });
            em.DestroyEntity(e1);

            // Create a new entity (may or may not reuse the same slot)
            var e2 = em.CreateEntity(onlyArrA);
            var components = em.GetArrayComponents<ArrA>(e2);
            Assert.AreEqual(0, components.Length, "freshly created entity should have no array components");
        }

        [Test]
        public void ReuseEntitySlot_ArrayComponentsStartEmpty()
        {
            // Fill and drain several times to exercise slot reuse
            for (int round = 0; round < 3; round++)
            {
                var e = em.CreateEntity(onlyArrA);
                Assert.AreEqual(0, em.GetArrayComponents<ArrA>(e).Length,
                    $"round {round}: entity should start with 0 array components");

                em.AddArrayComponent(e, new ArrA { value = round });
                Assert.AreEqual(1, em.GetArrayComponents<ArrA>(e).Length);

                em.DestroyEntity(e);
            }
        }

        // -------------------------------------------------------------------------
        // ComponentArrayDataAccess API
        // -------------------------------------------------------------------------

        [Test]
        public void ComponentArrayDataAccess_Has_ReturnsFalseForEntityNotInArchetype()
        {
            var eA = em.CreateEntity(onlyArrA);
            var eOther = em.CreateEntity(onlyRegA);  // different archetype

            em.AddArrayComponent(eA, new ArrA { value = 1 });
            var access = em.GetArrayDataAccessByEntity<ArrA>(eA);

            Assert.IsTrue(access.Has(eA));
            Assert.IsFalse(access.Has(eOther));
        }

        [Test]
        public void ComponentArrayDataAccess_GetCountAndGetComponents_SafeForEntityNotInChunk()
        {
            var eA = em.CreateEntity(onlyArrA);
            var eOther = em.CreateEntity(onlyRegA); // lives in a different chunk
            em.AddArrayComponent(eA, new ArrA { value = 1 });

            var access = em.GetArrayDataAccessByEntity<ArrA>(eA);

            Assert.AreEqual(1, access.GetCount(eA));
            Assert.AreEqual(0, access.GetCount(eOther));
            Assert.AreEqual(0, access.GetComponents(eOther).Length);
        }

        [Test]
        public void ComponentArrayDataAccess_Indexer_ThrowsOnOutOfRange()
        {
            var e = em.CreateEntity(onlyArrA);
            em.AddArrayComponent(e, new ArrA { value = 5 });

            var access = em.GetArrayDataAccessByEntity<ArrA>(e);

            // Index 0 is valid
            Assert.DoesNotThrow(() => { _ = access[e, 0].value; });

            // Index 1 is out of range
            Assert.Throws<IndexOutOfRangeException>(() => { _ = access[e, 1].value; });
        }

        [Test]
        public void ComponentArrayDataAccess_EntityIndexer_MatchesGetArrayComponents()
        {
            var e1 = em.CreateEntity(onlyArrA);
            var e2 = em.CreateEntity(onlyArrA);
            em.AddArrayComponent(e1, new ArrA { value = 10 });
            em.AddArrayComponent(e1, new ArrA { value = 20 });
            em.AddArrayComponent(e2, new ArrA { value = 30 });

            var access = em.GetArrayDataAccessByEntity<ArrA>(e1);

            Assert.AreEqual(10, access[e1, 0].value);
            Assert.AreEqual(20, access[e1, 1].value);
            Assert.AreEqual(30, access[e2, 0].value);

            // Should match GetArrayComponents
            var span1 = em.GetArrayComponents<ArrA>(e1);
            Assert.AreEqual(span1[0].value, access[e1, 0].value);
            Assert.AreEqual(span1[1].value, access[e1, 1].value);
        }

        // -------------------------------------------------------------------------
        // ForEachArray correctness with many differently-sized entities
        // -------------------------------------------------------------------------

        [Test]
        public void ForEachArray_EntitiesWithDifferentCounts_SumIsCorrect()
        {
            // Create entities with 0..4 components and track expected sum
            long expectedSum = 0;
            for (int i = 0; i < 20; i++)
            {
                var e = em.CreateEntity(onlyArrA);
                int count = i % 5; // 0, 1, 2, 3, 4, 0, 1, ...
                for (int j = 0; j < count; j++)
                {
                    em.AddArrayComponent(e, new ArrA { value = j + 1 });
                    expectedSum += j + 1;
                }
            }

            long actualSum = 0;
            onlyArrA.ForEachArray<ArrA>((itr, thread, start, end, arrAs) =>
            {
                for (int i = start; i < end; i++)
                {
                    var span = arrAs[i];
                    foreach (ref var c in span)
                        actualSum += c.value;
                }
            });

            Assert.AreEqual(expectedSum, actualSum);
        }

        [Test]
        public void ForEachArray_AfterSomeEntitiesDestroyed_SumIsCorrect()
        {
            var entities = new List<Entity>();
            long expectedSum = 0;

            for (int i = 0; i < 10; i++)
            {
                var e = em.CreateEntity(onlyArrA);
                entities.Add(e);
                em.AddArrayComponent(e, new ArrA { value = i + 1 });
                expectedSum += i + 1;
            }

            // Destroy every other entity
            for (int i = 0; i < entities.Count; i += 2)
            {
                expectedSum -= (i + 1);
                em.DestroyEntity(entities[i]);
            }

            long actualSum = 0;
            onlyArrA.ForEachArray<ArrA>((itr, thread, start, end, arrAs) =>
            {
                for (int i = start; i < end; i++)
                {
                    var span = arrAs[i];
                    foreach (ref var c in span)
                        actualSum += c.value;
                }
            });

            Assert.AreEqual(expectedSum, actualSum);
        }

        // -------------------------------------------------------------------------
        // Memory reuse (buddy allocator releases and recycles blocks)
        // -------------------------------------------------------------------------

        [Test]
        public void DestroyAndRecreate_DoesNotGrowArrayMemory()
        {
            var stats = new StatsManager();
            var manager = new EntityManager(stats, null!);
            var archetype = manager.NewArchetype().WithComponentData<ArrA>();

            void Cycle(int value)
            {
                var e = manager.CreateEntity(archetype);
                // 5 elements walks the entity's block through size classes 2 -> 4 -> 8
                for (int j = 0; j < 5; j++)
                    manager.AddArrayComponent(e, new ArrA { value = value + j });
                var components = manager.GetArrayComponents<ArrA>(e);
                Assert.AreEqual(5, components.Length);
                for (int j = 0; j < 5; j++)
                    Assert.AreEqual(value + j, components[j].value);
                manager.DestroyEntity(e);
            }

            // warm up, the first rounds are allowed to grow the backing buffers
            for (int round = 0; round < 10; round++)
                Cycle(round);

            var bytesAfterWarmup = stats.EntitiesUnmanagedBytes;

            // destroyed blocks must be merged and reused, so memory must not grow anymore
            for (int round = 0; round < 1000; round++)
                Cycle(round);

            Assert.AreEqual(bytesAfterWarmup, stats.EntitiesUnmanagedBytes,
                "array component memory should be recycled across create/destroy cycles");
        }

        // -------------------------------------------------------------------------
        // Capacity growth (triggers backing buffer doubling)
        // -------------------------------------------------------------------------

        [Test]
        public void AddManyComponents_ExceedingInitialCapacity_DataRemainsCorrect()
        {
            // Initial capacity is 16 — push well past it
            const int count = 100;
            var e = em.CreateEntity(onlyArrA);
            for (int i = 0; i < count; i++)
                em.AddArrayComponent(e, new ArrA { value = i });

            var components = em.GetArrayComponents<ArrA>(e);
            Assert.AreEqual(count, components.Length);
            for (int i = 0; i < count; i++)
                Assert.AreEqual(i, components[i].value, $"index {i}");
        }

        [Test]
        public void AddManyComponents_AcrossManyEntities_ExceedingInitialCapacity()
        {
            const int entityCount = 20;
            const int componentsEach = 20; // 20*20 = 400 > initial capacity of 16

            var entities = new Entity[entityCount];
            for (int i = 0; i < entityCount; i++)
            {
                entities[i] = em.CreateEntity(onlyArrA);
                for (int j = 0; j < componentsEach; j++)
                    em.AddArrayComponent(entities[i], new ArrA { value = i * 100 + j });
            }

            for (int i = 0; i < entityCount; i++)
            {
                var components = em.GetArrayComponents<ArrA>(entities[i]);
                Assert.AreEqual(componentsEach, components.Length, $"entity {i}");
                for (int j = 0; j < componentsEach; j++)
                    Assert.AreEqual(i * 100 + j, components[j].value, $"entity {i} component {j}");
            }
        }
    }
}
