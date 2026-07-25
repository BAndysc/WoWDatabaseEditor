using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using TheEngine.ECS;
using TheEngine.Managers;

namespace TheEngine.Test.ECS
{
    /// <summary>
    /// Performance benchmarks for the array component system.
    ///
    /// These tests print timing to Console and assert correctness of results.
    /// They do NOT assert that one approach is always faster than another — that
    /// would be fragile on CI — but they do print meaningful comparisons.
    ///
    /// Each test runs a warmup pass before measuring to reduce JIT noise.
    /// </summary>
    public class ArrayComponentPerformanceTests
    {
        [ArrayComponent]
        private struct Position : IComponentData
        {
            public float x, y, z;
        }

        [ArrayComponent]
        private struct Velocity : IComponentData
        {
            public float vx, vy, vz;
        }

        private struct Tag : IComponentData
        {
            public int id;
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static (IEntityManager em, Archetype archetype) BuildWithArrayComponents(
            int entityCount, int componentsPerEntity)
        {
            var em = new EntityManager(new StatsManager(), null!);
            var archetype = em.NewArchetype().WithComponentData<Position>();

            for (int i = 0; i < entityCount; i++)
            {
                var e = em.CreateEntity(archetype);
                for (int j = 0; j < componentsPerEntity; j++)
                    em.AddArrayComponent(e, new Position { x = i, y = j, z = 0 });
            }

            return (em, archetype);
        }

        // -----------------------------------------------------------------------
        // 1. ForEachArray iteration throughput vs plain C# array
        // -----------------------------------------------------------------------

        [Test]
        public void ForEachArray_Throughput_vs_PlainArray()
        {
            const int entityCount = 100_000;
            const int componentsPerEntity = 4;
            const int totalComponents = entityCount * componentsPerEntity;

            var (em, archetype) = BuildWithArrayComponents(entityCount, componentsPerEntity);

            // Reference: plain struct array
            var plain = new Position[totalComponents];
            for (int i = 0; i < plain.Length; i++)
                plain[i] = new Position { x = i / componentsPerEntity, y = i % componentsPerEntity, z = 0 };

            // --- Warmup ---
            float ecsWarmup = 0;
            archetype.ForEachArray<Position>((itr, thread, start, end, positions) =>
            {
                for (int i = start; i < end; i++)
                    foreach (ref var p in positions[i])
                        ecsWarmup += p.x;
            });
            float plainWarmup = 0;
            foreach (ref var p in plain.AsSpan())
                plainWarmup += p.x;

            // --- Measure ECS ---
            var sw = Stopwatch.StartNew();
            float ecsSum = 0;
            archetype.ForEachArray<Position>((itr, thread, start, end, positions) =>
            {
                for (int i = start; i < end; i++)
                {
                    var span = positions[i];
                    for (int j = 0; j < span.Length; j++)
                        ecsSum += span[j].x;
                }
            });
            var ecsTime = sw.Elapsed.TotalMilliseconds;

            // --- Measure plain ---
            sw.Restart();
            float plainSum = 0;
            for (int i = 0; i < plain.Length; i++)
                plainSum += plain[i].x;
            var plainTime = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"ForEachArray ({totalComponents:N0} components): {ecsTime:F2} ms");
            Console.WriteLine($"Plain array  ({totalComponents:N0} elements):   {plainTime:F2} ms");
            Console.WriteLine($"Ratio ECS/plain: {ecsTime / plainTime:F2}x");

            // Correctness: sums must match (both iterate x = entity index)
            Assert.AreEqual(plainSum, ecsSum, 1.0f, "sums should match");

            // Soft bound: ECS iteration should not be more than 20x slower than a raw array scan
            // (the indirection through ComponentArrayIndex is the overhead vs plain).
            // This is generous — in practice the ratio should be 1-3x.
            Assert.Less(ecsTime, plainTime * 20,
                "ForEachArray should not be more than 20x slower than a plain array scan");
        }

        // -----------------------------------------------------------------------
        // 2. ParallelForEachArray vs sequential ForEachArray
        // -----------------------------------------------------------------------

