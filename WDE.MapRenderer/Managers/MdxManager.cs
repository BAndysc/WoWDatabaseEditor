using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheEngine.Coroutines;
using TheEngine.Data;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheMaths;
using WDE.MpqReader;
using WDE.MpqReader.Readers;
using WDE.MpqReader.Structures;
using WDE.MapRenderer.Managers;
using System.IO;

namespace WDE.MapRenderer.Managers
{
    enum ModelPixelShader
    {
        Combiners_Opaque = 0,
        Combiners_Decal,
        Combiners_Add,
        Combiners_Mod2x,
        Combiners_Fade,
        Combiners_Mod,
        Combiners_Opaque_Opaque,
        Combiners_Opaque_Add,
        Combiners_Opaque_Mod2x,
        Combiners_Opaque_Mod2xNA,
        Combiners_Opaque_AddNA,
        Combiners_Opaque_Mod,
        Combiners_Mod_Opaque,
        Combiners_Mod_Add,
        Combiners_Mod_Mod2x,
        Combiners_Mod_Mod2xNA,
        Combiners_Mod_AddNA,
        Combiners_Mod_Mod,
        Combiners_Add_Mod,
        Combiners_Mod2x_Mod2x,
        Combiners_Opaque_Mod2xNA_Alpha,
        Combiners_Opaque_AddAlpha,
        Combiners_Opaque_AddAlpha_Alpha,
    };

    
    public class MdxManager : System.IDisposable
    {
        public class MdxInstance
        {
            public IMesh mesh;
            public Material[] materials;

            public void Dispose(IMeshManager meshManager)
            {
                meshManager.DisposeMesh(mesh);
            }
        }

        private Dictionary<string, MdxInstance?> meshes = new();
        private Dictionary<uint, MdxInstance?> creaturemeshes = new();
        private Dictionary<string, Task<MdxInstance?>> meshesCurrentlyLoaded = new();
        private Dictionary<uint, Task<MdxInstance?>> creaturemeshesCurrentlyLoaded = new();
        private readonly IGameFiles gameFiles;
        private readonly IMeshManager meshManager;
        private readonly IMaterialManager materialManager;
        private readonly WoWTextureManager textureManager;
        private readonly CreatureDisplayInfoStore creatureDisplayInfoStore;
        private readonly CreatureDisplayInfoExtraStore creatureDisplayInfoExtraStore;
        private readonly ItemDisplayInfoStore itemDisplayInfoStore;
        private readonly CharHairGeosetsStore charHairGeosetsStore;
        private readonly CharacterFacialHairStylesStore characterFacialHairStylesStore;
        private readonly CharSectionsStore charSectionsStore;

        public MdxManager(IGameFiles gameFiles, 
            IMeshManager meshManager, 
            IMaterialManager materialManager,
            WoWTextureManager textureManager,
            CreatureDisplayInfoStore creatureDisplayInfoStore,
            CreatureDisplayInfoExtraStore creatureDisplayInfoExtraStore,
            ItemDisplayInfoStore itemDisplayInfoStore,
            CharHairGeosetsStore charHairGeosetsStore,
            CharacterFacialHairStylesStore characterFacialHairStylesStore,
            CharSectionsStore charSectionsStore)
        {
            this.gameFiles = gameFiles;
            this.meshManager = meshManager;
            this.materialManager = materialManager;
            this.textureManager = textureManager;
            this.creatureDisplayInfoStore = creatureDisplayInfoStore;
            this.creatureDisplayInfoExtraStore = creatureDisplayInfoExtraStore;
            this.itemDisplayInfoStore = itemDisplayInfoStore;
            this.charHairGeosetsStore = charHairGeosetsStore;
            this.characterFacialHairStylesStore = characterFacialHairStylesStore;
            this.charSectionsStore = charSectionsStore;
        }

