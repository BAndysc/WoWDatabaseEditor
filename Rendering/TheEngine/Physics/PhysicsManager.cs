using System;
using System.Collections.Generic;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheMaths;

namespace TheEngine.Physics
{
    internal struct PhysicsMaterialData
    {
        public float Friction;
        public float Bounciness;
        public bool IsTrigger;
    }

    internal struct BodyExtraData
    {
        public float GravityScale;
        public float LinearDamping;
        public float AngularDamping;
    }

    public sealed partial class PhysicsManager : IPhysicsManager, IDisposable
    {
        private const float FixedDt = 1f / 60f;
        private const int MaxSubstepsPerFrame = 3;

        private sealed class GravityHolder
        {
            public Vector3 Value;
        }

        // Routes Bepu's per-collidable touch events back to entities through PhysicsManager.
        private sealed class ContactEventForwarder : IContactEventHandler
        {
            private readonly PhysicsManager owner;

            public ContactEventForwarder(PhysicsManager owner)
            {
                this.owner = owner;
            }

            public void OnStartedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int workerIndex) where TManifold : unmanaged, IContactManifold<TManifold>
            {
                // HandleManifold visits both sides of the pair; only act once.
                if (eventSource.Packed != pair.A.Packed)
                    return;
                owner.RaiseContact(owner.ContactBegin, pair.A, pair.B);
            }

