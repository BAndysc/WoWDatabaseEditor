// using System.Runtime.CompilerServices;
// using OpenTK.Graphics.OpenGL4;
// using ActiveUniformType = OpenGLBindings.ActiveUniformType;
// using BlendEquationMode = OpenGLBindings.BlendEquationMode;
// using BlendingFactorDest = OpenGLBindings.BlendingFactorDest;
// using BlendingFactorSrc = OpenGLBindings.BlendingFactorSrc;
// using BufferRangeTarget = OpenGLBindings.BufferRangeTarget;
// using BufferTarget = OpenGLBindings.BufferTarget;
// using BufferUsageHint = OpenGLBindings.BufferUsageHint;
// using ClampColorMode = OpenGLBindings.ClampColorMode;
// using ClampColorTarget = OpenGLBindings.ClampColorTarget;
// using ClearBufferMask = OpenGLBindings.ClearBufferMask;
// using CullFaceMode = OpenGLBindings.CullFaceMode;
// using DepthFunction = OpenGLBindings.DepthFunction;
// using DrawBuffersEnum = OpenGLBindings.DrawBuffersEnum;
// using DrawElementsType = OpenGLBindings.DrawElementsType;
// using EnableCap = OpenGLBindings.EnableCap;
// using FramebufferAttachment = OpenGLBindings.FramebufferAttachment;
// using FramebufferTarget = OpenGLBindings.FramebufferTarget;
// using GetPName = OpenGLBindings.GetPName;
// using GetProgramParameterName = OpenGLBindings.GetProgramParameterName;
// using InternalFormat = OpenGLBindings.InternalFormat;
// using PixelFormat = OpenGLBindings.PixelFormat;
// using PixelInternalFormat = OpenGLBindings.PixelInternalFormat;
// using PixelType = OpenGLBindings.PixelType;
// using PrimitiveType = OpenGLBindings.PrimitiveType;
// using RenderbufferStorage = OpenGLBindings.RenderbufferStorage;
// using RenderbufferTarget = OpenGLBindings.RenderbufferTarget;
// using ShaderType = OpenGLBindings.ShaderType;
// using SizedInternalFormat = OpenGLBindings.SizedInternalFormat;
// using TextureBufferTarget = OpenGLBindings.TextureBufferTarget;
// using TextureParameterName = OpenGLBindings.TextureParameterName;
// using TextureTarget = OpenGLBindings.TextureTarget;
// using TextureUnit = OpenGLBindings.TextureUnit;
// using VertexAttribPointerType = OpenGLBindings.VertexAttribPointerType;
//
// namespace TheAvaloniaOpenGL
// {
//     public class OpenTKDevice : IDevice
//     {
//         public void Begin()
//         {
//         }
//
//         public void DeleteVertexArrays(int count, int[] buffers)
//         {
//             GL.DeleteVertexArrays(count, buffers);
//         }
//
//         public void BindVertexArray(int array)
//         {
//             GL.BindVertexArray(array);
//         }
//
//         public int GetInteger(GetPName n)
//         {
//             return GL.GetInteger((OpenTK.Graphics.OpenGL4.GetPName)n);
//         }
//
//         public unsafe void GetIntegerv(GetPName n, int* rv)
//         {
//             GL.GetInteger((OpenTK.Graphics.OpenGL4.GetPName)n, rv);
//         }
//
//         public void GenVertexArrays(int n, int[] rv)
//         {
//             GL.GenVertexArrays(n, rv);
//         }
//
//         public void GenerateMipmap(TextureTarget target)
//         {
//             GL.GenerateMipmap((OpenTK.Graphics.OpenGL4.GenerateMipmapTarget)target);
//         }
//
//         public void Uniform1I(int location, int lalue)
//         {
//             GL.Uniform1(location, lalue);
//         }
//
//         public unsafe void GetProgramiv(int program, GetProgramParameterName pname, int* prams)
//         {
//             GL.GetProgram(program, (OpenTK.Graphics.OpenGL4.GetProgramParameterName)pname, prams);
//         }
//
//         public void GenSamplers(int len, int[] rv)
//         {
//             GL.GenSamplers(len, rv);
//         }
//
//         public void BindSampler(int unit, int sampler)
//         {
//             GL.BindSampler(unit, sampler);
//         }
//
//         public void UniformBlockBinding(int program, int uniformBlockIndex, int uniformBlockBinding)
//         {
//             GL.UniformBlockBinding(program, uniformBlockIndex, uniformBlockBinding);
//         }
//
//         public void BindBufferBase(BufferRangeTarget target, int index, int buffer)
//         {
//             GL.BindBufferBase((OpenTK.Graphics.OpenGL4.BufferRangeTarget)target, index, buffer);
//         }
//
//         public void CullFace(CullFaceMode mode)
//         {
//             GL.CullFace((OpenTK.Graphics.OpenGL4.CullFaceMode)mode);
//         }
//
//         public void Disable(EnableCap mode)
//         {
//             GL.Disable((OpenTK.Graphics.OpenGL4.EnableCap)mode);
//         }
//
//         public void Enable(EnableCap mode)
//         {
//             GL.Enable((OpenTK.Graphics.OpenGL4.EnableCap)mode);
//         }
//
//         public void BindBuffer(BufferTarget target, int buffer)
//         {
//             GL.BindBuffer((OpenTK.Graphics.OpenGL4.BufferTarget)target, buffer);
//         }
//
//         public void BufferData(BufferTarget target, IntPtr size, IntPtr data, BufferUsageHint usage)
//         {
//             GL.BufferData((OpenTK.Graphics.OpenGL4.BufferTarget)target, size, data, (OpenTK.Graphics.OpenGL4.BufferUsageHint)usage);
//         }
//
//         public void BufferSubData(BufferTarget target, IntPtr offset, IntPtr size, IntPtr data)
//         {
//             GL.BufferSubData((OpenTK.Graphics.OpenGL4.BufferTarget)target, offset, size, data);
//         }
//
//         public void ActiveTexture(TextureUnit texture)
//         {
//             GL.ActiveTexture((OpenTK.Graphics.OpenGL4.TextureUnit)texture);
//         }
//
//         public void BindTexture(TextureTarget target, int fb)
//         {
//             GL.BindTexture((OpenTK.Graphics.OpenGL4.TextureTarget)target, fb);
//         }
//
//         public unsafe void DeleteTextures(int count, int* textures)
//         {
//             GL.DeleteTextures(count, textures);
//         }
//
//         public unsafe void DeleteBuffers(int count, int* buffers)
//         {
//             GL.DeleteBuffers(count, buffers);
//         }
//
//         public unsafe void GenFramebuffers(int count, int* res)
//         {
//             GL.GenFramebuffers(count, res);
//         }
//
//         public unsafe void DeleteFramebuffers(int count, int* framebuffers)
//         {
//             GL.DeleteFramebuffers(count, framebuffers);
//         }
//
//         public void BindFramebuffer(FramebufferTarget target, int fb)
//         {
//             GL.BindFramebuffer((OpenTK.Graphics.OpenGL4.FramebufferTarget)target, fb);
//         }
//
//         public unsafe void GenRenderbuffers(int count, int* res)
//         {
//             GL.GenRenderbuffers(count, res);
//         }
//
//         public unsafe void DeleteRenderbuffers(int count, int* renderbuffers)
//         {
//             GL.DeleteRenderbuffers(count, renderbuffers);
//         }
//
//         public void BindRenderbuffer(RenderbufferTarget target, int fb)
//         {
//             GL.BindRenderbuffer((OpenTK.Graphics.OpenGL4.RenderbufferTarget)target, fb);
//         }
//
//         public void FramebufferTexture2D(FramebufferTarget target, FramebufferAttachment attachment, TextureTarget texTarget,
//             int texture, int level)
//         {
//             GL.FramebufferTexture2D((OpenTK.Graphics.OpenGL4.FramebufferTarget)target, (OpenTK.Graphics.OpenGL4.FramebufferAttachment)attachment,
//                 (OpenTK.Graphics.OpenGL4.TextureTarget)texTarget, texture, level);
//         }
//
//         public void RenderbufferStorage(RenderbufferTarget target, RenderbufferStorage internalFormat, int width, int height)
//         {
//             GL.RenderbufferStorage((OpenTK.Graphics.OpenGL4.RenderbufferTarget)target, (OpenTK.Graphics.OpenGL4.RenderbufferStorage)internalFormat, width, height);
//         }
//
//         public void FramebufferRenderbuffer(FramebufferTarget target, FramebufferAttachment attachment,
//             RenderbufferTarget renderbufferTarget, int renderbuffer)
//         {
//             GL.FramebufferRenderbuffer((OpenTK.Graphics.OpenGL4.FramebufferTarget)target, (OpenTK.Graphics.OpenGL4.FramebufferAttachment)attachment, (OpenTK.Graphics.OpenGL4.RenderbufferTarget)renderbufferTarget, renderbuffer);
//         }
//
//         public void ClearStencil(int buffer)
//         {
//             GL.ClearStencil(buffer);
//         }
//
//         public void ClearColor(float r, float g, float b, float a)
//         {
//             GL.ClearColor(r, g, b, a);
//         }
//
//         public void Clear(ClearBufferMask bits)
//         {
//             GL.Clear((OpenTK.Graphics.OpenGL4.ClearBufferMask)bits);
//         }
//
//         public void Viewport(int x, int y, int width, int height)
//         {
//             GL.Viewport(x, y, width, height);
//         }
//
//         public void TexImage3D(TextureTarget target, int level, InternalFormat internalFormat, int width, int height, int depth,
//             int border, PixelFormat format, PixelType type, IntPtr data)
//         {
//             GL.TexImage3D((OpenTK.Graphics.OpenGL4.TextureTarget)target, level, (OpenTK.Graphics.OpenGL4.PixelInternalFormat)internalFormat,
//                 width, height, depth, border, (OpenTK.Graphics.OpenGL4.PixelFormat)format, (OpenTK.Graphics.OpenGL4.PixelType)type, data);
//         }
//
//         public void TexSubImage2D(TextureTarget target, int level, int xoffset, int yoffset, int width, int height, PixelFormat format,
//             PixelType type, IntPtr data)
//         {
//             GL.TexSubImage2D((OpenTK.Graphics.OpenGL4.TextureTarget)target, level, xoffset, yoffset, width, height, (OpenTK.Graphics.OpenGL4.PixelFormat)format, (OpenTK.Graphics.OpenGL4.PixelType)type, data);
//         }
//
//         public void DrawElementsInstanced(PrimitiveType mode, int count, DrawElementsType type, IntPtr indices, int instancesCount)
//         {
//             GL.DrawElementsInstanced((OpenTK.Graphics.OpenGL4.PrimitiveType)mode, count, (OpenTK.Graphics.OpenGL4.DrawElementsType)type, indices, instancesCount);
//         }
//
//         public void ClampColor(ClampColorTarget target, ClampColorMode clamp)
//         {
//             GL.ClampColor((OpenTK.Graphics.OpenGL4.ClampColorTarget)target, (OpenTK.Graphics.OpenGL4.ClampColorMode)clamp);
//         }
//
//         public void TexBuffer(TextureBufferTarget target, SizedInternalFormat internalformat, int buffer)
//         {
//             GL.TexBuffer((OpenTK.Graphics.OpenGL4.TextureBufferTarget)target, (OpenTK.Graphics.OpenGL4.SizedInternalFormat)internalformat, buffer);
//         }
//
//         public void ValidateProgram(int program)
//         {
//             GL.ValidateProgram(program);
//         }
//
//         public int GetAttribLocation(int program, string name)
//         {
//             return GL.GetAttribLocation(program, name);
//         }
//
//         public void VertexAttribPointer(int index, int size, VertexAttribPointerType type, bool normalized, int stride,
//             IntPtr pointer)
//         {
//             GL.VertexAttribPointer(index, size, (OpenTK.Graphics.OpenGL4.VertexAttribPointerType)type, normalized, stride, pointer.ToInt32());
//         }
//
//         public void EnableVertexAttribArray(int index)
//         {
//             GL.EnableVertexAttribArray(index);
//         }
//
//         public void UseProgram(int program)
//         {
//             GL.UseProgram(program);
//         }
//
//         public unsafe void DrawBuffers(ReadOnlySpan<DrawBuffersEnum> buffers)
//         {
//             fixed (DrawBuffersEnum* ptr = buffers)
//             {
//                 GL.DrawBuffers(buffers.Length, (OpenTK.Graphics.OpenGL4.DrawBuffersEnum*)ptr);
//             }
//         }
//
//         public void DrawArrays(PrimitiveType mode, int first, IntPtr count)
//         {
//             GL.DrawArrays((OpenTK.Graphics.OpenGL4.PrimitiveType)mode, first, count.ToInt32());
//         }
//
//         public void DrawElements(PrimitiveType mode, int count, DrawElementsType type, IntPtr startIndexLocation)
//         {
//             GL.DrawElements((OpenTK.Graphics.OpenGL4.PrimitiveType)mode, count, (OpenTK.Graphics.OpenGL4.DrawElementsType)type, startIndexLocation);
//         }
//
//         public void DrawElementsBaseVertex(PrimitiveType mode, int count, DrawElementsType type, IntPtr startIndexLocation, int startVertexLocationBytes)
//         {
//             GL.DrawElementsBaseVertex((OpenTK.Graphics.OpenGL4.PrimitiveType)mode, count, (OpenTK.Graphics.OpenGL4.DrawElementsType)type, startIndexLocation, startVertexLocationBytes);
//         }
//
//         public int GetUniformLocation(int program, string name)
//         {
//             return GL.GetUniformLocation(program, name);
//         }
//
//         public void Uniform1f(int location, float falue)
//         {
//             GL.Uniform1(location, falue);
//         }
//
//         public void Uniform4f(int location, float a, float b, float c, float d)
//         {
//              GL.Uniform4(location, a, b, c, d);
//         }
//
//         public void Uniform3f(int location, float a, float b, float c)
//         {
//             GL.Uniform3(location, a, b, c);
//         }
//
//         public unsafe void UniformMatrix4f(int location, ref Matrix4x4 m, bool transpose)
//         {
//             GL.UniformMatrix4(location, transpose, ref Unsafe.AsRef<OpenTK.Mathematics.Matrix4>(Unsafe.AsPointer(ref m)));
//         }
//
//         public void TexImage2D(TextureTarget target, int level, PixelInternalFormat internalFormat, int width, int height, int border,
//             PixelFormat format, PixelType type, IntPtr data)
//         {
//             GL.TexImage2D((OpenTK.Graphics.OpenGL4.TextureTarget)target, level, (OpenTK.Graphics.OpenGL4.PixelInternalFormat)internalFormat, width, height, border, (OpenTK.Graphics.OpenGL4.PixelFormat)format, (OpenTK.Graphics.OpenGL4.PixelType)type, data);
//         }
//
//         public void CopyTexSubImage2D(TextureTarget target, int level, int xoffset, int yoffset, int x, int y, int width, int height)
//         {
//             GL.CopyTexSubImage2D((OpenTK.Graphics.OpenGL4.TextureTarget)target, level, xoffset, yoffset, x, y, width, height);
//         }
//
//         public void TexParameteri(TextureTarget target, TextureParameterName name, int value)
//         {
//             GL.TexParameter((OpenTK.Graphics.OpenGL4.TextureTarget)target, (OpenTK.Graphics.OpenGL4.TextureParameterName)name, value);
//         }
//
//         public unsafe string GetProgramInfoLog(int program, int maxLength = 2048)
//         {
//             GL.GetProgramInfoLog(program, out var str);
//             return str;
//         }
//
//         public void BlendEquation(BlendEquationMode mode)
//         {
//             GL.BlendEquation((OpenTK.Graphics.OpenGL4.BlendEquationMode)mode);
//         }
//
//         public int CreateProgram()
//         {
//             return GL.CreateProgram();
//         }
//
//         public void AttachShader(int program, int shader)
//         {
//             GL.AttachShader(program, shader);
//         }
//
//         public void LinkProgram(int program)
//         {
//             GL.LinkProgram(program);
//         }
//
//         public int CreateShader(ShaderType shaderType)
//         {
//             return GL.CreateShader((OpenTK.Graphics.OpenGL4.ShaderType)shaderType);
//         }
//
//         public void BlendFunc(BlendingFactorSrc src, BlendingFactorDest dst)
//         {
//             GL.BlendFunc((BlendingFactor)src, (BlendingFactor)dst);
//         }
//
//         public void BlendFuncSeparate(BlendingFactorSrc srcRGB, BlendingFactorDest dstRGB, BlendingFactorSrc srcAlpha, BlendingFactorDest dstAlpha)
//         {
//             GL.BlendFuncSeparate((OpenTK.Graphics.OpenGL4.BlendingFactorSrc)srcRGB, (OpenTK.Graphics.OpenGL4.BlendingFactorDest)dstRGB, (OpenTK.Graphics.OpenGL4.BlendingFactorSrc)srcAlpha, (OpenTK.Graphics.OpenGL4.BlendingFactorDest)dstAlpha);
//         }
//
//         public unsafe string CompileShaderAndGetError(int shader, string source)
//         {
//             GL.ShaderSource(shader, source);
//             GL.CompileShader(shader);
//             int num;
//             GL.GetShader(shader, ShaderParameter.CompileStatus, &num);
//             if (num != 0)
//                 return (string) null;
//             int maxLength;
//             GL.GetShader(shader, ShaderParameter.InfoLogLength, &maxLength);
//             if (maxLength == 0)
//                 maxLength = 4096;
//             int length;
//             GL.GetShaderInfoLog(shader, maxLength, out length, out var str);
//             return str;
//         }
//
//         public unsafe string LinkProgramAndGetError(int program)
//         {
//             GL.LinkProgram(program);
//             int num;
//             GL.GetProgram(program, OpenTK.Graphics.OpenGL4.GetProgramParameterName.LinkStatus, &num);
//             if (num != 0)
//                 return (string) null;
//             int maxLength;
//             GL.GetProgram(program, OpenTK.Graphics.OpenGL4.GetProgramParameterName.InfoLogLength, &maxLength);
//             int len;
//             GL.GetProgramInfoLog(program, maxLength, out len, out var str);
//             return str;
//         }
//
//         public void CheckError(string what)
//         {
//             ErrorCode err;
//             while ((err = GL.GetError()) != ErrorCode.NoError)
//                 Console.WriteLine(what + ": " + err);
//         }
//
//         public int GenVertexArray()
//         {
//             return GL.GenVertexArray();
//         }
//
//         public int GenBuffer()
//         {
//             return GL.GenBuffer();
//         }
//
//         public int GenTexture()
//         {
//             return GL.GenTexture();
//         }
//
//         public void ActiveTextureUnit(uint slot)
//         {
//             GL.ActiveTexture((OpenTK.Graphics.OpenGL4.TextureUnit.Texture0) + (int)slot);
//         }
//
//         public void ActiveTextureUnit(int slot)
//         {
//             GL.ActiveTexture((OpenTK.Graphics.OpenGL4.TextureUnit.Texture0) + slot);
//         }
//
//         public void DeleteProgram(int program)
//         {
//             GL.DeleteProgram(program);
//         }
//
//         public unsafe void DeleteTexture(int name)
//         {
//             GL.DeleteTexture(name);
//         }
//
//         public unsafe void DeleteBuffer(int name)
//         {
//             GL.DeleteBuffer(name);
//         }
//
//         public unsafe int GenFramebuffer()
//         {
//             return GL.GenFramebuffer();
//         }
//
//         public unsafe void DeleteFramebuffer(int name)
//         {
//             GL.DeleteFramebuffer(name);
//         }
//
//         public unsafe int GenRenderbuffer()
//         {
//             return GL.GenRenderbuffer();
//         }
//
//         public unsafe void DeleteRenderbuffer(int name)
//         {
//             GL.DeleteRenderbuffer(name);
//         }
//
//         public int GetUniformBlockIndex(int program, string uniformBlockName)
//         {
//             return GL.GetUniformBlockIndex(program, uniformBlockName);
//         }
//
//         public void TexParameter(TextureTarget target, TextureParameterName name, int value)
//         {
//             GL.TexParameter((OpenTK.Graphics.OpenGL4.TextureTarget)target, (OpenTK.Graphics.OpenGL4.TextureParameterName)name, value);
//         }
//
//         public unsafe int GetProgramParameter(int program, GetProgramParameterName pname)
//         {
//             GL.GetProgram(program, (OpenTK.Graphics.OpenGL4.GetProgramParameterName)pname, out var i);
//             return i;
//         }
//
//         public unsafe string GetActiveUniform(int unit, int index, int maxLength, out int length, out int size,
//             out ActiveUniformType type)
//         {
//             OpenTK.Graphics.OpenGL4.ActiveUniformType t;
//             GL.GetActiveUniform(unit, index, maxLength, out length, out size, out t, out var name);
//             type = (ActiveUniformType)t;
//             return name;
//         }
//
//         public void DepthMask(bool @on)
//         {
//             GL.DepthMask(@on);
//         }
//
//         public void DepthFunction(DepthFunction func)
//         {
//             GL.DepthFunc((OpenTK.Graphics.OpenGL4.DepthFunction)func);
//         }
//
//         public void Scissor(int x, int y, int width, int height)
//         {
//             GL.Scissor(x, y, width, height);
//         }
//
//         public void Flush()
//         {
//             GL.Flush();
//         }
//
//         public void Finish()
//         {
//             GL.Finish();
//         }
//
//         public void Debug(string msg)
//         {
//
//         }
//
//         public void BlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, ClearBufferMask mask, OpenGLBindings.BlitFramebufferFilter filter)
//         {
//             GL.BlitFramebuffer(srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, (OpenTK.Graphics.OpenGL4.ClearBufferMask)mask, (BlitFramebufferFilter)filter);
//         }
//
//         public void ReadBuffer(OpenGLBindings.ReadBufferMode buffer)
//         {
//             GL.ReadBuffer((OpenTK.Graphics.OpenGL4.ReadBufferMode)buffer);
//         }
//
//         public unsafe void ReadPixels<T>(int x, int y, int width, int height, OpenGLBindings.PixelFormat format, OpenGLBindings.PixelType type, Span<T> span) where T : unmanaged
//         {
//             fixed (void* ptr = span)
//                 GL.ReadPixels(x, y, width, height, (OpenTK.Graphics.OpenGL4.PixelFormat)format, (OpenTK.Graphics.OpenGL4.PixelType)type, (IntPtr)ptr);
//         }
//     }
// }