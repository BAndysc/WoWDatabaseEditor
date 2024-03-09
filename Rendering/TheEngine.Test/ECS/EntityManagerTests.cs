using NUnit.Framework;
using TheEngine.ECS;

namespace TheEngine.Test.ECS
{
    public class EntityManagerTests
    {
        private IEntityManager entityManager = null!;
        private Archetype empty = null!;
        
        [SetUp]
        public void Setup()
        {
            entityManager = new EntityManager();
            empty = entityManager.NewArchetype();
        }
        
        [Test]
        public void CreatedEntitiesExists()
        {
            var a = entityManager.CreateEntity(empty);
            var b = entityManager.CreateEntity(empty);
            Assert.AreNotEqual(a, b);
            Assert.IsTrue(entityManager.Exist(a));
            Assert.IsTrue(entityManager.Exist(b));
        }

        [Test]
        public void RemovedEntitiesDontExists()
        {
            var a = entityManager.CreateEntity(empty);
            var b = entityManager.CreateEntity(empty);
            Assert.AreNotEqual(a, b);
            entityManager.DestroyEntity(a);
            entityManager.DestroyEntity(b);
            Assert.IsFalse(entityManager.Exist(a));
            Assert.IsFalse(entityManager.Exist(b));
        }

        [Test]
        public void CreateAfterDelete()
        {
            var a = entityManager.CreateEntity(empty);
            entityManager.DestroyEntity(a);
            var b = entityManager.CreateEntity(empty);
            Assert.AreNotEqual(a, b);
            Assert.True(entityManager.Exist(b));
            Assert.IsFalse(entityManager.Exist(a));
        }
    }
}