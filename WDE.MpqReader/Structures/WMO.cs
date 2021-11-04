using System.Diagnostics;
using TheMaths;
using WDE.MpqReader.Readers;

namespace WDE.MpqReader.Structures
{
    public class ChunkedUtils
    {
        public static string[] ReadZeroTerminatedStringArrays(LimitedReader partialReader, bool withOffsets, out Dictionary<int, string>? offsets)
        {
            List<string> textures = new();
            offsets = withOffsets ? new() : null;
            var startOffset = partialReader.Offset;
            while (!partialReader.IsFinished())
            {
                int start = partialReader.Offset;
                while (partialReader.ReadByte() != 0) ;
                int end = partialReader.Offset - 1;

                if (end == start && partialReader.IsFinished())
                    break;
            
                partialReader.Offset = start;
                var textureNameBytes =partialReader.ReadBytes(end - start);
                var textureName = System.Text.Encoding.ASCII.GetString(textureNameBytes);
                textures.Add(textureName);
                if (withOffsets)
                    offsets![start - startOffset] = textureName;
            
                while (!partialReader.IsFinished() && partialReader.ReadByte() == 0) ;
                partialReader.Offset--;
            }

            return textures.ToArray();
        }
    }

    public class WMO
    {
        public WMOHeader Header { get; private set; }
        public string[] Textures { get; private set; }
        public string[] ModelNames { get; private set; }
        public string[] GroupNames { get; private set; }
        public SmoDoodadDef[] DoodadsDefinition { get; private set; }
        public WorldMapObjectMaterial[] Materials { get; private set; }
    
        private WMO(){}

        public static WMO Read(IBinaryReader reader)
        {
            var wmo = new WMO();
            Dictionary<int, string> offsets = null;
            Dictionary<int, string> textureOffsets = null;

            while (!reader.IsFinished())
            {
                var chunkName = reader.ReadChunkName();
                var size = reader.ReadInt32();

                var offset = reader.Offset;

                var partialReader = new LimitedReader(reader, size);

                if (chunkName == "MOHD")
                    wmo.Header = WMOHeader.Read(partialReader);
                else if (chunkName == "MOTX")
                    wmo.Textures = ChunkedUtils.ReadZeroTerminatedStringArrays(partialReader, true, out textureOffsets);
                else if (chunkName == "MODD")
                    wmo.DoodadsDefinition = ReadDoodads(partialReader, offsets, size);
                else if (chunkName == "MODN")
                    wmo.ModelNames = ChunkedUtils.ReadZeroTerminatedStringArrays(partialReader, true, out offsets);
                else if (chunkName == "MOGN")
                    wmo.GroupNames = ChunkedUtils.ReadZeroTerminatedStringArrays(partialReader, false, out _);
                else if (chunkName == "MOMT")
                    wmo.Materials = ReadMaterials(partialReader, textureOffsets, size);
            
                reader.Offset = offset + size;
            }

            return wmo;
        }

        private static WorldMapObjectMaterial[] ReadMaterials(IBinaryReader reader, Dictionary<int,string>? textureOffsets, int size)
        {
            var materials = new WorldMapObjectMaterial[size / 64];
            int i = 0;
            Debug.Assert(textureOffsets != null);
            while (!reader.IsFinished())
            {
                materials[i++] = new WorldMapObjectMaterial(reader, textureOffsets);
            }

            return materials;
        }

        private static SmoDoodadDef[] ReadDoodads(LimitedReader partialReader, Dictionary<int, string> nameOffsets, int size)
        {
            int count = size / 40;
            SmoDoodadDef[] array = new SmoDoodadDef[count];
            for (int i = 0; i < count; ++i)
            {
                array[i] = new SmoDoodadDef(nameOffsets, partialReader);
            }
            Debug.Assert(partialReader.IsFinished());
            return array;
        }
    }

    [Flags]
    public enum WorldMapObjectFlags : ushort
    {
        FlagDoNotAttenuateVerticesBasedOnDistanceToPortal = 1,
        FlagUseUnifiedRenderPath = 2,
        FlagUseLiquidTypeDbcId = 4,
        FlagDoNotFixVertexColorAlpha = 8,
        FlagLod = 16,
        FlagDefaultMaxLod = 32
    }

    public enum GxBlendMode
    {
        GxBlend_Opaque = 0,
        GxBlend_AlphaKey,
        GxBlend_Alpha,
        GxBlend_Add,
        GxBlend_Mod,
        GxBlend_Mod2x,
        GxBlend_ModAdd,
        GxBlend_InvSrcAlphaAdd,
        GxBlend_InvSrcAlphaOpaque,
        GxBlend_SrcAlphaOpaque,
        GxBlend_NoAlphaAdd,
        GxBlend_ConstantAlpha,
        GxBlend_Screen,
        GxBlend_BlendAdd
    }

    public class WorldMapObjectMaterial
    {
        [Flags]
        public enum Flags
        {
            unlit = 1,
            unfogged = 2,
            unculled = 4,
            extllight = 8,
            sidn = 16,
            window = 32,
            clamp_s = 64,
            clamp_t = 128
        }
    
