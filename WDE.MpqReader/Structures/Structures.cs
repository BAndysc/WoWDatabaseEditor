using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using TheMaths;
using WDE.MpqReader.Readers;

namespace WDE.MpqReader.Structures
{
    public enum M2Flags
    {
        FLAG_TILT_X,
        FLAG_TILT_Y,
        UNK0,
        FLAG_USE_TEXTURE_COMBINER_COMBOS
    }

    public class M2
    {
        public uint magic { get; init; }                                       // "MD20". Legion uses a chunked file format starting with MD21.
        public uint version { get; init; }
        public M2Array<char> name { get; init; }                                   // should be globally unique, used to reload by name in internal clients
        public M2Flags global_flags { get; init; }
/*0x014*/  public M2Array<uint> global_loops { get; init; }                        // Timestamps used in global looping animations.
/*0x01C*/  public M2Array<M2Sequence> sequences { get; init; }                       // Information about the animations in the model.
/*0x024*/  public M2Array<ushort> sequenceIdxHashById { get; init; }               // Mapping of sequence IDs to the entries in the Animation sequences block.
/*0x02C*/  public M2Array<M2CompBone> bones { get; init; }                           // MAX_BONES = 0x100 => Creature\SlimeGiant\GiantSlime.M2 has 312 bones (Wrath)
/*0x034*/  public M2Array<ushort> boneIndicesById { get; init; }                   //Lookup table for key skeletal bones. (alt. name: key_bone_lookup)
/*0x03C*/  public M2Array<M2Vertex> vertices { get; init; }
/*0x044*/  public uint num_skin_profiles { get; init; }                           // Views (LOD) are now in .skins.
/*0x048*/  public M2Array<M2Color> colors { get; init; }                             // Color and alpha animations definitions.
/*0x050*/  public M2Array<M2Texture> textures { get; init; }
/*0x058*/  public M2Array<M2TextureWeight> texture_weights { get; init; }            // Transparency of textures.
/*0x060*/  public M2Array<M2TextureTransform> texture_transforms { get; init; }
/*0x068*/  public M2Array<ushort> textureIndicesById { get; init; }                // (alt. name: replacable_texture_lookup)
/*0x070*/  public M2Array<M2Material> materials { get; init; }                       // Blending modes / render flags.
/*0x078*/  public M2Array<ushort> boneCombos { get; init; }                        // (alt. name: bone_lookup_table)
/*0x080*/  public M2Array<short> textureCombos { get; init; }                     // (alt. name: texture_lookup_table)
/*0x088*/  public M2Array<ushort> textureTransformBoneMap { get; init; }           // (alt. name: tex_unit_lookup_table)
/*0x090*/  public M2Array<ushort> textureWeightCombos { get; init; }               // (alt. name: transparency_lookup_table)
/*0x098*/  public M2Array<ushort> textureTransformCombos { get; init; }            // (alt. name: texture_transforms_lookup_table)
/*0x0A0*/  public CAaBox bounding_box { get; init; }                                 // min/max( [1].z, 2.0277779f ) - 0.16f seems to be the maximum camera height
/*0x0B8*/  public float bounding_sphere_radius { get; init; }                         // detail doodad draw dist = clamp (bounding_sphere_radius * detailDoodadDensityFade * detailDoodadDist, …)
/*0x0BC*/  public CAaBox collision_box { get; init; }
/*0x0D4*/  public float collision_sphere_radius { get; init; }
/*0x0D8*/  public M2Array<ushort> collisionIndices { get; init; }                    // (alt. name: collision_triangles)
/*0x0E0*/  public M2Array<Vector3> collisionPositions { get; init; }                  // (alt. name: collision_vertices)
/*0x0E8*/  public M2Array<Vector3> collisionFaceNormals { get; init; }                // (alt. name: collision_normals) 
/*0x0F0*/  public M2Array<M2Attachment> attachments { get; init; }                     // position of equipped weapons or effects
/*0x0F8*/  public M2Array<ushort> attachmentIndicesById { get; init; }               // (alt. name: attachment_lookup_table)
/*0x100*/  public M2Array<M2Event> events { get; init; }                               // Used for playing sounds when dying and a lot else.
/*0x108*/  public M2Array<M2Light> lights { get; init; }                               // Lights are mainly used in loginscreens but in wands and some doodads too.
/*0x110*/  public M2Array<M2Camera> cameras { get; init; }                             // The cameras are present in most models for having a model in the character tab. 
/*0x118*/  public M2Array<ushort> cameraIndicesById { get; init; }                   // (alt. name: camera_lookup_table)
/*0x120*/  public M2Array<M2Ribbon> ribbon_emitters { get; init; }                     // Things swirling around. See the CoT-entrance for light-trails.
        ///*0x128*/  M2Array<M2Particleⁱ> particle_emitters { get; init; }

