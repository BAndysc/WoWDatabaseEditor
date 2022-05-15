using System.Buffers;
using System.Diagnostics;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Interfaces;
using TheMaths;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers;

public class AnimationSystem
{
    private Stopwatch sw = new();
    public static int MAX_BONES = 312; // Creature\SlimeGiant\GiantSlime.M2 has 312 bones
    private readonly Archetypes archetypes;
    private readonly ICameraManager cameraManager;

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
        ICameraManager cameraManager)
    {
        this.archetypes = archetypes;
        this.cameraManager = cameraManager;
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

    private Matrix GetBoneMatrix(M2 m2, Matrix[] calculatedBones, int boneIndex, float t, int IDX)
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
    
    public void Update(float delta)
    {
        sw.Restart();

        ThreadLocal<long> counter = new(true);
        var cameraPosition = cameraManager.MainCamera.Transform.Position;
        ThreadLocal<List<(NativeBuffer<Matrix>, int, Matrix[])>> updates = new ThreadLocal<List<(NativeBuffer<Matrix>, int, Matrix[])>>(() => new(), true);
        archetypes.AnimatedEntityArchetype.ParallelForEach<RenderEnabledBit, LocalToWorld, M2AnimationComponentData>((itr, start, end, renderEnabledAccess, localToWorldAccess, animationAccess) =>
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
                    }
                    // bool found = false;
                    // for (int j = 0; j < animationData.Model.sequences.Length; ++j)
                    // {
                    //     if (animationData.Model.sequences[j].id == animationData.SetNewAnimation)
                    //     {
                    //         found = true;
                    //         animationData._animInternalIndex = j;
                    //         animationData._length = animationData.Model.sequences[j].duration;
                    //         break;
                    //     }
                    // }

                    // if (!found)
                    //     
                    // else
                    // {                 
                    // }
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
}