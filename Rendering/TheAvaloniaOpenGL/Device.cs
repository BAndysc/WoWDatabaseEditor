using System.Text;
using Avalonia.OpenGL;
using Avalonia.Platform.Interop;
using OpenGLBindings;
using BlitFramebufferFilter = OpenGLBindings.BlitFramebufferFilter;
using ClearBufferMask = OpenGLBindings.ClearBufferMask;
using CullFaceMode = OpenGLBindings.CullFaceMode;
using DepthFunction = OpenGLBindings.DepthFunction;
using DrawElementsType = OpenGLBindings.DrawElementsType;
using EnableCap = OpenGLBindings.EnableCap;
using ErrorCode = OpenGLBindings.ErrorCode;
using FramebufferAttachment = OpenGLBindings.FramebufferAttachment;
using FramebufferTarget = OpenGLBindings.FramebufferTarget;
using GetPName = OpenGLBindings.GetPName;
using InternalFormat = OpenGLBindings.InternalFormat;
using PixelFormat = OpenGLBindings.PixelFormat;
using PixelType = OpenGLBindings.PixelType;
using PrimitiveType = OpenGLBindings.PrimitiveType;
using ReadBufferMode = OpenGLBindings.ReadBufferMode;
using RenderbufferTarget = OpenGLBindings.RenderbufferTarget;
using ShaderType = OpenGLBindings.ShaderType;
using SizedInternalFormat = OpenGLBindings.SizedInternalFormat;
using TextureParameterName = OpenGLBindings.TextureParameterName;
using TextureTarget = OpenGLBindings.TextureTarget;
using TextureUnit = OpenGLBindings.TextureUnit;
using VertexAttribPointerType = OpenGLBindings.VertexAttribPointerType;

// ReSharper disable StringLiteralTypo
// ReSharper disable InconsistentNaming

// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable IdentifierTypo

namespace TheAvaloniaOpenGL
{
    public unsafe class RealDevice
    {
        private GlInterface Gl { get; }
        private int maxTextureImageUnits;
        
