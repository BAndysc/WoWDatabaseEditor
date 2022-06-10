using System;
using System.Collections.Generic;
using OpenGLBindings;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.Handles;
using TheMaths;

namespace TheEngine.Entities
{
    public enum CullingMode
    {
        Front,
        Back,
        Off
    }
    
    // openGL values on purpose
    public enum DepthCompare
    {
        Never = 0x200,
        Less,
        Equal,
        Lequal,
        Greater,
        Notequal,
        Gequal,
        Always
    }
    
    
    // openGL values on purpose
    public enum Blending
    {
        Zero = 0,
        SrcColor = 768,
        OneMinusSrcColor = 769,
        SrcAlpha = 770,
        OneMinusSrcAlpha = 771,
        DstAlpha = 772,
        OneMinusDstAlpha = 773,
        DstColor = 774,
        OneMinusDstColor = 775,
        SrcAlphaSaturate = 776,
        ConstantColor = 32769,
        OneMinusConstantColor = 32770,
        ConstantAlpha = 32771,
        OneMinusConstantAlpha = 32772,
        Src1Alpha = 34185,
        Src1Color = 35065,
        OneMinusSrc1Color = 35066,
        OneMinusSrc1Alpha = 35067,
        One = 1
    }

    public class Material
    {
        private readonly Engine engine;
        private readonly ShaderHandle shaderHandle;
        private readonly ShaderHandle? instancedShaderHandle;

        private Shader shader;
        private Shader? instancedShader;
        public bool ZWrite = true;
        public DepthCompare DepthTesting = DepthCompare.Lequal;
        public CullingMode Culling = CullingMode.Back;
        public bool BlendingEnabled = false;
        public Blending SourceBlending = Blending.One;
        public Blending DestinationBlending = Blending.Zero;

        internal Shader Shader => shader;
        internal Shader? InstancedShader => instancedShader;

        public MaterialHandle Handle { get; }
        public ShaderHandle ShaderHandle => shaderHandle;

        internal Dictionary<int, TextureHandle> textureHandles { get; } = new();
        internal Dictionary<int, INativeBuffer> structuredBuffers { get; } = new();
        internal Dictionary<int, int> intUniforms { get; } = new();
        internal Dictionary<int, float> floatUniforms { get; } = new();
        internal Dictionary<int, Vector4> vector4Uniforms { get; } = new();
        internal Dictionary<int, Vector3> vector3Uniforms { get; } = new();
        internal Dictionary<int, Matrix> matrixUniforms { get; } = new();
        
        internal Dictionary<int, TextureHandle> instancedTextureHandles { get; } = new();
        internal Dictionary<int, INativeBuffer> instancedStructuredBuffers { get; } = new();
        internal Dictionary<int, int> instancedIntUniforms { get; } = new();
        internal Dictionary<int, float> instancedFloatUniforms { get; } = new();
        internal Dictionary<int, Vector4> instancedVector4Uniforms { get; } = new();
        internal Dictionary<int, Vector3> instancedVector3Uniforms { get; } = new();
        internal Dictionary<int, Matrix> instancedMatrixUniforms { get; } = new();
        
        internal Material(Engine engine, ShaderHandle shaderHandle, ShaderHandle? instancedShaderHandle, MaterialHandle materialHandle)
        {
            this.engine = engine;
            Handle = materialHandle;
            this.shaderHandle = shaderHandle;
            this.instancedShaderHandle = instancedShaderHandle;
            this.shader = engine.shaderManager.GetShaderByHandle(shaderHandle);
            this.instancedShader = instancedShaderHandle.HasValue ? engine.shaderManager.GetShaderByHandle(instancedShaderHandle.Value) : null;
            ZWrite = shader.ZWrite;
            DepthTesting = (DepthCompare)shader.DepthTest;
        }

        public void InvalidateShaderCache()
        {
            shader = engine.shaderManager.GetShaderByHandle(shaderHandle);
            instancedShader = instancedShaderHandle.HasValue ? engine.shaderManager.GetShaderByHandle(instancedShaderHandle.Value) : null;
        }

        // public void SetStructuredBuffer<T>(int index, T[] data, StructuredBufferMode mode = StructuredBufferMode.VertexPixel) where T : unmanaged
        // {
        //     var bufferMode = BufferTypeEnum.StructuredBuffer;
        //     if (mode == StructuredBufferMode.PixelOnly)
        //         bufferMode = BufferTypeEnum.StructuredBufferPixelOnly;
        //     else if (mode == StructuredBufferMode.VertexOnly)
        //         bufferMode = BufferTypeEnum.StructuredBufferVertexOnly;
        //
        //     INativeBuffer buffer = engine.Device.CreateBuffer<T>(bufferMode, data);
        //
        //     if (mode == StructuredBufferMode.PixelOnly)
        //     {
        //         structuredPixelsBuffers[index] = buffer;
        //     }
        //     else if (mode == StructuredBufferMode.VertexOnly)
        //     {
        //         structuredVertexBuffers[index] = buffer;
        //     }
        //     else
        //     {
        //         structuredBuffers[index] = buffer;
        //     }
        // }

        public bool HasInstanceUniform(string name)
        {
            return instancedShader != null && instancedShader.GetUniformLocation(name).HasValue;
        }
        
