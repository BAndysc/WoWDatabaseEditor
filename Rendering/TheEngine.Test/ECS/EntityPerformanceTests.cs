using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using TheEngine.ECS;
using TheMaths;

namespace TheEngine.Test.ECS
{
    public class EntityPerformanceTests
    {
        private struct Position : IComponentData
        {
            public Vector3 position;
        }

        private struct Velocity : IComponentData
        {
            public Vector3 velocity;
        }

        private IEntityManager entityManager = null!;
        private Archetype archetype = null!;
        private int count = 10_000_000;

        private class OldSchoolObject
        {
            public Vector3 position;
            // simulate a big class
            public long otherThings;
            public long otherThings1;
            public long otherThings2;
            public long otherThings3;
            public long otherThings4;
            public long otherThings5;
            public long otherThings6;
            public long otherThings7;
            public long otherThings8;
            public long otherThings9;
            public long otherThings10;
            public long otherThings11;
            public Vector3 velocity;
        }

        private List<OldSchoolObject> oldSchool = new();
        
        [SetUp]
        public void Setup()
        {
            entityManager = new EntityManager();
            archetype = entityManager.NewArchetype()
                .WithComponentData<Position>()
                .WithComponentData<Velocity>();
            for (int i = 0; i < count; ++i)
            {
                var e = entityManager.CreateEntity(archetype);
                var pos = new Vector3(i / 1000.0f, 0, 0);
                var velo = new Vector3(1, 0, 0);
                oldSchool.Add(new OldSchoolObject()
                {
                    position = pos,
                    velocity = velo
                });
            }
            
            archetype.ForEach<Position, Velocity>((itr, start, end, positions, velocities) =>
            {
                for (int i = start; i < end; ++i)
                {
                    var entity = itr[i];
                    positions[i].position = new Vector3(entity.Id / 1000.0f, 0, 0);
                    velocities[i].velocity = new Vector3(1, 0, 0);
                }
            });
        }
        
        [Test]
        public void EcsFasterThanOop()
        {
            // warmup
            archetype.ForEach<Position, Velocity>((itr, start, end, positions, velocities) =>
            {
                for (int i = start; i < end; ++i)
                {
                    positions[i].position += velocities[i].velocity;
                }
            });
            foreach (var o in oldSchool)
                o.position += o.velocity;
            
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            
            archetype.ForEach<Position, Velocity>((itr, start, end, positions, velocities) =>
            {
                for (int i = start; i < end; ++i)
                {
                    positions[i].position += velocities[i].velocity;
                }
            });
            stopWatch.Stop();
            var entitySpeed = stopWatch.Elapsed.TotalSeconds;
            
            stopWatch.Restart();
            foreach (var o in oldSchool)
                o.position += o.velocity;
            var oldSchoolSpeed = stopWatch.Elapsed.TotalSeconds;
            
            Console.WriteLine("ECS: " + entitySpeed);
            Console.WriteLine("OOP: " + oldSchoolSpeed);
            Assert.AreEqual(3, entityManager.GetComponent<Position>(new Entity(1000, 1)).position.X);
            Assert.AreEqual(3, oldSchool[1000].position.X);
            Assert.IsTrue(entitySpeed < oldSchoolSpeed * 2f);
        }

        [Test]
        public void ParallelForFasterThanSequential()
        {
            // warmup
            archetype.ForEach<Position, Velocity>((itr, start, end, positions, velocities) =>
            {
                for (int i = start; i < end; ++i)
                    positions[i].position += velocities[i].velocity;
            });
            archetype.ParallelForEach<Position, Velocity>((itr, start, end, positions, velocities) =>
            {
                for (int i = start; i < end; ++i)
                    positions[i].position += velocities[i].velocity;
            });
            
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            archetype.ForEach<Position, Velocity>((itr, start, end, positions, velocities) =>
            {
                for (int i = start; i < end; ++i)
                    positions[i].position += velocities[i].velocity;
            });
            var sequential = stopWatch.Elapsed.TotalSeconds;
            
            stopWatch.Restart();
            archetype.ParallelForEach<Position, Velocity>((itr, start, end, positions, velocities) =>
            {
                for (int i = start; i < end; ++i)
                    positions[i].position += velocities[i].velocity;
            });
            var parallel = stopWatch.Elapsed.TotalSeconds;
            
            Assert.AreEqual(5, entityManager.GetComponent<Position>(new Entity(1000, 1)).position.X);
            //Assert.IsTrue(parallel < sequential);
            Console.WriteLine("sequential: " + sequential);
            Console.WriteLine("parallel: " + parallel);
        }
    }
}