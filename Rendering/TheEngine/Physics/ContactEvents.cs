// Ported from BepuPhysics2's Demos/Demos/ContactEventsDemo.cs (IContactEventHandler + ContactEvents).
// The library itself has no concept of contact events - only narrow-phase callbacks reporting
// manifold state - so this helper (built around those callbacks) is required and lives here
// rather than in the BepuPhysics package. ContactEventCallbacks is NOT ported verbatim: PhysicsManager
// merges this dispatch logic with its own per-pair PhysicsMaterial blending in a single callbacks struct.
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuUtilities;
using BepuUtilities.Collections;
using BepuUtilities.Memory;

namespace TheEngine.Physics
{
    /// <summary>
    /// Implements handlers for various collision events.
    /// </summary>
    public interface IContactEventHandler
    {
        void OnContactAdded<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold,
            Vector3 contactOffset, Vector3 contactNormal, float depth, int featureId, int contactIndex, int workerIndex) where TManifold : unmanaged, IContactManifold<TManifold>
        {
        }

        void OnContactRemoved<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int removedFeatureId, int workerIndex) where TManifold : unmanaged, IContactManifold<TManifold>
        {
        }

        void OnStartedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int workerIndex) where TManifold : unmanaged, IContactManifold<TManifold>
        {
        }

        void OnTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int workerIndex) where TManifold : unmanaged, IContactManifold<TManifold>
        {
        }

        void OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int workerIndex) where TManifold : unmanaged, IContactManifold<TManifold>
        {
        }

        void OnPairCreated<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int workerIndex) where TManifold : unmanaged, IContactManifold<TManifold>
        {
        }

        void OnPairUpdated<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int workerIndex) where TManifold : unmanaged, IContactManifold<TManifold>
        {
        }

        void OnPairEnded(CollidableReference eventSource, CollidablePair pair)
        {
        }
    }

    /// <summary>
    /// Watches a set of bodies and statics for contact changes and reports events.
    /// </summary>
    public class ContactEvents : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        struct PreviousCollision
        {
            public CollidableReference Collidable;
            public bool Fresh;
            public bool WasTouching;
            public int ContactCount;
            public int FeatureId0;
            public int FeatureId1;
            public int FeatureId2;
            public int FeatureId3;
        }

        Simulation simulation;
        IThreadDispatcher threadDispatcher;
        BufferPool pool;

        CollidableProperty<int> listenerIndices;
        IndexSet staticListenerFlags;
        IndexSet bodyListenerFlags;
        int listenerCount;

        struct Listener
        {
            public CollidableReference Source;
            public IContactEventHandler Handler;
            public QuickList<PreviousCollision> PreviousCollisions;
        }
        Listener[] listeners;

        struct PendingWorkerAdd
        {
            public int ListenerIndex;
            public PreviousCollision Collision;
        }
        QuickList<PendingWorkerAdd>[] pendingWorkerAdds;

        public ContactEvents(IThreadDispatcher threadDispatcher = null, BufferPool pool = null, int initialListenerCapacity = 64)
        {
            this.threadDispatcher = threadDispatcher;
            this.pool = pool;
            listeners = new Listener[initialListenerCapacity];
        }

        BufferPool GetPoolForWorker(int workerIndex)
        {
            return threadDispatcher == null ? pool : threadDispatcher.WorkerPools[workerIndex];
        }

        /// <remarks>The constructor and initialization are split because this is expected to be passed
        /// into a simulation's constructor as a part of its contact callbacks, so there is no simulation
        /// available at the time of construction.</remarks>
        public void Initialize(Simulation simulation)
        {
            this.simulation = simulation;
            if (pool == null)
                pool = simulation.BufferPool;
            simulation.Timestepper.BeforeCollisionDetection += SetFreshnessForCurrentActivityStatus;
            listenerIndices = new CollidableProperty<int>(simulation, pool);
            pendingWorkerAdds = new QuickList<PendingWorkerAdd>[threadDispatcher == null ? 1 : threadDispatcher.ThreadCount];
        }

        public void Register(CollidableReference collidable, IContactEventHandler handler)
        {
            Debug.Assert(!IsListener(collidable), "Should only try to register listeners that weren't previously registered");
            if (collidable.Mobility == CollidableMobility.Static)
                staticListenerFlags.Add(collidable.RawHandleValue, pool);
            else
                bodyListenerFlags.Add(collidable.RawHandleValue, pool);
            if (listenerCount >= listeners.Length)
            {
                Array.Resize(ref listeners, listeners.Length * 2);
            }
            listeners[listenerCount] = new Listener { Handler = handler, Source = collidable };
            listenerIndices[collidable] = listenerCount;
            ++listenerCount;
        }

        public void Register(BodyHandle body, IContactEventHandler handler)
        {
            Register(simulation.Bodies[body].CollidableReference, handler);
        }

        public void Register(StaticHandle staticHandle, IContactEventHandler handler)
        {
            Register(new CollidableReference(staticHandle), handler);
        }

        public void Unregister(CollidableReference collidable)
        {
            Debug.Assert(IsListener(collidable), "Should only try to unregister listeners that actually exist.");
            if (collidable.Mobility == CollidableMobility.Static)
            {
                staticListenerFlags.Remove(collidable.RawHandleValue);
            }
            else
            {
                bodyListenerFlags.Remove(collidable.RawHandleValue);
            }
            var index = listenerIndices[collidable];
            --listenerCount;
            ref var removedSlot = ref listeners[index];
            if (removedSlot.PreviousCollisions.Span.Allocated)
                removedSlot.PreviousCollisions.Dispose(pool);
            ref var lastSlot = ref listeners[listenerCount];
            if (index < listenerCount)
            {
                listenerIndices[lastSlot.Source] = index;
                removedSlot = lastSlot;
            }
            lastSlot = default;
        }

        public void Unregister(BodyHandle body)
        {
            Unregister(simulation.Bodies[body].CollidableReference);
        }

        public void Unregister(StaticHandle staticHandle)
        {
            Unregister(new CollidableReference(staticHandle));
        }

        public bool IsListener(CollidableReference collidable)
        {
            if (collidable.Mobility == CollidableMobility.Static)
            {
                return staticListenerFlags.Contains(collidable.RawHandleValue);
            }
            else
            {
                return bodyListenerFlags.Contains(collidable.RawHandleValue);
            }
        }

        void SetFreshnessForCurrentActivityStatus(float dt, IThreadDispatcher threadDispatcher)
        {
            var bodyHandleToLocation = simulation.Bodies.HandleToLocation;
            for (int listenerIndex = 0; listenerIndex < listenerCount; ++listenerIndex)
            {
                ref var listener = ref listeners[listenerIndex];
                var source = listener.Source;
                var sourceExpectsUpdates = source.Mobility != CollidableMobility.Static && bodyHandleToLocation[source.BodyHandle.Value].SetIndex == 0;
                if (sourceExpectsUpdates)
                {
                    var previousCollisions = listeners[listenerIndex].PreviousCollisions;
                    for (int j = 0; j < previousCollisions.Count; ++j)
                    {
                        previousCollisions[j].Fresh = false;
                    }
                }
                else
                {
                    var previousCollisions = listeners[listenerIndex].PreviousCollisions;
                    for (int j = 0; j < previousCollisions.Count; ++j)
                    {
                        ref var previousCollision = ref previousCollisions[j];
                        previousCollision.Fresh = previousCollision.Collidable.Mobility == CollidableMobility.Static || bodyHandleToLocation[previousCollision.Collidable.BodyHandle.Value].SetIndex > 0;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void UpdatePreviousCollision<TManifold>(ref PreviousCollision collision, ref TManifold manifold, bool isTouching) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            Debug.Assert(manifold.Count <= 4, "This was built on the assumption that nonconvex manifolds will have a maximum of four contacts, but that might have changed.");
            for (int j = 0; j < manifold.Count; ++j)
            {
                Unsafe.Add(ref collision.FeatureId0, j) = manifold.GetFeatureId(j);
            }
            collision.ContactCount = manifold.Count;
            collision.Fresh = true;
            collision.WasTouching = isTouching;
        }

        void HandleManifoldForCollidable<TManifold>(int workerIndex, CollidableReference source, CollidableReference other, CollidablePair pair, ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            if (IsListener(source))
            {
                var listenerIndex = listenerIndices[source];
                ref var listener = ref listeners[listenerIndex];

                int previousCollisionIndex = -1;
                bool isTouching = false;
                for (int i = 0; i < listener.PreviousCollisions.Count; ++i)
                {
                    ref var collision = ref listener.PreviousCollisions[i];
                    if (collision.Collidable.Packed == other.Packed)
                    {
                        previousCollisionIndex = i;
                        int previousContactsStillExist = 0;
                        for (int contactIndex = 0; contactIndex < manifold.Count; ++contactIndex)
                        {
                            var featureId = manifold.GetFeatureId(contactIndex);
                            var featureIdWasInPreviousCollision = false;
                            for (int previousContactIndex = 0; previousContactIndex < collision.ContactCount; ++previousContactIndex)
                            {
                                if (featureId == Unsafe.Add(ref collision.FeatureId0, previousContactIndex))
                                {
                                    featureIdWasInPreviousCollision = true;
                                    previousContactsStillExist |= 1 << previousContactIndex;
                                    break;
                                }
                            }
                            if (!featureIdWasInPreviousCollision)
                            {
                                manifold.GetContact(contactIndex, out var offset, out var normal, out var depth, out _);
                                listener.Handler.OnContactAdded(source, pair, ref manifold, offset, normal, depth, featureId, contactIndex, workerIndex);
                            }
                            if (manifold.GetDepth(contactIndex) >= 0)
                                isTouching = true;
                        }
                        if (previousContactsStillExist != (1 << collision.ContactCount) - 1)
                        {
                            for (int previousContactIndex = 0; previousContactIndex < collision.ContactCount; ++previousContactIndex)
                            {
                                if ((previousContactsStillExist & (1 << previousContactIndex)) == 0)
                                {
                                    listener.Handler.OnContactRemoved(source, pair, ref manifold, Unsafe.Add(ref collision.FeatureId0, previousContactIndex), workerIndex);
                                }
                            }
                        }
                        if (!collision.WasTouching && isTouching)
                        {
                            listener.Handler.OnStartedTouching(source, pair, ref manifold, workerIndex);
                        }
                        else if (collision.WasTouching && !isTouching)
                        {
                            listener.Handler.OnStoppedTouching(source, pair, ref manifold, workerIndex);
                        }
                        if (isTouching)
                        {
                            listener.Handler.OnTouching(source, pair, ref manifold, workerIndex);
                        }
                        UpdatePreviousCollision(ref collision, ref manifold, isTouching);
                        break;
                    }
                }
                if (previousCollisionIndex < 0)
                {
                    ref var addsforWorker = ref pendingWorkerAdds[workerIndex];
                    addsforWorker.EnsureCapacity(Math.Max(addsforWorker.Count + 1, 64), GetPoolForWorker(workerIndex));
                    ref var pendingAdd = ref addsforWorker.AllocateUnsafely();
                    pendingAdd.ListenerIndex = listenerIndex;
                    pendingAdd.Collision.Collidable = other;
                    listener.Handler.OnPairCreated(source, pair, ref manifold, workerIndex);
                    for (int i = 0; i < manifold.Count; ++i)
                    {
                        manifold.GetContact(i, out var offset, out var normal, out var depth, out var featureId);
                        listener.Handler.OnContactAdded(source, pair, ref manifold, offset, normal, depth, featureId, i, workerIndex);
                        if (depth >= 0)
                            isTouching = true;
                    }
                    if (isTouching)
                    {
                        listener.Handler.OnStartedTouching(source, pair, ref manifold, workerIndex);
                        listener.Handler.OnTouching(source, pair, ref manifold, workerIndex);
                    }
                    UpdatePreviousCollision(ref pendingAdd.Collision, ref manifold, isTouching);
                }
                listener.Handler.OnPairUpdated(source, pair, ref manifold, workerIndex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void HandleManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            HandleManifoldForCollidable(workerIndex, pair.A, pair.B, pair, ref manifold);
            HandleManifoldForCollidable(workerIndex, pair.B, pair.A, pair, ref manifold);
        }

        struct EmptyManifold : IContactManifold<EmptyManifold>
        {
            public int Count => 0;
            public bool Convex => true;
            public Contact this[int contactIndex] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public static ref ConvexContact GetConvexContactReference(ref EmptyManifold manifold, int contactIndex) => throw new NotImplementedException();
            public static ref float GetDepthReference(ref EmptyManifold manifold, int contactIndex) => throw new NotImplementedException();
            public static ref int GetFeatureIdReference(ref EmptyManifold manifold, int contactIndex) => throw new NotImplementedException();
            public static ref Contact GetNonconvexContactReference(ref EmptyManifold manifold, int contactIndex) => throw new NotImplementedException();
            public static ref Vector3 GetNormalReference(ref EmptyManifold manifold, int contactIndex) => throw new NotImplementedException();
            public static ref Vector3 GetOffsetReference(ref EmptyManifold manifold, int contactIndex) => throw new NotImplementedException();
            public void GetContact(int contactIndex, out Vector3 offset, out Vector3 normal, out float depth, out int featureId) => throw new NotImplementedException();
            public void GetContact(int contactIndex, out Contact contactData) => throw new NotImplementedException();
            public float GetDepth(int contactIndex) => throw new NotImplementedException();
            public int GetFeatureId(int contactIndex) => throw new NotImplementedException();
            public Vector3 GetNormal(int contactIndex) => throw new NotImplementedException();
            public Vector3 GetOffset(int contactIndex) => throw new NotImplementedException();
        }

        public void Flush()
        {
            for (int i = 0; i < listenerCount; ++i)
            {
                ref var listener = ref listeners[i];
                for (int j = listener.PreviousCollisions.Count - 1; j >= 0; --j)
                {
                    ref var collision = ref listener.PreviousCollisions[j];
                    if (!collision.Fresh)
                    {
                        CollidablePair pair;
                        NarrowPhase.SortCollidableReferencesForPair(listener.Source, collision.Collidable, out _, out _, out pair.A, out pair.B);
                        if (collision.ContactCount > 0)
                        {
                            var emptyManifold = new EmptyManifold();
                            for (int previousContactCount = 0; previousContactCount < collision.ContactCount; ++previousContactCount)
                            {
                                listener.Handler.OnContactRemoved(listener.Source, pair, ref emptyManifold, Unsafe.Add(ref collision.FeatureId0, previousContactCount), 0);
                            }
                            if (collision.WasTouching)
                                listener.Handler.OnStoppedTouching(listener.Source, pair, ref emptyManifold, 0);
                        }
                        listener.Handler.OnPairEnded(collision.Collidable, pair);
                        listener.PreviousCollisions.FastRemoveAt(j);
                        if (listener.PreviousCollisions.Count == 0)
                        {
                            listener.PreviousCollisions.Dispose(pool);
                            listener.PreviousCollisions = default;
                        }
                    }
                    else
                    {
                        collision.Fresh = false;
                    }
                }
            }

            for (int i = 0; i < pendingWorkerAdds.Length; ++i)
            {
                ref var pendingAdds = ref pendingWorkerAdds[i];
                for (int j = 0; j < pendingAdds.Count; ++j)
                {
                    ref var add = ref pendingAdds[j];
                    ref var collisions = ref listeners[add.ListenerIndex].PreviousCollisions;
                    collisions.EnsureCapacity(Math.Max(8, collisions.Count + 1), pool);
                    collisions.AllocateUnsafely() = pendingAdds[j].Collision;
                }
                if (pendingAdds.Span.Allocated)
                    pendingAdds.Dispose(GetPoolForWorker(i));
                pendingAdds = default;
            }
        }

        public void Dispose()
        {
            if (bodyListenerFlags.Flags.Allocated)
                bodyListenerFlags.Dispose(pool);
            if (staticListenerFlags.Flags.Allocated)
                staticListenerFlags.Dispose(pool);
            listenerIndices.Dispose();
            simulation.Timestepper.BeforeCollisionDetection -= SetFreshnessForCurrentActivityStatus;
            for (int i = 0; i < pendingWorkerAdds.Length; ++i)
            {
                Debug.Assert(!pendingWorkerAdds[i].Span.Allocated, "The pending worker adds should have been disposed by the previous flush.");
            }
        }
    }
}
