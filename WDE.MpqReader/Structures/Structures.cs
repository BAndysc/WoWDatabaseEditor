using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using TheMaths;
using WDE.Common.MPQ;
using WDE.MpqReader.Readers;

namespace WDE.MpqReader.Structures
{
    [Flags]
    public enum M2Flags
    {
        FLAG_TILT_X = 1,
        FLAG_TILT_Y = 2,
        UNK0 = 4,
        FLAG_USE_TEXTURE_COMBINER_COMBOS = 8,
        LOAD_PHYS_DATA = 32,
        TEXTURE_TRANSFORMS_USE_BONE_SEQUENCES = 2048,
        CHUNKED_ANIM_FILES = 0x2000,
        CHUNKED_ANIM_FILES_2 = 0x200000
    }

    public class M2
    {
        public uint magic { get; init; }                                       // "MD20". Legion uses a chunked file format starting with MD21.
        public uint version { get; init; }
        // public M2Array<char> name { get; init; }                                   // should be globally unique, used to reload by name in internal clients
        public M2Flags global_flags { get; init; }
/*0x014*/  //public readonly M2Array<uint> global_loops;                        // Timestamps used in global looping animations.
/*0x01C*/  public readonly M2Array<M2Sequence> sequences;                       // Information about the animations in the model.
/*0x024*/  public readonly M2Array<short> sequenceIdToAnimationId;               // Mapping of sequence IDs to the entries in the Animation sequences block.
           public readonly short[] sequenceIdToAnimationLookup; // custom lookup table, because the algorithm for sequenceIdToAnimationId is not always correct
/*0x02C*/  public readonly M2CompBoneArray bones;                           // MAX_BONES = 0x100 => Creature\SlimeGiant\GiantSlime.M2 has 312 bones (Wrath)
/*0x034*/  public readonly M2Array<ushort> boneIndicesById;                   //Lookup table for key skeletal bones. (alt. name: key_bone_lookup)
/*0x03C*/  public readonly M2Array<M2Vertex> vertices;
/*0x044*/  public readonly uint num_skin_profiles;                           // Views (LOD) are now in .skins.
/*0x048*/  public readonly M2Array<M2Color> colors;                             // Color and alpha animations definitions.
/*0x050*/  public readonly M2Array<M2Texture> textures;
/*0x058*/  public readonly M2Array<M2TextureWeight> textureWeights;            // Transparency of textures.
/*0x060*/  public readonly M2Array<M2TextureTransform> texture_transforms;
/*0x068*/  public readonly M2Array<ushort> textureIndicesById;                // (alt. name: replacable_texture_lookup)
/*0x070*/  public readonly M2Array<M2Material> materials;                       // Blending modes / render flags.
/*0x078*/  // public readonly M2Array<ushort> boneCombos;                        // (alt. name: bone_lookup_table)
/*0x080*/  public readonly M2Array<short> textureLookupTable;                     // (alt. name: texture_lookup_table)
/*0x088*/  public readonly M2Array<ushort> textureUnitLookupTable;           // (alt. name: tex_unit_lookup_table)
/*0x090*/  public readonly M2Array<ushort> textureTransparencyLookupTable;               // (alt. name: transparency_lookup_table)
/*0x098*/  public readonly M2Array<ushort> textureUVAnimationLookup;            // (alt. name: texture_transforms_lookup_table)
/*0x0A0*/  public readonly CAaBox bounding_box;                                 // min/max( [1].z, 2.0277779f ) - 0.16f seems to be the maximum camera height
/*0x0B8*/  public readonly float bounding_sphere_radius;                         // detail doodad draw dist = clamp (bounding_sphere_radius * detailDoodadDensityFade * detailDoodadDist, …)
/*0x0BC*/  public readonly CAaBox collision_box;
/*0x0D4*/  public readonly float collision_sphere_radius;
/*0x0D8*/  public readonly M2Array<ushort> collisionIndices;                    // (alt. name: collision_triangles)
/*0x0E0*/  public readonly M2Array<Vector3> collisionPositions;                  // (alt. name: collision_vertices)
/*0x0E8*/  public readonly M2Array<Vector3> collisionFaceNormals;                // (alt. name: collision_normals) 
/*0x0F0*/  public readonly M2Array<M2Attachment> attachments;                     // position of equipped weapons or effects
/*0x0F8*/  public readonly M2Array<ushort> attachmentIndicesById;               // (alt. name: attachment_lookup_table)
/*0x100*/  //public readonly M2Array<M2Event> events;                               // Used for playing sounds when dying and a lot else.
/*0x108*/  //public readonly M2Array<M2Light> lights;                               // Lights are mainly used in loginscreens but in wands and some doodads too.
/*0x110*/  //public readonly M2Array<M2Camera> cameras;                             // The cameras are present in most models for having a model in the character tab. 
/*0x118*/  //public readonly M2Array<ushort> cameraIndicesById;                   // (alt. name: camera_lookup_table)
/*0x120*/  //public readonly M2Array<M2Ribbon> ribbon_emitters;                     // Things swirling around. See the CoT-entrance for light-trails.
        ///*0x128*/  M2Array<M2Particleⁱ> particle_emitters { get; init; }
           public M2Array<ushort>? textureCombinerCombos { get; init; }
        public readonly FileId skinFileId;

        public int? GetAnimationIndexByAnimationId(int anim_id)
        {
            if (sequenceIdToAnimationId.Length == 0 && anim_id == 0 && sequences.Length >= 1)
                return 0;
            
            if (anim_id == -1 || sequenceIdToAnimationId.Length == 0)
                return null;

            if (anim_id >= sequenceIdToAnimationLookup.Length)
                return null;

            return sequenceIdToAnimationLookup[anim_id];
            
            // this algorithm doesn't always work, it needs to be researched more
            // int i = anim_id % sequenceIdToAnimationId.Length;
            //
            // for (int stride = 1; true; ++stride)
            // {
            //     if (sequenceIdToAnimationId[i] == -1)
            //     {
            //         return null;
            //     }
            //     if (sequences[sequenceIdToAnimationId[i]].id == anim_id)
            //     {
            //         return sequenceIdToAnimationId[i];
            //     }
            //
            //     i = (i + stride * stride) % sequenceIdToAnimationId.Length;
            //     // so original_i + 1, original_i + 1 + 4, original_i + 1 + 4 + 9, …
            // }
            // unreachable
        }

