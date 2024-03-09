using System.Runtime.CompilerServices;
using OpenGLBindings;

namespace TheAvaloniaOpenGL
{
    public class RealDeviceWrapper : IDevice
    {
        private RealDevice device;

        public RealDeviceWrapper(RealDevice device)
        {
            this.device = device;
        }

        public void Begin()
        {
        }
    
        public void DeleteVertexArrays(int count, int[] buffers)
        {
            device.DeleteVertexArrays(count, buffers);
        }
        public void BindVertexArray(int array)
        {
            device.BindVertexArray(array);
        }

        public int GetInteger(GetPName n)
        {
            return device.GetInteger(n);
        }

        public void GenVertexArrays(int n, int[] rv)
        {
            device.GenVertexArrays(n, rv);
        }
        public void GenerateMipmap(TextureTarget target)
        {
            device.GenerateMipmap(target);
        }
        public void Uniform1I(int location, int lalue)
        {
            device.Uniform1I(location, lalue);
        }
        public unsafe   void GetProgramiv(int program, GetProgramParameterName pname, int* prams)
        {
            device.GetProgramiv(program, pname, prams);
        }
        public void GenSamplers(int len, int[] rv)
        {
            device.GenSamplers(len, rv);
        }
        public void BindSampler(int unit, int sampler)
        {
            device.BindSampler(unit, sampler);
        }
        public void UniformBlockBinding(int program, int uniformBlockIndex, int uniformBlockBinding)
        {
            device.UniformBlockBinding(program, uniformBlockIndex, uniformBlockBinding);
        }
        public void BindBufferBase(BufferRangeTarget target, int index, int buffer)
        {
            device.BindBufferBase(target, index, buffer);
        }
        public void CullFace(CullFaceMode mode)
        {
            device.CullFace(mode);
        }
        public void Disable(EnableCap mode)
        {
            device.Disable(mode);
        }
        public void Enable(EnableCap mode)
        {
            device.Enable(mode);
        }
        public void BindBuffer(BufferTarget target, int buffer)
        {
            device.BindBuffer(target, buffer);
        }
        public void BufferData(BufferTarget target, IntPtr size, IntPtr data, BufferUsageHint usage)
        {
            device.BufferData(target, size, data, usage);
        }
        public void BufferSubData(BufferTarget target, IntPtr offset, IntPtr size, IntPtr data)
        {
            device.BufferSubData(target, offset, size, data);
        }
        public void ActiveTexture(TextureUnit texture)
        {
            device.ActiveTexture(texture);
        }
        public void BindTexture(TextureTarget target, int fb)
        {
            device.BindTexture(target, fb);
        }
        public unsafe   void DeleteTextures(int count, int* textures)
        {
            device.DeleteTextures(count, textures);
        }
        public unsafe   void DeleteBuffers(int count, int* buffers)
        {
            device.DeleteBuffers(count, buffers);
        }
        public unsafe   void GenFramebuffers(int count, int* res)
        {
            device.GenFramebuffers(count, res);
        }
        public unsafe   void DeleteFramebuffers(int count, int* framebuffers)
        {
            device.DeleteFramebuffers(count, framebuffers);
        }
        public void BindFramebuffer(FramebufferTarget target, int fb)
        {
            device.BindFramebuffer(target, fb);
        }
        public unsafe   void GenRenderbuffers(int count, int* res)
        {
            device.GenRenderbuffers(count, res);
        }
        public unsafe   void DeleteRenderbuffers(int count, int* renderbuffers)
        {
            device.DeleteRenderbuffers(count, renderbuffers);
        }
        public void BindRenderbuffer(RenderbufferTarget target, int fb)
        {
            device.BindRenderbuffer(target, fb);
        }
        public void FramebufferTexture2D(FramebufferTarget target,FramebufferAttachment attachment,TextureTarget texTarget,int texture,int level)
        {
            device.FramebufferTexture2D(target, attachment, texTarget, texture, level);
        }
        public void RenderbufferStorage(RenderbufferTarget target, RenderbufferStorage internalFormat, int width, int height)
        {
            device.RenderbufferStorage(target, internalFormat, width, height);
        }
        public void FramebufferRenderbuffer(FramebufferTarget target, FramebufferAttachment attachment, RenderbufferTarget renderbufferTarget, int renderbuffer)
        {
            device.FramebufferRenderbuffer(target, attachment, renderbufferTarget, renderbuffer);
        }
        public void ClearStencil(int buffer)
        {
            device.ClearStencil(buffer);
        }
        public void ClearColor(float r, float g, float b, float a)
        {
            device.ClearColor(r, g, b, a);
        }
        public void Clear(ClearBufferMask bits)
        {
            device.Clear(bits);
        }
        public void Viewport(int x, int y, int width, int height)
        {
            device.Viewport(x, y, width, height);
        }