            public void OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int workerIndex) where TManifold : unmanaged, IContactManifold<TManifold>
            {
                if (eventSource.Packed != pair.A.Packed)
                    return;
                owner.RaiseContact(owner.ContactEnd, pair.A, pair.B);
            }
        }

        private struct PhysicsNarrowPhaseCallbacks : INarrowPhaseCallbacks
        {
            public CollidableProperty<PhysicsMaterialData> Materials;
            public ContactEvents Events;

            public void Initialize(Simulation simulation)
            {
                Materials.Initialize(simulation);
                Events.Initialize(simulation);
            }

            public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin) => true;

            public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;

            public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
            {
                var a = Materials[pair.A];
                var b = Materials[pair.B];
                pairMaterial.FrictionCoefficient = a.Friction * b.Friction;
                var bounciness = MathF.Max(a.Bounciness, b.Bounciness);
                // Bepu v2 has no native restitution coefficient. Approximate bounciness with a
                // softer, less-damped recovery spring and an unclamped recovery velocity, calibrated
                // against BepuPhysics2's BouncinessDemo (rigid: Spring(30,1)+MaxRecovery=2).
                pairMaterial.SpringSettings = new SpringSettings(30f - 24f * bounciness, 1f - bounciness);
                pairMaterial.MaximumRecoveryVelocity = bounciness > 0f ? float.MaxValue : 2f;
                Events.HandleManifold(workerIndex, pair, ref manifold);
                return !(a.IsTrigger || b.IsTrigger);
            }

            public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold) => true;

            public void Dispose()
            {
            }
        }

        private struct PhysicsPoseIntegratorCallbacks : IPoseIntegratorCallbacks
        {
            public GravityHolder Gravity;
            public CollidableProperty<BodyExtraData> Extra;
            private Simulation simulation;

            public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
            public bool AllowSubstepsForUnconstrainedBodies => false;
            public bool IntegrateVelocityForKinematics => false;

            public void Initialize(Simulation simulation)
            {
                this.simulation = simulation;
                Extra.Initialize(simulation);
            }

            public void PrepareForIntegration(float dt)
            {
            }

            public void IntegrateVelocity(System.Numerics.Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, System.Numerics.Vector<int> integrationMask, int workerIndex, System.Numerics.Vector<float> dt, ref BodyVelocityWide velocity)
            {
                var indexToHandle = simulation.Bodies.ActiveSet.IndexToHandle;
                var gravity = Gravity.Value;
                for (int i = 0; i < System.Numerics.Vector<int>.Count; ++i)
                {
                    if (integrationMask[i] == 0)
                        continue;
                    var handle = indexToHandle[bodyIndices[i]];
                    ref var extra = ref Extra[handle];
                    var dtScalar = dt[i];
                    Vector3Wide.ReadSlot(ref velocity.Linear, i, out var linear);
                    Vector3Wide.ReadSlot(ref velocity.Angular, i, out var angular);
                    linear += gravity * extra.GravityScale * dtScalar;
                    linear *= MathF.Max(0f, 1f - extra.LinearDamping * dtScalar);
                    angular *= MathF.Max(0f, 1f - extra.AngularDamping * dtScalar);
                    Vector3Wide.WriteSlot(linear, i, ref velocity.Linear);
                    Vector3Wide.WriteSlot(angular, i, ref velocity.Angular);
                }
            }
        }

        private readonly Engine engine;
        private readonly IEntityManager entityManager;
        private readonly GravityHolder gravityHolder = new() { Value = new Vector3(0, 0, -9.81f) };

        private readonly BufferPool bufferPool;
        private readonly Simulation simulation;
        private readonly ContactEvents contactEvents;
        private readonly ContactEventForwarder contactForwarder;
        private readonly CollidableProperty<PhysicsMaterialData> materials;
        private readonly CollidableProperty<BodyExtraData> bodyExtras;

        private readonly Dictionary<int, Entity> bodyHandleToEntity = new();
        private readonly Dictionary<int, Entity> staticHandleToEntity = new();

        private readonly Archetype boxColliderArchetype;
        private readonly Archetype sphereColliderArchetype;
        private readonly Archetype capsuleColliderArchetype;
        private readonly Archetype meshColliderArchetype;
        private readonly Archetype bodyRefArchetype;

        private readonly List<Entity> pendingNewColliders = new();

        private float accumulator;

        public Vector3 Gravity
        {
            get => gravityHolder.Value;
            set => gravityHolder.Value = value;
        }

        public bool Enabled { get; set; } = true;

        public event Action<Entity, Entity> ContactBegin;
        public event Action<Entity, Entity> ContactEnd;

        internal PhysicsManager(Engine engine)
        {
            this.engine = engine;
            entityManager = engine.EntityManager;

            bufferPool = new BufferPool();
            materials = new CollidableProperty<PhysicsMaterialData>(bufferPool);
            bodyExtras = new CollidableProperty<BodyExtraData>(bufferPool);
            contactEvents = new ContactEvents(null, bufferPool);
            contactForwarder = new ContactEventForwarder(this);

            var narrowPhaseCallbacks = new PhysicsNarrowPhaseCallbacks { Materials = materials, Events = contactEvents };
            var poseIntegratorCallbacks = new PhysicsPoseIntegratorCallbacks { Gravity = gravityHolder, Extra = bodyExtras };
            simulation = Simulation.Create(bufferPool, narrowPhaseCallbacks, poseIntegratorCallbacks, new SolveDescription(8, 1));

            boxColliderArchetype = entityManager.NewArchetype().WithComponentData<BoxCollider>().WithComponentData<LocalToWorld>();
            sphereColliderArchetype = entityManager.NewArchetype().WithComponentData<SphereCollider>().WithComponentData<LocalToWorld>();
            capsuleColliderArchetype = entityManager.NewArchetype().WithComponentData<CapsuleCollider>().WithComponentData<LocalToWorld>();
            meshColliderArchetype = entityManager.NewArchetype().WithComponentData<MeshCollider>().WithComponentData<LocalToWorld>();
            bodyRefArchetype = entityManager.NewArchetype().WithComponentData<PhysicsBodyRef>()
                .WithComponentData<LocalToWorld>().WithComponentData<DirtyPosition>();
        }

        public void Step(float deltaMs)
        {
            if (!Enabled)
                return;

            ReconcileNewColliders();

            new PushKinematicPosesJob { simulation = simulation }.Run(bodyRefArchetype);

            accumulator += deltaMs / 1000f;
            int substeps = 0;
            while (accumulator >= FixedDt && substeps < MaxSubstepsPerFrame)
            {
                simulation.Timestep(FixedDt);
                contactEvents.Flush();
                accumulator -= FixedDt;
                ++substeps;
            }

            new WriteBackDynamicPosesJob(){simulation = simulation}.Run(bodyRefArchetype);
        }

        private void ReconcileNewColliders()
        {
            GatherUncreated<BoxCollider>(boxColliderArchetype);
            foreach (var entity in pendingNewColliders)
                CreateBoxBody(entity);

            GatherUncreated<SphereCollider>(sphereColliderArchetype);
            foreach (var entity in pendingNewColliders)
                CreateSphereBody(entity);

            GatherUncreated<CapsuleCollider>(capsuleColliderArchetype);
            foreach (var entity in pendingNewColliders)
                CreateCapsuleBody(entity);

            GatherUncreated<MeshCollider>(meshColliderArchetype);
            foreach (var entity in pendingNewColliders)
                CreateMeshBody(entity);
        }

        private partial struct GatherUncreatedJob<TCollider> : IJob where TCollider : unmanaged, IComponentData
        {
            public IChunkDataIterator itr;
            public ComponentDataAccess<TCollider> collider;
            public ComponentDataAccess<PhysicsBodyRef>? maybePhysicsBodyRef;

            public List<Entity> pendingNewColliders;

            public void Execute(int start, int end)
            {
                if (maybePhysicsBodyRef.HasValue)
                {
                    return;
                }
                for (int i = start; i < end; ++i)
                {
                    var entity = itr[i];
                    pendingNewColliders.Add(entity);
                }
            }
        }

        private void GatherUncreated<TCollider>(Archetype archetype) where TCollider : unmanaged, IComponentData
        {
            pendingNewColliders.Clear();
            new GatherUncreatedJob<TCollider>(){pendingNewColliders = pendingNewColliders}.Run(archetype);
        }

        private void CreateBoxBody(Entity entity)
        {
            ref var collider = ref entityManager.GetComponent<BoxCollider>(entity);
            CreateConvexBody(entity, new Box(collider.Size.X, collider.Size.Y, collider.Size.Z), collider.Center);
        }

        private void CreateSphereBody(Entity entity)
        {
            ref var collider = ref entityManager.GetComponent<SphereCollider>(entity);
            CreateConvexBody(entity, new Sphere(collider.Radius), collider.Center);
        }

        private void CreateCapsuleBody(Entity entity)
        {
            ref var collider = ref entityManager.GetComponent<CapsuleCollider>(entity);
            CreateConvexBody(entity, new Capsule(collider.Radius, collider.Length), collider.Center);
        }

        private void CreateConvexBody<TShape>(Entity entity, in TShape shape, in Vector3 center) where TShape : unmanaged, IConvexShape
        {
            ref var localToWorld = ref entityManager.GetComponent<LocalToWorld>(entity);
            var rotation = localToWorld.Rotation;
            var pose = new RigidPose(localToWorld.Position + Vector3.Transform(center, rotation), rotation);
            var materialData = ReadMaterial(entity);

            PhysicsBodyRef bodyRef;
            CollidableReference collidableRef;
            if (entityManager.HasComponent<RigidBody>(entity))
            {
                ref var rigidBody = ref entityManager.GetComponent<RigidBody>(entity);
                if (rigidBody.Type == BodyType.Kinematic)
                {
                    var description = BodyDescription.CreateConvexKinematic(pose, simulation.Shapes, shape);
                    var handle = simulation.Bodies.Add(description);
                    bodyExtras.Allocate(handle) = default;
                    materials.Allocate(handle) = materialData;
                    bodyHandleToEntity[handle.Value] = entity;
                    bodyRef = new PhysicsBodyRef { Handle = handle.Value, IsStatic = false, IsKinematic = true, Shape = description.Collidable.Shape };
                    collidableRef = simulation.Bodies[handle].CollidableReference;
                }
                else
                {
                    var mass = rigidBody.Mass > 0 ? rigidBody.Mass : 1f;
                    var description = BodyDescription.CreateConvexDynamic(pose, mass, simulation.Shapes, shape);
                    var handle = simulation.Bodies.Add(description);
                    bodyExtras.Allocate(handle) = new BodyExtraData { GravityScale = rigidBody.GravityScale, LinearDamping = rigidBody.LinearDamping, AngularDamping = rigidBody.AngularDamping };
                    materials.Allocate(handle) = materialData;
                    bodyHandleToEntity[handle.Value] = entity;
                    bodyRef = new PhysicsBodyRef { Handle = handle.Value, IsStatic = false, IsKinematic = false, Shape = description.Collidable.Shape };
                    collidableRef = simulation.Bodies[handle].CollidableReference;
                }
            }
            else
            {
                var shapeIndex = simulation.Shapes.Add(shape);
                var handle = simulation.Statics.Add(new StaticDescription(pose, shapeIndex));
                materials.Allocate(handle) = materialData;
                staticHandleToEntity[handle.Value] = entity;
                bodyRef = new PhysicsBodyRef { Handle = handle.Value, IsStatic = true, IsKinematic = false, Shape = shapeIndex };
                collidableRef = simulation.Statics[handle].CollidableReference;
            }

            entityManager.AddComponent(entity, bodyRef);
            contactEvents.Register(collidableRef, contactForwarder);
        }

        private void CreateMeshBody(Entity entity)
        {
            ref var collider = ref entityManager.GetComponent<MeshCollider>(entity);
            ref var localToWorld = ref entityManager.GetComponent<LocalToWorld>(entity);

            if (entityManager.HasComponent<RigidBody>(entity))
                Console.WriteLine($"[Physics] MeshCollider on entity {entity} has a RigidBody attached; mesh colliders have no inertia tensor and are always treated as static.");

            var mesh = engine.meshManager.GetMeshByHandle(collider.Mesh);
            int triangleCount = 0;
            foreach (var _ in mesh.GetFaces(collider.SubMesh))
                ++triangleCount;

            bufferPool.Take<Triangle>(triangleCount, out var triangles);
            int index = 0;
            foreach (var face in mesh.GetFaces(collider.SubMesh))
            {
                triangles[index++] = new Triangle(face.Item1.XYZ(), face.Item2.XYZ(), face.Item3.XYZ());
            }

            var meshShape = new BepuPhysics.Collidables.Mesh(triangles, localToWorld.Scale, bufferPool);
            var shapeIndex = simulation.Shapes.Add(meshShape);
            var pose = new RigidPose(localToWorld.Position, localToWorld.Rotation);
            var handle = simulation.Statics.Add(new StaticDescription(pose, shapeIndex));
            materials.Allocate(handle) = ReadMaterial(entity);
            staticHandleToEntity[handle.Value] = entity;

            var bodyRef = new PhysicsBodyRef { Handle = handle.Value, IsStatic = true, IsKinematic = false, Shape = shapeIndex };
            entityManager.AddComponent(entity, bodyRef);
            contactEvents.Register(simulation.Statics[handle].CollidableReference, contactForwarder);
        }

        private PhysicsMaterialData ReadMaterial(Entity entity)
        {
            float friction = 1f, bounciness = 0f;
            if (entityManager.HasComponent<PhysicsMaterial>(entity))
            {
                ref var material = ref entityManager.GetComponent<PhysicsMaterial>(entity);
                friction = material.Friction;
                bounciness = material.Bounciness;
            }
            return new PhysicsMaterialData { Friction = friction, Bounciness = bounciness, IsTrigger = entityManager.HasComponent<Trigger>(entity) };
        }

        private partial struct PushKinematicPosesJob : IParallelJob
        {
            private IChunkDataIterator itr;
            private ComponentDataAccess<PhysicsBodyRef> bodyRefs;
            private ComponentDataAccess<LocalToWorld> localToWorlds;

            public Simulation simulation;

            public void Execute(int thread, int start, int end)
            {
                for (int i = start; i < end; ++i)
                {
                    if (bodyRefs[i].IsStatic || !bodyRefs[i].IsKinematic)
                        continue;
                    var bodyReference = simulation.Bodies[new BodyHandle(bodyRefs[i].Handle)];
                    ref var pose = ref bodyReference.Pose;
                    ref var l2w = ref localToWorlds[i];
                    pose = new RigidPose(l2w.Position, l2w.Rotation);
                }
            }
        }

        private partial struct WriteBackDynamicPosesJob : IParallelJob
        {
            private IChunkDataIterator itr;
            private ComponentDataAccess<PhysicsBodyRef> bodyRefs;
            private ComponentDataAccess<LocalToWorld> localToWorlds;
            private ComponentDataAccess<DirtyPosition> dirtyPositions;

            public Simulation simulation;

            public void Execute(int thread, int start, int end)
            {
                for (int i = start; i < end; ++i)
                {
                    if (bodyRefs[i].IsStatic || bodyRefs[i].IsKinematic)
                        continue;
                    var bodyReference = simulation.Bodies[new BodyHandle(bodyRefs[i].Handle)];
                    ref var pose = ref bodyReference.Pose;
                    ref var l2w = ref localToWorlds[i];
                    l2w.Matrix = Utilities.TRS(pose.Position, pose.Orientation, l2w.Scale);
                    dirtyPositions[i].Enable();
                }
            }
        }

        /// <summary>
        /// Called by the editor when the user drags a dynamic body's transform (gizmo or
        /// inspector field). Snaps the Bepu body straight to the edited pose and zeroes its
        /// velocity, so the drag doesn't fight gravity/collision; on release it just falls again
        /// from wherever it was left. No-op for static colliders and kinematic bodies (the latter
        /// already get the entity's pose pushed into Bepu every step via <see cref="PushKinematicPoses"/>).
        /// </summary>
        internal void SyncDynamicPoseFromEditor(Entity entity, Vector3 position, Quaternion rotation)
        {
            if (!entityManager.HasComponent<PhysicsBodyRef>(entity))
                return;
            ref var bodyRef = ref entityManager.GetComponent<PhysicsBodyRef>(entity);
            if (bodyRef.IsStatic || bodyRef.IsKinematic)
                return;
            var bodyReference = simulation.Bodies[new BodyHandle(bodyRef.Handle)];
            bodyReference.Awake = true;
            ref var pose = ref bodyReference.Pose;
            pose = new RigidPose(position, rotation);
            bodyReference.Velocity.Linear = Vector3.Zero;
            bodyReference.Velocity.Angular = Vector3.Zero;
        }

        internal void RemoveBody(Entity entity, in PhysicsBodyRef bodyRef)
        {
            if (bodyRef.IsStatic)
            {
                var handle = new StaticHandle(bodyRef.Handle);
                var collidableRef = simulation.Statics[handle].CollidableReference;
                if (contactEvents.IsListener(collidableRef))
                    contactEvents.Unregister(collidableRef);
                staticHandleToEntity.Remove(bodyRef.Handle);
                simulation.Statics.Remove(handle);
            }
            else
            {
                var handle = new BodyHandle(bodyRef.Handle);
                var collidableRef = simulation.Bodies[handle].CollidableReference;
                if (contactEvents.IsListener(collidableRef))
                    contactEvents.Unregister(collidableRef);
                bodyHandleToEntity.Remove(bodyRef.Handle);
                simulation.Bodies.Remove(handle);
            }
            simulation.Shapes.RemoveAndDispose(bodyRef.Shape, bufferPool);
        }

        private void RaiseContact(Action<Entity, Entity> @event, CollidableReference a, CollidableReference b)
        {
            if (@event == null)
                return;
            if (TryResolveEntity(a, out var entityA) && TryResolveEntity(b, out var entityB))
                @event.Invoke(entityA, entityB);
        }

        private bool TryResolveEntity(CollidableReference collidable, out Entity entity)
        {
            if (collidable.Mobility == CollidableMobility.Static)
                return staticHandleToEntity.TryGetValue(collidable.StaticHandle.Value, out entity);
            return bodyHandleToEntity.TryGetValue(collidable.BodyHandle.Value, out entity);
        }

        public bool RayCast(in Ray ray, float maxDistance, out PhysicsHit hit)
        {
            var handler = new RayHitHandler();
            simulation.RayCast(ray.Position, ray.Direction, maxDistance, bufferPool, ref handler);
            if (handler.Hit)
            {
                hit = new PhysicsHit
                {
                    Point = ray.Position + ray.Direction * handler.T,
                    Normal = handler.Normal,
                    Distance = handler.T,
                };
                if (TryResolveEntity(handler.Collidable, out var entity))
                    hit.Entity = entity;
                return true;
            }
            hit = default;
            return false;
        }

        private struct RayHitHandler : IRayHitHandler
        {
            public bool Hit;
            public float T;
            public Vector3 Normal;
            public CollidableReference Collidable;

            public bool AllowTest(CollidableReference collidable) => true;
            public bool AllowTest(CollidableReference collidable, int childIndex) => true;

            public void OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable, int childIndex)
            {
                if (t < maximumT)
                {
                    maximumT = t;
                }
                if (!Hit || t < T)
                {
                    Hit = true;
                    T = t;
                    Normal = normal;
                    Collidable = collidable;
                }
            }
        }

        public void Dispose()
        {
            contactEvents.Dispose();
            materials.Dispose();
            bodyExtras.Dispose();
            simulation.Dispose();
            bufferPool.Clear();
        }
    }
}