        private M2(IBinaryReader reader, GameFilesVersion wowVersion, FileId path, Func<FileId, IBinaryReader?> opener)
        {
            Dictionary<(ushort, ushort), uint>? animSubAnimToFileId = null;
            FileId[]? boneFileIds = null;
            FileId? skelFileId = null;
            magic = reader.ReadUInt32();
            if (magic != 0x3032444D)
            {
                int md21Offset = 0;
                int md21Size = 0;
                reader.Offset -= 4;
                while (!reader.IsFinished())
                {
                    var chunkName = reader.ReadChunkNameReversed();
                    var size = reader.ReadInt32();
                    
                    var offset = reader.Offset;
                    if (chunkName == "MD21")
                    {
                        md21Offset = offset;
                        md21Size = size;
                    }
                    else if (chunkName == "SFID")
                    {
                        skinFileId = reader.ReadUInt32();
                    }
                    else if (chunkName == "SKID")
                    {
                        skelFileId = reader.ReadUInt32();
                        throw new Exception("Skel files not supported");
                    }
                    else if (chunkName == "BFID")
                    {
                        boneFileIds = new FileId[size/4];
                        int i = 0;
                        var partialReader = new LimitedReader(reader, size);
                        while (!partialReader.IsFinished())
                        {
                            var fileId = partialReader.ReadUInt32();
                            boneFileIds[i++] = fileId;
                        }
                    }
                    else if (chunkName == "AFID")
                    {
                        animSubAnimToFileId = new();
                        var partialReader = new LimitedReader(reader, size);
                        while (!partialReader.IsFinished())
                        {
                            var animId = partialReader.ReadUInt16();
                            var subAnimId = partialReader.ReadUInt16();
                            var fileId = partialReader.ReadUInt32();
                            if (fileId != 0)
                                animSubAnimToFileId[(animId, subAnimId)] = fileId;
                        }
                    }
                    reader.Offset = offset + size;
                }

                reader.Offset = md21Offset;
                reader = new LimitedReader(reader, md21Size);
                reader.Offset += 4;
            }
            version = reader.ReadUInt32();
            reader.SkipM2Array(); // name = reader.ReadArray(r => (char)r.ReadByte());
            global_flags = (M2Flags)reader.ReadUInt32();
            reader.SkipM2Array(); // global_loops = reader.ReadArrayUInt32();
            sequences = reader.ReadArray(r => new M2Sequence(r));
            var maxAnimId = 0;
            for (int i = sequences.Length - 1; i >= 0; --i)
                maxAnimId = (short)Math.Max(maxAnimId, sequences[i].id);
            sequenceIdToAnimationLookup = new short[maxAnimId + 1];
            for (int i = sequences.Length - 1; i >= 0; --i)
                sequenceIdToAnimationLookup[sequences[i].id] = (short)i;
            sequenceIdToAnimationId = reader.ReadArrayInt16();
            Func<int, IBinaryReader?> openAnimFile = (idx) =>
            {
                var animId = sequences[idx].id;
                var animVariation = sequences[idx].variationIndex;
                FileId animPath;
                if (path.FileType == FileId.Type.FileName)
                {
                    animPath = Path.ChangeExtension(path.FileName, null);
                    animPath = animPath + animId.ToString().PadLeft(4, '0') + "-" + animVariation.ToString().PadLeft(2, '0') +".anim";
                }
                else
                {
                    if (!animSubAnimToFileId!.TryGetValue((animId, animVariation), out var fileId))
                        return null;
                    animPath = fileId;
                }
                var contentReader = opener(animPath);
                if (contentReader == null)
                    return null;
                
                if (global_flags.HasFlagFast(M2Flags.CHUNKED_ANIM_FILES_2))
                {
                    while (!contentReader.IsFinished())
                    {
                        var chunkName = contentReader.ReadChunkName();
                        var size = contentReader.ReadInt32();
                        if (chunkName == "2MFA")
                            return new LimitedReader(contentReader, size);
                        contentReader.Offset += size;
                    }
                    Debug.Assert(false, "Couldn't find chunk AFM2 in anim file");
                }
                return contentReader;
            };
            bones = new M2CompBoneArray(reader, in sequences, openAnimFile);
            boneIndicesById = reader.ReadArrayUInt16();
            vertices = reader.ReadArray(M2Vertex.Read);
            num_skin_profiles = reader.ReadUInt32();
            colors = reader.ReadArray(r => new M2Color(r));
            textures = reader.ReadArray(M2Texture.Read);
            textureWeights = reader.ReadArray(M2TextureWeight.Read);
            texture_transforms = reader.ReadArray(M2TextureTransform.Read);
            textureIndicesById = reader.ReadArrayUInt16();
            materials = reader.ReadArray(M2Material.Read);
            reader.SkipM2Array(); //boneCombos = reader.ReadArrayUInt16();
            textureLookupTable = reader.ReadArrayInt16();
            textureUnitLookupTable = reader.ReadArrayUInt16();
            textureTransparencyLookupTable = reader.ReadArrayUInt16();
            textureUVAnimationLookup = reader.ReadArrayUInt16();
            bounding_box = CAaBox.Read(reader);
            bounding_sphere_radius = reader.ReadFloat();
            collision_box = CAaBox.Read(reader);
            collision_sphere_radius = reader.ReadFloat();
            collisionIndices = reader.ReadArrayUInt16();
            collisionPositions = reader.ReadArrayVector3();
            collisionFaceNormals = reader.ReadArrayVector3();
            attachments = reader.ReadArray(M2Attachment.Read);
            attachmentIndicesById = reader.ReadArrayUInt16();
            reader.SkipM2Array(); // events = reader.ReadArray(M2Event.Read);
            reader.SkipM2Array(); // lights = reader.ReadArray(M2Light.Read);
            reader.SkipM2Array(); // cameras = reader.ReadArray(M2Camera.Read);
            reader.SkipM2Array(); // cameraIndicesById = reader.ReadArrayUInt16();
            reader.SkipM2Array(); // ribbon_emitters = reader.ReadArray(M2Ribbon.Read);
            reader.SkipM2Array(); // particles
            textureCombinerCombos = (global_flags & M2Flags.FLAG_USE_TEXTURE_COMBINER_COMBOS) != 0 ? reader.ReadArrayUInt16() : null;
        }

        public static M2 Read(IBinaryReader reader, GameFilesVersion version, FileId path, Func<FileId, IBinaryReader?> opener)
        {
            return new M2(reader, version, path, opener);
        }
    }

    public readonly struct CAaBox
    {
        public readonly Vector3 min;
        public readonly Vector3 max;

        private CAaBox(Vector3 minimum, Vector3 maximum)
        {
            this.max = maximum;
            this.min = minimum;
        }

        public static CAaBox Read(IBinaryReader binaryReader)
        {
            return new CAaBox(binaryReader.ReadVector3(), binaryReader.ReadVector3());
        }
    };

    public readonly struct M2Bounds {
        public readonly CAaBox extent;
        public readonly float radius;

        private M2Bounds(CAaBox extent, float radius)
        {
            this.extent = extent;
            this.radius = radius;
        }

        public static M2Bounds Read(IBinaryReader binaryReader)
        {
            return new M2Bounds(CAaBox.Read(binaryReader), binaryReader.ReadFloat());
        }
    };

    public readonly struct M2SplineKey<T> 
    {
        public readonly T value;
        public readonly T inTan;
        public readonly T outTan;

        public M2SplineKey(T value, T inTan, T outTan)
        {
            this.value = value;
            this.inTan = inTan;
            this.outTan = outTan;
        }
    }

    public readonly struct M2Range 
    {
        public readonly uint minimum;
        public readonly uint maximum;

        private M2Range(uint minimum, uint maximum)
        {
            this.maximum = maximum;
            this.minimum = minimum;
        }

        public static M2Range Read(IBinaryReader binaryReader)
        {
            return new M2Range(binaryReader.ReadUInt32(), binaryReader.ReadUInt32());
        }
    };

