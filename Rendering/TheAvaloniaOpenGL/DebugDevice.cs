using System;
using System.Collections.Generic;
using OpenGLBindings;

namespace TheAvaloniaOpenGL
{
    public class DebugDevice : IDevice
    {
        private RealDevice device;

        public List<string> commands = new();

        public DebugDevice(RealDevice device)
        {
            this.device = device;
        }

        public void Begin()
        {
            commands.Clear();
        }
    
        private void Report(string name)
        {
            commands.Add(name);
        }
    
        public void DeleteVertexArrays(int count, int[] buffers)
        {
            Report($"DeleteVertexArrays({count}, {string.Join(", ", buffers)})");
            device.DeleteVertexArrays(count, buffers);
        }
        public void BindVertexArray(int array)
        {
            Report($"BindVertexArray({array})");
            device.BindVertexArray(array);
        }
        public unsafe void GetIntegerv(GetPName n, int* rv)
        {
            device.GetIntegerv(n, rv);
            Report($"GetIntegerv({n}) = {*rv}");
        }
        public void GenVertexArrays(int n, int[] rv)
        {
            Report($"GenVertexArrays({n}, {string.Join(", ", rv)}");
            device.GenVertexArrays(n, rv);
        }
        public void GenerateMipmap(TextureTarget target)
        {
            Report($"GenerateMipmap({target})");
            device.GenerateMipmap(target);
        }
        public void Uniform1I(int location, int lalue)
        {
            Report($"Uniform1I({location}, {lalue})");
            device.Uniform1I(location, lalue);
        }
        public unsafe   void GetProgramiv(int program, GetProgramParameterName pname, int* prams)
        {
            device.GetProgramiv(program, pname, prams);
            Report($"GetProgramiv({program}, {pname}) = {*prams}");
        }
        public void GenSamplers(int len, int[] rv)
        {
            Report($"GenSamplers({len})");
            device.GenSamplers(len, rv);
        }
        public void BindSampler(int unit, int sampler)
        {
            Report($"BindSampler({unit}, {sampler})");
            device.BindSampler(unit, sampler);
        }
        public void UniformBlockBinding(int program, int uniformBlockIndex, int uniformBlockBinding)
        {
            Report($"UniformBlockBinding({program}, {uniformBlockIndex}, {uniformBlockBinding})");
            device.UniformBlockBinding(program, uniformBlockIndex, uniformBlockBinding);
        }
        public void BindBufferBase(BufferRangeTarget target, int index, int buffer)
        {
            Report($"BindBufferBase({target}, {index}, {buffer})");
            device.BindBufferBase(target, index, buffer);
        }
        public void CullFace(CullFaceMode mode)
        {
            Report($"CullFace({mode})");
            device.CullFace(mode);
        }
        public void Disable(EnableCap mode)
        {
            Report($"Disable({mode})");
            device.Disable(mode);
        }
        public void Enable(EnableCap mode)
        {
            Report($"Enable({mode})");
            device.Enable(mode);
        }
        public void BindBuffer(BufferTarget target, int buffer)
        {
            Report($"BindBuffer({target}, {buffer})");
            device.BindBuffer(target, buffer);
        }
        public void BufferData(BufferTarget target, IntPtr size, IntPtr data, BufferUsageHint usage)
        {
            Report($"BufferData({target}, {size}, {data}, {usage})");
            device.BufferData(target, size, data, usage);
        }
        public void BufferSubData(BufferTarget target, IntPtr offset, IntPtr size, IntPtr data)
        {
            Report($"BufferSubData({target}, {offset}, {size}, {data})");
            device.BufferSubData(target, offset, size, data);
        }
        public void ActiveTexture(TextureUnit texture)
        {
            Report($"ActiveTexture({texture})");
            device.ActiveTexture(texture);
        }
        public void BindTexture(TextureTarget target, int fb)
        {
            Report($"BindTexture({target}, {fb})");
            device.BindTexture(target, fb);
        }
        public unsafe   void DeleteTextures(int count, int* textures)
        {
            Report($"DeleteTextures({count})");
            device.DeleteTextures(count, textures);
        }
        public unsafe   void DeleteBuffers(int count, int* buffers)
        {
            Report($"DeleteBuffers({count})");
            device.DeleteBuffers(count, buffers);
        }
        public unsafe   void GenFramebuffers(int count, int* res)
        {
            Report($"GenFramebuffers({count})");
            device.GenFramebuffers(count, res);
        }
        public unsafe   void DeleteFramebuffers(int count, int* framebuffers)
        {
            Report($"DeleteFramebuffers({count})");
            device.DeleteFramebuffers(count, framebuffers);
        }
        public void BindFramebuffer(FramebufferTarget target, int fb)
        {
            Report($"BindFramebuffer({target}, {fb})");
            device.BindFramebuffer(target, fb);
        }
        public unsafe   void GenRenderbuffers(int count, int* res)
        {
            Report($"GenRenderbuffers({count})");
            device.GenRenderbuffers(count, res);
        }
        public unsafe   void DeleteRenderbuffers(int count, int* renderbuffers)
        {
            Report($"DeleteRenderbuffers({count})");
            device.DeleteRenderbuffers(count, renderbuffers);
        }
        public void BindRenderbuffer(RenderbufferTarget target, int fb)
        {
            Report($"BindRenderbuffer({target}, {fb})");
            device.BindRenderbuffer(target, fb);
        }
        public void FramebufferTexture2D(FramebufferTarget target,FramebufferAttachment attachment,TextureTarget texTarget,int texture,int level)
        {
            Report($"FramebufferTexture2D({target}, {attachment}, {texTarget}, {texture}, {level})");
            device.FramebufferTexture2D(target, attachment, texTarget, texture, level);
        }
        public void RenderbufferStorage(RenderbufferTarget target, RenderbufferStorage internalFormat, int width, int height)
        {
            Report($"RenderbufferStorage({target}, {internalFormat}, {width}, {height})");
            device.RenderbufferStorage(target, internalFormat, width, height);
        }
        public void FramebufferRenderbuffer(FramebufferTarget target, FramebufferAttachment attachment, RenderbufferTarget renderbufferTarget, int renderbuffer)
        {
            Report($"FramebufferRenderbuffer({target}, {attachment}, {renderbufferTarget}, {renderbuffer})");
            device.FramebufferRenderbuffer(target, attachment, renderbufferTarget, renderbuffer);
        }
        public void ClearStencil(int buffer)
        {
            Report($"ClearStencil({buffer})");
            device.ClearStencil(buffer);
        }
        public void ClearColor(float r, float g, float b, float a)
        {
            Report($"ClearColor({r}, {g}, {b}, {a})");
            device.ClearColor(r, g, b, a);
        }
        public void Clear(ClearBufferMask bits)
        {
            Report($"Clear({bits})");
            device.Clear(bits);
        }
        public void Viewport(int x, int y, int width, int height)
        {
            Report($"Viewport({x}, {y}, {width}, {height})");
            device.Viewport(x, y, width, height);
        }
        public void TexImage3D(TextureTarget target, int level, InternalFormat internalFormat, int width, int height, int depth, int border, PixelFormat format, PixelType type, IntPtr data)
        {
            Report($"TexImage3D({target}, {level}, {internalFormat}, {width}, {height}, {depth}, {border}, {format}, {type}, {data})");
            device.TexImage3D(target, level, internalFormat, width, height, depth, border, format, type, data);
        }
        public void TexSubImage2D(TextureTarget target, int level, int xoffset, int yoffset, int width, int height, PixelFormat format, PixelType type, IntPtr data)
        {
            Report($"TexSubImage2D({target}, {level}, {xoffset}, {yoffset}, {width}, {height}, {format}, {type}, {data})");
            device.TexSubImage2D(target, level, xoffset, yoffset, width, height, format, type, data);
        }
        public void DrawElementsInstanced(PrimitiveType mode, int count, DrawElementsType type, IntPtr indices, int instancesCount)
        {
            Report($"DrawElementsInstanced({mode}, {count}, {type}, {indices}, {instancesCount})");
            device.DrawElementsInstanced(mode, count, type, indices, instancesCount);
        }
        public void ClampColor(ClampColorTarget target, ClampColorMode clamp)
        {
            Report($"ClampColor({target}, {clamp})");
            device.ClampColor(target, clamp);
        }
        public void TexBuffer(TextureBufferTarget target, SizedInternalFormat internalformat, int buffer)
        {
            Report($"TexBuffer({target}, {internalformat}, {buffer})");
            device.TexBuffer(target, internalformat, buffer);
        }
        public void ValidateProgram(int program)
        {
            Report($"ValidateProgram({program})");
            device.ValidateProgram(program);
        }
        
