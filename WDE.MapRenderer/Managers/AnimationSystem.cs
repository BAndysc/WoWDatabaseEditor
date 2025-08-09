using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Interfaces;
using TheEngine.Managers;
using TheEngine.Utils;
using TheMaths;
using WDE.Common.Database;
using WDE.MapRenderer.Managers.Entities;
using WDE.Module.Attributes;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MapRenderer.Managers;

public class AnimationSystem
{
    private Stopwatch sw = new();
    public static int MAX_BONES = 8192; // Creature\SlimeGiant\GiantSlime.M2 has 312 bones, but CREATURE\DRUIDTREEFORM\DRUIDTREEFORM.M2 is 723. Not sure what is max now. Let's use some really high value for now.
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
    private static Vector4[] staticIdentityColors = new Vector4[MAX_BONES];
    static AnimationSystem()
    {
        for (int i = 0; i < MAX_BONES; ++i)
        {
            staticIdentityBones[i] = Matrix.Identity;
            staticIdentityColors[i] = Vector4.One;
        }
    }

    public static Memory<Vector4> IdentityColors(int count) => staticIdentityColors.AsMemory(0, count);

    public static Memory<Matrix> IdentityMatrix(int count) => staticIdentityBones.AsMemory(0, count);
    
    public AnimationSystem(Archetypes archetypes,
        ICameraManager cameraManager,
        EmoteStore emoteStore,
        AnimationDataStore animationDataStore,
        IUIManager uiManager,
        EntityInspector entityInspector,
        M2AnimationComponentInspector m2Inspector)
    {
        this.archetypes = archetypes;
        this.cameraManager = cameraManager;
        this.emoteStore = emoteStore;
        this.animationDataStore = animationDataStore;
        entityInspector.RegisterInspectorDrawer(m2Inspector);
    }

    private static T Get<T>(int IDX, ref readonly M2Track<T> track, T @default, float t, Func<T, T, float, T> lerp)
    {
        if (track.Length <= IDX)
            return @default;
        ref readonly var values = ref track.Values(IDX);
        if (values.Length == 0)
            return @default;
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
        return lerp(prev, nextValue, pct);
    }
    
    private static T GetFirstOrDefault<T>(int IDX, MutableM2Track<T> track, T def, float t, Func<T, T, float, T> lerp)
    {
        if (track.Length == 0)
            return def;
        if (track.Length <= IDX)
            IDX = 0;
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
        return lerp(prev, nextValue, pct);
    }

