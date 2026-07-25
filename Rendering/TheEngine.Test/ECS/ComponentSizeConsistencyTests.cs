using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NUnit.Framework;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Managers;

namespace TheEngine.Test.ECS
{
    /// <summary>
    /// The component storage is indexed with managed sizeof(T) strides (ComponentDataAccess,
    /// ComponentArrayDataAccess, AddArrayComponent), so ComponentTypeData.SizeBytes - used by the
    /// non-generic paths (UnsafeCopy on archetype migration, RemoveEntity) - must report the managed
    /// size too. Marshal.SizeOf differs for structs with bool fields (bool marshals as 4 bytes),
    /// which used to corrupt array components during archetype migration.
    /// </summary>
    public class ComponentSizeConsistencyTests
    {
        // bool fields make Marshal.SizeOf(typeof(BoolPadded)) != Unsafe.SizeOf<BoolPadded>()
        [ArrayComponent]
        private struct BoolPadded : IComponentData
        {
            public long payload;
            public bool a;
            public bool b;
            public bool c;
        }

        private struct RegA : IComponentData
        {
            public int value;
        }

        [Test]
        public void SizeBytes_IsManagedSize()
        {
            Assert.AreNotEqual(Marshal.SizeOf(typeof(BoolPadded)), Unsafe.SizeOf<BoolPadded>(),
                "test struct must have differing marshaled/managed sizes to be meaningful");
            Assert.AreEqual(Unsafe.SizeOf<MeshRenderer>(), new ComponentTypeData<MeshRenderer>(0).SizeBytes);
            Assert.AreEqual(Unsafe.SizeOf<BoolPadded>(), new ComponentTypeData<BoolPadded>(0).SizeBytes);
        }

        [Test]
        public void ArchetypeMigration_PreservesArrayComponents_WithMarshalSizeMismatch()
        {
            var em = new EntityManager(new StatsManager(), null!);
            var sourceArchetype = em.NewArchetype().WithComponentData<BoolPadded>();
            var targetArchetype = sourceArchetype.WithComponentData<RegA>();

            // fillers occupy the offset-0 blocks in both chunks, so the migrated entity's block
            // lands at a nonzero element offset - a size/stride mismatch corrupts data there
            var sourceFiller = em.CreateEntity(sourceArchetype);
            em.AddArrayComponent(sourceFiller, new BoolPadded { payload = 1 });
            em.AddArrayComponent(sourceFiller, new BoolPadded { payload = 2 });
            var targetFiller = em.CreateEntity(targetArchetype);
            em.AddArrayComponent(targetFiller, new BoolPadded { payload = 3 });
            em.AddArrayComponent(targetFiller, new BoolPadded { payload = 4 });

            var e = em.CreateEntity(sourceArchetype);
            em.AddArrayComponent(e, new BoolPadded { payload = 111, a = true });
            em.AddArrayComponent(e, new BoolPadded { payload = 222, b = true });
            em.AddArrayComponent(e, new BoolPadded { payload = 333, c = true });

            // migrate to the target archetype - UnsafeCopy uses SizeBytes for the copy
            em.AddComponent(e, new RegA { value = 1 });

            var components = em.GetArrayComponents<BoolPadded>(e);
            Assert.AreEqual(3, components.Length);
            Assert.AreEqual(111, components[0].payload);
            Assert.IsTrue(components[0].a);
            Assert.AreEqual(222, components[1].payload);
            Assert.IsTrue(components[1].b);
            Assert.AreEqual(333, components[2].payload);
            Assert.IsTrue(components[2].c);

            var fillerComponents = em.GetArrayComponents<BoolPadded>(targetFiller);
            Assert.AreEqual(2, fillerComponents.Length);
            Assert.AreEqual(3, fillerComponents[0].payload);
            Assert.AreEqual(4, fillerComponents[1].payload);
        }
    }
}
