using System;
using System.Collections.Generic;
using System.Diagnostics;
using TheMaths;
using WDE.Common.MPQ;
using WDE.MpqReader.Readers;
using Plane = TheMaths.Plane;

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
                var textureNameBytes = partialReader.ReadBytes(end - start);
                var textureName = System.Text.Encoding.ASCII.GetString(textureNameBytes.Span);
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
        public readonly WMOHeader Header;
        public readonly string[] Textures;
        public readonly string[] ModelNames;
        public readonly string[] GroupNames;
        public readonly SmoDoodadDef[] DoodadsDefinition;
        public readonly WorldMapObjectMaterial[] Materials;
        public readonly Vector3[] PortalVertices;
        public readonly WmoPortal[] Portals;
        public readonly uint[,]? GroupFileDataIdsPerLods;

        public WMO(IBinaryReader reader, GameFilesVersion version)
        {
            Dictionary<int, string> offsets = null!;
            Dictionary<int, string> textureOffsets = null!;

            while (!reader.IsFinished())
            {
                var chunkName = reader.ReadChunkName();
                var size = reader.ReadInt32();

                var offset = reader.Offset;

                var partialReader = new LimitedReader(reader, size);

                if (chunkName == "MOHD")
                    Header = WMOHeader.Read(partialReader, version);
                else if (chunkName == "MOTX")
                    Textures = ChunkedUtils.ReadZeroTerminatedStringArrays(partialReader, true, out textureOffsets);
                else if (chunkName == "MODD")
                    DoodadsDefinition = ReadDoodads(partialReader, offsets, size);
                else if (chunkName == "MODN")
                    ModelNames = ChunkedUtils.ReadZeroTerminatedStringArrays(partialReader, true, out offsets);
                else if (chunkName == "MOGN")
                    GroupNames = ChunkedUtils.ReadZeroTerminatedStringArrays(partialReader, false, out _);
                else if (chunkName == "MOMT")
                    Materials = ReadMaterials(partialReader, textureOffsets, size);
                else if (chunkName == "MOPV")
                    PortalVertices = ReadVertices(partialReader);
                else if (chunkName == "MOPT")
                    Portals = ReadPortals(partialReader);
                else if (chunkName == "GFID")
                    GroupFileDataIdsPerLods = ReadGroupFileIds(partialReader);
            
                reader.Offset = offset + size;
            }
        }

        private uint[,]? ReadGroupFileIds(LimitedReader reader)
        {
            var lodsCount = !Header.flags.HasFlagFast(WorldMapObjectFlags.FlagLod) ? 1 : (Header.lodCount == 0 ? 3 : Header.lodCount);
            uint[,] data = new uint[lodsCount, Header.nGroups];
            for (int i = 0; i < lodsCount; ++i)
            {
                for (int j = 0; j < Header.nGroups; ++j)
                {
                    data[i, j] = reader.ReadUInt32();
                }
            }

            return data;
        }

        public static WMO Read(IBinaryReader reader, GameFilesVersion version)
        {
            return new WMO(reader, version);
        }

        private WmoPortal[] ReadPortals(LimitedReader reader)
        {
            var portals = new WmoPortal[reader.Size / 20];
            int i = 0;
            while (!reader.IsFinished())
            {
                portals[i++] = new WmoPortal(reader);
            }

            return portals;
        }

        private static Vector3[] ReadVertices(IBinaryReader reader)
        {
            var vectors = new Vector3[reader.Size / (3 * 4)];
            int i = 0;
            while (!reader.IsFinished())
            {
                vectors[i++] = reader.ReadVector3();
            }

            return vectors;
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

    public readonly struct WmoPortal
    {
        public readonly ushort startVetex;
        public readonly ushort count;
        public readonly Plane plane;

        public WmoPortal(IBinaryReader reader)
        {
            startVetex = reader.ReadUInt16();
            count = reader.ReadUInt16();
            var planeNormal = reader.ReadVector3();
            var planeDistance = reader.ReadFloat();
            plane = new Plane(planeNormal, planeDistance);
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

    public readonly struct WorldMapObjectMaterial
    {
        [Flags]
        public enum Flags
        {
            unlit = 1,
            unfogged = 2,
            unculled = 4,
            extllight = 8,
            brightAtNight = 16,
            window = 32,
            clamp_s = 64,
            clamp_t = 128
        }
    
        public readonly Flags flags;
        public readonly uint shader;                         // Index into CMapObj::s_wmoShaderMetaData. See below (shader types).
        public readonly GxBlendMode blendMode;                      // Blending: see EGxBlend
        public readonly uint texture_1;                      // offset into MOTX; ≥ Battle (8.1.0.27826) No longer references MOTX but is a filedata id directly.
        public readonly CImVector sidnColor;                    // emissive color; see below (emissive color)
        public readonly CImVector frameSidnColor;               // sidn emissive color; set at runtime; gets sidn-manipulated emissive color; see below (emissive color)
        public readonly uint texture_2;                      // offset into MOTX
        public readonly CArgb diffColor;
        public readonly uint ground_type;                    // according to CMapObjDef::GetGroundType 
        public readonly uint texture_3;                      // offset into MOTX
        public readonly uint color_2;
        public readonly uint flags_2;
        //public readonly uint[] runTimeData { get; init; }                 // This data is explicitly nulled upon loading. Contains textures or similar stuff.

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
            reader.Offset += 4 * 4;
            //runTimeData = new uint[] { reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32() };

            texture2Name = null;
            texture3Name = null;
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

        public Vector4 ToRgbaVector()
        {
            return new Vector4(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
        }

        internal CArgb(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
        
        public static CArgb FromRGB(uint bgra)
        {
            return new CArgb(
                (byte)((bgra & 0xFF0000) >> 16),
                (byte)((bgra & 0xFF00) >> 8),
                (byte)((bgra & 0xFF)),
                255);
        }
        
        public static CArgb FromBGR(uint bgra)
        {
            return new CArgb(
                (byte)((bgra & 0xFF)),
                (byte)((bgra & 0xFF00) >> 8),
                (byte)((bgra & 0xFF0000) >> 16),
                255);
        }
        
        public static CArgb FromBGRA(uint bgra)
        {
            return new CArgb(
                (byte)((bgra & 0x0000FF00) >> 8),
                (byte)((bgra & 0x00FF0000) >> 16),
                (byte)((bgra & 0xFF000000) >> 24),
                (byte)((bgra & 0x000000FF)));
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

    public readonly struct WMOHeader
    {
        public readonly uint nTextures;    
        public readonly uint nGroups;    
        public readonly uint nPortals;   
        public readonly uint nLights;                                        // Blizzard seems to add one to the MOLT entry count when there are MOLP chunks in the groups (and maybe for MOLS too?)ᵘ
        public readonly uint nDoodadNames; 
        public readonly uint nDoodadDefs;                                    // *
        public readonly uint nDoodadSets;    
        public readonly uint ambColorRgba;                                         // Color settings for base (ambient) color. See the flag at /*03Ch*/.   
        public readonly uint wmoID;
        public readonly CAaBox bounding_box;                                    // in the alpha, this bounding box was computed upon loading
        public readonly WorldMapObjectFlags flags;
        public readonly ushort lodCount;

        public WMOHeader(IBinaryReader reader, GameFilesVersion version)
        {
            nTextures = reader.ReadUInt32();
            nGroups = reader.ReadUInt32();
            nPortals = reader.ReadUInt32();
            nLights = reader.ReadUInt32();
            nDoodadNames = reader.ReadUInt32();
            nDoodadDefs = reader.ReadUInt32();
            nDoodadSets = reader.ReadUInt32();
            ambColorRgba = reader.ReadUInt32();
            wmoID = reader.ReadUInt32();
            bounding_box = CAaBox.Read(reader);
            flags = (WorldMapObjectFlags)reader.ReadUInt16();
            if (version >= GameFilesVersion.Legion_7_3_5)
                lodCount = reader.ReadUInt16();
            else
                lodCount = 0;
        }

        public static WMOHeader Read(IBinaryReader reader, GameFilesVersion version)
        {
            return new WMOHeader(reader, version);
        }
    }
}