    [Flags]
    public enum M2CompBoneFlag
    {
        ignoreParentTranslate = 0x1,
        ignoreParentScale = 0x2,
        ignoreParentRotation = 0x4,
        spherical_billboard = 0x8,
        cylindrical_billboard_lock_x = 0x10,
        cylindrical_billboard_lock_y = 0x20,
        cylindrical_billboard_lock_z = 0x40,
        transformed = 0x200,
        kinematic_bone = 0x400,       // MoP+: allow physics to influence this bone
        helmet_anim_scaled = 0x1000,  // set blend_modificator to helmetAnimScalingRec.m_amount for this bone
        something_sequence_id = 0x2000, // <=bfa+, parent_bone+submesh_id are a sequence id instead?!
    }

    public enum M2AttachmentType
    {
        ItemVisual0 = 0,
        MountMain = 0,
        Shield = 0,
        HandRight = 1,
        ItemVisual1 = 1,
        HandLeft = 2,
        ItemVisual2 = 2,
        ElbowRight = 3,
        ItemVisual3 = 3,
        ElbowLeft = 4,
        ItemVisual4 = 4,
        ShoulderRight = 5,
        ShoulderLeft = 6,
        KneeRight = 7,
        KneeLeft = 8,
        HipRight = 9,
        HipLeft = 10,
        Helm = 11,
        Back = 12,
        ShoulderFlapRight = 13,
        ShoulderFlapLeft = 14,
        ChestBloodFront = 15,
        ChestBloodBack = 16,
        Breath = 17,
        PlayerName = 18,
        Base = 19,
        Head = 20,
        SpellLeftHand = 21,
        SpellRightHand = 22,
        Special1 = 23,
        Special2 = 24,
        Special3 = 25,
        SheathMainHand = 26,
        SheathOffHand = 27,
        SheathShield = 28,
        PlayerNameMounted = 29,
        LargeWeaponLeft = 30,
        LargeWeaponRight = 31,
        HipWeaponLeft = 32,
        HipWeaponRight = 33,
        Chest = 34,
        HandArrow = 35,
        Bullet = 36,
        SpellHandOmni = 37,
        SpellHandDirected = 38,
        VehicleSeat1 = 39,
        VehicleSeat2 = 40,
        VehicleSeat3 = 41,
        VehicleSeat4 = 42,
        VehicleSeat5 = 43,
        VehicleSeat6 = 44,
        VehicleSeat7 = 45,
        VehicleSeat8 = 46,
        LeftFoot = 47,
        RightFoot = 48,
        ShieldNoGlove = 49,
        SpineLow = 50,
        AlteredShoulderR = 51,
        AlteredShoulderL = 52,
        BeltBuckle = 53,
        SheathCrossbow = 54,
        HeadTop = 55,
        VirtualSpellDirected = 57,
        Unknown = 60
    }

    public readonly struct M2Texture
    {
        [Flags]
        public enum Flags : uint
        {
            TextureWrapX = 0x1,
            TextureWrapY = 0x2
        }

        public enum TextureType : uint
        {
            None = 0,
            TEX_COMPONENT_SKIN = 1,
            TEX_COMPONENT_OBJECT_SKIN = 2,
            TEX_COMPONENT_WEAPON_BLADE = 3,
            TEX_COMPONENT_WEAPON_HANDLE = 4,
            TEX_COMPONENT_ENVIRONMENT = 5,
            TEX_COMPONENT_CHAR_HAIR = 6,
            TEX_COMPONENT_CHAR_FACIAL_HAIR = 7,
            TEX_COMPONENT_SKIN_EXTRA = 8,
            TEX_COMPONENT_UI_SKIN = 9,
            TEX_COMPONENT_TAUREN_MANE = 10,
            TEX_COMPONENT_MONSTER_1 = 11,
            TEX_COMPONENT_MONSTER_2 = 12,
            TEX_COMPONENT_MONSTER_3 = 13,
            TEX_COMPONENT_ITEM_ICON = 14
        }

        public readonly TextureType type;          // see below
        public readonly Flags flags;          // see below
        public readonly M2Array<char> filename;  // for non-hardcoded textures (type != 0), this still points to a zero-byte-only string.

        private M2Texture(IBinaryReader reader)
        {
            type = (TextureType)reader.ReadUInt32();
            flags = (Flags)reader.ReadUInt32();
            filename = reader.ReadArray(r => (char)r.ReadByte());
        }

        public static M2Texture Read(IBinaryReader reader)
        {
            return new M2Texture(reader);
        }
    }

    public readonly struct M2TextureWeight
    {
        public readonly M2Track<Fixed16> weight;

        private M2TextureWeight(IBinaryReader binaryReader)
        {
            weight = M2Track<Fixed16>.Read(binaryReader, r => new Fixed16(r.ReadUInt16()));
        }

        public static M2TextureWeight Read(IBinaryReader binaryReader)
        {
            return new M2TextureWeight(binaryReader);
        }
    }

    public readonly struct M2TextureTransform
    {
        public readonly MutableM2Track<Vector3> translation;
        public readonly MutableM2Track<Quaternion> rotation;    // rotation center is texture center (0.5, 0.5)
        public readonly MutableM2Track<Vector3> scaling;

        public M2TextureTransform(IBinaryReader reader)
        {
            translation = MutableM2Track<Vector3>.Read(reader, r => r.ReadVector3());
            rotation = MutableM2Track<Quaternion>.Read(reader, r => r.ReadQuaternion());
            scaling = MutableM2Track<Vector3>.Read(reader, r => r.ReadVector3());
        }
        
        public static M2TextureTransform Read(IBinaryReader reader)
        {
            return new M2TextureTransform(reader);
        }
    }

    [Flags]
    public enum M2MaterialFlags
    {
        Unlit = 0x1,
        Unfogged = 0x2,
        TwoSided = 0x4,
        DepthTest = 0x8,
        DepthWrite = 0x10
    }

    public enum M2Blend
    {
        M2BlendOpaque = 0,
        M2BlendAlphaKey = 1,
        M2BlendAlpha = 2,
        M2BlendNoAlphaAdd = 3,
        M2BlendAdd = 4,
        M2BlendMod = 5,
        M2BlendMod2X = 6,
        M2BlendBlendAdd = 7
    }

    public readonly struct M2Material
    {
        public readonly M2MaterialFlags flags;
        public readonly M2Blend blending_mode; // apparently a bitfield

        public M2Material(IBinaryReader reader)
        {
            flags = (M2MaterialFlags)reader.ReadUInt16();
            blending_mode = (M2Blend)reader.ReadUInt16();
        }
        
        public static M2Material Read(IBinaryReader r)
        {
            return new M2Material(r);
        }
    }