        public int GetAttribLocation(int program, string name)
        {
            var loc = device.GetAttribLocation(program, name);
            Report($"GetAttribLocation({program}, \"{name}\") = {loc}");
            return loc;
        }
    
        public void VertexAttribPointer(int index, int size, VertexAttribPointerType type, bool normalized, int stride, IntPtr pointer)
        {
            Report($"VertexAttribPointer({index}, {size}, {type}, {normalized}, {stride}, {pointer})");
            device.VertexAttribPointer(index, size, type, normalized, stride, pointer);
        }
        public void EnableVertexAttribArray(int index)
        {
            Report($"EnableVertexAttribArray({index})");
            device.EnableVertexAttribArray(index);
        }
        public void UseProgram(int program)
        {
            Report($"UseProgram({program})");
            device.UseProgram(program);
        }
        public void DrawArrays(PrimitiveType mode, int first, IntPtr count)
        {
            Report($"DrawArrays({mode}, {first}, {count})");
            device.DrawArrays(mode, first, count);
        }
        public void DrawElements(PrimitiveType mode, int count, DrawElementsType type, IntPtr indices)
        {
            Report($"DrawElements({mode}, {count}, {type}, {indices})");
            device.DrawElements(mode, count, type, indices);
        }
        
        public int GetUniformLocation(int program, string name)
        {
            var loc = device.GetUniformLocation(program, name);
            Report($"GetUniformLocation({program}, \"{name}\") = {loc}");
            return loc;
        }
        
