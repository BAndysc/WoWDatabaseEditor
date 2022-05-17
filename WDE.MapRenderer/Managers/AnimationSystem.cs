using System.Buffers;
using System.Diagnostics;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Interfaces;
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
    
    private Vector3 GetFirstOrDefaultVector3(int IDX, in M2Track<Vector3> track, Vector3 def, float t)
    {
        if (track.values.Length <= IDX)
            return def;
        var values = track.values[IDX];
        if (values.Length == 0)
            return def;
        if (values.Length == 1)
            return track.values[IDX][0];
        
        int firstIndexGreaterThan = FirstIndexGreaterThan(in track.timestamps[IDX], (uint)t);
        if (firstIndexGreaterThan == 0)
        {
            return values[firstIndexGreaterThan];
        }
        
        var prev = values[firstIndexGreaterThan - 1];
        var nextValue = values[firstIndexGreaterThan];
        var start = track.timestamps[IDX][firstIndexGreaterThan - 1];
        var end = track.timestamps[IDX][firstIndexGreaterThan];
        var pct = (t - start) / (end - start);
        return Vector3.Lerp(prev, nextValue, pct);
    }

    private int FirstIndexGreaterThan(in M2Array<uint> array, uint target)
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
    
    private Quaternion GetFirstOrDefaultQuaternion(int IDX, in M2Track<Quaternion> track, Quaternion def, float t)
    {
        if (track.values.Length <= IDX)
            return def;
        var values = track.values[IDX];
        if (values.Length == 0)
            return def;
        if (values.Length == 1)
            return track.values[IDX][0];

        int firstIndexGreaterThan = FirstIndexGreaterThan(in track.timestamps[IDX], (uint)t);
        if (firstIndexGreaterThan == 0)
        {
            return values[firstIndexGreaterThan];
        }
        
        var prev = values[firstIndexGreaterThan - 1];
        var nextValue = values[firstIndexGreaterThan];
        var start = track.timestamps[IDX][firstIndexGreaterThan - 1];
        var end = track.timestamps[IDX][firstIndexGreaterThan];
        var pct = (t - start) / (end - start);
        return Quaternion.Slerp(prev, nextValue, pct);
    }

    private Matrix GetBoneMatrixWithoutParent(M2 m2, int boneIndex, float t, int IDX)
    {
        Matrix boneMatrix;
        ref readonly var boneData = ref m2.bones[boneIndex];

        if ((boneData.flags & M2CompBoneFlag.transformed) != 0)// || true)
        {
            var position = GetFirstOrDefaultVector3(IDX, in boneData.translation, Vector3.Zero, t);
            var scaling = GetFirstOrDefaultVector3(IDX, in boneData.scale, Vector3.One, t);
            var rotation = GetFirstOrDefaultQuaternion(IDX, in boneData.rotation, Quaternion.Identity, t);
            boneMatrix = Matrix.RotationQuaternion(in rotation);
            if (scaling.X != 1 || scaling.Y != 1 || scaling.Z != 1)
                boneMatrix *= Matrix.Scaling(in scaling);
            if (!position.IsZero)
                boneMatrix *= Matrix.Translation(in position);
        }
        else
        {
            boneMatrix = Matrix.Identity;
        }

        if (!boneData.pivot.IsZero)
        {
            var mPivot = Matrix.Translation(boneData.pivot);
            var mInvPivot = Matrix.Translation(-boneData.pivot);
            boneMatrix = mInvPivot * boneMatrix * mPivot;            
        }

        return boneMatrix;
    }
    
    private Matrix GetBoneMatrix(M2 m2, Matrix[] calculatedBones, int boneIndex, float t, int IDX)
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
    private Matrix GetBoneMatrixRecursive(M2 m2, int boneIndex, float t, int IDX)
    {
        Matrix boneMatrix = GetBoneMatrixWithoutParent(m2, boneIndex, t, IDX);
        ref readonly var boneData = ref m2.bones[boneIndex];
        
        if (boneData.parent_bone >= 0)
            boneMatrix *= GetBoneMatrixRecursive(m2, boneData.parent_bone, t, IDX);

        return boneMatrix;
    }
    
    public void Update(float delta)
    {
        sw.Restart();

        ThreadLocal<long> counter = new(true);
        var cameraPosition = cameraManager.MainCamera.Transform.Position;
        ThreadLocal<List<(NativeBuffer<Matrix>, int, Matrix[])>> updates = new ThreadLocal<List<(NativeBuffer<Matrix>, int, Matrix[])>>(() => new(), true);
        archetypes.AnimatedEntityArchetype.ParallelForEach<RenderEnabledBit, LocalToWorld, MeshBounds, M2AnimationComponentData>((itr, start, end, renderEnabledAccess, localToWorldAccess, meshBoundsAccess, animationAccess) =>
        {
            int sum = 0;
            for (int i = start; i < end; ++i)
            {
                if (!renderEnabledAccess[i] ||
                     Vector3.Distance(cameraPosition, localToWorldAccess[i].Position) > 100)
                     continue;
                sum++;
                var animationData = animationAccess[i];

                if (animationData._currentAnimation != animationData.SetNewAnimation)
                {
                    int? lookup = animationData.Model.GetAnimationIndexByAnimationId(animationData.SetNewAnimation);
                    if (!lookup.HasValue)
                    {
                        animationData._currentAnimation = -1;
                    }
                    else
                    {
                        animationData._animInternalIndex = lookup.Value;
                        animationData._length = animationData.Model.sequences[lookup.Value].duration;
                        animationData._currentAnimation = animationData.SetNewAnimation;
                        animationData._time = 0;

                        var bounds = animationData.Model.sequences[lookup.Value].bounds;
                        meshBoundsAccess[i].box = new BoundingBox(bounds.extent.min, bounds.extent.max);
                    }
                }

                if (animationData._currentAnimation == -1)
                    continue;

                animationData._time += delta;
                while (animationData._time > animationData._length)
                    animationData._time -= animationData._length;

                var bonesLength = animationData.Model.bones.Length;
                var localBones = ArrayPool<Matrix>.Shared.Rent(bonesLength);

                for (int j = 0; j < bonesLength; ++j)
                {
                    var boneMatrix = GetBoneMatrix(animationData.Model, localBones, j, animationData._time, animationData._animInternalIndex);
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
                        foreach (var attachment in attachedTo.Model.attachments)
                        {
                            if (attachment.id == attachmentType)
                            {
                                attachedToBone = attachment.bone;
                                offset = attachment.position;
                                break;
                            }
                        }

                        if (attachedToBone != -1)
                        {
                            var parentMatrix = GetBoneMatrixRecursive(attachedTo.Model, attachedToBone, attachedTo._time, attachedTo._animInternalIndex);
                            for (int j = 0; j < bonesLength; ++j)
                                localBones[j] *= Matrix.Translation(offset) * parentMatrix;
                        }

                        attachmentType = attachedTo.AttachmentType ?? M2AttachmentType.ItemVisual0;
                        attachedTo = attachedTo.AttachedTo;
                    }
                }
                
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

            if (animationDataStore.TryGetValue((uint)animationId, out var animationData))
            {
                if (animationData.Fallback != 0)
                    animationId = (M2AnimationType)animationData.Fallback;
            }
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
                    animationId = M2AnimationType.FlyStand;
                    break;
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