    public readonly struct M2Attachment
    {
        public readonly M2AttachmentType id;                      // Referenced in the lookup-block below.
        public readonly short bone;                    // attachment base
        public readonly ushort unknown;                 // see BogBeast.m2 in vanilla for a model having values here
        public readonly Vector3 position;              // relative to bone; Often this value is the same as bone's pivot point 
        public readonly M2Track<char> animate_attached;  // whether or not the attached model is animated when this model is. only a bool is used. default is true.
    
        public M2Attachment(IBinaryReader reader)
        {
            id = (M2AttachmentType)reader.ReadUInt32();
            bone = reader.ReadInt16();
            unknown = reader.ReadUInt16();
            position = reader.ReadVector3();
            animate_attached = M2Track<char>.Read(reader, r => (char)r.ReadByte());
        }

        public static M2Attachment Read(IBinaryReader r)
        {
            return new M2Attachment(r);
        }
    }

    public class M2Event
    {
        public uint identifier { get; init; }  // mostly a 3 character name prefixed with '$'.
        public uint data { get; init; }       // This data is passed when the event is fired. 
        public uint bone { get; init; }        // Somewhere it has to be attached.
        public Vector3 position { get; init; }   // Relative to that bone of course, animated. Pivot without animating.
        public M2TrackBase enabled { get; init; }  // This is a timestamp-only animation block. It is built up the same as a normal AnimationBlocks, but is missing values, as every timestamp is an implicit "fire now".
    
        private M2Event(){}

        public static M2Event Read(IBinaryReader reader)
        {
            return new M2Event()
            {
                identifier = reader.ReadUInt32(),
                data = reader.ReadUInt32(),
                bone = reader.ReadUInt32(),
                position = reader.ReadVector3(),
                enabled = M2TrackBase.Read(reader)
            };
        }
    }

    public class M2Light
    {
        public ushort type { get; init; }                      // Types are listed below.
        public short bone { get; init; }                       // -1 if not attached to a bone
        public Vector3 position { get; init; }                 // relative to bone, if given
        public M2Track<Vector3> ambient_color { get; init; }
        public M2Track<float> ambient_intensity { get; init; }   // defaults to 1.0
        public M2Track<Vector3> diffuse_color { get; init; }
        public M2Track<float> diffuse_intensity { get; init; }   // defaults to 1.0
        public M2Track<float> attenuation_start { get; init; }
        public M2Track<float> attenuation_end { get; init; }
        public M2Track<byte> visibility { get; init; }        // enabled?

        private M2Light() {}

        public static M2Light Read(IBinaryReader reader)
        {
            return new M2Light()
            {
                type = reader.ReadUInt16(),
                bone = reader.ReadInt16(),
                position = reader.ReadVector3(),
                ambient_color = M2Track<Vector3>.ReadVector3(reader),
                ambient_intensity = M2Track<float>.ReadFloat(reader),
                diffuse_color = M2Track<Vector3>.ReadVector3(reader),
                diffuse_intensity = M2Track<float>.ReadFloat(reader),
                attenuation_start = M2Track<float>.ReadFloat(reader),
                attenuation_end = M2Track<float>.ReadFloat(reader),
                visibility = M2Track<byte>.Read(reader, r => r.ReadByte())
            };
        }
    }

    /*struct M2Particle
    {
        uint particleId;
        uint flags;

        Vector3 position;
        ushort bone;

        ushort texture;

        M2Array<char> geometryModelFileName;
        M2Array<char> recursionModelFileName;

        byte blendingType;
        byte emitterType;
        ushort particleColorIndex;

        byte particleType;
        byte headOrTail;

        ushort textureTileRotation;
        ushort textureDImensionsRows;
        ushort textureDImensionsColumns;

        M2Track<float> emissionSpeed;
        M2Track<float> speedVariation;
        M2Track<float> verticalRange;
        M2Track<float> horizontalRange;
        M2Track<float> gravity;
        M2Track<float> lifespan;
        float lifespanVariation;
        M2Track<float> emissionRate;
        float emissionRateVariation;
        M2Track<float> emissionAreaLength;
        M2Track<float> emissionAreaWidth;
        M2Track<float> zSource;

        FBlock<Vector3> colorTrack;
        FBlock<short> alphaTrack; // This is referred to as a "Fixed16" basically divide by 0x7FFF to get the f32 value
        FBlock<Vector2> scaleTrack;
        Vector2 scaleVariation;
        FBlock<ushort> headCellTrack;
        FBlock<ushort> tailCellTrack;

        float tailLength = 0;
        float twinkleSpeed = 0;
        float twinklePercent = 0;
        float twinkleScaleMin = 0;
        float twinkleScaleMax = 0;
        float burstMultiplier = 0;
        float drag = 0;
        float baseSpin = 0;
        float baseSpinVariation = 0;
        float spin = 0;
        float spinVariation = 0;

        CAaBox tumble;
        Vector3 windVector;
        float windTime;

        float followSpeed1;
        float followScale1;
        float followSpeed2;
        float followScale2;

        M2Array<Vector3> splinePoints;
        M2Track<byte> enabledIn;
    }*/
    
    public class M2Ribbon
    {
        public uint ribbonId { get; init; }                  // Always (as I have seen): -1.
        public uint boneIndex { get; init; }                 // A bone to attach to.
        public Vector3 position { get; init; }                 // And a position, relative to that bone.
        public M2Array<ushort> textureIndices { get; init; }   // into textures
        public M2Array<ushort> materialIndices { get; init; }  // into materials
        public M2Track<Vector3> colorTrack { get; init; }      // An RGB multiple for the material.[1]
        public M2Track<Fixed16> alphaTrack { get; init; }       // And an alpha value in a short, where: 0 - transparent, 0x7FFF - opaque.
        public M2Track<float> heightAboveTrack { get; init; }    // Above and Below – These fields define the width of a ribbon in units based on their offset from the origin.[1]
        public M2Track<float> heightBelowTrack { get; init; }    // do not set to same!
        public float edgesPerSecond { get; init; }               // this defines how smooth the ribbon is. A low value may produce a lot of edges. 
        // Edges/Sec – The number of quads generated.[1]
        public float edgeLifetime { get; init; }                 // the length aka Lifespan. in seconds 
        // Time in seconds that the quads stay around after being generated.[1]
        public float gravity { get; init; }                      // use arcsin(val) to get the emission angle in degree 
        // Can be positive or negative. Will cause the ribbon to sink or rise in the z axis over time.[1]
        public ushort textureRows { get; init; }               // tiles in texture
        public ushort textureCols { get; init; }               // Texture Rows and Cols – Allows an animating texture similar to BlizParticle. Set the number of rows and columns equal to the texture.[1]
        public M2Track<ushort> texSlotTrack { get; init; }     // Pick the index number of rows and columns, and animate this number to get a cycle.[1]
        public M2Track<char> visibilityTrack { get; init; }
        public ushort priorityPlane { get; init; } // TODO: verify version
        public sbyte RibbonColorIndex { get; init; }
        public sbyte textureTransformLookupIndex { get; init; } //Index into m2data.header.textureTransformCombos. Applied only if m2data.header.global_flags.flag_unk_0x20000 flag is set
    
        private M2Ribbon() {}