        public void Uniform1f(int location, float falue)
        {
            Report($"Uniform1f({location}, {falue})");
            device.Uniform1f(location, falue);
        }
        public void Uniform4f(int location, float a, float b, float c, float d)
        {
            Report($"Uniform4f({location}, {a}, {b}, {c}, {d})");
            device.Uniform4f(location, a, b, c, d);
        }
        public void Uniform3f(int location, float a, float b, float c)
        {
            Report($"Uniform3f({location}, {a}, {b}, {c})");
            device.Uniform3f(location, a, b, c);
        }
        public void TexImage2D(TextureTarget target, int level, PixelInternalFormat internalFormat, int width, int height, int border, PixelFormat format, PixelType type, IntPtr data)
        {
            Report($"TexImage2D({target}, {level}, {internalFormat}, {width}, {height}, {border}, {format}, {type}, {data})");
            device.TexImage2D(target, level, internalFormat, width, height, border, format, type, data);
        }
        public void CopyTexSubImage2D(TextureTarget target, int level, int xoffset, int yoffset, int x, int y, int width, int height)
        {
            Report($"CopyTexSubImage2D");
            device.CopyTexSubImage2D(target, level, xoffset, yoffset, x, y, width, height);
        }
        public void TexParameteri(TextureTarget target, TextureParameterName name, int value)
        {
            Report($"TexParameteri({target}, {name}, {value})");
            device.TexParameteri(target, name, value);
        }
        public unsafe   string GetProgramInfoLog(int program, int maxLength = 2048)
        {
            Report($"GetProgramInfoLog");
            return device.GetProgramInfoLog(program, maxLength);
        }
        public int CreateProgram()
        {
            Report($"CreateProgram");
            return device.CreateProgram();
        }
        public void AttachShader(int program, int shader)
        {
            Report($"AttachShader");
            device.AttachShader(program, shader);
        }
        public void LinkProgram(int program)
        {
            Report($"LinkProgram");
            device.LinkProgram(program);
        }
        public int CreateShader(ShaderType shaderType)
        {
            Report($"CreateShader");
            return device.CreateShader(shaderType);
        }
        public void BlendFunc(BlendingFactorSrc src, BlendingFactorDest dst)
        {
            Report($"BlendFunc({src}, {dst})");
            device.BlendFunc(src, dst);
        }
    