        public int? GetInstancedUniformLocation(string name)
        {
            if (instancedShader == null)
                return null;
            var instancedLoc = instancedShader.GetUniformLocation(name);
            if (!instancedLoc.HasValue)
                throw new Exception("Location " + name + " not found");
            return instancedLoc.Value;
        }
        
        public int GetUniformLocation(string name)
        {
            var loc = shader.GetUniformLocation(name);
            if (!loc.HasValue)
                throw new Exception("Location " + name + " not found");
            return loc.Value;
        }

        private void Set<T>(Dictionary<int, T> dict, Dictionary<int, T> instanced, string name, T type)
        {
            var loc = GetUniformLocation(name);
            if (loc != -1)
                dict[loc] = type;
            var instLoc = GetInstancedUniformLocation(name);
            if (instLoc.HasValue && instLoc != -1)
                instanced[instLoc.Value] = type;
        }

        public void SetBuffer(string name, INativeBuffer buffer)
        {
            Set(structuredBuffers, instancedStructuredBuffers, name, buffer);
        }
        
        public void SetUniformInt(string name, int value)
        {
            Set(intUniforms, instancedIntUniforms, name, value);
        }

        public void SetUniform(string name, float value)
        {
            Set(floatUniforms, instancedFloatUniforms, name, value);
        }
        
        public void SetUniform(string name, Vector3 value)
        {
            Set(vector3Uniforms, instancedVector3Uniforms, name, value);
        }
        
        public void SetUniform(string name, Vector4 value)
        {
            Set(vector4Uniforms, instancedVector4Uniforms, name, value);
        }
        
        public void SetUniform(string name, Matrix value)
        {
            Set(matrixUniforms, instancedMatrixUniforms, name, value);
        }
        
        public void SetTexture(string name, TextureHandle texture)
        {
            Set(textureHandles, instancedTextureHandles, name, texture);
        }
        
        public TextureHandle GetTexture(string name)
        {
            return textureHandles[GetUniformLocation(name)];
        }
        
        public INativeBuffer GetBuffer(string name)
        {
            return structuredBuffers[GetUniformLocation(name)];
        }

        public float GetUniformFloat(string name)
        {
            return floatUniforms[GetUniformLocation(name)];
        }
        
        public void ActivateUniforms(bool instanced, MaterialInstanceRenderData? instanceData = null)
        {
            int slot = 0;
            // done in RenderManager
            // shader.Activate();

            if (instanced)
            {
                foreach (var buffer in instancedStructuredBuffers)
                {
                    if (instanceData != null && instanceData.instancedStructuredBuffers != null &&
                        instanceData.instancedStructuredBuffers.ContainsKey(buffer.Key))
                        continue;
                    buffer.Value.Activate(slot);
                    instancedShader!.SetUniformInt(buffer.Key, slot);
                    slot++;
                }
                foreach (var pair in instancedTextureHandles)
                {
                    var texture = engine.textureManager.GetTextureByHandle(pair.Value);
                    texture.Activate(slot);
                    instancedShader!.SetUniformInt(pair.Key, slot);
                    slot++;
                }
                
                foreach (var floats in instancedFloatUniforms)
                {
                    instancedShader!.SetUniform(floats.Key, floats.Value);
                }
            
                foreach (var ints in instancedIntUniforms)
                {
                    instancedShader!.SetUniformInt(ints.Key, ints.Value);
                }
            
                foreach (var vector in instancedVector4Uniforms)
                {
                    instancedShader!.SetUniform(vector.Key, vector.Value.X, vector.Value.Y, vector.Value.Z, vector.Value.W);
                }
            
                foreach (var vector in instancedVector3Uniforms)
                {
                    instancedShader!.SetUniform(vector.Key, vector.Value.X, vector.Value.Y, vector.Value.Z);
                }
                
                foreach (var vector in instancedMatrixUniforms)
                {
                    instancedShader!.SetUniform(vector.Key, vector.Value);
                }
            }
            else
            {
                foreach (var buffer in structuredBuffers)
                {
                    if (instanceData != null && instanceData.structuredBuffers != null &&
                        instanceData.structuredBuffers.ContainsKey(buffer.Key))
                        continue;
                    buffer.Value.Activate(slot);
                    shader.SetUniformInt(buffer.Key, slot);
                    slot++;
                }
                foreach (var pair in textureHandles)
                {
                    var texture = engine.textureManager.GetTextureByHandle(pair.Value);
                    texture.Activate(slot);
                    shader.SetUniformInt(pair.Key, slot);
                    slot++;
                }
                foreach (var floats in floatUniforms)
                {
                    shader.SetUniform(floats.Key, floats.Value);
                }
            
                foreach (var ints in intUniforms)
                {
                    shader.SetUniformInt(ints.Key, ints.Value);
                }
            
                foreach (var vector in vector4Uniforms)
                {
                    shader.SetUniform(vector.Key, vector.Value.X, vector.Value.Y, vector.Value.Z, vector.Value.W);
                }
            
                foreach (var vector in vector3Uniforms)
                {
                    shader.SetUniform(vector.Key, vector.Value.X, vector.Value.Y, vector.Value.Z);
                }
                
                foreach (var vector in matrixUniforms)
                {
                    shader.SetUniform(vector.Key, vector.Value);
                }
            }
            
            
            instanceData?.Activate(this, instanced, slot);
        }
        
        public enum StructuredBufferMode
        {
            VertexOnly,
            PixelOnly,
            VertexPixel
        }
    }
}