        public static M2Ribbon Read(IBinaryReader reader)
        {
            return new M2Ribbon()
            {
                ribbonId = reader.ReadUInt32(),
                boneIndex = reader.ReadUInt32(),
                position = reader.ReadVector3(),
                textureIndices = reader.ReadArrayUInt16(),
                materialIndices = reader.ReadArrayUInt16(),
                colorTrack = M2Track<Vector3>.Read(reader, r => r.ReadVector3()),
                alphaTrack = M2Track<Fixed16>.Read(reader, r => new Fixed16(r.ReadUInt16())),
                heightAboveTrack = M2Track<float>.Read(reader, r => r.ReadFloat()),
                heightBelowTrack = M2Track<float>.Read(reader, r => r.ReadFloat()),
                edgesPerSecond = reader.ReadFloat(),
                edgeLifetime = reader.ReadFloat(),
                gravity = reader.ReadFloat(),
                textureRows = reader.ReadUInt16(),
                textureCols = reader.ReadUInt16(),
                texSlotTrack = M2Track<ushort>.Read(reader, r => r.ReadUInt16()),
                visibilityTrack = M2Track<char>.Read(reader, r => (char)r.ReadByte()),
                priorityPlane = reader.ReadUInt16(),
                RibbonColorIndex = (sbyte)reader.ReadByte(),
                textureTransformLookupIndex = (sbyte)reader.ReadByte()
            };
        }
    }

    public class M2Camera
    {
        public uint type { get; init; } // 0: portrait, 1: characterinfo; -1: else (flyby etc.); referenced backwards in the lookup table.
        // only #if < Cata
        public float fov { get; init; } // Diagonal FOV in radians. See below for conversion.
        public float far_clip { get; init; }
        public float near_clip { get; init; }
        public M2Track<M2SplineKey<Vector3>> positions { get; init; } // How the camera's position moves. Should be 3*3 floats.
        public Vector3 position_base { get; init; }
        public M2Track<M2SplineKey<Vector3>> target_position { get; init; } // How the target moves. Should be 3*3 floats.
        public Vector3 target_position_base { get; init; }
        public M2Track<M2SplineKey<float>> roll { get; init; } // The camera can have some roll-effect. Its 0 to 2*Pi. 
        // only in #if ≥ Cata
        M2Track<M2SplineKey<float>> FoV; //Diagonal FOV in radians. See below for conversion.
    
        private M2Camera() {}

        public M2Camera(IBinaryReader reader, GameFilesVersion version)
        {
            type = reader.ReadUInt32();
            if (version <= GameFilesVersion.Wrath_3_3_5a)
                fov = reader.ReadFloat();
            far_clip = reader.ReadFloat();
            near_clip = reader.ReadFloat();
            positions = M2Track<M2SplineKey<Vector3>>.ReadSplineKey(reader, r => r.ReadVector3());
            position_base = reader.ReadVector3();
            target_position = M2Track<M2SplineKey<Vector3>>.ReadSplineKey(reader, r => r.ReadVector3());
            target_position_base = reader.ReadVector3();
            roll = M2Track<M2SplineKey<float>>.ReadSplineKey(reader, r => r.ReadFloat());
            if (version >= GameFilesVersion.Cataclysm_4_3_4)
            {
                FoV = M2Track<M2SplineKey<float>>.ReadSplineKey(reader, r => r.ReadFloat());
            }
        }
    }

    public struct M2Color
    {
        public readonly M2Track<Vector3> color; // vertex colors in rgb order
        public readonly M2Track<Fixed16> alpha; // 0 - transparent, 0x7FFF - opaque. Normaly NonInterp
    
        public M2Color(IBinaryReader reader)
        {
            color = M2Track<Vector3>.Read(reader, r => r.ReadVector3());
            alpha = M2Track<Fixed16>.Read(reader, r => new Fixed16(r.ReadUInt16()));
        }
    }

    public readonly struct Fixed16
    {
        private readonly ushort value;
        public float Value => (float)value / 0x7fff;

        public Fixed16(ushort value)
        {
            this.value = value;
        }
    }

    public struct Byte4Array
    {
        private readonly uint num;

        private Byte4Array(uint num)
        {
            this.num = num;
        }

        public byte this[int i]
        {
            get
            {
                if (i == 0)
                    return (byte)(num & 0xff);
                else if (i == 1)
                    return (byte)((num >> 8) & 0xff);
                else if (i == 2)
                    return (byte)((num >> 16) & 0xff);
                else
                    return (byte)((num >> 24) & 0xff);
            }
        }

        public static implicit operator Byte4Array(uint num)
        {
            return new Byte4Array(num);
        }
    }

    public struct M2Vertex
    {
        public Vector3 pos { get; init; }
        public Color bone_weights { get; init; }
        public Color bone_indices { get; init; }
        public Vector3 normal { get; init; }
        public Vector2 tex_coord1 { get; init; }
        public Vector2 tex_coord2 { get; init; }  // two textures, depending on shader used
        
        public static M2Vertex Read(IBinaryReader reader)
        {
            return new M2Vertex()
            {
                pos = reader.ReadVector3(),
                bone_weights = new Color(reader.ReadUInt32()),
                bone_indices = new Color(reader.ReadUInt32()),
                normal = reader.ReadVector3(),
                tex_coord1 = reader.ReadVector2(),
                tex_coord2 = reader.ReadVector2()
            };
        }
    }

    public struct M2CompQuat
    {
        private short x;
        private short y;
        private short z;
        private short w;
        
        public static M2CompQuat Identity  => new M2CompQuat(32767, 32767, 32767, -1);

        public Quaternion Value => new Quaternion((x < 0 ? x + 32768 : x - 32767) / 32767.0f,
            (y < 0 ? y + 32768 : y - 32767) / 32767.0f,
            (z < 0 ? z + 32768 : z - 32767) / 32767.0f,
            (w < 0 ? w + 32768 : w - 32767) / 32767.0f);

        public M2CompQuat(short x, short y, short z, short w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static M2CompQuat Read(IBinaryReader reader)
        {
            return new M2CompQuat(reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16());
        }
    }

    public struct M2CompBone
    {
        public readonly int key_bone_id;            // Back-reference to the key bone lookup table. -1 if this is no key bone.
        public readonly M2CompBoneFlag flags;
        public readonly short parent_bone;            // Parent bone ID or -1 if there is none.
        public readonly ushort submesh_id;            // Mesh part ID OR uDistToParent?
        public readonly int boneNameCRC;
        public MutableM2Track<Vector3> translation;
        public MutableM2Track<M2CompQuat> rotation;   // compressed values, default is (32767,32767,32767,65535) == (0,0,0,1) == identity
        public MutableM2Track<Vector3> scale;
        public Vector3 pivot;                 // The pivot point of that bone.
        
        public M2CompBone(IBinaryReader reader, BitArray embeddedValues)
        {
            key_bone_id = reader.ReadInt32();
            flags = (M2CompBoneFlag)reader.ReadInt32();
            parent_bone = reader.ReadInt16();
            submesh_id = reader.ReadUInt16();
            boneNameCRC = reader.ReadInt32();
            translation = MutableM2Track<Vector3>.Read(reader, embeddedValues, r => r.ReadVector3());
            rotation = MutableM2Track<M2CompQuat>.Read(reader, embeddedValues, M2CompQuat.Read);
            scale = MutableM2Track<Vector3>.Read(reader, embeddedValues, r => r.ReadVector3());
            pivot = reader.ReadVector3();
        }
    }