        public RealDevice(GlInterface gl)
        {
            Gl = gl;
            maxTextureImageUnits = GetInteger(GetPName.MaxTextureImageUnits);
            Console.WriteLine("Max texture units: " + maxTextureImageUnits);
            GlGenerateMipmap = (delegate* unmanaged[Stdcall]<int, void>)gl.GetProcAddress("glGenerateMipmap");
            addr_Uniform1I = (delegate* unmanaged[Stdcall]<int, int, void>)Gl.GetProcAddress("glUniform1i");
            addr_GenSamplers = (delegate* unmanaged[Stdcall]<int, int*, void>)Gl.GetProcAddress("glGenSamplers");
            addr_BindSampler = (delegate* unmanaged[Stdcall]<int, int, void>)Gl.GetProcAddress("glBindSampler");
            addr_GetUniformBlockIndex = (delegate* unmanaged[Stdcall]<int, IntPtr, int>)Gl.GetProcAddress("glGetUniformBlockIndex");
            addr_UniformBlockBinding = (delegate* unmanaged[Stdcall]<int, int, int, void>)Gl.GetProcAddress("glUniformBlockBinding");
            addr_BindBufferBase = (delegate* unmanaged[Stdcall]<BufferRangeTarget, int, int, void>)Gl.GetProcAddress("glBindBufferBase");
            addr_GlCullFace = (delegate* unmanaged[Stdcall]<CullFaceMode, void>)Gl.GetProcAddress("glCullFace");
            addr_GlDisable = (delegate* unmanaged[Stdcall]<EnableCap, void>)Gl.GetProcAddress("glDisable");
            addr_GlEnable = (delegate* unmanaged[Stdcall]<EnableCap, void>)Gl.GetProcAddress("glEnable");
            addr_BindBuffer = (delegate* unmanaged[Stdcall]<BufferTarget, int, void>)Gl.GetProcAddress("glBindBuffer");
            addr_BufferData = (delegate* unmanaged[Stdcall]<BufferTarget, IntPtr, IntPtr, BufferUsageHint, void>)Gl.GetProcAddress("glBufferData");
            addr_BufferSubData = (delegate* unmanaged[Stdcall]<BufferTarget, IntPtr, IntPtr, IntPtr, void>)Gl.GetProcAddress("glBufferSubData");
            addr_GlActiveTexture = (delegate* unmanaged[Stdcall]<TextureUnit, void>)Gl.GetProcAddress("glActiveTexture");
            addr_BindTexture = (delegate* unmanaged[Stdcall]<TextureTarget, int, void>)Gl.GetProcAddress("glBindTexture");
            addr_BindFramebuffer = (delegate* unmanaged[Stdcall]<FramebufferTarget, int, void>)Gl.GetProcAddress("glBindFramebuffer");
            addr_BindRenderbuffer = (delegate* unmanaged[Stdcall]<RenderbufferTarget, int, void>)Gl.GetProcAddress("glBindRenderbuffer");
            addr_GlClearStencil = (delegate* unmanaged[Stdcall]<int, void>)Gl.GetProcAddress("glClearStencil");
            addr_ClearColor = (delegate* unmanaged[Stdcall]<float, float, float, float, void>)Gl.GetProcAddress("glClearColor");
            addr_GlClear = (delegate* unmanaged[Stdcall]<ClearBufferMask, void>)Gl.GetProcAddress("glClear");
            addr_Viewport = (delegate* unmanaged[Stdcall]<int, int, int, int, void>)Gl.GetProcAddress("glViewport");
            addr_DrawElementsInstanced = (delegate* unmanaged[Stdcall]<PrimitiveType, int, DrawElementsType, IntPtr, int, void>)Gl.GetProcAddress("glDrawElementsInstanced");
            addr_ClampColor = (delegate* unmanaged[Stdcall]<ClampColorTarget, ClampColorMode, void>)Gl.GetProcAddress("glClampColor");
            addr_TexBuffer = (delegate* unmanaged[Stdcall]<TextureBufferTarget, SizedInternalFormat, int, void>)Gl.GetProcAddress("glTexBuffer");
            addr_GlValidateProgram = (delegate* unmanaged[Stdcall]<int, void>)Gl.GetProcAddress("glValidateProgram");
            addr_GetAttribLocation = (delegate* unmanaged[Stdcall]<int, IntPtr, int>)Gl.GetProcAddress("glGetAttribLocation");
            addr_GlEnableVertexAttribArray = (delegate* unmanaged[Stdcall]<int, void>)Gl.GetProcAddress("glEnableVertexAttribArray");
            addr_GlUseProgram = (delegate* unmanaged[Stdcall]<int, void>)Gl.GetProcAddress("glUseProgram");
            addr_GlDepthMask = (delegate* unmanaged[Stdcall]<int, void>)Gl.GetProcAddress("glDepthMask");
            addr_DrawArrays = (delegate* unmanaged[Stdcall]<PrimitiveType, int, IntPtr, void>)Gl.GetProcAddress("glDrawArrays");
            addr_DrawElements = (delegate* unmanaged[Stdcall]<PrimitiveType, int, DrawElementsType, IntPtr, void>)Gl.GetProcAddress("glDrawElements");
            addr_DrawElementsBaseVertex = (delegate* unmanaged[Stdcall]<PrimitiveType, int, DrawElementsType, IntPtr, int, void>)Gl.GetProcAddress("glDrawElementsBaseVertex");
            addr_GetUniformLocation = (delegate* unmanaged[Stdcall]<int, IntPtr, int>)Gl.GetProcAddress("glGetUniformLocation");
            addr_Uniform1f = (delegate* unmanaged[Stdcall]<int, float, void>)Gl.GetProcAddress("glUniform1f");
            addr_Uniform4f = (delegate* unmanaged[Stdcall]<int, float, float, float, float, void>)Gl.GetProcAddress("glUniform4f");
            addr_Uniform3f = (delegate* unmanaged[Stdcall]<int, float, float, float, void>)Gl.GetProcAddress("glUniform3f");
            addr_TexParameteri = (delegate* unmanaged[Stdcall]<TextureTarget, TextureParameterName, int, void>)Gl.GetProcAddress("glTexParameteri");
            addr_GlDeleteProgram = (delegate* unmanaged[Stdcall]<int, void>)Gl.GetProcAddress("glDeleteProgram");
            addr_AttachShader = (delegate* unmanaged[Stdcall]<int, int, void>)Gl.GetProcAddress("glAttachShader");
            addr_GlLinkProgram = (delegate* unmanaged[Stdcall]<int, void>)Gl.GetProcAddress("glLinkProgram");
            addr_GlCreateShader = (delegate* unmanaged[Stdcall]<ShaderType, int>)Gl.GetProcAddress("glCreateShader");
            addr_BlendFunc = (delegate* unmanaged[Stdcall]<BlendingFactorSrc, BlendingFactorDest, void>)Gl.GetProcAddress("glBlendFunc");
            addr_BlendFuncSeparate = (delegate* unmanaged[Stdcall]<BlendingFactorSrc, BlendingFactorDest, BlendingFactorSrc, BlendingFactorDest, void>)Gl.GetProcAddress("glBlendFuncSeparate");
            addr_GlBlendEquation = (delegate* unmanaged[Stdcall]<BlendEquationMode, void>)Gl.GetProcAddress("glBlendEquation");
            addr_BlitFramebuffer = (delegate* unmanaged[Stdcall]<int, int, int, int, int, int, int, int, ClearBufferMask, BlitFramebufferFilter, void>)Gl.GetProcAddress("glBlitFramebuffer");
            addr_GlDepthFunc = (delegate* unmanaged[Stdcall]<DepthFunction, void>)Gl.GetProcAddress("glDepthFunc");
            addr_Scissor = (delegate* unmanaged[Stdcall]<int, int, int, int, void>)Gl.GetProcAddress("glScissor");
            addr_DrawBuffers = (delegate* unmanaged[Stdcall]<int, IntPtr, void>)Gl.GetProcAddress("glDrawBuffers");
            addr_GlReadBuffer = (delegate* unmanaged[Stdcall]<ReadBufferMode, void>)Gl.GetProcAddress("glReadBuffer");
            addr_ReadPixels = (delegate* unmanaged[Stdcall]<int, int, int, int, PixelFormat, PixelType, IntPtr, void>)Gl.GetProcAddress("glReadPixels");
            addr_GetProgramiv = (delegate* unmanaged[Stdcall]<int, GetProgramParameterName, int*, void>)Gl.GetProcAddress("glGetProgramiv");
            addr_GetActiveUniform = (delegate* unmanaged[Stdcall]<int, int, int, out int, out int, out ActiveUniformType, void*, void>)Gl.GetProcAddress("glGetActiveUniform");
            addr_TexImage3D = (delegate* unmanaged[Stdcall]<TextureTarget, int, InternalFormat, int, int, int, int, PixelFormat, PixelType, IntPtr, void>)Gl.GetProcAddress("glTexImage3D");
            addr_TexImage2D = (delegate* unmanaged[Stdcall]<TextureTarget, int, int, int, int, int, PixelFormat, PixelType, IntPtr, void>)Gl.GetProcAddress("glTexSubImage2D");
            addr_UniformMatrix4fv = (delegate* unmanaged[Stdcall]<int, int, int, float*, void>)Gl.GetProcAddress("glUniformMatrix4fv");
        }

