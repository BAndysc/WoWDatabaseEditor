using System.Collections;
using TheMaths;
using WDE.MpqReader.Readers;

namespace WDE.MpqReader.Structures;

public class M2CompBoneArray
{
    private readonly M2CompBone[] array;
    private readonly Func<int, IBinaryReader?> externalAnimOpener;
    private readonly BitArray loadedAnims;
    public int Length => array.Length;

    public void LoadAnimation(int animIndex)
    {
        if (loadedAnims.Length <= animIndex || loadedAnims[animIndex])
            return;
        
        var opener = externalAnimOpener(animIndex);
        if (opener == null)
        {
            for (int boneIndex = 0; boneIndex < array.Length; ++boneIndex)
            {
                if (array[boneIndex].translation.Length > animIndex)
                {
                    array[boneIndex].translation.Timestamps(animIndex) = new MutableM2Array<uint>(0, 0, Array.Empty<uint>());
                    array[boneIndex].translation.Values(animIndex) = new MutableM2Array<Vector3>(0, 0, Array.Empty<Vector3>());
                }

                if (array[boneIndex].rotation.Length > animIndex)
                {
                    array[boneIndex].rotation.Timestamps(animIndex) = new MutableM2Array<uint>(0, 0, Array.Empty<uint>());
                    array[boneIndex].rotation.Values(animIndex) = new MutableM2Array<M2CompQuat>(0, 0, Array.Empty<M2CompQuat>());
                }

                if (array[boneIndex].scale.Length > animIndex)
                {
                    array[boneIndex].scale.Timestamps(animIndex) = new MutableM2Array<uint>(0, 0, Array.Empty<uint>());
                    array[boneIndex].scale.Values(animIndex) = new MutableM2Array<Vector3>(0, 0, Array.Empty<Vector3>());
                }
            }
        }
        else
        {
            for (int boneIndex = 0; boneIndex < array.Length; ++boneIndex)
            {
                if (array[boneIndex].translation.Length > animIndex)
                {
                    array[boneIndex].translation.Timestamps(animIndex).LoadContent(opener, r => r.ReadUInt32());
                    array[boneIndex].translation.Values(animIndex).LoadContent(opener, r => r.ReadVector3());
                }

                if (array[boneIndex].rotation.Length > animIndex)
                {
                    array[boneIndex].rotation.Timestamps(animIndex).LoadContent(opener, r => r.ReadUInt32());
                    array[boneIndex].rotation.Values(animIndex).LoadContent(opener, M2CompQuat.Read);
                }

                if (array[boneIndex].scale.Length > animIndex)
                {
                    array[boneIndex].scale.Timestamps(animIndex).LoadContent(opener, r => r.ReadUInt32());
                    array[boneIndex].scale.Values(animIndex).LoadContent(opener, r => r.ReadVector3());
                }
            }
        }
        loadedAnims[animIndex] = true;
    }
    
    public M2CompBoneArray(IBinaryReader reader, in M2Array<M2Sequence> sequences, Func<int, IBinaryReader?> externalAnimOpener)
    {
        this.externalAnimOpener = externalAnimOpener;
        var size = reader.ReadInt32();
        var offset = reader.ReadInt32();
        var currentOffset = reader.Offset;

        loadedAnims = new BitArray(sequences.Length);
        for (int i = 0; i < sequences.Length; ++i)
            loadedAnims[i] = sequences[i].flags.HasFlagFast(M2SequenceFlags.HasEmbeddedAnimationData);
        
        reader.Offset = offset;
        array = new M2CompBone[size];

        for (int i = 0; i < size; ++i)
        {
            array[i] = new M2CompBone(reader, loadedAnims);
        }

        reader.Offset = currentOffset;
    }

    public ref readonly M2CompBone this[int boneIndex] => ref array[boneIndex];
}