    private static int FirstIndexGreaterThan(in M2Array<uint> array, uint target)
    {
        if (target >= array[^1])
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

    private static int FirstIndexGreaterThan(in MutableM2Array<uint> array, uint target)
    {
        if (target >= array[^1])
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

    private static Matrix GetBoneMatrixWithoutParent(M2 m2, int boneIndex, float t, int IDX, ref readonly LocalToWorld objectMatrix, ref readonly Matrix cameraViewMatrix)
    {
        Matrix boneMatrix;
        ref readonly var boneData = ref m2.bones[boneIndex];

        if ((boneData.flags & M2CompBoneFlag.transformed) != 0)// || true)
        {
            var position = GetFirstOrDefault(IDX, boneData.translation, Vector3.Zero, t, Vector3.Lerp);
            var scaling = GetFirstOrDefault(IDX, boneData.scale, Vector3.One, t, Vector3.Lerp);
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
        if ((boneData.flags & M2CompBoneFlag.spherical_billboard) != 0)
        {
            Matrix4x4.Invert(cameraViewMatrix, out var cam);
            var cameraUp = Vector3.TransformNormal(new Vector3(-1, 0, 0), cam);
            var cameraForward = Vector3.TransformNormal(new Vector3(0, 0, -1), cam);

            boneMatrix = Matrix.CreateBillboard(objectMatrix.Position, cam.Translation, cameraUp, cameraForward);
            // Extract the camera's orientation
            //Vector3 forward = new Vector3(cam.M11, cam.M12, cam.M13);               // X
            //Vector3 right   = -new Vector3(cam.M21, cam.M22, cam.M23);              // -Y
            //Vector3 up      = new Vector3(cam.M31, cam.M32, cam.M33);               // Z

// Optionally: Flip forward if needed (depends on winding order)
// camForward = -camForward;

// Build rotation matrix (camera-facing)
            // boneMatrix = new Matrix4x4(
            //     forward.X, right.X, up.X, 0,
            //     forward.Y, right.Y, up.Y, 0,
            //     forward.Z, right.Z, up.Z, 0,
            //     0,         0,       0,    1
            // );
        }
        else if ((boneData.flags & M2CompBoneFlag.cylindrical_billboard_lock_x) != 0)
        {
            // Lock X-axis, billboard around X (face camera in YZ plane)
            var cameraForward = new Vector3(cameraViewMatrix.M13, cameraViewMatrix.M23, cameraViewMatrix.M33);
            var projectedForward = new Vector3(0, cameraForward.Y, cameraForward.Z);
            if (projectedForward.LengthSquared() > 0.001f)
            {
                projectedForward = Vector3.Normalize(projectedForward);
                var angle = MathF.Atan2(projectedForward.Z, projectedForward.Y);
                boneMatrix *= Matrix.CreateRotationX(angle);
            }
        }
        else if ((boneData.flags & M2CompBoneFlag.cylindrical_billboard_lock_y) != 0)
        {
            // Lock Y-axis, billboard around Y (face camera in XZ plane)
            var cameraForward = new Vector3(cameraViewMatrix.M13, cameraViewMatrix.M23, cameraViewMatrix.M33);
            var projectedForward = new Vector3(cameraForward.X, 0, cameraForward.Z);
            if (projectedForward.LengthSquared() > 0.001f)
            {
                projectedForward = Vector3.Normalize(projectedForward);
                var angle = MathF.Atan2(-projectedForward.X, -projectedForward.Z);
                boneMatrix *= Matrix.CreateRotationY(angle);
            }
        }
        else if ((boneData.flags & M2CompBoneFlag.cylindrical_billboard_lock_z) != 0)
        {
            // Lock Z-axis, billboard around Z (face camera in XY plane)
            var cameraForward = new Vector3(cameraViewMatrix.M13, cameraViewMatrix.M23, cameraViewMatrix.M33);
            var projectedForward = new Vector3(cameraForward.X, cameraForward.Y, 0);
            if (projectedForward.LengthSquared() > 0.001f)
            {
                projectedForward = Vector3.Normalize(projectedForward);
                var angle = MathF.Atan2(projectedForward.Y, projectedForward.X);
                boneMatrix *= Matrix.CreateRotationZ(angle);
            }
        }

        if (boneData.pivot.X != 0 || boneData.pivot.Y != 0 || boneData.pivot.Z != 0)
        {
            var mPivot = Matrix.CreateTranslation(boneData.pivot);
            var mInvPivot = Matrix.CreateTranslation(-boneData.pivot);
            boneMatrix = mInvPivot * boneMatrix * mPivot;
        }

        return boneMatrix;
    }
    
    private static Matrix GetBoneMatrix(M2 m2, Matrix[] calculatedBones, int boneIndex, float t, int IDX, ref readonly LocalToWorld objectMatrix, ref readonly Matrix cameraViewMatrix)
    {
        Matrix boneMatrix = GetBoneMatrixWithoutParent(m2, boneIndex, t, IDX, in objectMatrix, in cameraViewMatrix);
        ref readonly var boneData = ref m2.bones[boneIndex];
        
        if (boneData.parent_bone >= 0)
        {
            if (boneData.parent_bone > boneIndex)
            {
                Console.WriteLine("boneData.parent_bone > boneIndex");
                calculatedBones[boneData.parent_bone] = GetBoneMatrix(m2, calculatedBones, boneData.parent_bone, t, IDX, in objectMatrix, in cameraViewMatrix);
            }

            if ((boneData.flags & M2CompBoneFlag.ignoreParentTransformMask) == 0)
            {
                boneMatrix *= calculatedBones[boneData.parent_bone];
            }
            else
            {
                var parent = calculatedBones[boneData.parent_bone];
                var translation = parent.Translation;
                var rotation = parent.Rotation();
                var scale = parent.ScaleVector();
                parent = Utilities.TRS(
                    boneData.flags.HasFlagFast(M2CompBoneFlag.ignoreParentTranslate) ? default : translation,
                    boneData.flags.HasFlagFast(M2CompBoneFlag.ignoreParentRotation) ? default : rotation,
                    boneData.flags.HasFlagFast(M2CompBoneFlag.ignoreParentScale) ? Vector3.One : scale);
                boneMatrix *= parent;
            }
        }

        return boneMatrix;
    }
    
    // slower, but not additional array required
    private static Matrix GetBoneMatrixRecursive(M2 m2, int boneIndex, float t, int IDX, ref readonly LocalToWorld objectMatrix, ref readonly Matrix cameraViewMatrix)
    {
        Matrix boneMatrix = GetBoneMatrixWithoutParent(m2, boneIndex, t, IDX, in objectMatrix, in cameraViewMatrix);
        ref readonly var boneData = ref m2.bones[boneIndex];

        if (boneData.parent_bone >= 0)
        {
            if ((boneData.flags & M2CompBoneFlag.ignoreParentTransformMask) == 0)
            {
                boneMatrix *= GetBoneMatrixRecursive(m2, boneData.parent_bone, t, IDX, in objectMatrix, in cameraViewMatrix);
            }
            else
            {
                var parent = GetBoneMatrixRecursive(m2, boneData.parent_bone, t, IDX, in objectMatrix, in cameraViewMatrix);
                var translation = parent.Translation;
                var rotation = parent.Rotation();
                var scale = parent.ScaleVector();
                parent = Utilities.TRS(
                    boneData.flags.HasFlagFast(M2CompBoneFlag.ignoreParentTranslate) ? default : translation,
                    boneData.flags.HasFlagFast(M2CompBoneFlag.ignoreParentRotation) ? default : rotation,
                    boneData.flags.HasFlagFast(M2CompBoneFlag.ignoreParentScale) ? Vector3.One : scale);
                boneMatrix *= parent;
            }
        }

        return boneMatrix;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AnimationTickTime(float delta, M2AnimationComponentData animationData)
    {
        if (animationData._currentAnimation == -1 || animationData._length == 0)
            return false;

        if ((animationData.Flags & AnimationDataFlags.FallbackPlayBackwards) != 0)
            animationData._time -= delta * animationData.SpeedModifier;
        else
            animationData._time += delta * animationData.SpeedModifier;

        if (animationData.SetNewOneShotAnimation != 0 && animationData._time < 0 ||
            animationData._time > animationData._length)
        {
            // rethink
            animationData.SetNewOneShotAnimation = 0;
        }
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
        int? switchAnimation = null;
        if (animationData.SetNewOneShotAnimation > 0 && animationData._currentAnimation != animationData.SetNewOneShotAnimation)
        {
            switchAnimation = animationData.SetNewOneShotAnimation;
        }
        if (animationData.SetNewOneShotAnimation == 0 && animationData._currentAnimation != animationData.SetNewAnimation)
        {
            switchAnimation = animationData.SetNewAnimation;
        }

        if (switchAnimation.HasValue)
        {
            int? lookup = animationData.Model.GetAnimationIndexByAnimationId(switchAnimation.Value);
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
                animationData._currentAnimation = switchAnimation.Value;
                animationData._time = 0;

                animationData.Model.bones.LoadAnimation(animationData._animInternalIndex);
                return true;
            }
        }

        return false;
    }

    private static Vector3 pivotPoint = new(0.5f, 0.5f, 0);

    private static Matrix AnimationTextureTransform(
        int animationIndex,
        float time,
        // ref M2Array<uint> globalLoops,
        // ref List<AnimTime> globalSequenceTimes,
        ref readonly M2TextureTransform textureTransform
    )
    {
        Matrix transformMat = Matrix.Identity;
        if (textureTransform.rotation.Length > 0)
        {
            Quaternion quaternionResult = GetFirstOrDefault<Quaternion>(animationIndex,
                textureTransform.rotation,
                Quaternion.Identity,
                time,
                Quaternion.Slerp
            );

            transformMat *= Matrix.CreateTranslation(pivotPoint);
            transformMat *= Matrix.CreateFromQuaternion(quaternionResult);
            transformMat *= Matrix.CreateTranslation(-pivotPoint);
        }

        if (textureTransform.scaling.Length > 0)
        {
            Vector3 scaleResult = GetFirstOrDefault(
                animationIndex,
                textureTransform.scaling,
                Vector3.One,
                time,
                Vector3.Lerp
            );

            transformMat *= Matrix.CreateTranslation(pivotPoint);
            transformMat *= Matrix.CreateScale(scaleResult);
            transformMat *= Matrix.CreateTranslation(-pivotPoint);
        }

        if (textureTransform.translation.Length > 0)
        {
            Vector3 transVec = GetFirstOrDefault(
                animationIndex,
                textureTransform.translation,
                Vector3.Zero,
                time,
                Vector3.Lerp
            );

            transformMat *= Matrix4x4.CreateTranslation(transVec);
        }
        return transformMat;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AnimationCalculateBones(int bonesLength, M2AnimationComponentData animationData, Matrix4x4[] localBones, ref readonly LocalToWorld objectMatrix, ref readonly Matrix cameraViewMatrix)
    {
        for (int j = 0; j < bonesLength; ++j)
        {
            var boneMatrix = GetBoneMatrix(animationData.Model, localBones, j, animationData._time,
                animationData._animInternalIndex, in objectMatrix, in cameraViewMatrix);
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
                        attachedTo._animInternalIndex, in objectMatrix, in cameraViewMatrix);
                    for (int j = 0; j < bonesLength; ++j)
                        localBones[j] *= Matrix.CreateTranslation(offset) * parentMatrix;
                }

                attachmentType = attachedTo.AttachmentType ?? M2AttachmentType.ItemVisual0;
                attachedTo = attachedTo.AttachedTo;
            }
        }
    }

    public static bool ManualAnimationStep(float delta, M2AnimationComponentData animationData, ref readonly LocalToWorld objectMatrix, ref readonly Matrix cameraViewMatrix)
    {
        UpdateAnimationData(animationData);
        if (!AnimationTickTime(delta, animationData)) 
            return false;
        
        var bonesLength = animationData.Model.bones.Length;
        var localBones = ArrayPool<Matrix>.Shared.Rent(bonesLength);
        
        AnimationCalculateBones(bonesLength, animationData, localBones, in objectMatrix, in cameraViewMatrix);
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
        ThreadLocal<List<(NativeBuffer<Vector4>, int, Vector4[])>> updateColors = new ThreadLocal<List<(NativeBuffer<Vector4>, int, Vector4[])>>(() => new(), true);

        var cameraViewMatrix = cameraManager.MainCamera.ViewMatrix;

        archetypes.AnimatedEntityArchetype.ParallelForEachRRROOO<RenderEnabledBit, LocalToWorld, M2AnimationComponentData, MeshBounds, DirtyPosition, WorldMeshBounds>((itr, thread, start, end, renderEnabledAccess, localToWorldAccess, animationAccess, meshBoundsAccess, dirtPositionAccess, worldMeshBounds) =>
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

                ref var objectMatrix = ref localToWorldAccess[i];

                AnimationCalculateBones(bonesLength, animationData, localBones, in objectMatrix, in cameraViewMatrix);

                updates.Value!.Add((animationData._buffer, bonesLength, localBones));
                //animationData._buffer.UpdateBuffer(localBones.AsSpan(0, bonesLength));

                var localColors = ArrayPool<Vector4>.Shared.Rent(animationData.Model.colors.Length);
                for (int colorIndex = 0; colorIndex < animationData.Model.colors.Length; ++colorIndex)
                {
                    var color = Get(animationData._animInternalIndex, in animationData.Model.colors[colorIndex].color, Vector3.One, animationData._time, Vector3.Lerp);
                    var alpha = Get(animationData._animInternalIndex, in animationData.Model.colors[colorIndex].alpha, Fixed16.One, animationData._time, Fixed16.Lerp);
                    localColors[colorIndex] = new Vector4(color.X, color.Y, color.Z, alpha.Value);
                }

                updateColors.Value!.Add((animationData._colors, animationData.Model.colors.Length, localColors));
                var textureTransforms = ArrayPool<Matrix>.Shared.Rent(animationData.Model.texture_transforms.Length + 1);
                for (int transformIndex = 0; transformIndex < animationData.Model.texture_transforms.Length; ++transformIndex)
                {
                    textureTransforms[transformIndex] = AnimationTextureTransform(animationData._animInternalIndex, animationData._time,
                        in animationData.Model.texture_transforms[transformIndex]);
                }
                textureTransforms[animationData.Model.texture_transforms.Length] = Matrix.Identity; // last one is always identity
                updates.Value!.Add((animationData._textureTransforms, animationData.Model.texture_transforms.Length + 1, textureTransforms));
            }
            counter.Value += sum;
        });
        foreach (var tuple in updates.Values.SelectMany(x => x))
        {
            tuple.Item1.UpdateBuffer(tuple.Item3.AsSpan(0, tuple.Item2));
            ArrayPool<Matrix>.Shared.Return(tuple.Item3);
        }
        foreach (var tuple in updateColors.Values.SelectMany(x => x))
        {
            tuple.Item1.UpdateBuffer(tuple.Item3.AsSpan(0, tuple.Item2));
            ArrayPool<Vector4>.Shared.Return(tuple.Item3);
        }
        sw.Stop();
    }

    public M2AnimationType? GetAnimationType(M2? model, uint? emoteState, uint? standState, AnimTier? animTier)
    {
        M2AnimationType? animationId = null;
        if (emoteState.HasValue && emoteState.Value != 0 && emoteStore.TryGetValue(emoteState.Value, out var emote))
        {
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