        public void BlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, ClearBufferMask mask, BlitFramebufferFilter filter)
        {
            device.BlitFramebuffer(srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, filter);
        }

        public void TexImage3D(TextureTarget target, int level, InternalFormat internalFormat, int width, int height, int depth, int border, PixelFormat format, PixelType type, IntPtr data)
        {
            device.TexImage3D(target, level, internalFormat, width, height, depth, border, format, type, data);
        }
        public void TexSubImage2D(TextureTarget target, int level, int xoffset, int yoffset, int width, int height, PixelFormat format, PixelType type, IntPtr data)
        {
            device.TexSubImage2D(target, level, xoffset, yoffset, width, height, format, type, data);
        }
        public void DrawElementsInstanced(PrimitiveType mode, int count, DrawElementsType type, IntPtr indices, int instancesCount)
        {
            device.DrawElementsInstanced(mode, count, type, indices, instancesCount);
        }
        public void ClampColor(ClampColorTarget target, ClampColorMode clamp)
        {
            device.ClampColor(target, clamp);
        }
        public void TexBuffer(TextureBufferTarget target, SizedInternalFormat internalformat, int buffer)
        {
            device.TexBuffer(target, internalformat, buffer);
        }
        public void ValidateProgram(int program)
        {
            device.ValidateProgram(program);
        }
        
        public int GetAttribLocation(int program, string name)
        {
            var loc = device.GetAttribLocation(program, name);
            return loc;
        }
    
        public void VertexAttribPointer(int index, int size, VertexAttribPointerType type, bool normalized, int stride, IntPtr pointer)
        {
            device.VertexAttribPointer(index, size, type, normalized, stride, pointer);
        }
        public void EnableVertexAttribArray(int index)
        {
            device.EnableVertexAttribArray(index);
        }
        public void UseProgram(int program)
        {
            device.UseProgram(program);
        }

        public void ReadBuffer(ReadBufferMode buffer)
        {
            device.ReadBuffer(buffer);
        }

        public unsafe void ReadPixels<T>(int x, int y, int width, int height, PixelFormat format, PixelType type, Span<T> data) where T : unmanaged
        {
            device.ReadPixels(x, y, width, height, format, type, data);
        }

        public void DrawBuffers(ReadOnlySpan<DrawBuffersEnum> buffers)
        {
            device.DrawBuffers(buffers);
        }

        public void DrawArrays(PrimitiveType mode, int first, IntPtr count)
        {
            device.DrawArrays(mode, first, count);
        }
        public void DrawElements(PrimitiveType mode, int count, DrawElementsType type, IntPtr startIndexLocation)
        {
            device.DrawElements(mode, count, type, startIndexLocation);
        }

        public void DrawElementsBaseVertex(PrimitiveType mode, int count, DrawElementsType type, IntPtr startIndexLocation, int startVertexLocationBytes)
        {
            device.DrawElementsBaseVertex(mode, count, type, startIndexLocation, startVertexLocationBytes);
        }

        public int GetUniformLocation(int program, string name)
        {
            var loc = device.GetUniformLocation(program, name);
            return loc;
        }
        
        public void Uniform1f(int location, float falue)
        {
            device.Uniform1f(location, falue);
        }
        
        public void Uniform4f(int location, float a, float b, float c, float d)
        {
            device.Uniform4f(location, a, b, c, d);
        }

        public void Uniform3f(int loc, float a, float b, float c)
        {
            device.Uniform3f(loc, a, b, c);
        }

        public unsafe void UniformMatrix4f(int location, ref Matrix4x4 m, bool transpose)
        {
            device.UniformMatrix4fv(location, 1, transpose, (float*)Unsafe.AsPointer(ref m));
        }