    [Flags]
    public enum M2SequenceFlags
    {
        HasEmbeddedAnimationData = 0x20,
        IsAlias = 0x40,
    }
    
    public readonly struct M2Sequence
    {
        public readonly ushort id;                   // Animation id in AnimationData.dbc
        public readonly ushort variationIndex;       // Sub-animation id: Which number in a row of animations this one is.
        public readonly uint duration;             // The length of this animation sequence in milliseconds.
        public readonly float movespeed;               // This is the speed the character moves with in this animation.
        public readonly M2SequenceFlags flags;                // See below.
        public readonly short frequency;             // This is used to determine how often the animation is played. For all animations of the same type, this adds up to 0x7FFF (32767).
        public readonly ushort _padding;
        public readonly M2Range replay;                // May both be 0 to not repeat. Client will pick a random number of repetitions within bounds if given.
        public readonly uint blendTime;
        public readonly M2Bounds bounds;
        public readonly short variationNext;         // id of the following animation of this AnimationID, points to an Index or is -1 if none.
        public readonly ushort aliasNext;            // id in the list of animations. Used to find actual animation if this sequence is an alias (flags & 0x40)
    
        public M2Sequence(IBinaryReader reader)
        {
            id = reader.ReadUInt16();
            variationIndex = reader.ReadUInt16();
            duration = reader.ReadUInt32();
            movespeed = reader.ReadFloat();
            flags = (M2SequenceFlags)reader.ReadUInt32();
            frequency = reader.ReadInt16();
            _padding = reader.ReadUInt16();
            replay = M2Range.Read(reader);
            blendTime = reader.ReadUInt32();
            bounds = M2Bounds.Read(reader);
            variationNext = reader.ReadInt16();
            aliasNext = reader.ReadUInt16();
        }
    }

    public struct M2Skin
    {
        public readonly M2Array<ushort> Vertices;
        public readonly M2Array<ushort> Indices;
        public readonly M2Array<Uint8x4> BoneIndices;  // note: in fact those are 4 bytes
        public M2Array<M2SkinSection> SubMeshes;
        public readonly M2Array<M2Batch> Batches;
        public readonly uint BonesMaxCount;

        public M2Skin()
        {
            Vertices = new M2Array<ushort>();
            Indices = new M2Array<ushort>();
            BoneIndices = new M2Array<Uint8x4>();
            SubMeshes = new M2Array<M2SkinSection>();
            Batches = new M2Array<M2Batch>();
            BonesMaxCount = 0;
        }
        
        public M2Skin(IBinaryReader reader)
        {
            var magic = reader.ReadInt32();
            if (magic != 0x4E494B53)
                throw new Exception($"Invalid M2 skin magic ({magic})");
            Vertices = reader.ReadArrayUInt16();
            Indices = reader.ReadArrayUInt16();
            BoneIndices = reader.ReadArray(r => new Uint8x4(r.ReadByte(), r.ReadByte(), r.ReadByte(), r.ReadByte()));
            SubMeshes = reader.ReadArray(r => new M2SkinSection(r));
            Batches = reader.ReadArray(r => new M2Batch(r));
            BonesMaxCount = reader.ReadUInt32();
        }
    }
    
    public struct Uint8x4
    {
        public byte a, b, c, d;
        public Uint8x4(uint val)
        {
            a = (byte)(val & 0xFF);
            b = (byte)((val >> 8) & 0xFF);
            c = (byte)((val >> 16) & 0xFF);
            d = (byte)((val >> 24) & 0xFF);
        }
        
        public Uint8x4(byte a, byte b, byte c, byte d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }
    }

    public readonly struct M2Batch 
    {
        public readonly byte flags;                       // Usually 16 for static textures, and 0 for animated textures. &0x1: materials invert something; &0x2: transform &0x4: projected texture; &0x10: something batch compatible; &0x20: projected texture?; &0x40: possibly don't multiply transparency by texture weight transparency to get final transparency value(?)
        public readonly sbyte priorityPlane;
        public readonly ushort shaderId;                  // See below.
        public readonly ushort skinSectionIndex;           // A duplicate entry of a submesh from the list above.
        public readonly ushort geosetIndex;                // See below. New name: flags2. 0x2 - projected. 0x8 - EDGF chunk in m2 is mandatory and data from is applied to this mesh
        public readonly short colorIndex;                 // A Color out of the Colors-Block or -1 if none.
        public readonly ushort materialIndex;              // The renderflags used on this texture-unit.
        public readonly ushort materialLayer;              // Capped at 7 (see CM2Scene::BeginDraw)
        public readonly ushort textureCount;               // 1 to 4. See below. Also seems to be the number of textures to load, starting at the texture lookup in the next field (0x10).
        public readonly ushort textureLookupId;          // Index into Texture lookup table
        public readonly ushort textureUnitLookupId;     // Index into the texture unit lookup table.
        public readonly short textureTransparencyLookupId;         // Index into transparency lookup table.
        public readonly short textureUVAnimationLookupId;          // Index into uvanimation lookup table. 

        public M2Batch(IBinaryReader binaryReader)
        {
            flags = binaryReader.ReadByte();
            priorityPlane = (sbyte)binaryReader.ReadByte();
            shaderId = binaryReader.ReadUInt16();
            skinSectionIndex = binaryReader.ReadUInt16();
            geosetIndex = binaryReader.ReadUInt16();
            colorIndex = binaryReader.ReadInt16();
            materialIndex = binaryReader.ReadUInt16();
            materialLayer = binaryReader.ReadUInt16();
            textureCount = binaryReader.ReadUInt16();
            textureLookupId = binaryReader.ReadUInt16();
            textureUnitLookupId = binaryReader.ReadUInt16();
            textureTransparencyLookupId = binaryReader.ReadInt16();
            textureUVAnimationLookupId = binaryReader.ReadInt16();
        }
    }

    public readonly struct M2SkinSection
    {
        public readonly ushort skinSectionId;       // Mesh part ID, see below.
        public readonly ushort Level;               // (level << 16) is added (|ed) to startTriangle and alike to avoid having to increase those fields to uint32s.
        public readonly ushort vertexStart;         // Starting vertex number.
        public readonly ushort vertexCount;         // Number of vertices.
        public readonly ushort indexStart;          // Starting triangle index (that's 3* the number of triangles drawn so far).
        public readonly ushort indexCount;          // Number of triangle indices.
        public readonly ushort boneCount;           // Number of elements in the bone lookup table. Max seems to be 256 in Wrath. Shall be ≠ 0.
        public readonly ushort boneComboIndex;      // Starting index in the bone lookup table.
        public readonly ushort boneInfluences;      // <= 4
        public readonly ushort centerBoneIndex;  
        public readonly Vector3 centerPosition;     // Average position of all the vertices in the sub mesh.
        public readonly Vector3 sortCenterPosition; // The center of the box when an axis aligned box is built around the vertices in the submesh.
        public readonly float sortRadius;             // Distance of the vertex farthest from CenterBoundingBox.

        public M2SkinSection(IBinaryReader binaryReader)
        {
            skinSectionId = binaryReader.ReadUInt16();
            Level = binaryReader.ReadUInt16();
            vertexStart = binaryReader.ReadUInt16();
            vertexCount = binaryReader.ReadUInt16();
            indexStart = binaryReader.ReadUInt16();
            indexCount = binaryReader.ReadUInt16();
            boneCount = binaryReader.ReadUInt16();
            boneComboIndex = binaryReader.ReadUInt16();
            boneInfluences = binaryReader.ReadUInt16();
            centerBoneIndex = binaryReader.ReadUInt16();
            centerPosition = binaryReader.ReadVector3();
            sortCenterPosition = binaryReader.ReadVector3();
            sortRadius = binaryReader.ReadFloat();
        }
    }