        public void DeleteVertexArrays(int count, int[] args)
        {
            fixed (int* ptr = args)
                Gl.DeleteVertexArrays(count, ptr);
        }

        public void BindVertexArray(int array)
        {
            Gl.BindVertexArray(array);
        }
        
        public int GetInteger(GetPName name)
        {
            int i;
            Gl.GetIntegerv((int)name, out i);
            return i;
        }
        
        public void GenVertexArrays(int n, int[] rv)
        {
            fixed (int* ptr = rv)
                Gl.GenVertexArrays(n, ptr);
        }


        public void GenerateMipmap(TextureTarget target)
        {
            GlGenerateMipmap((int)target);
        }
        delegate* unmanaged[Stdcall]<int, void> GlGenerateMipmap;

        delegate* unmanaged[Stdcall]<int, int, void> addr_Uniform1I;
        public void Uniform1I(int location, int lalue) => addr_Uniform1I(location, lalue);

        delegate* unmanaged[Stdcall]<int, GetProgramParameterName, int*, void> addr_GetProgramiv;
        public void GetProgramiv(int program, GetProgramParameterName pname, int* prams)
        {
            addr_GetProgramiv(program, pname, prams);            
        }
        
        public int GetProgramParameter(int program, GetProgramParameterName pname)
        {
            int x;
            GetProgramiv(program, pname, &x);
            return x;
        }
        