        public void TexImage2D(TextureTarget target, int level, PixelInternalFormat internalFormat, int width, int height, int border, PixelFormat format, PixelType type, IntPtr data)
        {
            device.TexImage2D(target, level, internalFormat, width, height, border, format, type, data);
        }
        public void CopyTexSubImage2D(TextureTarget target, int level, int xoffset, int yoffset, int x, int y, int width, int height)
        {
            device.CopyTexSubImage2D(target, level, xoffset, yoffset, x, y, width, height);
        }
        public void TexParameteri(TextureTarget target, TextureParameterName name, int value)
        {
            device.TexParameteri(target, name, value);
        }
        public unsafe   string GetProgramInfoLog(int program, int maxLength = 2048)
        {
            return device.GetProgramInfoLog(program, maxLength);
        }

        public void BlendEquation(BlendEquationMode mode)
        { 
            device.BlendEquation(mode);
        }

        public void BlendFuncSeparate(BlendingFactorSrc srcRGB, BlendingFactorDest dstRGB, BlendingFactorSrc srcAlpha, BlendingFactorDest dstAlpha)
        {
            device.BlendFuncSeparate(srcRGB, dstRGB, srcAlpha, dstAlpha);
        }

        public int CreateProgram()
        {
            return device.CreateProgram();
        }
        public void AttachShader(int program, int shader)
        {
            device.AttachShader(program, shader);
        }
        public void LinkProgram(int program)
        {
            device.LinkProgram(program);
        }
        public int CreateShader(ShaderType shaderType)
        {
            return device.CreateShader(shaderType);
        }
        public void BlendFunc(BlendingFactorSrc src, BlendingFactorDest dst)
        {
            device.BlendFunc(src, dst);
        }
    
        public string CompileShaderAndGetError(int shader, string source)
        {
            return device.CompileShaderAndGetError(shader, source);
        }

        public string LinkProgramAndGetError(int program)
        {
            return device.LinkProgramAndGetError(program);
        }
    
        public void CheckError(string what)
        {
            device.CheckError(what);
        }

        public int GenVertexArray()
        {
            return device.GenVertexArray();
        }

        public int GenBuffer()
        {
            return device.GenBuffer();
        }

        public int GenTexture()
        {
            return device.GenTexture();
        }
    
        public void ActiveTextureUnit(uint slot)
        {
            device.ActiveTextureUnit(slot);
        }

        public void ActiveTextureUnit(int slot)
        {
            device.ActiveTextureUnit(slot);
        }

        public void DeleteProgram(int program)
        {
            device.DeleteProgram(program);
        }

        public unsafe void DeleteTexture(int name)
        {
            device.DeleteTexture(name);
        }

        public unsafe void DeleteBuffer(int name)
        {
            device.DeleteBuffer(name);
        }

        public unsafe int GenFramebuffer()
        {
            return device.GenFramebuffer();
        }

        public unsafe void DeleteFramebuffer(int name)
        {
            device.DeleteFramebuffer(name);
        }

        public unsafe int GenRenderbuffer()
        {
            return device.GenRenderbuffer();
        }

        public unsafe void DeleteRenderbuffer(int name)
        {
            device.DeleteRenderbuffer(name);
        }

        public int GetUniformBlockIndex(int program, string uniformBlockName)
        {
            return device.GetUniformBlockIndex(program, uniformBlockName);
        }

        public void TexParameter(TextureTarget target, TextureParameterName name, int value)
        {
            device.TexParameter(target, name, value);
        }

        public unsafe int GetProgramParameter(int program, GetProgramParameterName pname)
        {
            return device.GetProgramParameter(program, pname);
        }
    
        public unsafe string GetActiveUniform(int unit, int index, int maxLength, out int length, out int size, out ActiveUniformType type)
        {
            return device.GetActiveUniform(unit, index, maxLength, out length, out size, out type);
        }

        public void DepthMask(bool @on)
        {
            device.DepthMask(@on ? 1 : 0);
        }

        public void DepthFunction(DepthFunction func)
        {
            device.DepthFunc(func);
        }

        public void Scissor(int x, int y, int width, int height)
        {
            device.Scissor(x, y, width, height);
        }

        public void Flush()
        {
            device.Flush();
        }

        public void Finish()
        {
            device.Finish();
        }

        public void Debug(string msg)
        {
            
        }
    }
}