    public static class ReaderExtensions
    {
        public static string AsString(this M2Array<char> chr)
        {
            return new string(chr.Raw.AsSpan(0, chr.Length - 1));
        }

        public static uint ReadUint24(this IBinaryReader reader)
        {
            uint res = (uint)(reader.ReadByte() | (reader.ReadByte() << 8) | (reader.ReadByte() << 16));
            return res;
        }

        public static Quaternion ReadQuaternion(this IBinaryReader reader)
        {
            var x = reader.ReadFloat();
            var y = reader.ReadFloat();
            var z = reader.ReadFloat();
            var w = reader.ReadFloat();
            return new Quaternion(x, y, z, w);
        }
    
        public static Vector3 ReadVector3(this IBinaryReader reader)
        {
            return new Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());
        }

        public static string ReadChunkName(this IBinaryReader reader)
        {
            Span<char> array = stackalloc char[4];
            array[3] = (char)reader.ReadByte();
            array[2] = (char)reader.ReadByte();
            array[1] = (char)reader.ReadByte();
            array[0] = (char)reader.ReadByte();
            return new string(array);
        }
        
        public static string ReadChunkNameReversed(this IBinaryReader reader)
        {
            Span<char> array = stackalloc char[4];
            array[0] = (char)reader.ReadByte();
            array[1] = (char)reader.ReadByte();
            array[2] = (char)reader.ReadByte();
            array[3] = (char)reader.ReadByte();
            return new string(array);
        }
    
        public static string ReadCString(this IBinaryReader reader)
        {
            byte b = reader.ReadByte();
            StringBuilder sb = new StringBuilder();
            while (b != 0)
            {
                sb.Append(Convert.ToChar(b));
                b = reader.ReadByte();
            }
            return sb.ToString();
        }

        public static Vector2 ReadVector2(this IBinaryReader reader)
        {
            return new Vector2(reader.ReadFloat(), reader.ReadFloat());
        }
    
        public static M2Array<ushort> ReadArrayUInt16(this IBinaryReader binaryReader)
        {
            return binaryReader.ReadArray(br => br.ReadUInt16());
        }
    
        public static M2Array<short> ReadArrayInt16(this IBinaryReader binaryReader)
        {
            return binaryReader.ReadArray(br => br.ReadInt16());
        }
        
        public static M2Array<Vector3> ReadArrayVector3(this IBinaryReader binaryReader)
        {
            return binaryReader.ReadArray(br => br.ReadVector3());
        }
    
        public static M2Array<uint> ReadArrayUInt32(this IBinaryReader binaryReader)
        {
            return binaryReader.ReadArray(br => br.ReadUInt32());
        }

        public static IBinaryReader SkipM2Array(this IBinaryReader reader)
        {
            reader.Offset += 8; // size + offset (uint32 + uint32)
            return reader;
        }
    
        public static M2Array<T> ReadArray<T>(this IBinaryReader binaryReader, System.Func<IBinaryReader, T> read)
        {
            return ReadArrayDataFromSeparateReader(binaryReader, binaryReader, (_, br) => read(br));
        }
        
        public static M2Array<T> ReadArrayDataFromSeparateReader<T>(this IBinaryReader binaryReader, IBinaryReader contentReader, System.Func<int, IBinaryReader, T> read)
        {
            var size = binaryReader.ReadInt32();
            if (size > 0x100000)
            {
                Console.WriteLine("wtf");
            }
            var offset = binaryReader.ReadInt32();
            var currentOffset = binaryReader.Offset;

            contentReader.Offset = offset;
            T[] array = new T[size];

            for (int i = 0; i < size; ++i)
            {
                array[i] = read(i, contentReader);
            }

            binaryReader.Offset = currentOffset;
            return new M2Array<T>(size, offset, array);
        }
        
        
        public static MutableM2Array<T> ReadMutableArray<T>(this IBinaryReader binaryReader, System.Func<IBinaryReader, T> read)
        {
            return ReadMutableArrayDataFromSeparateReader(binaryReader, binaryReader, (_, br) => read(br));
        }
        
        public static MutableM2Array<T> ReadMutableArrayContent<T>(this IBinaryReader binaryReader, int size, int offset, System.Func<IBinaryReader, T> read)
        {
            binaryReader.Offset = offset;
            T[] array = new T[size];

            for (int i = 0; i < size; ++i)
            {
                array[i] = read(binaryReader);
            }
            return new MutableM2Array<T>(size, offset, array);
        }
        
        public static MutableM2Array<T> ReadMutableArrayDataFromSeparateReader<T>(this IBinaryReader binaryReader, IBinaryReader contentReader, System.Func<int, IBinaryReader, T> read)
        {
            var size = binaryReader.ReadInt32();
            var offset = binaryReader.ReadInt32();
            var currentOffset = binaryReader.Offset;

            contentReader.Offset = offset;
            T[] array = new T[size];

            for (int i = 0; i < size; ++i)
            {
                array[i] = read(i, contentReader);
            }

            binaryReader.Offset = currentOffset;
            return new MutableM2Array<T>(size, offset, array);
        }
    }

    public class M2TrackBase
    {
        public ushort interpolation_type { get; init; }
        public ushort global_sequence { get; init; }
        public M2Array<M2Array<uint>> timestamps { get; init; }
    
        protected M2TrackBase(){}

        public static M2TrackBase Read(IBinaryReader reader)
        {
            return new M2TrackBase()
            {
                interpolation_type = reader.ReadUInt16(),
                global_sequence = reader.ReadUInt16(),
                timestamps = reader.ReadArray(r => r.ReadArrayUInt32())
            };
        }
    }
 
    public class FBlock<T>
    {
        public uint timestampsNum;
        public uint timestmapsOffset;
        public uint keysNum;
        public uint keysOffset;

        M2Array<M2Array<T>> values;
        
        protected FBlock(){}

        public static FBlock<T> Read(IBinaryReader reader, System.Func<IBinaryReader, T> read)
        {
            return new FBlock<T>()
            {
                timestampsNum = reader.ReadUInt32(),
                timestmapsOffset = reader.ReadUInt32(),
                keysNum = reader.ReadUInt32(),
                keysOffset = reader.ReadUInt32(),
                values = reader.ReadArray(r => r.ReadArray(read))
            };
        }
    }

    public class MutableM2Track<T>
    {
        public ushort interpolation_type { get; init; }
        public ushort global_sequence { get; init; }
        private MutableM2Array<MutableM2Array<uint>> timestamps;
        private MutableM2Array<MutableM2Array<T>> values;
        public int Length => values.Length;