        public IEnumerator LoadM2Mesh(string path, TaskCompletionSource<MdxInstance?> result, uint displayid = 0)
        {
            //if (meshes.ContainsKey(path))
            if (displayid == 0 && meshes.ContainsKey(path))
            {
                result.SetResult(meshes[path]);
                yield break;
            }
            // titi test
            else if (displayid > 0 && creaturemeshes.ContainsKey(displayid))
            {
                result.SetResult(creaturemeshes[displayid]);
                yield break;
            }

            // if (meshesCurrentlyLoaded.TryGetValue(path, out var loadInProgress))
            if (displayid == 0 && meshesCurrentlyLoaded.TryGetValue(path, out var loadInProgress))
            {
                yield return new WaitForTask(loadInProgress);
                result.SetResult(meshes[path]);
                yield break;
            }

            // titi test
            else if (displayid > 0 && creaturemeshesCurrentlyLoaded.TryGetValue(displayid, out var crloadInProgress))
            {
                yield return new WaitForTask(crloadInProgress);
                result.SetResult(creaturemeshes[displayid]);
                yield break;
            }

            var completion = new TaskCompletionSource<MdxInstance?>();
            if (displayid == 0)
                meshesCurrentlyLoaded[path] = completion.Task;
            else
                creaturemeshesCurrentlyLoaded[displayid] = completion.Task;


            var m2FilePath = path.Replace("mdx", "M2", StringComparison.InvariantCultureIgnoreCase);
            m2FilePath = m2FilePath.Replace("mdl", "M2", StringComparison.InvariantCultureIgnoreCase); // apaprently there are still some MDL models

            var skinFilePath = m2FilePath.Replace(".m2", "00.skin", StringComparison.InvariantCultureIgnoreCase);
            var file = gameFiles.ReadFile(m2FilePath);

            yield return new WaitForTask(file);

            var skinFile = gameFiles.ReadFile(skinFilePath);
            
            yield return new WaitForTask(skinFile);
            
            if (file.Result == null || skinFile.Result == null)
            {
                Console.WriteLine("Cannot find model " + path);
                meshes[path] = null;
                creaturemeshes[displayid] = null;
                completion.SetResult(null);
                meshesCurrentlyLoaded.Remove(path);
                creaturemeshesCurrentlyLoaded.Remove(displayid);
                result.SetResult(null);
                yield break;
            }

            M2 m2 = null!;
            M2Skin skin = null!;
            Vector3[] vertices = null!;
            Vector3[] normals = null!;
            Vector2[] uv1 = null!;
            Vector2[] uv2 = null!;

            yield return new WaitForTask(Task.Run(() =>
            {
                m2 = M2.Read(new MemoryBinaryReader(file.Result));
                file.Result.Dispose();
                skin = M2Skin.Read(new MemoryBinaryReader(skinFile.Result));
                skinFile.Result.Dispose();
                var count = m2.vertices.Length;
                vertices = new Vector3[count];
                normals = new Vector3[count];
                uv1 = new Vector2[count];
                uv2 = new Vector2[count];

                for (int i = 0; i < count; ++i)
                {
                    var vert = m2.vertices[skin.Vertices[i]];
                    vertices[i] = vert.pos;
                    normals[i] = vert.normal;
                    uv1[i] = vert.tex_coords[0];
                    uv2[i] = vert.tex_coords[1];
                }
            }));

            var md = new MeshData(vertices, normals, uv1, new int[] { }, null, null, uv2);
            
            var mesh = meshManager.CreateMesh(md);
            mesh.SetSubmeshCount(skin.Batches.Length);

            // titi : set active batches/sections
            int[] activeskinsections = Array.Empty<int>();

            // Sources : wowdev.wiki/DB/ItemDisplayInfo#Geoset_Group_Field_Meaning and wowdev.wiki/Character_Customization#Geosets
            int geosetSkin = 0;
            int geosetHair = 1;
            int geosetFacial1 = 101;
            int geosetFacial2 = 201;
            int geosetFacial3 = 301;
            int geosetGlove = 401;  // {0: No Geoset; 1: Default; 2: Thin; 3: Folded; 4: Thick}
            int geosetBoots = 501; // {0: No Geoset; 1: Default; 2: High Boot; 3: Folded Boot; 4: Puffed; 5: Boot 4}
            int geosetTail = 600; 
            int geosetEars = 700;
            int geosetSleeves = 801; // {1: Default (No Geoset); 2: Flared Sleeve; 3: Puffy Sleeve; 4: Panda Collar Shirt}
            int geosetlegcuffs = 901; // {1: Default (No Geoset); 2: Flared Pant Cuff; 3: Knickers; 4: Panda Pants}
            int geosetChest = 1001; // {1: Default (No Geoset); 2: Doublet; 3: Body 2; 4: Body 3}
            int geosetpants = 1101; // {1: Default (No Geoset); 2: Mini Skirt; 4: Heavy}
            int geosetTabard = 1201; // {1: Default (No Geoset); 2: Tabard}
            int geosetTrousers = 1301; // {0: No Geoset; 1: Default; 2: Long Skirt}
            int geosetFemaleLoincloth = 1400; // DH/Pandaren female Loincloth
            int geosetCloak = 1501; // {1: Default (No Geoset); 2: Ankle Length; 3: Knee Length; 4: Split Banner; 5: Tapered Waist;
                                    // 6: Notched Back; 7: Guild Cloak; 8: Split (Long); 9: Tapered (Long); 10: Notched (Long)}
            int geosetNoseEarrings = 1600; // geoset4 in CharacterFacialHairStyles ?
            int geosetEyeglows = 1700; // small\big ears for BloodElves
            int geosetBelt = 1801; // {0: No Geoset; 1: Default; 2: Heavy Belt; 3: Panda Cord Belt}
            int geosetBone = 1900; // ?
            int geosetFeet = 2001; // {0: No Geoset; 1: Default (Basic Shoes); 2: Toes}
            int geosetHead = 2101; // {0: No Geoset; 1: Show Head}. default = show head ?
            int geosetTorso = 2201;// {0: No Geoset; 1: Default; 2: Covered Torso}
            int geosetHandsAttachments = 2301;  // {0: No Geoset; 1: Default}
            int geosetHeadAttachments = 2400;
            int geosetBlindfolds = 2500;
            int geosetShoulders = 2600; // {0: No Geoset; 1: Show Shoulders, SL+ 2: Non-Mythic only; 3: Mythic}
            int geosetHelm = 2701; // {1: Default (No Geoset); 2: Helm 1; SL+ 3: Non-Mythic only; 4: Mythic}
            int geosetUNK28 = 2801; // {0: No Geoset; 1: Default}
            // BFA/SL+ geosets.

            // 1 : load items to define active geosets
            // 2 : if no item, set default geosets
            bool ischaractermodel = false;

            if (displayid > 0)
            {
                if (creatureDisplayInfoStore.Contains(displayid))
                {
                    if (creatureDisplayInfoStore[displayid].ExtendedDisplayInfoID > 0)
                    {
                        if (creatureDisplayInfoExtraStore.Contains(creatureDisplayInfoStore[displayid].ExtendedDisplayInfoID))
                        {
                            ischaractermodel = true;
                            // load items to define active geosets
                            var displayinfoextra = creatureDisplayInfoExtraStore[
                                creatureDisplayInfoStore[displayid].ExtendedDisplayInfoID];

                            // TODO : SKIN SECTIONS !!!!!!!

                            // TODO :  CharHairGeosets.dbc

                            // hair
                            if (displayinfoextra.HairStyle > 0)
                            {
                                int hairstyle = charHairGeosetsStore.First(x => x.RaceID == displayinfoextra.Race
                                && x.SexId == displayinfoextra.Gender && x.VariationId + 1 == displayinfoextra.HairStyle).GeosetId; // maybe +1 like beards ?
                                
                                geosetHair += hairstyle;
                                // use CharHairGeosetsStore.ShowScalp (bald) or is it only some client stuff ?
                            }

                            // facial hair
                            if (displayinfoextra.BeardStyle > 0)
                            {
                                // System.Diagnostics.Debug.WriteLine($"facial hair stylrs count  :  {characterFacialHairStylesStore.Count}");

                                CharacterFacialHairStyles facialhairstyle = characterFacialHairStylesStore.First(x => x.RaceID == displayinfoextra.Race
                                && x.SexId == displayinfoextra.Gender && x.VariationId +1 == displayinfoextra.BeardStyle );
                                
                                geosetFacial1 += facialhairstyle.Geoset1;
                                geosetFacial2 += facialhairstyle.Geoset3; // apparently this is group 3 ? verify in game.
                                geosetFacial3 += facialhairstyle.Geoset2;
                                geosetNoseEarrings += facialhairstyle.Geoset4;
                                geosetEyeglows += facialhairstyle.Geoset5;
                            }

                            if (displayinfoextra.Helm > 0)
                                geosetHelm = 2702 + itemDisplayInfoStore[(uint)displayinfoextra.Helm].geosetGroup1;
                                // geoset group 2 ? some enable/disable 2100 (head) ?

                            if (displayinfoextra.Shoulder > 0)
                                geosetShoulders = 2601 + itemDisplayInfoStore[(uint)displayinfoextra.Shoulder].geosetGroup1;

                            if (displayinfoextra.Shirt > 0)
                            {
                                geosetSleeves = 801 + itemDisplayInfoStore[(uint)displayinfoextra.Shirt].geosetGroup1;
                                geosetChest = 1001 + itemDisplayInfoStore[(uint)displayinfoextra.Shirt].geosetGroup2;
                            }
                            if (displayinfoextra.Cuirass > 0)
                            {
                                geosetSleeves = 801 + itemDisplayInfoStore[(uint)displayinfoextra.Cuirass].geosetGroup1;
                                geosetChest = 1001 + itemDisplayInfoStore[(uint)displayinfoextra.Cuirass].geosetGroup2;
                                // 1301 trousers set below
                                // in later expensions, geoset group 4 and 5 ?
                            }

                            if (displayinfoextra.Legs > 0)
                            {
                                geosetpants = 801 + itemDisplayInfoStore[(uint)displayinfoextra.Legs].geosetGroup1;
                                geosetlegcuffs = 1001 + itemDisplayInfoStore[(uint)displayinfoextra.Legs].geosetGroup2;
                                geosetTrousers = 1301 + itemDisplayInfoStore[(uint)displayinfoextra.Legs].geosetGroup3;
                            }

                            if (displayinfoextra.Boots > 0)
                            {
                                geosetBoots = 501 + itemDisplayInfoStore[(uint)displayinfoextra.Boots].geosetGroup1;
                                geosetFeet = 2002 + itemDisplayInfoStore[(uint)displayinfoextra.Boots].geosetGroup2;
                            }

                            if (displayinfoextra.Gloves > 0)
                            {
                                geosetGlove = 401 + itemDisplayInfoStore[(uint)displayinfoextra.Gloves].geosetGroup1;
                                geosetHandsAttachments = 2301 + itemDisplayInfoStore[(uint)displayinfoextra.Gloves].geosetGroup2;
                            }
                            if (displayinfoextra.Cape > 0)
                                geosetCloak = 1501 + itemDisplayInfoStore[(uint)displayinfoextra.Cape].geosetGroup1;

                            if (displayinfoextra.Tabard > 0)
                                geosetTabard = 1201 + itemDisplayInfoStore[(uint)displayinfoextra.Tabard].geosetGroup1;

                            if (displayinfoextra.Belt > 0) // priority : belt > tabard
                                geosetBelt = 1801 + itemDisplayInfoStore[(uint)displayinfoextra.Belt].geosetGroup1;

                            // Priority : Chest geosetGroup[2] (1301 set) > Pants geosetGroup[2] (1301 set) > Boots geosetGroup[0] (501 set) > Pants geosetGroup[1] (901 set)
                            if (displayinfoextra.Cuirass > 0)
                                geosetTrousers = 1301 + itemDisplayInfoStore[(uint)displayinfoextra.Cuirass].geosetGroup3;
                        }
                    }
                }
            }
            activeskinsections = new int[30]{ geosetSkin, geosetHair, geosetFacial1, geosetFacial2, geosetFacial3, geosetGlove, geosetBoots, geosetTail, geosetEars,
                geosetSleeves, geosetlegcuffs, geosetChest, geosetpants, geosetTabard, geosetTrousers, geosetFemaleLoincloth, geosetCloak, geosetNoseEarrings, geosetEyeglows, geosetBelt, geosetBone, geosetFeet,
                geosetHead, geosetTorso, geosetHandsAttachments, geosetHeadAttachments, geosetBlindfolds, geosetShoulders, geosetHelm, geosetUNK28 };

            // TODO : Attachements

            Material[] materials = new Material[skin.Batches.Length];
            int j = 0;
            foreach (var batch in skin.Batches)
            {
                if (batch.skinSectionIndex == ushort.MaxValue ||
                    batch.materialIndex >= m2.materials.Length ||
                    batch.skinSectionIndex >= skin.SkinSections.Length)
                {
                    Console.WriteLine("Sth wrong with batch " + j + " in model " + path);
                    continue;
                }

                var section = skin.SkinSections[batch.skinSectionIndex];

                // titi, check if element is active
                // loading all meshes for non humanoids (crdisplayinfoextra users), might need tob e tweaked
                if (activeskinsections.Contains(section.skinSectionId) == false && ischaractermodel == true)
                    continue;

                using var indices = new PooledArray<int>(section.indexCount);
                for (int i = 0; i < Math.Min(section.indexCount, skin.Indices.Length - section.indexStart); ++i)
                {
                    indices[i] = skin.Indices[section.indexStart + i];
                }

                mesh.SetIndices(indices.AsSpan(), j++);

                TextureHandle? th = null;
                TextureHandle? th2 = null;
                for (int i = 0; i < (batch.textureCount >= 5 ? 1 : batch.textureCount); ++i)
                {
                    if (batch.textureComboIndex + i >= m2.textureCombos.Length)
                    {
                        if (th.HasValue)
                            th2 = textureManager.EmptyTexture;
                        else
                            th = textureManager.EmptyTexture;
                        Console.WriteLine("File " + path + " batch " + j + " tex " + i + " out of range");
                        continue;
                    }
                    var texId = m2.textureCombos[batch.textureComboIndex + i];
                    if (texId == -1)
                    {
                        if (th.HasValue)
                            th2 = textureManager.EmptyTexture;
                        else
                            th = textureManager.EmptyTexture;
                    }
                    else
                    {
                        var textureDef = m2.textures[texId];
                        string texFile = "";
                        
                        if (textureDef.type == 0) // if tetx is hardcoded
                            texFile = textureDef.filename.AsString();

                        // character models
                        if (ischaractermodel == true)
                        {
                            CreatureDisplayInfoExtra displayinfoextra = creatureDisplayInfoExtraStore[
                                creatureDisplayInfoStore[displayid].ExtendedDisplayInfoID];

                            // how the fuck to differenciate between body and face ?
                            if (textureDef.type == M2Texture.TextureType.TEX_COMPONENT_SKIN) // character skin
                            {
                                // This is for player characters... Creatures always come with a baked texture.
                                // texFile = charSectionsStore.First(x => x.RaceID == displayinfoextra.Race
                                // && x.BaseSection == 0 && x.ColorIndex == displayinfoextra.SkinColor).TextureName1;
                                texFile = "textures\\BakedNpcTextures\\" + displayinfoextra.Texture;
                            }

                            // else if (textureDef.type == M2Texture.TextureType.TEX_COMPONENT_SKIN_EXTRA) // character skin
                            // {
                            //     texFile = charSectionsStore.First(x => x.RaceID == displayinfoextra.Race
                            //     && x.BaseSection == 0 && x.ColorIndex == displayinfoextra.SkinColor).TextureName2;
                            // }

                            else if (textureDef.type == M2Texture.TextureType.TEX_COMPONENT_CHAR_HAIR) // character skin
                            {
                                // CharHairTextures or charsection ?
                                // 1st version, it's awful.
                                // string hairid = displayinfoextra.HairColor.ToString();
                                // 
                                // if (displayinfoextra.HairColor < 10)
                                //     hairid = "0" + hairid;
                                // 
                                // var pathsplit = m2FilePath.Split('\\');
                                // texFile = pathsplit[0] + "\\" + pathsplit[1] + "\\Hair00_" + hairid + ".blp";

                                // 2nd way : 
                                texFile = charSectionsStore.First(x => x.RaceID == displayinfoextra.Race && x.SexId == displayinfoextra.Gender
                                    &&  x.ColorIndex == displayinfoextra.HairColor && x.VariationIndex == displayinfoextra.HairStyle && x.BaseSection == 3).TextureName1;
                            }
                        }

                        // TITI, set creature texture
                        else if (textureDef.type == M2Texture.TextureType.TEX_COMPONENT_MONSTER_1) // creature skin1
                        {
                            if (creatureDisplayInfoStore.Contains(displayid))
                            {
                                texFile = m2FilePath.Replace(m2FilePath.Split('\\').Last(), creatureDisplayInfoStore[displayid].TextureVariation1
                                    + ".blp", StringComparison.InvariantCultureIgnoreCase); // replace m2 name by tetxure name
                            }
                        }

                        else if (textureDef.type == M2Texture.TextureType.TEX_COMPONENT_MONSTER_2) // creature skin2
                        {
                            if (creatureDisplayInfoStore.Contains(displayid))
                            {
                                texFile = m2FilePath.Replace(m2FilePath.Split('\\').Last(), creatureDisplayInfoStore[displayid].TextureVariation2
                                    + ".blp", StringComparison.InvariantCultureIgnoreCase); // replace m2 name by tetxure name
                            }
                        }

                        else if (textureDef.type == M2Texture.TextureType.TEX_COMPONENT_MONSTER_3) // creature skin3
                        {
                            if (creatureDisplayInfoStore.Contains(displayid))
                            {
                                texFile = m2FilePath.Replace(m2FilePath.Split('\\').Last(), creatureDisplayInfoStore[displayid].TextureVariation3
                                    + ".blp", StringComparison.InvariantCultureIgnoreCase); // replace m2 name by tetxure name
                            }
                        }

                        // System.Diagnostics.Debug.WriteLine($"M2 texture path :  {texFile}");
                        // var texFile = textureDef.filename.AsString();
                        var tcs = new TaskCompletionSource<TextureHandle>();
                        yield return textureManager.GetTexture(texFile, tcs);
                        var resTex = tcs.Task.Result;
                        if (th.HasValue)
                            th2 = resTex;
                        else
                            th = resTex;
                    }
                }

                var materialDef = m2.materials[batch.materialIndex];
                var material = materialManager.CreateMaterial("data/m2.json");

                material.SetTexture("texture1", th ?? textureManager.EmptyTexture);
                material.SetTexture("texture2", th2 ?? textureManager.EmptyTexture);

                var trans = 1.0f;
                if (batch.colorIndex != -1 && m2.colors.Length < batch.colorIndex)
                {
                    if (m2.colors[batch.colorIndex].alpha.values.Length == 0 || m2.colors[batch.colorIndex].alpha.values[0].Length == 0)
                        trans = 1;
                    else
                        trans = m2.colors[batch.colorIndex].alpha.values[0][0].Value;
                }

                if (batch.transparencyIndex != -1 && m2.texture_weights.Length < batch.transparencyIndex)
                {
                    if (m2.texture_weights[batch.transparencyIndex].weight.values.Length > 0 && m2.texture_weights[batch.transparencyIndex].weight.values[0].Length > 0)
                        trans *= m2.texture_weights[batch.transparencyIndex].weight.values[0][0].Value;
                }
                
                Vector4 mesh_color = new Vector4(1.0f, 1.0f, 1.0f, trans);
    
                material.SetUniform("mesh_color", mesh_color);
                material.SetUniformInt("pixel_shader", (int)(M2GetPixelShaderID(batch.textureCount, batch.shader_id) ?? ModelPixelShader.Combiners_Opaque));
                //Console.WriteLine(path + " INDEX: " + j + " Pixel shader: " + M2GetPixelShaderID(batch.textureCount, batch.shader_id) + " tex count: " + batch.textureCount + " shader id: " + batch.shader_id + " blend: " + materialDef.blending_mode + " priority " + batch.priorityPlane + " start ");
                
                material.SetUniform("highlight", 0);
                material.SetUniform("notSupported", 0);
                if (materialDef.blending_mode == M2Blend.M2BlendOpaque)
                {
                    material.BlendingEnabled = false;
                    material.SetUniform("alphaTest", -1);
                }
                else if (materialDef.blending_mode == M2Blend.M2BlendAlphaKey)
                {
                    material.BlendingEnabled = false;
                    //material.SourceBlending = Blending.One;
                    //material.DestinationBlending = Blending.Zero;
                    material.SetUniform("alphaTest", 224.0f / 255.0f);
                }
                else if (materialDef.blending_mode == M2Blend.M2BlendAlpha)
                {
                    material.BlendingEnabled = true;
                    material.SourceBlending = Blending.SrcAlpha;
                    material.DestinationBlending = Blending.OneMinusSrcAlpha;
                    material.SetUniform("alphaTest", 1.0f / 255.0f);
                }
                else if (materialDef.blending_mode == M2Blend.M2BlendNoAlphaAdd)
                {
                    material.BlendingEnabled = true;
                    material.SourceBlending = Blending.One;
                    material.DestinationBlending = Blending.One;
                    material.SetUniform("alphaTest", 1.0f / 255.0f);
                }
                else if (materialDef.blending_mode == M2Blend.M2BlendAdd)
                {
                    material.BlendingEnabled = true;
                    material.SourceBlending = Blending.SrcAlpha;
                    material.DestinationBlending = Blending.One;
                    material.SetUniform("alphaTest", 1.0f / 255.0f);
                }
                else if (materialDef.blending_mode == M2Blend.M2BlendMod)
                {
                    material.BlendingEnabled = true;
                    material.SourceBlending = Blending.DstColor;
                    material.DestinationBlending = Blending.Zero;
                    material.SetUniform("alphaTest", 1.0f / 255.0f);
                }
                else if (materialDef.blending_mode == M2Blend.M2BlendMod2X)
                {
                    material.BlendingEnabled = true;
                    material.SourceBlending = Blending.DstColor;
                    material.DestinationBlending = Blending.SrcColor;
                    material.SetUniform("alphaTest", 1.0f / 255.0f);
                }
                else
                    material.SetUniform("notSupported", 1);

                material.ZWrite = !material.BlendingEnabled;
                //material.DepthTesting = materialDef.flags.HasFlag(M2MaterialFlags.DepthTest); // produces wrong results :thonk:

                if (materialDef.flags.HasFlag(M2MaterialFlags.TwoSided))
                    material.Culling = CullingMode.Off;
                material.SetUniformInt("unlit", materialDef.flags.HasFlag(M2MaterialFlags.Unlit) ? 1 : 0);

                materials[j - 1] = material;
            }

            mesh.Rebuild();

            var mdx = new MdxInstance
            {
                mesh = mesh,
                materials = materials.AsSpan(0, j).ToArray()
            };
            if (displayid == 0)
                meshes.Add(path, mdx);
            else
                creaturemeshes.Add(displayid, mdx); // titi test
            completion.SetResult(null);
            meshesCurrentlyLoaded.Remove(path);
            // creaturemeshesCurrentlyLoaded.Remove(displayid);
            result.SetResult(mdx);
        }