        delegate* unmanaged[Stdcall]<int, int*, void> addr_GenSamplers;
        public void GenSamplers(int len, int[] rv)
        {
            fixed(int* ptr = rv)
                addr_GenSamplers(len, ptr);
        }

        public int GenSampler()
        {
            int[] rv = new int[1];
            this.GenSamplers(1, rv);
            return rv[0];
        }
        
        delegate* unmanaged[Stdcall]<int, int, void> addr_BindSampler;
        public void BindSampler(int unit, int sampler)
        {
            addr_BindSampler(unit, sampler);
        }
        
        delegate* unmanaged[Stdcall]<int, int, int, out int, out int, out ActiveUniformType, void*, void> addr_GetActiveUniform;
        public void GetActiveUniform(int unit, int index, int bufSize, out int length, out int size, out ActiveUniformType type, void* name)
        {
            addr_GetActiveUniform(unit, index, bufSize, out length, out size, out type, name);
        }
        
        public string GetActiveUniform(int unit, int index, int maxLength, out int length, out int size, out ActiveUniformType type)
        {
            byte[] bytes = new byte[maxLength];
            fixed (byte* infoLog = bytes)
                GetActiveUniform(unit, index, maxLength, out length,  out size, out type, (void*) infoLog);
            return Encoding.UTF8.GetString(bytes, 0, length);
        }
        
        public string GetProgramInfoLog(int program, int maxLength = 2048)
        {
            byte[] bytes = new byte[maxLength];
            int length;
            fixed (byte* infoLog = bytes)
                Gl.GetProgramInfoLog(program, maxLength, out length, infoLog);
            return Encoding.UTF8.GetString(bytes, 0, length);
        }
        
        delegate* unmanaged[Stdcall]<int, IntPtr, int> addr_GetUniformBlockIndex;
        public int GetUniformBlockIndex_(int program, IntPtr uniformBlockName)
        {
            return addr_GetUniformBlockIndex(program, uniformBlockName);
        }

        public int GetUniformBlockIndex(int program, string uniformBlockName)
        {
            var bytes = Encoding.UTF8.GetByteCount(uniformBlockName);
            Span<byte> buffer = stackalloc byte[bytes + 1];
            Encoding.UTF8.GetBytes(uniformBlockName, buffer);
            
            fixed (byte* ptr = buffer)
                return GetUniformBlockIndex_(program, new IntPtr(ptr));
        }
        
        delegate* unmanaged[Stdcall]<int, int, int, void> addr_UniformBlockBinding;
        public void UniformBlockBinding(int program, int uniformBlockIndex, int uniformBlockBinding)
        {
            addr_UniformBlockBinding(program, uniformBlockIndex, uniformBlockBinding);
        }

        delegate* unmanaged[Stdcall]<BufferRangeTarget, int, int, void> addr_BindBufferBase;
        public void BindBufferBase(BufferRangeTarget target, int index, int buffer)
        {
            addr_BindBufferBase(target, index, buffer);
        }
        
        delegate* unmanaged[Stdcall]<CullFaceMode, void> addr_GlCullFace;
        public void CullFace(CullFaceMode mode)
        {
            addr_GlCullFace(mode);
        }
        
        delegate* unmanaged[Stdcall]<EnableCap, void> addr_GlDisable;
        public void Disable(EnableCap mode)
        {
            addr_GlDisable(mode);
        }
        
        delegate* unmanaged[Stdcall]<EnableCap, void> addr_GlEnable;
        public void Enable(EnableCap mode)
        {
            addr_GlEnable(mode);
        }

        public int GenBuffer() => Gl.GenBuffer();
        
        delegate* unmanaged[Stdcall]<BufferTarget, int, void> addr_BindBuffer;
        public void BindBuffer(BufferTarget target, int buffer)
        {
            addr_BindBuffer(target, buffer);
        }
        
        delegate* unmanaged[Stdcall]<BufferTarget, IntPtr, IntPtr, BufferUsageHint, void> addr_BufferData;
        public void BufferData(BufferTarget target, IntPtr size, IntPtr data, BufferUsageHint usage)
        { 
            addr_BufferData(target, size, data, usage);
        }
        