        public ref MutableM2Array<uint> Timestamps(int idx)
        {
            return ref timestamps[idx];
        }

        public ref MutableM2Array<T> Values(int idx)
        {
            return ref values[idx];
        }
        
        public static MutableM2Track<T> Read(IBinaryReader reader, BitArray embeddedValues, Func<IBinaryReader, T> read)
        {
            var interpolation_type = reader.ReadUInt16();
            var global_sequence = reader.ReadUInt16();
            var timestamps = reader.ReadMutableArrayDataFromSeparateReader(reader, (idx, r) =>
            {
                if (embeddedValues[idx])
                {
                    return r.ReadMutableArray(r2 => r2.ReadUInt32());
                }
                else
                {
                    return new MutableM2Array<uint>(r.ReadInt32(), r.ReadInt32(), null!);
                }
            });
            var values = reader.ReadMutableArrayDataFromSeparateReader(reader, (idx, r) =>
            {
                if (embeddedValues[idx])
                    return r.ReadMutableArray(read);
                else
                    return new MutableM2Array<T>(r.ReadInt32(), r.ReadInt32(), null!);
            });
            return new MutableM2Track<T>()
            {
                interpolation_type = interpolation_type,
                global_sequence = global_sequence,
                timestamps = timestamps,
                values = values
            };
        }
        
        public static MutableM2Track<T> Read(IBinaryReader reader, Func<IBinaryReader, T> read)
        {
            var interpolation_type = reader.ReadUInt16();
            var global_sequence = reader.ReadUInt16();
            var timestamps = reader.ReadMutableArray(x => x.ReadMutableArray(x2 => x2.ReadUInt32()));
            var values = reader.ReadMutableArray(x => x.ReadMutableArray(read));
            return new MutableM2Track<T>()
            {
                interpolation_type = interpolation_type,
                global_sequence = global_sequence,
                timestamps = timestamps,
                values = values
            };
        }
    }

    public struct M2Track<T> // : M2TrackBase
    {
        public ushort interpolation_type { get; init; }
        public ushort global_sequence { get; init; }
        public M2Array<M2Array<uint>> timestamps;
        public M2Array<M2Array<T>> values;

        public static M2Track<T> Read(IBinaryReader reader, Func<IBinaryReader, T> read)
        {
            return new M2Track<T>()
            {
                interpolation_type = reader.ReadUInt16(),
                global_sequence = reader.ReadUInt16(),
                timestamps = reader.ReadArray(r => r.ReadArrayUInt32()),
                values = reader.ReadArray(r => r.ReadArray(read))
            };
        }
        
        public static M2Track<T> Read(Dictionary<(int, int), IBinaryReader> readers, IBinaryReader reader, M2Array<M2Sequence> sequences, Func<IBinaryReader, T> read)
        {
            var interpolation_type = reader.ReadUInt16();
            var global_sequence = reader.ReadUInt16();
            var timestamps = reader.ReadArrayDataFromSeparateReader(reader, (idx, r) =>
            {
                if (sequences[idx].flags.HasFlagFast(M2SequenceFlags.HasEmbeddedAnimationData))
                    return r.ReadArrayUInt32();
                else
                {
                    if (!readers.TryGetValue((sequences[idx].id, sequences[idx].variationIndex), out var contentReader))
                    {
                        r.SkipM2Array();
                        return new M2Array<uint>(0, 0, Array.Empty<uint>());
                    }
                    return r.ReadArrayDataFromSeparateReader(contentReader, (_, r2) => r2.ReadUInt32());
                } 
            });
            var values = reader.ReadArrayDataFromSeparateReader(reader, (idx, r) =>
            {
                if (sequences[idx].flags.HasFlagFast(M2SequenceFlags.HasEmbeddedAnimationData))
                    return r.ReadArray(read);
                else
                {
                    if (!readers.TryGetValue((sequences[idx].id, sequences[idx].variationIndex), out var contentReader))
                    {
                        r.SkipM2Array();
                        return new M2Array<T>(0, 0, Array.Empty<T>());
                    }
                    return r.ReadArrayDataFromSeparateReader(contentReader, (_, r2) => read(r2));
                }
            });
            return new M2Track<T>()
            {
                interpolation_type = interpolation_type,
                global_sequence = global_sequence,
                timestamps = timestamps,
                values = values
            };
        }
        // public M2Array<M2Array<T>> values { get; init; }
        //
        // protected M2Track(){}
        //
        // public static M2Track<T> Read(IBinaryReader reader, System.Func<IBinaryReader, T> read)
        // {
        //     return new M2Track<T>()
        //     {
        //         interpolation_type = reader.ReadUInt16(),
        //         global_sequence = reader.ReadUInt16(),
        //         timestamps = reader.ReadArray(r => r.ReadArrayUInt32()),
        //         values = reader.ReadArray(r => r.ReadArray(read))
        //     };
        // }

        public static M2Track<M2SplineKey<T>> ReadSplineKey<T>(IBinaryReader reader, System.Func<IBinaryReader, T> read)
        {
            return M2Track<M2SplineKey<T>>.Read(reader, r => new M2SplineKey<T>(read(r), read(r), read(r)));
        }
    
        public static M2Track<Vector3> ReadVector3(IBinaryReader reader)
        {
            return M2Track<Vector3>.Read(reader, r => r.ReadVector3());
        }

        public static M2Track<float> ReadFloat(IBinaryReader reader)
        {
            return M2Track<float>.Read(reader, r => r.ReadFloat());
        }
    }
    
    // lazy version of M2Array that can hot load data
    public struct MutableM2Array<T> : IEnumerable<T>
    {
        private int offset;
        private int size;
        private T?[] array;

        public MutableM2Array(int size, int offset, T?[] array)
        {
            this.offset = offset;
            this.size = size;
            this.array = array;
        }

        public int Length => size;

        public bool IsLoaded => array != null;
    
        internal T?[] Raw => array;
        public int Offset => offset;

        public ref T this[int i] => ref array[i]!;
        
        public void LoadContent(IBinaryReader reader, Func<IBinaryReader, T> read)
        {
            reader.Offset = offset;
            array = new T[size];
            for (int i = 0; i < size; ++i)
                array[i] = read(reader);
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var e in array)
                yield return e;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return array.GetEnumerator();
        }

        public ReadOnlySpan<T> AsSpan(int start, int length)
        {
            return array.AsSpan(start, length)!;
        }
    }

    public readonly struct M2Array<T> : IEnumerable<T>
    {
        private readonly int offset;
        private readonly T[] array;

        public M2Array(int size, int offset, T[] array)
        {
            Debug.Assert(size == array.Length);
            this.offset = offset;
            this.array = array;
        }

        public int Length => array.Length;
    
        internal T[] Raw => array;

        public ref readonly T this[int i] => ref array[i];

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var e in array)
                yield return e;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return array.GetEnumerator();
        }

        public ReadOnlySpan<T> AsSpan(int start, int length)
        {
            return array.AsSpan(start, length);
        }

        public ReadOnlySpan<T> AsSpan()
        {
            return array.AsSpan();
        }
    }
}