        [Test]
        public void ParallelForEachArray_vs_Sequential()
        {
            // Use enough entities for parallelism to pay off
            const int entityCount = 200_000;
            const int componentsPerEntity = 4;

            var (em, archetype) = BuildWithArrayComponents(entityCount, componentsPerEntity);

            // --- Warmup both ---
            float w = 0;
            archetype.ForEachArray<Position>((itr, thread, start, end, positions) =>
            {
                for (int i = start; i < end; i++)
                    foreach (ref var p in positions[i]) w += p.x;
            });
            archetype.ParallelForEachArray<Position>((itr, thread, start, end, positions) =>
            {
                for (int i = start; i < end; i++)
                    foreach (ref var p in positions[i]) Interlocked.Exchange(ref w, w + p.x);
            });

            // --- Sequential ---
            var sw = Stopwatch.StartNew();
            long seqSum = 0;
            archetype.ForEachArray<Position>((itr, thread, start, end, positions) =>
            {
                for (int i = start; i < end; i++)
                {
                    var span = positions[i];
                    for (int j = 0; j < span.Length; j++)
                        seqSum += (long)span[j].x;
                }
            });
            var seqTime = sw.Elapsed.TotalMilliseconds;

            // --- Parallel: each thread accumulates locally to avoid Interlocked on the hot path ---
            sw.Restart();
            long parallelSum = 0;
            archetype.ParallelForEachArray<Position>((itr, thread, start, end, positions) =>
            {
                long localSum = 0;
                for (int i = start; i < end; i++)
                {
                    var span = positions[i];
                    for (int j = 0; j < span.Length; j++)
                        localSum += (long)span[j].x;
                }
                Interlocked.Add(ref parallelSum, localSum);
            });
            var parallelTime = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"Sequential ForEachArray:  {seqTime:F2} ms  (sum={seqSum})");
            Console.WriteLine($"Parallel   ForEachArray:  {parallelTime:F2} ms  (sum={parallelSum})");
            Console.WriteLine($"Speedup: {seqTime / parallelTime:F2}x");

            // Correctness
            Assert.AreEqual(seqSum, parallelSum, $"parallel sum {parallelSum} != sequential {seqSum}");

            // If the machine has more than 1 logical core, parallel should finish in reasonable time.
            // We only assert it's not *slower* than 4x sequential (very conservative — covers single-core CI).
            if (Environment.ProcessorCount > 1)
                Assert.Less(parallelTime, seqTime * 4,
                    "parallel should not be dramatically slower than sequential");
        }

        // -----------------------------------------------------------------------
        // 3. Batch add (spawn scenario): N entities × M components each
        // -----------------------------------------------------------------------

