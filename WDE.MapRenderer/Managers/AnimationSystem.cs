using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Interfaces;
using TheEngine.Managers;
using TheMaths;
using WDE.Common.Database;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers;

public class AnimationSystem
{
    private Stopwatch sw = new();
    public static int MAX_BONES = 312; // Creature\SlimeGiant\GiantSlime.M2 has 312 bones
    private readonly Archetypes archetypes;
    private readonly ICameraManager cameraManager;
    private readonly EmoteStore emoteStore;
    private readonly AnimationDataStore animationDataStore;

    // private readonly ThreadLocal<Matrix[]> bones = new ThreadLocal<Matrix[]>(() =>
    // {
    //     var b = new Matrix[MAX_BONES];
    //     for (int i = 0; i < MAX_BONES; ++i)
    //         b[i] = Matrix.Identity;
    //     return b;
    // });

    private static Matrix[] staticIdentityBones = new Matrix[MAX_BONES];
    static AnimationSystem()
    {
        for (int i = 0; i < MAX_BONES; ++i)
            staticIdentityBones[i] = Matrix.Identity;
    }

    public static Memory<Matrix> IdentityBones(int count) => staticIdentityBones.AsMemory(0, count);
    
    public AnimationSystem(Archetypes archetypes,
        ICameraManager cameraManager,
        EmoteStore emoteStore,
        AnimationDataStore animationDataStore)
    {
        this.archetypes = archetypes;
        this.cameraManager = cameraManager;
        this.emoteStore = emoteStore;
        this.animationDataStore = animationDataStore;
    }
    
    private static Vector3 GetFirstOrDefaultVector3(int IDX, MutableM2Track<Vector3> track, Vector3 def, float t)
    {
        if (track.Length <= IDX)
            return def;
        ref readonly var values = ref track.Values(IDX);
        if (values.Length == 0)
            return def;
        if (values.Length == 1)
            return values[0];
            
        ref readonly var timestamps = ref track.Timestamps(IDX);
        
        int firstIndexGreaterThan = FirstIndexGreaterThan(in timestamps, (uint)t);
        if (firstIndexGreaterThan == 0)
        {
            return values[firstIndexGreaterThan];
        }
        
        var prev = values[firstIndexGreaterThan - 1];
        var nextValue = values[firstIndexGreaterThan];
        var start = timestamps[firstIndexGreaterThan - 1];
        var end = timestamps[firstIndexGreaterThan];
        var pct = (t - start) / (end - start);
        return Vector3.Lerp(prev, nextValue, pct);
    }

    private static int FirstIndexGreaterThan(in MutableM2Array<uint> array, uint target)
    {
        if (target >= array[array.Length - 1])
            return array.Length - 1;

        int st = 0; 
        int end = array.Length - 1; 
        while(st <= end) {
            int mid = (st + end) / 2;   // or elegant way of st + (end - st) / 2; 
            if (array[mid] <= target) {
                st = mid + 1; 
            } else { // mid > target
                end = mid - 1; 
            }
        }
        return st > array.Length - 1 ? array.Length - 1 : st; // or return end + 1
    }
    
    private static Quaternion GetFirstOrDefaultQuaternion(int IDX, MutableM2Track<M2CompQuat> track, Quaternion def, float t)
    {
        if (track.Length <= IDX)
            return def;
        ref readonly var values = ref track.Values(IDX);
        if (values.Length == 0)
            return def;
        if (values.Length == 1)
            return values[0].Value;

        ref readonly var timestamps = ref track.Timestamps(IDX);
        
        int firstIndexGreaterThan = FirstIndexGreaterThan(in timestamps, (uint)t);
        if (firstIndexGreaterThan == 0)
        {
            return values[firstIndexGreaterThan].Value;
        }
        
        var prev = values[firstIndexGreaterThan - 1];
        var nextValue = values[firstIndexGreaterThan];
        var start = timestamps[firstIndexGreaterThan - 1];
        var end = timestamps[firstIndexGreaterThan];
        var pct = (t - start) / (end - start);
        return Quaternion.Slerp(prev.Value, nextValue.Value, pct);
    }

    private static Matrix GetBoneMatrixWithoutParent(M2 m2, int boneIndex, float t, int IDX)
    {
        Matrix boneMatrix;
        ref readonly var boneData = ref m2.bones[boneIndex];

        if ((boneData.flags & M2CompBoneFlag.transformed) != 0)// || true)
        {
            var position = GetFirstOrDefaultVector3(IDX, boneData.translation, Vector3.Zero, t);
            var scaling = GetFirstOrDefaultVector3(IDX, boneData.scale, Vector3.One, t);
            var rotation = GetFirstOrDefaultQuaternion(IDX, boneData.rotation, Quaternion.Identity, t);
            boneMatrix = Matrix.CreateFromQuaternion(rotation);
            if (scaling.X != 1 || scaling.Y != 1 || scaling.Z != 1)
                boneMatrix *= Matrix.CreateScale(scaling);
            if (position.X != 0 || position.Y != 0 || position.Z != 0)
                boneMatrix *= Matrix.CreateTranslation(position);
        }
        else
        {
            boneMatrix = Matrix.Identity;
        }

        if (boneData.pivot.X != 0 || boneData.pivot.Y != 0 || boneData.pivot.Z != 0)
        {
            var mPivot = Matrix.CreateTranslation(boneData.pivot);
            var mInvPivot = Matrix.CreateTranslation(-boneData.pivot);
            boneMatrix = mInvPivot * boneMatrix * mPivot;            
        }

        return boneMatrix;
    }
    