        // https://wowdev.wiki/M2/.skin/WotLK_shader_selection
        ModelPixelShader? GetPixelShader(ushort texture_count, ushort shader_id)
        {
            ushort texture1_fragment_mode = (ushort)((shader_id >> 4) & 7);
              ushort texture2_fragment_mode = (ushort)(shader_id & 7);
          // uint16_t texture1_env_map = (shader_id >> 4) & 8;
          // uint16_t texture2_env_map = shader_id & 8;

          ModelPixelShader? pixel_shader = null;

          if (texture_count == 1)
          {
            switch (texture1_fragment_mode)
            {
            case 0:
              pixel_shader = ModelPixelShader.Combiners_Opaque;
              break;
            case 2:
              pixel_shader = ModelPixelShader.Combiners_Decal;
              break;
            case 3:
              pixel_shader = ModelPixelShader.Combiners_Add;
              break;
            case 4:
              pixel_shader = ModelPixelShader.Combiners_Mod2x;
              break;
            case 5:
              pixel_shader = ModelPixelShader.Combiners_Fade;
              break;
            default:
              pixel_shader = ModelPixelShader.Combiners_Mod;
              break;
            }
          }
          else
          {
            if (texture1_fragment_mode == 0)
            {
              switch (texture2_fragment_mode)
              {
              case 0:
                pixel_shader = ModelPixelShader.Combiners_Opaque_Opaque;
                break;
              case 3:
                pixel_shader = ModelPixelShader.Combiners_Opaque_Add;
                break;
              case 4:
                pixel_shader = ModelPixelShader.Combiners_Opaque_Mod2x;
                break;
              case 6:
                pixel_shader = ModelPixelShader.Combiners_Opaque_Mod2xNA;
                break;
              case 7:
                pixel_shader = ModelPixelShader.Combiners_Opaque_AddNA;
                break;
              default:
                pixel_shader = ModelPixelShader.Combiners_Opaque_Mod;
                break;
              }
            }
            else if (texture1_fragment_mode == 1)
            {
              switch (texture2_fragment_mode)
              {
              case 0:
                pixel_shader = ModelPixelShader.Combiners_Mod_Opaque;
                break;
              case 3:
                pixel_shader = ModelPixelShader.Combiners_Mod_Add;
                break;
              case 4:
                pixel_shader = ModelPixelShader.Combiners_Mod_Mod2x;
                break;
              case 6:
                pixel_shader = ModelPixelShader.Combiners_Mod_Mod2xNA;
                break;
              case 7:
                pixel_shader = ModelPixelShader.Combiners_Mod_AddNA;
                break;
              default:
                pixel_shader = ModelPixelShader.Combiners_Mod_Mod;
                break;
              }
            }
            else if (texture1_fragment_mode == 3)
            {
              if (texture2_fragment_mode == 1)
              {
                pixel_shader = ModelPixelShader.Combiners_Add_Mod;
              }
            }
            else if (texture1_fragment_mode == 4 && texture2_fragment_mode == 4)
            {
              pixel_shader = ModelPixelShader.Combiners_Mod2x_Mod2x;
            }
            else if (texture2_fragment_mode == 1)
            {
              pixel_shader = ModelPixelShader.Combiners_Mod_Mod2x;
            }
          }
         

          return pixel_shader;
        }
        
        ModelPixelShader? M2GetPixelShaderID(ushort texture_count, ushort shader_id)
        {
            ModelPixelShader? pixel_shader = null;

            if ((shader_id & 0x8000) == 0)
            {
                pixel_shader = GetPixelShader(texture_count, shader_id);

                if (!pixel_shader.HasValue)
                {
                    pixel_shader = GetPixelShader(texture_count, 0x11);
                }
            }
            else
            {
                switch (shader_id & 0x7FFF)
                {
                    case 1:
                        pixel_shader = ModelPixelShader.Combiners_Opaque_Mod2xNA_Alpha;
                        break;
                    case 2:
                        pixel_shader = ModelPixelShader.Combiners_Opaque_AddAlpha;
                        break;
                    case 3:
                        pixel_shader = ModelPixelShader.Combiners_Opaque_AddAlpha_Alpha;
                        break;
                }  
            }

            return pixel_shader;
        }
        
        public void Dispose()
        {
            foreach (var mesh in meshes.Values)
                mesh?.Dispose(meshManager);

            // titi test
            foreach (var creaturemesh in creaturemeshes.Values)
                creaturemesh?.Dispose(meshManager);
        }
    }
}