        [Test]
        public void BatchAdd_NEntities_MComponentsEach()
        {
            const int entityCount = 50_000;
            const int componentsPerEntity = 5;

            var em = new EntityManager(new StatsManager(), null!);
            var archetype = em.NewArchetype().WithComponentData<Position>();

            // Warmup
            {
                var warmupEm = new EntityManager(new StatsManager(), null!);
                var warmupArch = warmupEm.NewArchetype().WithComponentData<Position>();
                for (int i = 0; i < 1000; i++)
                {
                    var e = warmupEm.CreateEntity(warmupArch);
                    for (int j = 0; j < componentsPerEntity; j++)
                        warmupEm.AddArrayComponent(e, new Position { x = i });
                }
                warmupEm.Dispose();
            }

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < entityCount; i++)
            {
                var e = em.CreateEntity(archetype);
                for (int j = 0; j < componentsPerEntity; j++)
                    em.AddArrayComponent(e, new Position { x = i, y = j });
            }
            var addTime = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"Batch add: {entityCount:N0} entities × {componentsPerEntity} components " +
                              $"= {entityCount * componentsPerEntity:N0} total adds in {addTime:F2} ms");
            Console.WriteLine($"Per-add: {addTime * 1_000_000 / (entityCount * componentsPerEntity):F0} ns");

            // Correctness: verify a sample
            int verifyCount = 0;
            archetype.ForEachArray<Position>((itr, thread, start, end, positions) =>
            {
                for (int i = start; i < end; i++)
                    verifyCount += positions[i].Length;
            });
            Assert.AreEqual(entityCount * componentsPerEntity, verifyCount,
                "total component count after batch add");

            // Soft perf bound: adding 250k components should complete in under 5 seconds on any
            // reasonable machine (extremely conservative).
            Assert.Less(addTime, 5000, "batch add should complete within 5 seconds");

            em.Dispose();
        }

        // -----------------------------------------------------------------------
        // 4. Iteration with variable component counts per entity
        //    (tests that variable spans don't hurt cache too badly)
        // -----------------------------------------------------------------------

        [Test]
        public void ForEachArray_VariableCountsPerEntity_vs_FixedCounts()
        {
            const int entityCount = 50_000;
            const int avgComponents = 4;

            // Fixed: all entities have exactly avgComponents
            var emFixed = new EntityManager(new StatsManager(), null!);
            var archFixed = emFixed.NewArchetype().WithComponentData<Position>();
            for (int i = 0; i < entityCount; i++)
            {
                var e = emFixed.CreateEntity(archFixed);
                for (int j = 0; j < avgComponents; j++)
                    emFixed.AddArrayComponent(e, new Position { x = i });
            }

            // Variable: entities have 1..7 components (avg ≈ 4)
            var emVar = new EntityManager(new StatsManager(), null!);
            var archVar = emVar.NewArchetype().WithComponentData<Position>();
            int totalVar = 0;
            for (int i = 0; i < entityCount; i++)
            {
                var e = emVar.CreateEntity(archVar);
                int count = (i % 7) + 1;
                totalVar += count;
                for (int j = 0; j < count; j++)
                    emVar.AddArrayComponent(e, new Position { x = i });
            }

            // Warmup
            float w = 0;
            archFixed.ForEachArray<Position>((itr, t, s, end, pos) =>
                { for (int i = s; i < end; i++) foreach (ref var p in pos[i]) w += p.x; });
            archVar.ForEachArray<Position>((itr, t, s, end, pos) =>
                { for (int i = s; i < end; i++) foreach (ref var p in pos[i]) w += p.x; });

            // Fixed
            var sw = Stopwatch.StartNew();
            float fixedSum = 0;
            archFixed.ForEachArray<Position>((itr, thread, start, end, positions) =>
            {
                for (int i = start; i < end; i++)
                {
                    var span = positions[i];
                    for (int j = 0; j < span.Length; j++)
                        fixedSum += span[j].x;
                }
            });
            var fixedTime = sw.Elapsed.TotalMilliseconds;

            // Variable
            sw.Restart();
            float varSum = 0;
            archVar.ForEachArray<Position>((itr, thread, start, end, positions) =>
            {
                for (int i = start; i < end; i++)
                {
                    var span = positions[i];
                    for (int j = 0; j < span.Length; j++)
                        varSum += span[j].x;
                }
            });
            var varTime = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"Fixed-count ({entityCount * avgComponents:N0} components):   {fixedTime:F2} ms");
            Console.WriteLine($"Variable-count ({totalVar:N0} components): {varTime:F2} ms");

            // Normalise by component count for fair comparison
            double fixedNs = fixedTime * 1e6 / (entityCount * avgComponents);
            double varNs   = varTime   * 1e6 / totalVar;
            Console.WriteLine($"Per-component: fixed={fixedNs:F1} ns  variable={varNs:F1} ns");

            // Correctness: variable sum should be a reasonable positive number
            Assert.Greater(varSum, 0);
            Assert.Greater(fixedSum, 0);

            emFixed.Dispose();
            emVar.Dispose();
        }

        // -----------------------------------------------------------------------
        // 5. ForEachArray with mixed archetype (array + regular) overhead
        // -----------------------------------------------------------------------

        [Test]
        public void ForEachArray_Mixed_vs_ArrayOnly_Overhead()
        {
            const int entityCount = 100_000;
            const int componentsPerEntity = 3;

            // Array-only archetype
            var emA = new EntityManager(new StatsManager(), null!);
            var archA = emA.NewArchetype().WithComponentData<Position>();
            for (int i = 0; i < entityCount; i++)
            {
                var e = emA.CreateEntity(archA);
                for (int j = 0; j < componentsPerEntity; j++)
                    emA.AddArrayComponent(e, new Position { x = i });
            }

            // Mixed archetype (array + one regular tag)
            var emM = new EntityManager(new StatsManager(), null!);
            var archM = emM.NewArchetype().WithComponentData<Position>().WithComponentData<Tag>();
            for (int i = 0; i < entityCount; i++)
            {
                var e = emM.CreateEntity(archM);
                emM.GetComponent<Tag>(e).id = i;
                for (int j = 0; j < componentsPerEntity; j++)
                    emM.AddArrayComponent(e, new Position { x = i });
            }

            // Warmup
            float w = 0;
            archA.ForEachArray<Position>((itr, t, s, end, pos)
                => { for (int i = s; i < end; i++) foreach (ref var p in pos[i]) w += p.x; });
            archM.ForEachArray<Position, Tag>((itr, t, s, end, pos, tags)
                => { for (int i = s; i < end; i++) { w += tags[i].id; foreach (ref var p in pos[i]) w += p.x; } });

            // Array-only
            var sw = Stopwatch.StartNew();
            float sumA = 0;
            archA.ForEachArray<Position>((itr, thread, start, end, positions) =>
            {
                for (int i = start; i < end; i++)
                {
                    var span = positions[i];
                    for (int j = 0; j < span.Length; j++)
                        sumA += span[j].x;
                }
            });
            var timeA = sw.Elapsed.TotalMilliseconds;

            // Mixed
            sw.Restart();
            float sumM = 0;
            archM.ForEachArray<Position, Tag>((itr, thread, start, end, positions, tags) =>
            {
                for (int i = start; i < end; i++)
                {
                    var span = positions[i];
                    for (int j = 0; j < span.Length; j++)
                        sumM += span[j].x;
                    _ = tags[i].id; // access regular component to model realistic workload
                }
            });
            var timeM = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"Array-only archetype:   {timeA:F2} ms");
            Console.WriteLine($"Mixed archetype:        {timeM:F2} ms");
            Console.WriteLine($"Overhead ratio: {timeM / timeA:F2}x");

            // Sums over x = entity index should be equal
            Assert.AreEqual(sumA, sumM, 1.0f, "both archetypes iterate the same x values");

            emA.Dispose();
            emM.Dispose();
        }
    }
}