    private static Matrix GetBoneMatrix(M2 m2, Matrix[] calculatedBones, int boneIndex, float t, int IDX)
    {
        Matrix boneMatrix = GetBoneMatrixWithoutParent(m2, boneIndex, t, IDX);
        ref readonly var boneData = ref m2.bones[boneIndex];
        
        if (boneData.parent_bone >= 0)
        {
            if (boneData.parent_bone > boneIndex)
            {
                Console.WriteLine("boneData.parent_bone > boneIndex");
                calculatedBones[boneData.parent_bone] = GetBoneMatrix(m2, calculatedBones, boneData.parent_bone, t, IDX);
            }

            boneMatrix *= calculatedBones[boneData.parent_bone]; // GetBoneMatrix(m2, boneData.parent_bone, t, IDX);
        }

        return boneMatrix;
    }
    
    // slower, but not additional array required
    private static Matrix GetBoneMatrixRecursive(M2 m2, int boneIndex, float t, int IDX)
    {
        Matrix boneMatrix = GetBoneMatrixWithoutParent(m2, boneIndex, t, IDX);
        ref readonly var boneData = ref m2.bones[boneIndex];
        
        if (boneData.parent_bone >= 0)
            boneMatrix *= GetBoneMatrixRecursive(m2, boneData.parent_bone, t, IDX);

        return boneMatrix;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AnimationTickTime(float delta, M2AnimationComponentData animationData)
    {
        if (animationData._currentAnimation == -1 || animationData._length == 0)
            return false;

        if ((animationData.Flags & AnimationDataFlags.FallbackPlayBackwards) != 0)
            animationData._time -= delta;
        else
            animationData._time += delta;

        if ((animationData.Flags & AnimationDataFlags.FallbackHoldsLastFrame) != 0)
        {
            if (animationData._time > animationData._length)
                animationData._time = animationData._length;
            else if (animationData._time < 0)
                animationData._time = 0;
        }
        else
        {
            while (animationData._time > animationData._length)
                animationData._time -= animationData._length;
            if (animationData._time < 0)
                animationData._time = animationData._length;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool UpdateAnimationData(M2AnimationComponentData animationData)
    {
        if (animationData._currentAnimation != animationData.SetNewAnimation)
        {
            int? lookup = animationData.Model.GetAnimationIndexByAnimationId(animationData.SetNewAnimation);
            if (!lookup.HasValue)
            {
                animationData._currentAnimation = -1;
            }
            else
            {

                var internalIndex = lookup.Value;
                do
                {
                    bool isAlias = animationData.Model.sequences[internalIndex].flags.HasFlagFast(M2SequenceFlags.IsAlias);
                    if (!isAlias || animationData.Model.sequences[internalIndex].aliasNext == internalIndex)
                        break;
                    internalIndex = animationData.Model.sequences[internalIndex].aliasNext;
                } while (true);
                
                animationData._animInternalIndex = internalIndex;
                animationData._length = animationData.Model.sequences[internalIndex].duration;
                animationData._currentAnimation = animationData.SetNewAnimation;
                animationData._time = 0;
                
                animationData.Model.bones.LoadAnimation(animationData._animInternalIndex);
                return true;
            }
        }

        return false;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AnimationCalculateBones(int bonesLength, M2AnimationComponentData animationData, Matrix4x4[] localBones)
    {
        for (int j = 0; j < bonesLength; ++j)
        {
            var boneMatrix = GetBoneMatrix(animationData.Model, localBones, j, animationData._time,
                animationData._animInternalIndex);
            localBones[j] = boneMatrix;
        }

        if (animationData.AttachmentType.HasValue)
        {
            var attachedTo = animationData.AttachedTo;
            var attachmentType = animationData.AttachmentType.Value;
            while (attachedTo != null)
            {
                int attachedToBone = -1;
                Vector3 offset = Vector3.Zero;
                for (var index = 0; index < attachedTo.Model.attachments.Length; index++)
                {
                    ref readonly var attachment = ref attachedTo.Model.attachments[index];
                    if (attachment.id == attachmentType)
                    {
                        attachedToBone = attachment.bone;
                        offset = attachment.position;
                        break;
                    }
                }

                if (attachedToBone != -1)
                {
                    var parentMatrix = GetBoneMatrixRecursive(attachedTo.Model, attachedToBone, attachedTo._time,
                        attachedTo._animInternalIndex);
                    for (int j = 0; j < bonesLength; ++j)
                        localBones[j] *= Matrix.CreateTranslation(offset) * parentMatrix;
                }

                attachmentType = attachedTo.AttachmentType ?? M2AttachmentType.ItemVisual0;
                attachedTo = attachedTo.AttachedTo;
            }
        }
    }

    public static bool ManualAnimationStep(float delta, M2AnimationComponentData animationData)
    {
        UpdateAnimationData(animationData);
        if (!AnimationTickTime(delta, animationData)) 
            return false;
        
        var bonesLength = animationData.Model.bones.Length;
        var localBones = ArrayPool<Matrix>.Shared.Rent(bonesLength);
        
        AnimationCalculateBones(bonesLength, animationData, localBones);
        animationData._buffer.UpdateBuffer(localBones.AsSpan(0, bonesLength));
        ArrayPool<Matrix>.Shared.Return(localBones);
        
        return true;
    }

    public void Update(float delta)
    {
        sw.Restart();

        ThreadLocal<long> counter = new(true);
        var cameraPosition = cameraManager.MainCamera.Transform.Position;
        ThreadLocal<List<(NativeBuffer<Matrix>, int, Matrix[])>> updates = new ThreadLocal<List<(NativeBuffer<Matrix>, int, Matrix[])>>(() => new(), true);
    
        archetypes.AnimatedEntityArchetype.ParallelForEachRRROOO<RenderEnabledBit, LocalToWorld, M2AnimationComponentData, MeshBounds, DirtyPosition, WorldMeshBounds>((itr, start, end, renderEnabledAccess, localToWorldAccess, animationAccess, meshBoundsAccess, dirtPositionAccess, worldMeshBounds) =>
        {
            int sum = 0;
            for (int i = start; i < end; ++i)
            {
                if (!renderEnabledAccess[i] || Vector3.Distance(cameraPosition, localToWorldAccess[i].Position) > 100)
                     continue;
                sum++;
                var animationData = animationAccess[i];

                if (UpdateAnimationData(animationData))
                {
                    var bounds = animationData.Model.sequences[animationData._animInternalIndex].bounds;
                    var bb = new BoundingBox(bounds.extent.min, bounds.extent.max);
                    
                    if (meshBoundsAccess.HasValue)
                        meshBoundsAccess.Value[i].box = bb;

                    if (worldMeshBounds.HasValue)
                        worldMeshBounds.Value[i].box = RenderManager.LocalToWorld((MeshBounds)bb, in localToWorldAccess[i]);
                
                    if (dirtPositionAccess.HasValue)
                        dirtPositionAccess.Value[i].Enable();
                }

                if (!AnimationTickTime(delta, animationData)) 
                    continue;

                var bonesLength = animationData.Model.bones.Length;
                var localBones = ArrayPool<Matrix>.Shared.Rent(bonesLength);

                AnimationCalculateBones(bonesLength, animationData, localBones);

                updates.Value!.Add((animationData._buffer, bonesLength, localBones));
                //animationData._buffer.UpdateBuffer(localBones.AsSpan(0, bonesLength));
            }
            counter.Value += sum;
        });
        foreach (var tuple in updates.Values.SelectMany(x => x))
        {
            tuple.Item1.UpdateBuffer(tuple.Item3.AsSpan(0, tuple.Item2));
            ArrayPool<Matrix>.Shared.Return(tuple.Item3);
        }
        sw.Stop();
    }

    public M2AnimationType? GetAnimationType(M2? model, uint? emoteState, uint? standState, AnimTier? animTier)
    {
        M2AnimationType? animationId = null;
        if (emoteState.HasValue && emoteState.Value != 0)
        {
            var emote = emoteStore[emoteState.Value];
            animationId = (M2AnimationType)emote.AnimId;
        }
        else if (standState.HasValue)
        {
            if (standState.Value == 1)
                animationId = M2AnimationType.SitGround;
            else if (standState.Value == 2)
                animationId = M2AnimationType.SitChairMed;
            else if (standState.Value == 3)
                animationId = M2AnimationType.Sleep;
            else if (standState.Value == 4)
                animationId = M2AnimationType.SitChairLow;
            else if (standState.Value == 5)
                animationId = M2AnimationType.SitChairMed;
            else if (standState.Value == 6)
                animationId = M2AnimationType.SitChairHigh;
            else if (standState.Value == 7)
                animationId = M2AnimationType.Dead;
            else if (standState.Value == 8)
                animationId = M2AnimationType.KneelLoop;
            else if (standState.Value == 9)
                animationId = M2AnimationType.Submerged;
        }

        if (!animationId.HasValue && animTier.HasValue)
        {
            switch (animTier)
            {
                case AnimTier.Ground:
                    animationId = M2AnimationType.Stand;
                    break;
                case AnimTier.Swim:
                    animationId = M2AnimationType.Swim;
                    break;
                case AnimTier.Hover:
                    animationId = M2AnimationType.Hover;
                    break;
                case AnimTier.Fly:
                {
                    if (model?.GetAnimationIndexByAnimationId((int)M2AnimationType.FlyStand) == null)
                        animationId = M2AnimationType.Fly;
                    else
                        animationId = M2AnimationType.FlyStand;
                    break;
                }
                case AnimTier.Submerged:
                    animationId = M2AnimationType.Submerged;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(animTier), animTier, null);
            }
        }

        return animationId;
    }
}