        public Flags flags { get; init; }
        public uint shader { get; init; }                         // Index into CMapObj::s_wmoShaderMetaData. See below (shader types).
        public GxBlendMode blendMode { get; init; }                      // Blending: see EGxBlend
        public uint texture_1 { get; init; }                      // offset into MOTX; ≥ Battle (8.1.0.27826) No longer references MOTX but is a filedata id directly.
        public CImVector sidnColor { get; init; }                    // emissive color; see below (emissive color)
        public CImVector frameSidnColor { get; init; }               // sidn emissive color; set at runtime; gets sidn-manipulated emissive color; see below (emissive color)
        public uint texture_2 { get; init; }                      // offset into MOTX
        public CArgb diffColor { get; init; }
        public uint ground_type { get; init; }                    // according to CMapObjDef::GetGroundType 
        public uint texture_3 { get; init; }                      // offset into MOTX
        public uint color_2 { get; init; }
        public uint flags_2 { get; init; }
        public uint[] runTimeData { get; init; }                 // This data is explicitly nulled upon loading. Contains textures or similar stuff.

        public string? texture1Name { get; init; }
        public string? texture2Name { get; init; }
        public string? texture3Name { get; init; }
    
        public WorldMapObjectMaterial(IBinaryReader reader, Dictionary<int, string> textures)
        {
            flags = (Flags)reader.ReadUInt32();
            shader = reader.ReadUInt32();
            blendMode = (GxBlendMode)reader.ReadUInt32();
            texture_1 = reader.ReadUInt32();
            sidnColor = new CImVector(reader);
            frameSidnColor = new CImVector(reader);
            texture_2 = reader.ReadUInt32();
            diffColor = new CArgb(reader);
            ground_type = reader.ReadUInt32();
            texture_3 = reader.ReadUInt32();
            color_2 = reader.ReadUInt32();
            flags_2 = reader.ReadUInt32();
            runTimeData = new uint[] { reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32() };

            texture1Name = textures[(int)texture_1];
            if (textures.TryGetValue((int)texture_2, out var texname))
                texture2Name = texname;
            if (textures.TryGetValue((int)texture_3, out texname))
                texture3Name = texname;
        }
    }

    public readonly struct CArgb
    {
        public readonly byte r;
        public readonly byte g;
        public readonly byte b;
        public readonly byte a;

        public CArgb(IBinaryReader reader)
        {
            r = reader.ReadByte();
            g = reader.ReadByte();
            b = reader.ReadByte();
            a = reader.ReadByte();
        }
    };

    public readonly struct CImVector
    {
        public readonly byte b;
        public readonly byte g;
        public readonly byte r;
        public readonly byte a;

        public CImVector(IBinaryReader reader)
        {
            b = reader.ReadByte();
            g = reader.ReadByte();
            r = reader.ReadByte();
            a = reader.ReadByte();
        }
    };

    public readonly struct SmoDoodadDef
    {
        public enum DoodadFlags
        {
            AcceptProjTex
        }
    
        public readonly uint NameIndex;          // reference offset into MODN, or MODI, depending on version and presence.
        public readonly string M2Path;
        public readonly DoodadFlags Flags;
        public readonly Vector3 Position;               // (X,Z,-Y)
        public readonly Quaternion Rotation;        // (X, Y, Z, W)
        public readonly float Scale;                      // scale factor
        public readonly uint Color;                 // (B,G,R,A) overrides pc_sunColor

        public SmoDoodadDef(Dictionary<int, string> nameOffsets, IBinaryReader reader)
        {
            Debug.Assert(nameOffsets != null);
            NameIndex = reader.ReadUint24();
            M2Path = nameOffsets[(int)NameIndex];
            Flags = (DoodadFlags)reader.ReadByte();
            Position = reader.ReadVector3();
            Rotation = reader.ReadQuaternion();
            Scale = reader.ReadFloat();
            Color = reader.ReadUInt32();
        }
    }

    public class WMOHeader
    {
        public uint nTextures { get; init; }    
        public uint nGroups { get; init; }    
        public uint nPortals { get; init; }   
        public uint nLights { get; init; }                                        // Blizzard seems to add one to the MOLT entry count when there are MOLP chunks in the groups (and maybe for MOLS too?)ᵘ
        public uint nDoodadNames { get; init; } 
        public uint nDoodadDefs { get; init; }                                    // *
        public uint nDoodadSets { get; init; }    
        public uint ambColorRgba { get; init; }                                         // Color settings for base (ambient) color. See the flag at /*03Ch*/.   
        public uint wmoID { get; init; }
        public CAaBox bounding_box { get; init; }                                    // in the alpha, this bounding box was computed upon loading
        public WorldMapObjectFlags flags { get; init; }
    
        private WMOHeader(){}

        public static WMOHeader Read(IBinaryReader reader)
        {
            return new WMOHeader()
            {
                nTextures = reader.ReadUInt32(),
                nGroups = reader.ReadUInt32(),
                nPortals = reader.ReadUInt32(),
                nLights = reader.ReadUInt32(),
                nDoodadNames = reader.ReadUInt32(),
                nDoodadDefs = reader.ReadUInt32(),
                nDoodadSets = reader.ReadUInt32(),
                ambColorRgba = reader.ReadUInt32(),
                wmoID = reader.ReadUInt32(),
                bounding_box = CAaBox.Read(reader),
                flags = (WorldMapObjectFlags)reader.ReadUInt16()
            };
        }
    }
}