        private M2(){}

        public static M2 Read(IBinaryReader reader)
        {
            return new M2()
            {
                magic = reader.ReadUInt32(),
                version = reader.ReadUInt32(),
                name = reader.ReadArray(r => (char)r.ReadByte()),
                global_flags = (M2Flags)reader.ReadUInt32(),
                global_loops = reader.ReadArrayUInt32(),
                sequences = reader.ReadArray(M2Sequence.Read),
                sequenceIdxHashById = reader.ReadArrayUInt16(),
                bones = reader.ReadArray(M2CompBone.Read),
                boneIndicesById = reader.ReadArrayUInt16(),
                vertices = reader.ReadArray(M2Vertex.Read),
                num_skin_profiles = reader.ReadUInt32(),
                colors = reader.ReadArray(M2Color.Read),
                textures = reader.ReadArray(M2Texture.Read),
                texture_weights = reader.ReadArray(M2TextureWeight.Read),
                texture_transforms = reader.ReadArray(M2TextureTransform.Read),
                textureIndicesById = reader.ReadArrayUInt16(),
                materials = reader.ReadArray(M2Material.Read),
                boneCombos = reader.ReadArrayUInt16(),
                textureCombos = reader.ReadArrayInt16(),
                textureTransformBoneMap = reader.ReadArrayUInt16(),
                textureWeightCombos = reader.ReadArrayUInt16(),
                textureTransformCombos = reader.ReadArrayUInt16(),
                bounding_box = CAaBox.Read(reader),
                bounding_sphere_radius = reader.ReadFloat(),
                collision_box = CAaBox.Read(reader),
                collision_sphere_radius = reader.ReadFloat(),
                collisionIndices = reader.ReadArrayUInt16(),
                collisionPositions = reader.ReadArrayVector3(),
                collisionFaceNormals = reader.ReadArrayVector3(),
                attachments = reader.ReadArray(M2Attachment.Read),
                attachmentIndicesById = reader.ReadArrayUInt16(),
                events = reader.ReadArray(M2Event.Read),
                lights = reader.ReadArray(M2Light.Read),
                cameras = reader.ReadArray(M2Camera.Read),
                cameraIndicesById = reader.ReadArrayUInt16(),
                ribbon_emitters = reader.ReadArray(M2Ribbon.Read),
            };
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

    public readonly struct M2Range {
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


    public class M2Texture
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
    
        public TextureType type { get; init; }          // see below
        public Flags flags{ get; init; }          // see below
        public M2Array<char> filename { get; init; }  // for non-hardcoded textures (type != 0), this still points to a zero-byte-only string.
    
        private M2Texture() {}

        public static M2Texture Read(IBinaryReader reader)
        {
            return new M2Texture()
            {
                type = (TextureType)reader.ReadUInt32(),
                flags = (Flags)reader.ReadUInt32(),
                filename = reader.ReadArray(r => (char)r.ReadByte())
            };
        }
    }

    public class M2TextureWeight
    {
        public M2Track<Fixed16> weight { get; init; }
    
        private M2TextureWeight(){}

        public static M2TextureWeight Read(IBinaryReader binaryReader)
        {
            return new M2TextureWeight()
            {
                weight = M2Track<Fixed16>.Read(binaryReader, r => new Fixed16(r.ReadUInt16()))
            };
        }
    }

    public class M2TextureTransform
    {
        public M2Track<Vector3> translation { get; init; }
        public M2Track<Quaternion> rotation { get; init; }    // rotation center is texture center (0.5, 0.5)
        public M2Track<Vector3> scaling { get; init; }
    
        private M2TextureTransform(){}

        public static M2TextureTransform Read(IBinaryReader reader)
        {
            return new M2TextureTransform()
            {
                translation = M2Track<Vector3>.Read(reader, r => r.ReadVector3()),
                rotation = M2Track<Quaternion>.Read(reader, r => r.ReadQuaternion()),
                scaling = M2Track<Vector3>.Read(reader, r => r.ReadVector3()),
            };
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
        M2BlendAlphaKey,
        M2BlendAlpha,
        M2BlendNoAlphaAdd,
        M2BlendAdd,
        M2BlendMod,
        M2BlendMod2X,
        M2BlendBlendAdd
    }

    public struct M2Material
    {
        public M2MaterialFlags flags { get; init; }
        public M2Blend blending_mode { get; init; } // apparently a bitfield

        public static M2Material Read(IBinaryReader reader)
        {
            return new M2Material()
            {
                flags = (M2MaterialFlags)reader.ReadUInt16(),
                blending_mode = (M2Blend)reader.ReadUInt16()
            };
        }
    }

    public class M2Attachment
    {
        public uint id { get; init; }                      // Referenced in the lookup-block below.
        public ushort bone { get; init; }                    // attachment base
        public ushort unknown { get; init; }                 // see BogBeast.m2 in vanilla for a model having values here
        public Vector3 position { get; init; }               // relative to bone; Often this value is the same as bone's pivot point 
        public M2Track<char> animate_attached { get; init; }  // whether or not the attached model is animated when this model is. only a bool is used. default is true.
    
        private M2Attachment(){}

        public static M2Attachment Read(IBinaryReader reader)
        {
            return new M2Attachment()
            {
                id = reader.ReadUInt32(),
                bone = reader.ReadUInt16(),
                unknown = reader.ReadUInt16(),
                position = reader.ReadVector3(),
                animate_attached = M2Track<char>.Read(reader, r => (char)r.ReadByte())
            };
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
        //    M2Track<M2SplineKey<float>> FoV; //Diagonal FOV in radians. See below for conversion.
    
        private M2Camera() {}

        public static M2Camera Read(IBinaryReader reader)
        {
            return new M2Camera()
            {
                type = reader.ReadUInt32(),
                fov = reader.ReadFloat(),
                far_clip = reader.ReadFloat(),
                near_clip = reader.ReadFloat(),
                positions = M2Track<M2SplineKey<Vector3>>.ReadSplineKey(reader, r => r.ReadVector3()),
                position_base = reader.ReadVector3(),
                target_position = M2Track<M2SplineKey<Vector3>>.ReadSplineKey(reader, r => r.ReadVector3()),
                target_position_base = reader.ReadVector3(),
                roll = M2Track<M2SplineKey<float>>.ReadSplineKey(reader, r => r.ReadFloat()),
            };
        }
    }

    public class M2Color
    {
        public M2Track<Vector3> color { get; init; } // vertex colors in rgb order
        public M2Track<Fixed16> alpha { get; init; } // 0 - transparent, 0x7FFF - opaque. Normaly NonInterp
    
        private M2Color(){}

        public static M2Color Read(IBinaryReader reader)
        {
            return new M2Color()
            {
                color = M2Track<Vector3>.Read(reader, r => r.ReadVector3()),
                alpha = M2Track<Fixed16>.Read(reader, r => new Fixed16(r.ReadUInt16()))
            };
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

    public class M2Vertex
    {
        public Vector3 pos { get; init; }
        public byte[] bone_weights { get; init; }
        public byte[] bone_indices { get; init; }
        public Vector3 normal { get; init; }
        public Vector2[] tex_coords { get; init; }  // two textures, depending on shader used
    
        private M2Vertex(){}

        public static M2Vertex Read(IBinaryReader reader)
        {
            return new M2Vertex()
            {
                pos = reader.ReadVector3(),
                bone_weights = reader.ReadBytes(4),
                bone_indices = reader.ReadBytes(4),
                normal = reader.ReadVector3(),
                tex_coords = new[] { reader.ReadVector2(), reader.ReadVector2() }
            };
        }
    }

    public struct M2CompQuat
    {
        private short x;
        private short y;
        private short z;
        private short w;

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

    public class M2CompBone
    {
        public int key_bone_id { get; init; }            // Back-reference to the key bone lookup table. -1 if this is no key bone.
        public M2CompBoneFlag flags { get; init; }                 
        public short parent_bone { get; init; }            // Parent bone ID or -1 if there is none.
        public ushort submesh_id { get; init; }            // Mesh part ID OR uDistToParent?
        public int boneNameCRC { get; init; }
        public M2Track<Vector3> translation { get; init; }
        public M2Track<M2CompQuat> rotation { get; init; }   // compressed values, default is (32767,32767,32767,65535) == (0,0,0,1) == identity
        public M2Track<Vector3> scale { get; init; }
        public Vector3 pivot { get; init; }                 // The pivot point of that bone.
    
        private M2CompBone() {}

        public static M2CompBone Read(IBinaryReader reader)
        {
            return new M2CompBone()
            {
                key_bone_id = reader.ReadInt32(),
                flags = (M2CompBoneFlag)reader.ReadInt32(),
                parent_bone = reader.ReadInt16(),
                submesh_id = reader.ReadUInt16(),
                boneNameCRC = reader.ReadInt32(),
                translation = M2Track<Vector3>.ReadVector3(reader),
                rotation = M2Track<M2CompQuat>.Read(reader, M2CompQuat.Read),
                scale = M2Track<Vector3>.ReadVector3(reader),
                pivot = reader.ReadVector3()
            };
        }
    }

    public class M2Sequence
    {
        public ushort id { get; init; }                   // Animation id in AnimationData.dbc
        public ushort variationIndex { get; init; }       // Sub-animation id: Which number in a row of animations this one is.
        public uint duration { get; init; }             // The length of this animation sequence in milliseconds.
        public float movespeed { get; init; }               // This is the speed the character moves with in this animation.
        public uint flags { get; init; }                // See below.
        public short frequency { get; init; }             // This is used to determine how often the animation is played. For all animations of the same type, this adds up to 0x7FFF (32767).
        public ushort _padding { get; init; }
        public M2Range replay { get; init; }                // May both be 0 to not repeat. Client will pick a random number of repetitions within bounds if given.
        public uint blendTime { get; init; }
        public M2Bounds bounds { get; init; }
        public short variationNext { get; init; }         // id of the following animation of this AnimationID, points to an Index or is -1 if none.
        public ushort aliasNext { get; init; }            // id in the list of animations. Used to find actual animation if this sequence is an alias (flags & 0x40)
    
        private M2Sequence(){}

        public static M2Sequence Read(IBinaryReader reader)
        {
            return new M2Sequence()
            {
                id = reader.ReadUInt16(),
                variationIndex = reader.ReadUInt16(),
                duration = reader.ReadUInt32(),
                movespeed = reader.ReadFloat(),
                flags = reader.ReadUInt32(),
                frequency = reader.ReadInt16(),
                _padding = reader.ReadUInt16(),
                replay = M2Range.Read(reader),
                blendTime = reader.ReadUInt32(),
                bounds = M2Bounds.Read(reader),
                variationNext = reader.ReadInt16(),
                aliasNext = reader.ReadUInt16()
            };
        }
    }

    public class M2Skin
    {
        public M2Array<ushort> Vertices { get; init; }
        public M2Array<ushort> Indices{ get; init; }
        public M2Array<uint> Bones { get; init; }  // note: in fact those are 4 bytes
        public M2Array<M2SkinSection> SkinSections { get; init; }
        public M2Array<M2Batch> Batches { get; init; }
        public uint BonesMaxCount { get; init; }
    
        private M2Skin(){}

        public static M2Skin Read(IBinaryReader reader)
        {
            var magic = reader.ReadInt32();
            return new M2Skin()
            {
                Vertices = reader.ReadArrayUInt16(),
                Indices = reader.ReadArrayUInt16(),
                Bones = reader.ReadArrayUInt32(),
                SkinSections = reader.ReadArray(M2SkinSection.Read),
                Batches = reader.ReadArray(M2Batch.Read),
                BonesMaxCount = reader.ReadUInt32(),
            };
        }
    }

    public class M2Batch 
    {
        public byte flags;                       // Usually 16 for static textures, and 0 for animated textures. &0x1: materials invert something; &0x2: transform &0x4: projected texture; &0x10: something batch compatible; &0x20: projected texture?; &0x40: possibly don't multiply transparency by texture weight transparency to get final transparency value(?)
        public sbyte priorityPlane;
        public ushort shader_id;                  // See below.
        public ushort skinSectionIndex;           // A duplicate entry of a submesh from the list above.
        public ushort geosetIndex;                // See below. New name: flags2. 0x2 - projected. 0x8 - EDGF chunk in m2 is mandatory and data from is applied to this mesh
        public short colorIndex;                 // A Color out of the Colors-Block or -1 if none.
        public ushort materialIndex;              // The renderflags used on this texture-unit.
        public ushort materialLayer;              // Capped at 7 (see CM2Scene::BeginDraw)
        public ushort textureCount;               // 1 to 4. See below. Also seems to be the number of textures to load, starting at the texture lookup in the next field (0x10).
        public ushort textureComboIndex;          // Index into Texture lookup table
        public ushort textureCoordComboIndex;     // Index into the texture unit lookup table.
        public short transparencyIndex;         // Index into transparency lookup table.
        public ushort textureAnimIndex;          // Index into uvanimation lookup table. 

        private M2Batch(){}

        public static M2Batch Read(IBinaryReader binaryReader)
        {
            return new M2Batch()
            {
                flags = binaryReader.ReadByte(),
                priorityPlane = (sbyte)binaryReader.ReadByte(),
                shader_id = binaryReader.ReadUInt16(),
                skinSectionIndex = binaryReader.ReadUInt16(),
                geosetIndex = binaryReader.ReadUInt16(),
                colorIndex = binaryReader.ReadInt16(),
                materialIndex = binaryReader.ReadUInt16(),
                materialLayer = binaryReader.ReadUInt16(),
                textureCount = binaryReader.ReadUInt16(),
                textureComboIndex = binaryReader.ReadUInt16(),
                textureCoordComboIndex = binaryReader.ReadUInt16(),
                transparencyIndex = binaryReader.ReadInt16(),
                textureAnimIndex = binaryReader.ReadUInt16()
            };
        }
    };

    public class M2SkinSection
    {
        public ushort skinSectionId;       // Mesh part ID, see below.
        public ushort Level;               // (level << 16) is added (|ed) to startTriangle and alike to avoid having to increase those fields to uint32s.
        public ushort vertexStart;         // Starting vertex number.
        public ushort vertexCount;         // Number of vertices.
        public ushort indexStart;          // Starting triangle index (that's 3* the number of triangles drawn so far).
        public ushort indexCount;          // Number of triangle indices.
        public ushort boneCount;           // Number of elements in the bone lookup table. Max seems to be 256 in Wrath. Shall be ≠ 0.
        public ushort boneComboIndex;      // Starting index in the bone lookup table.
        public ushort boneInfluences;      // <= 4
        public ushort centerBoneIndex;  
        public Vector3 centerPosition;     // Average position of all the vertices in the sub mesh.
        public Vector3 sortCenterPosition; // The center of the box when an axis aligned box is built around the vertices in the submesh.
        public float sortRadius;             // Distance of the vertex farthest from CenterBoundingBox.

        private M2SkinSection(){}

        public static M2SkinSection Read(IBinaryReader binaryReader)
        {
            return new M2SkinSection()
            {
                skinSectionId = binaryReader.ReadUInt16(),
                Level = binaryReader.ReadUInt16(),
                vertexStart = binaryReader.ReadUInt16(),
                vertexCount = binaryReader.ReadUInt16(),
                indexStart = binaryReader.ReadUInt16(),
                indexCount = binaryReader.ReadUInt16(),
                boneCount = binaryReader.ReadUInt16(),
                boneComboIndex = binaryReader.ReadUInt16(),
                boneInfluences = binaryReader.ReadUInt16(),
                centerBoneIndex = binaryReader.ReadUInt16(),
                centerPosition = binaryReader.ReadVector3(),
                sortCenterPosition = binaryReader.ReadVector3(),
                sortRadius = binaryReader.ReadFloat()
            };
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
    
        public static M2Array<T> ReadArray<T>(this IBinaryReader binaryReader, System.Func<IBinaryReader, T> read)
        {
            var size = binaryReader.ReadInt32();
            var offset = binaryReader.ReadInt32();
            var currentOffset = binaryReader.Offset;

            binaryReader.Offset = offset;
            T[] array = new T[size];

            for (int i = 0; i < size; ++i)
            {
                array[i] = read(binaryReader);
            }

            binaryReader.Offset = currentOffset;
            return new M2Array<T>(size, offset, array);
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
    };
 

    public class M2Track<T> : M2TrackBase
    {
        public M2Array<M2Array<T>> values { get; init; }
    
        protected M2Track(){}

        public static M2Track<T> Read(IBinaryReader reader, System.Func<IBinaryReader, T> read)
        {
            return new M2Track<T>()
            {
                interpolation_type = reader.ReadUInt16(),
                global_sequence = reader.ReadUInt16(),
                timestamps = reader.ReadArray(r => r.ReadArrayUInt32()),
                values = reader.ReadArray(r => r.ReadArray(read))
            };
        }

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
    };

    public class M2Array<T> : IEnumerable<T>
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

        public T this[int i] => array[i];

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var e in array)
                yield return e;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return array.GetEnumerator();
        }
    }
}