        delegate* unmanaged[Stdcall]<BufferTarget, IntPtr, IntPtr, IntPtr, void> addr_BufferSubData;
        public void BufferSubData(BufferTarget target, IntPtr offset, IntPtr size, IntPtr data)
        {
            addr_BufferSubData(target, offset, size, data);
        }
        
        
        delegate* unmanaged[Stdcall]<TextureUnit, void> addr_GlActiveTexture;
        public void ActiveTexture(TextureUnit texture)
        {
            addr_GlActiveTexture(texture);
        }

        private int currentTextureSlot = -1;
        
        public void ActiveTextureUnit(uint slot)
        {
            if (currentTextureSlot == (int)slot)
                return;
            //if (slot >= maxTextureImageUnits)
            //    throw new Exception("Texture unit exceding your GPU limit :(");
            ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + slot));
            currentTextureSlot = (int)slot;
        }

        public void ActiveTextureUnit(int slot)
        {
            if (currentTextureSlot == (int)slot)
                return;
            //if (slot >= maxTextureImageUnits)
            //    throw new Exception("Texture unit exceding your GPU limit :(");
            ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + slot));
            currentTextureSlot = (int)slot;
        }

        delegate* unmanaged[Stdcall]<TextureTarget, int, void> addr_BindTexture;
        public void BindTexture(TextureTarget target, int fb)
        {
            addr_BindTexture(target, fb);
        }

        public void DeleteTextures(int count, int* textures)
        {
            Gl.DeleteTextures(count, textures);
        }
            
        public void DeleteTexture(int name) => DeleteTextures(1, &name);

        public void DeleteBuffers(int count, int* buffers)
        {
            Gl.DeleteBuffers(count, buffers);
        }
        
        public void DeleteBuffer(int name) => DeleteBuffers(1, &name);
        
        public void GenFramebuffers(int count, int* res)
        {
            Gl.GenFramebuffers(count, res);
        }

        public int GenFramebuffer()
        {
            int x;
            GenFramebuffers(1, &x);
            return x;
        }

        public void DeleteFramebuffers(int count, int* framebuffers)
        {
            Gl.DeleteFramebuffers(count, framebuffers);
        }

        public void DeleteFramebuffer(int name) => DeleteFramebuffers(1, &name);
        
        delegate* unmanaged[Stdcall]<FramebufferTarget, int, void> addr_BindFramebuffer;
        public void BindFramebuffer(FramebufferTarget target, int fb)
        {
            addr_BindFramebuffer(target, fb);
        }

        public void GenRenderbuffers(int count, int* res)
        {
            Gl.GenRenderbuffers(count, res);
        }

        public int GenRenderbuffer()
        {
            int x;
            GenRenderbuffers(1, &x);
            return x;
        }

        public void DeleteRenderbuffers(int count, int* renderbuffers)
        {
            Gl.DeleteRenderbuffers(count, renderbuffers);
        }

        public void DeleteRenderbuffer(int name) => DeleteRenderbuffers(1, &name);

        delegate* unmanaged[Stdcall]<RenderbufferTarget, int, void> addr_BindRenderbuffer;
        public void BindRenderbuffer(RenderbufferTarget target, int fb)
        {
            addr_BindRenderbuffer(target, fb);
        }

        public void FramebufferTexture2D(
            FramebufferTarget target,
            FramebufferAttachment attachment,
            TextureTarget texTarget,
            int texture,
            int level)
        {
            Gl.FramebufferTexture2D((int)target, (int)attachment, (int)texTarget, texture, level);    
        }
        
        public void RenderbufferStorage(
            RenderbufferTarget target,
            RenderbufferStorage internalFormat,
            int width,
            int height)
        {
            Gl.RenderbufferStorage((int)target, (int)internalFormat, width, height);
        }
        
        public void FramebufferRenderbuffer(
            FramebufferTarget target,
            FramebufferAttachment attachment,
            RenderbufferTarget renderbufferTarget,
            int renderbuffer)
        {
            Gl.FramebufferRenderbuffer((int)target, (int)attachment, (int)renderbufferTarget, renderbuffer);
        }
        
        
        delegate* unmanaged[Stdcall]<int, void> addr_GlClearStencil;
        public void ClearStencil(int buffer)
        {
             addr_GlClearStencil(buffer);
        }

        delegate* unmanaged[Stdcall]<float, float, float, float, void> addr_ClearColor;
        public void ClearColor(float r, float g, float b, float a)
        {
            addr_ClearColor(r, g, b, a);
        }

        delegate* unmanaged[Stdcall]<ClearBufferMask, void> addr_GlClear;
        public void Clear(ClearBufferMask bits)
        {
            addr_GlClear(bits);
        }

        delegate* unmanaged[Stdcall]<int, int, int, int, void> addr_Viewport;
        public void Viewport(int x, int y, int width, int height)
        {
             addr_Viewport(x, y, width, height);
        }
        
        delegate* unmanaged[Stdcall]<TextureTarget, int, InternalFormat, int, int, int, int, PixelFormat, PixelType, IntPtr, void> addr_TexImage3D;

        public void TexImage3D(
            TextureTarget target,
            int level,
            InternalFormat internalFormat,
            int width,
            int height,
            int depth,
            int border,
            PixelFormat format,
            PixelType type,
            IntPtr data)
        {
            addr_TexImage3D(target, level, internalFormat, width, height, depth, border, format, type, data);
        }
        
        
        delegate* unmanaged[Stdcall]<TextureTarget, int, int, int, int, int, PixelFormat, PixelType, IntPtr, void> addr_TexImage2D;
        public void TexSubImage2D(
            TextureTarget target,
            int level,
            int xoffset,
            int yoffset,
            int width,
            int height,
            PixelFormat format,
            PixelType type,
            IntPtr data)
        {
            addr_TexImage2D(target, level, xoffset, yoffset, width, height, format, type, data);
        }

        delegate* unmanaged[Stdcall]<PrimitiveType, int, DrawElementsType, IntPtr, int, void> addr_DrawElementsInstanced;
        public void DrawElementsInstanced(PrimitiveType mode, int count, DrawElementsType type, IntPtr indices, int instancesCount)
        {
            addr_DrawElementsInstanced(mode, count, type, indices, instancesCount);
        }
        
        delegate* unmanaged[Stdcall]<ClampColorTarget, ClampColorMode, void> addr_ClampColor;
        public void ClampColor(ClampColorTarget target, ClampColorMode clamp)
        {
            addr_ClampColor(target, clamp);
        }
        
        delegate* unmanaged[Stdcall]<TextureBufferTarget, SizedInternalFormat, int, void> addr_TexBuffer;
        public void TexBuffer(TextureBufferTarget target, SizedInternalFormat internalformat, int buffer)
        {
            addr_TexBuffer(target, internalformat, buffer);
        }
        
        delegate* unmanaged[Stdcall]<int, void> addr_GlValidateProgram;
        public void ValidateProgram(int program)
        {
            addr_GlValidateProgram(program);
        }
        
        delegate* unmanaged[Stdcall]<int, IntPtr, int> addr_GetAttribLocation;
        public int GetAttribLocation(int program, IntPtr name)
        {
            return addr_GetAttribLocation(program, name);
        }

        public int GetAttribLocation(int program, string name)
        {
            var bytes = Encoding.UTF8.GetByteCount(name);
            Span<byte> buffer = stackalloc byte[bytes + 1];
            Encoding.UTF8.GetBytes(name, buffer);
            
            fixed (byte* ptr = buffer)
                return GetAttribLocation(program, new IntPtr(ptr));
        }

        public void VertexAttribPointer(
            int index,
            int size,
            VertexAttribPointerType type,
            bool normalized,
            int stride,
            IntPtr pointer)
        {
            Gl.VertexAttribPointer(index, size, (int)type, normalized ? 1 : 0, stride, pointer);
        }

        delegate* unmanaged[Stdcall]<int, void> addr_GlEnableVertexAttribArray;
        public void EnableVertexAttribArray(int index)
        {
            addr_GlEnableVertexAttribArray(index);
        }

        delegate* unmanaged[Stdcall]<int, void> addr_GlUseProgram;
        public void UseProgram(int program)
        {
            addr_GlUseProgram(program);
        }
        

        delegate* unmanaged[Stdcall]<int, void> addr_GlDepthMask;
        public void DepthMask(int on)
        {
            addr_GlDepthMask(on);
        } 
        
        delegate* unmanaged[Stdcall]<PrimitiveType, int, IntPtr, void> addr_DrawArrays;
        public void DrawArrays(PrimitiveType mode, int first, IntPtr count)
        {
            addr_DrawArrays(mode, first, count);
        }
        
        delegate* unmanaged[Stdcall]<PrimitiveType, int, DrawElementsType, IntPtr, void> addr_DrawElements;
        public void DrawElements(PrimitiveType mode, int count, DrawElementsType type, IntPtr indices)
        {
            addr_DrawElements(mode, count, type, indices);
        }
        
        delegate* unmanaged[Stdcall]<PrimitiveType, int, DrawElementsType, IntPtr, int, void> addr_DrawElementsBaseVertex;
        public void DrawElementsBaseVertex(PrimitiveType mode, int count, DrawElementsType type, IntPtr startIndexLocation, int startVertexPosBytes)
        {
            addr_DrawElementsBaseVertex(mode, count, type, startIndexLocation, startVertexPosBytes);
        }

        delegate* unmanaged[Stdcall]<int, int, int, float*, void> addr_UniformMatrix4fv;
        public void UniformMatrix4fv(int location, int count, bool transpose, float* ptr)
        {
            addr_UniformMatrix4fv(location, count, transpose ? 1 : 0, ptr);
        }

        
        delegate* unmanaged[Stdcall]<int, IntPtr, int> addr_GetUniformLocation;
        public int GetUniformLocation(int program, IntPtr name)
        {
            return addr_GetUniformLocation(program, name);
        }
        
        public int GetUniformLocation(int program, string name)
        {
            var bytes = Encoding.UTF8.GetByteCount(name);
            Span<byte> buffer = stackalloc byte[bytes + 1];
            Encoding.UTF8.GetBytes(name, buffer);
            
            fixed (byte* ptr = buffer)
                return GetUniformLocation(program, new IntPtr(ptr));
        }

        delegate* unmanaged[Stdcall]<int, float, void> addr_Uniform1f;
        public void Uniform1f(int location, float falue)
        {
            addr_Uniform1f(location, falue);
        }
        
        delegate* unmanaged[Stdcall]<int, float, float, float, float, void> addr_Uniform4f;
        public void Uniform4f(int location, float a, float b, float c, float d)
        {
            addr_Uniform4f(location, a, b, c, d);
        }


        delegate* unmanaged[Stdcall]<int, float, float, float, void> addr_Uniform3f;
        public void Uniform3f(int location, float a, float b, float c)
        {
            addr_Uniform3f(location, a, b, c);
        }

        public void TexImage2D(
            TextureTarget target,
            int level,
            PixelInternalFormat internalFormat,
            int width,
            int height,
            int border,
            PixelFormat format,
            PixelType type,
            IntPtr data)
        {
            Gl.TexImage2D((int)target, level, (int)internalFormat, width, height, border, (int)format, (int)type, data);
        }

        public void CopyTexSubImage2D(
            TextureTarget target,
            int level,
            int xoffset,
            int yoffset,
            int x,
            int y,
            int width,
            int height)
        {
            Gl.CopyTexSubImage2D((int)target, level, xoffset, yoffset, x, y, width, height);
        }

        delegate* unmanaged[Stdcall]<TextureTarget, TextureParameterName, int, void> addr_TexParameteri;
        public void TexParameteri(TextureTarget target, TextureParameterName name, int value)
        {
            addr_TexParameteri(target, name, value);
        }

        public void TexParameter(TextureTarget target, TextureParameterName name, int value) =>
            TexParameteri(target, name, value);

        public delegate void GlGetProgramInfoLog(
            int program,
            int maxLength,
            out int len,
            void* infoLog);

        public int CreateProgram()
        {
            return Gl.CreateProgram();
        }
        
        delegate* unmanaged[Stdcall]<int, void> addr_GlDeleteProgram;
        public void DeleteProgram(int program)
        {
            addr_GlDeleteProgram(program);
        }

        delegate* unmanaged[Stdcall]<int, int, void> addr_AttachShader;
        public void AttachShader(int program, int shader)
        {
            addr_AttachShader(program, shader);
        }

        delegate* unmanaged[Stdcall]<int, void> addr_GlLinkProgram;
        public void LinkProgram(int program)
        {
            addr_GlLinkProgram(program);
        }

        delegate* unmanaged[Stdcall]<ShaderType, int> addr_GlCreateShader;
        public int CreateShader(ShaderType shaderType)
        {
            return addr_GlCreateShader(shaderType);
        }
        
        public string CompileShaderAndGetError(int shader, string source)
        {
            return Gl.CompileShaderAndGetError(shader, source);
        }

        public string LinkProgramAndGetError(int program)
        {
            return Gl.LinkProgramAndGetError(program);
        }
        
        delegate* unmanaged[Stdcall]<BlendingFactorSrc, BlendingFactorDest, void> addr_BlendFunc;
        public void BlendFunc(BlendingFactorSrc src, BlendingFactorDest dst)
        {
            addr_BlendFunc(src, dst);
        }
        
        delegate* unmanaged[Stdcall]<BlendingFactorSrc, BlendingFactorDest, BlendingFactorSrc, BlendingFactorDest, void> addr_BlendFuncSeparate;
        public void BlendFuncSeparate(BlendingFactorSrc srcRGB, BlendingFactorDest dstRGB, BlendingFactorSrc srcAlpha, BlendingFactorDest dstAlpha)
        {
            addr_BlendFuncSeparate(srcRGB, dstRGB, srcAlpha, dstAlpha);
        }

        delegate* unmanaged[Stdcall]<BlendEquationMode, void> addr_GlBlendEquation;
        public void BlendEquation(BlendEquationMode mode)
        {
            addr_GlBlendEquation(mode);
        }

        delegate* unmanaged[Stdcall]<int, int, int, int, int, int, int, int, ClearBufferMask, BlitFramebufferFilter, void> addr_BlitFramebuffer;
        public void BlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, ClearBufferMask mask, BlitFramebufferFilter filter)
        {
            addr_BlitFramebuffer(srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, filter);
        }
        
        delegate* unmanaged[Stdcall]<DepthFunction, void> addr_GlDepthFunc;
        public void DepthFunc(DepthFunction func)
        {
            addr_GlDepthFunc(func);
        }
        
        delegate* unmanaged[Stdcall]<int, int, int, int, void> addr_Scissor;
        public void Scissor(int x, int y, int width, int height)
        {
            addr_Scissor(x, y, width, height);
        }
        
        delegate* unmanaged[Stdcall]<int, IntPtr, void> addr_DrawBuffers;
        public void DrawBuffers(int n, IntPtr bufs)
        {
            addr_DrawBuffers(n, bufs);
        }
        
        delegate* unmanaged[Stdcall]<ReadBufferMode, void> addr_GlReadBuffer;
        public void ReadBuffer(ReadBufferMode mode)
        {
            addr_GlReadBuffer(mode);
        }
        
        delegate* unmanaged[Stdcall]<int, int, int, int, PixelFormat, PixelType, IntPtr, void> addr_ReadPixels;
        public void ReadPixels(int x, int y, int width, int height, PixelFormat format, PixelType type, IntPtr data)
        {
            addr_ReadPixels(x, y, width, height, format, type, data);
        }

        public void Finish()
        {
            Gl.Finish();
        }

        public void Flush()
        {
            Gl.Flush();
        }

        public void CheckError(string what)
        {
            int err;
            while ((err = Gl.GetError()) != (int)ErrorCode.NoError)
                Console.WriteLine(what + ": " + ((ErrorCode)err).ToString());
        }
        
        
        public int GenVertexArray()
        {
            var rv = new int[1];
            GenVertexArrays(1, rv);
            return rv[0];
        }

        public int GenTexture()
        {
            return Gl.GenTexture();
        }

        public void DrawBuffers(ReadOnlySpan<DrawBuffersEnum> buffers)
        {
            fixed (DrawBuffersEnum* ptr = buffers)
                DrawBuffers(buffers.Length, (IntPtr)ptr);
        }

        public void ReadPixels<T>(int x, int y, int width, int height, PixelFormat format, PixelType type, Span<T> data) where T : unmanaged
        {
            fixed (void* ptr = data)
                ReadPixels(x, y, width, height, format, type, (IntPtr)ptr); 
        }
    }
}