        public string CompileShaderAndGetError(int shader, string source)
        {
            Report($"CompileShaderAndGetError");
            return device.CompileShaderAndGetError(shader, source);
        }

        public string LinkProgramAndGetError(int program)
        {
            Report($"LinkProgramAndGetError");
            return device.LinkProgramAndGetError(program);
        }
    
        public void CheckError(string what)
        {
            device.CheckError(what);
        }

        public int GenVertexArray()
        {
            Report($"GenVertexArray");
            return device.GenVertexArray();
        }

        public int GenBuffer()
        {
            Report($"GenBuffer");
            return device.GenBuffer();
        }

        public int GenTexture()
        {
            Report($"GenTexture");
            return device.GenTexture();
        }
    
        public void ActiveTextureUnit(uint slot)
        {
            Report($"ActiveTextureUnit({slot})");
            device.ActiveTextureUnit(slot);
        }

        public void ActiveTextureUnit(int slot)
        {
            Report($"ActiveTextureUnit({slot})");
            device.ActiveTextureUnit(slot);
        }

        public unsafe void DeleteTexture(int name)
        {
            Report($"DeleteTexture({name})");
            device.DeleteTexture(name);
        }

        public unsafe void DeleteBuffer(int name)
        {
            Report($"DeleteBuffer({name})");
            device.DeleteBuffer(name);
        }

        public unsafe int GenFramebuffer()
        {
            Report($"GenFramebuffer");
            return device.GenFramebuffer();
        }

        public unsafe void DeleteFramebuffer(int name)
        {
            Report($"DeleteFramebuffer");
            device.DeleteFramebuffer(name);
        }

        public unsafe int GenRenderbuffer()
        {
            Report($"GenRenderbuffer");
            return device.GenRenderbuffer();
        }

        public unsafe void DeleteRenderbuffer(int name)
        {
            Report($"DeleteRenderbuffer");
            device.DeleteRenderbuffer(name);
        }

        public int GetUniformBlockIndex(int program, string uniformBlockName)
        {
            Report($"GetUniformBlockIndex");
            return device.GetUniformBlockIndex(program, uniformBlockName);
        }

        public void TexParameter(TextureTarget target, TextureParameterName name, int value)
        {
            Report($"TexParameter({target}, {name}, {value})");
            device.TexParameter(target, name, value);
        }

        public unsafe int GetProgramParameter(int program, GetProgramParameterName pname)
        {
            Report($"ProgramParameter({program}, {pname})");
            return device.GetProgramParameter(program, pname);
        }
    
        public unsafe string GetActiveUniform(int unit, int index, int maxLength, out int length, out int size, out ActiveUniformType type)
        {
            Report($"GetActiveUniform");
            return device.GetActiveUniform(unit, index, maxLength, out length, out size, out type);
        }

        public void DepthMask(bool @on)
        {
            Report($"DepthMask({@on})");
            device.DepthMask(@on ? 1 : 0);
        }

        public void DepthFunction(DepthFunction func)
        {
            Report($"DepthFunc({func})");
            device.DepthFunc(func);
        }
    }
}