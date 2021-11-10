using System;
using System.Text;
using Avalonia.OpenGL;
using Avalonia.Platform.Interop;
using OpenGLBindings;

// ReSharper disable StringLiteralTypo
// ReSharper disable InconsistentNaming

// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable IdentifierTypo

namespace TheAvaloniaOpenGL
{
    public class RealDevice : GlInterfaceBase<GlInterface.GlContextInfo>
    {
        private GlInterface Gl { get; }
        private int maxTextureImageUnits;
        
        public RealDevice(GlInterface gl) : base(gl.GetProcAddress, gl.ContextInfo)
        {
            Gl = gl;
            maxTextureImageUnits = GetInteger(GetPName.MaxTextureImageUnits);
            Console.WriteLine("Max texture units: " + maxTextureImageUnits);
        }
            
        public delegate void GlDeleteVertexArrays(int count, int[] buffers);
        [GlMinVersionEntryPoint("glDeleteVertexArrays", 3,0)]
        [GlExtensionEntryPoint("glDeleteVertexArraysOES", "GL_OES_vertex_array_object")]
        public GlDeleteVertexArrays DeleteVertexArrays { get; }
            
        public delegate void GlBindVertexArray(int array);
        [GlMinVersionEntryPoint("glBindVertexArray", 3,0)]
        [GlExtensionEntryPoint("glBindVertexArrayOES", "GL_OES_vertex_array_object")]
        public GlBindVertexArray BindVertexArray { get; }
        
        
        public unsafe delegate void GlGetIntegerv(GetPName n, int* rv);
        [GlMinVersionEntryPoint("glGetIntegerv",2,0)]
        public GlGetIntegerv GetIntegerv { get; }

        public unsafe int GetInteger(GetPName name)
        {
            int i;
            GetIntegerv(name, &i);
            return i;
        }
        
        public delegate void GlGenVertexArrays(int n, int[] rv);
        [GlMinVersionEntryPoint("glGenVertexArrays",3,0)]
        [GlExtensionEntryPoint("glGenVertexArraysOES", "GL_OES_vertex_array_object")]
        public GlGenVertexArrays GenVertexArrays { get; }
            
        
        public delegate void GlGenerateMipmap(TextureTarget target);
        [GlMinVersionEntryPoint("glGenerateMipmap", 3,0)]
        public GlGenerateMipmap GenerateMipmap { get; }
        
        
        public delegate void GlUniform1I(int location, int lalue);
        [GlMinVersionEntryPoint("glUniform1i", 2, 0)]
        public GlUniform1I Uniform1I { get; }
        
        public unsafe delegate void GlGetProgramiv(int program, GetProgramParameterName pname, int* prams);
        [GlMinVersionEntryPoint("glGetProgramiv", 2, 0)]
        public GlGetProgramiv GetProgramiv { get; }

        public unsafe int GetProgramParameter(int program, GetProgramParameterName pname)
        {
            int x;
            GetProgramiv(program, pname, &x);
            return x;
        }
        
        public delegate void GlGenSamplers(int len, int[] rv);
        [GlMinVersionEntryPoint("glGenSamplers", 3, 3)]
        public GlGenSamplers GenSamplers { get; }
        public int GenSampler()
        {
            int[] rv = new int[1];
            this.GenSamplers(1, rv);
            return rv[0];
        }
        
        public delegate void GlBindSampler(int unit, int sampler);
        [GlMinVersionEntryPoint("glBindSampler", 3, 3)]
        public GlBindSampler BindSampler { get; }
        
        public unsafe delegate void GlGetActiveUniform(int unit, int index, int bufSize, out int length, out int size, out ActiveUniformType type, void* name);
        [GlMinVersionEntryPoint("glGetActiveUniform", 3, 3)]
        public GlGetActiveUniform GetActiveUniform_ { get; }

        public unsafe string GetActiveUniform(int unit, int index, int maxLength, out int length, out int size, out ActiveUniformType type)
        {
            byte[] bytes = new byte[maxLength];
            fixed (byte* infoLog = bytes)
                GetActiveUniform_(unit, index, maxLength, out length,  out size, out type, (void*) infoLog);
            return Encoding.UTF8.GetString(bytes, 0, length);
        }
        
        public unsafe string GetProgramInfoLog(int program, int maxLength = 2048)
        {
            byte[] bytes = new byte[maxLength];
            int length;
            fixed (byte* infoLog = bytes)
                Gl.GetProgramInfoLog(program, maxLength, out length, infoLog);
            return Encoding.UTF8.GetString(bytes, 0, length);
        }
        
        public delegate int GlGetUniformBlockIndex(int program, IntPtr uniformBlockName);
        [GlMinVersionEntryPoint("glGetUniformBlockIndex", 3, 1)]
        public GlGetUniformBlockIndex GetUniformBlockIndex_ { get; }

        public int GetUniformBlockIndex(int program, string uniformBlockName)
        {
            using Utf8Buffer utf8Buffer = new Utf8Buffer(uniformBlockName);
            return GetUniformBlockIndex_(program, utf8Buffer.DangerousGetHandle());
        }
        
        public delegate void GlUniformBlockBinding(int program, int uniformBlockIndex, int uniformBlockBinding);
        [GlMinVersionEntryPoint("glUniformBlockBinding", 3, 1)]
        public GlUniformBlockBinding UniformBlockBinding { get; }

        public delegate void GlBindBufferBase(BufferRangeTarget target, int index, int buffer);
        [GlMinVersionEntryPoint("glBindBufferBase", 3, 0)]
        public GlBindBufferBase BindBufferBase { get; }
        
        public delegate void GlCullFace(CullFaceMode mode);
        [GlMinVersionEntryPoint("glCullFace", 2, 0)]
        public GlCullFace CullFace { get; }
        
        public delegate void GlDisable(EnableCap mode);
        [GlMinVersionEntryPoint("glDisable", 2, 0)]
        public GlDisable Disable { get; }
        
        public delegate void GlEnable(EnableCap mode);
        [GlMinVersionEntryPoint("glEnable", 2, 0)]
        public GlEnable Enable { get; }

        public int GenBuffer() => Gl.GenBuffer();
        
        public delegate void GlBindBuffer(BufferTarget target, int buffer);
        [GlEntryPoint("glBindBuffer")]
        public GlBindBuffer BindBuffer { get; }
        
        public delegate void GlBufferData(BufferTarget target, IntPtr size, IntPtr data, BufferUsageHint usage);
        [GlEntryPoint("glBufferData")]
        public GlBufferData BufferData { get; }
        
        public delegate void GlBufferSubData(BufferTarget target, IntPtr offset, IntPtr size, IntPtr data);
        [GlEntryPoint("glBufferSubData")]
        public GlBufferSubData BufferSubData { get; }
        
        
        public delegate void GlActiveTexture(TextureUnit texture);
        [GlEntryPoint("glActiveTexture")]
        public GlActiveTexture ActiveTexture { get; }

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

        public delegate void GlBindTexture(TextureTarget target, int fb);
        [GlEntryPoint("glBindTexture")]
        public GlBindTexture BindTexture { get; }

        public unsafe delegate void GlDeleteTextures(int count, int* textures);
        [GlEntryPoint("glDeleteTextures")]
        public GlDeleteTextures DeleteTextures { get; }
        
        public unsafe void DeleteTexture(int name) => DeleteTextures(1, &name);

        public unsafe delegate void GlDeleteBuffers(int count, int* buffers);
        [GlEntryPoint("glDeleteBuffers")]
        public GlDeleteBuffers DeleteBuffers { get; }
        
        public unsafe void DeleteBuffer(int name) => DeleteBuffers(1, &name);
        
        public unsafe delegate void GlGenFramebuffers(int count, int* res);
        [GlEntryPoint("glGenFramebuffers")]
        public GlGenFramebuffers GenFramebuffers { get; }

        public unsafe int GenFramebuffer()
        {
            int x;
            GenFramebuffers(1, &x);
            return x;
        }
        
        public unsafe delegate void GlDeleteFramebuffers(int count, int* framebuffers);
        [GlEntryPoint("glDeleteFramebuffers")]
        public GlDeleteFramebuffers DeleteFramebuffers { get; }

        public unsafe void DeleteFramebuffer(int name) => DeleteFramebuffers(1, &name);
        
        public delegate void GlBindFramebuffer(FramebufferTarget target, int fb);
        [GlEntryPoint("glBindFramebuffer")]
        public GlBindFramebuffer BindFramebuffer { get; }
        
        public unsafe delegate void GlGenRenderbuffers(int count, int* res);
        [GlEntryPoint("glGenRenderbuffers")]
        public GlGenRenderbuffers GenRenderbuffers { get; }

        public unsafe int GenRenderbuffer()
        {
            int x;
            GenRenderbuffers(1, &x);
            return x;
        }
        
        public unsafe delegate void GlDeleteRenderbuffers(int count, int* renderbuffers);
        [GlEntryPoint("glDeleteRenderbuffers")]
        public GlDeleteTextures DeleteRenderbuffers { get; }

        public unsafe void DeleteRenderbuffer(int name) => DeleteRenderbuffers(1, &name);

        public delegate void GlBindRenderbuffer(RenderbufferTarget target, int fb);
        [GlEntryPoint("glBindRenderbuffer")]
        public GlBindRenderbuffer BindRenderbuffer { get; }
        
        public delegate void GlFramebufferTexture2D(
            FramebufferTarget target,
            FramebufferAttachment attachment,
            TextureTarget texTarget,
            int texture,
            int level);
        [GlEntryPoint("glFramebufferTexture2D")]
        public GlFramebufferTexture2D FramebufferTexture2D { get; }
        
        public delegate void GlRenderbufferStorage(
            RenderbufferTarget target,
            RenderbufferStorage internalFormat,
            int width,
            int height);
        [GlEntryPoint("glRenderbufferStorage")]
        public GlRenderbufferStorage RenderbufferStorage { get; }
        
        public delegate void GlFramebufferRenderbuffer(
            FramebufferTarget target,
            FramebufferAttachment attachment,
            RenderbufferTarget renderbufferTarget,
            int renderbuffer);
        [GlEntryPoint("glFramebufferRenderbuffer")]
        public GlFramebufferRenderbuffer FramebufferRenderbuffer { get; }
        
        
        public delegate void GlClearStencil(int buffer);
        [GlEntryPoint("glClearStencil")]
        public GlClearStencil ClearStencil { get; }

        public delegate void GlClearColor(float r, float g, float b, float a);
        [GlEntryPoint("glClearColor")]
        public GlClearColor ClearColor { get; }

        public delegate void GlClear(ClearBufferMask bits);
        [GlEntryPoint("glClear")]
        public GlClear Clear { get; }

        public delegate void GlViewport(int x, int y, int width, int height);
        [GlEntryPoint("glViewport")]
        public GlViewport Viewport { get; }
        
        public delegate void GlTexImage3D(
            TextureTarget target,
            int level,
            InternalFormat internalFormat,
            int width,
            int height,
            int depth,
            int border,
            PixelFormat format,
            PixelType type,
            IntPtr data);
        [GlMinVersionEntryPoint("glTexImage3D", 2, 0)]
        public GlTexImage3D TexImage3D { get; }
        
        
        public delegate void GlTexSubImage2D(
            TextureTarget target,
            int level,
            int xoffset,
            int yoffset,
            int width,
            int height,
            PixelFormat format,
            PixelType type,
            IntPtr data);
        [GlMinVersionEntryPoint("glTexSubImage2D", 2, 0)]
        public GlTexSubImage2D TexSubImage2D { get; }
        
        
        public delegate void GlDrawElementsInstanced(PrimitiveType mode, int count, DrawElementsType type, IntPtr indices, int instancesCount);
        [GlMinVersionEntryPoint("glDrawElementsInstanced", 3, 1)]
        public GlDrawElementsInstanced DrawElementsInstanced { get; }
        
        public delegate void GlClampColor(ClampColorTarget target, ClampColorMode clamp);
        [GlMinVersionEntryPoint("glClampColor", 3, 0)]
        public GlClampColor ClampColor { get; }
        
        public delegate void GlTexBuffer(TextureBufferTarget target, SizedInternalFormat internalformat, int buffer);
        [GlMinVersionEntryPoint("glTexBuffer", 3, 1)]
        public GlTexBuffer TexBuffer { get; }
        
        public delegate void GlValidateProgram(int program);
        [GlMinVersionEntryPoint("glValidateProgram", 2, 0)]
        public GlValidateProgram ValidateProgram { get; }
        
        public delegate int GlGetAttribLocation(int program, IntPtr name);
        [GlEntryPoint("glGetAttribLocation")]
        public GlGetAttribLocation GetAttribLocation_ { get; }

        public int GetAttribLocation(int program, string name)
        {
            using Utf8Buffer utf8Buffer = new Utf8Buffer(name);
            return GetAttribLocation_(program, utf8Buffer.DangerousGetHandle());
        }

        public delegate void GlVertexAttribPointer(
            int index,
            int size,
            VertexAttribPointerType type,
            bool normalized,
            int stride,
            IntPtr pointer);
        [GlEntryPoint("glVertexAttribPointer")]
        public GlVertexAttribPointer VertexAttribPointer { get; }

        public delegate void GlEnableVertexAttribArray(int index);
        [GlEntryPoint("glEnableVertexAttribArray")]
        public GlEnableVertexAttribArray EnableVertexAttribArray { get; }

        public delegate void GlUseProgram(int program);
        [GlEntryPoint("glUseProgram")]
        public GlUseProgram UseProgram { get; }
        
        public delegate void GlDrawArrays(PrimitiveType mode, int first, IntPtr count);
        [GlEntryPoint("glDrawArrays")]
        public GlDrawArrays DrawArrays { get; }
        
        public delegate void GlDrawElements(PrimitiveType mode, int count, DrawElementsType type, IntPtr indices);
        [GlEntryPoint("glDrawElements")]
        public GlDrawElements DrawElements { get; }

        public delegate int GlGetUniformLocation(int program, IntPtr name);
        [GlEntryPoint("glGetUniformLocation")]
        public GlGetUniformLocation GetUniformLocation_ { get; }

        public int GetUniformLocation(int program, string name)
        {
            using (Utf8Buffer utf8Buffer = new Utf8Buffer(name))
                return this.GetUniformLocation_(program, utf8Buffer.DangerousGetHandle());
        }

        public delegate void GlUniform1f(int location, float falue);
        [GlEntryPoint("glUniform1f")]
        public GlUniform1f Uniform1f { get; }
        
        public delegate void GlUniform4f(int location, float a, float b, float c, float d);
        [GlEntryPoint("glUniform4f")]
        public GlUniform4f Uniform4f { get; }

        [GlEntryPoint("glUniformMatrix4fv")]
        public GlInterface.GlUniformMatrix4fv UniformMatrix4fv { get; }
        
        public delegate void GlTexImage2D(
            TextureTarget target,
            int level,
            PixelInternalFormat internalFormat,
            int width,
            int height,
            int border,
            PixelFormat format,
            PixelType type,
            IntPtr data);
        [GlEntryPoint("glTexImage2D")]
        public GlTexImage2D TexImage2D { get; }

        public delegate void GlCopyTexSubImage2D(
            TextureTarget target,
            int level,
            int xoffset,
            int yoffset,
            int x,
            int y,
            int width,
            int height);
        [GlEntryPoint("glCopyTexSubImage2D")]
        public GlCopyTexSubImage2D CopyTexSubImage2D { get; }

        public delegate void GlTexParameteri(TextureTarget target, TextureParameterName name, int value);
        [GlEntryPoint("glTexParameteri")]
        public GlTexParameteri TexParameteri { get; }

        public void TexParameter(TextureTarget target, TextureParameterName name, int value) =>
            TexParameteri(target, name, value);
        
        
        public unsafe delegate void GlGetProgramInfoLog(
            int program,
            int maxLength,
            out int len,
            void* infoLog);
        public delegate int GlCreateProgram();
        [GlEntryPoint("glCreateProgram")]
        public GlCreateProgram CreateProgram { get; }

        public delegate void GlAttachShader(int program, int shader);
        [GlEntryPoint("glAttachShader")]
        public GlAttachShader AttachShader { get; }

        public delegate void GlLinkProgram(int program);
        [GlEntryPoint("glLinkProgram")]
        public GlLinkProgram LinkProgram { get; }

        public delegate int GlCreateShader(ShaderType shaderType);
        [GlEntryPoint("glCreateShader")]
        public GlCreateShader CreateShader { get; }
        
        public string CompileShaderAndGetError(int shader, string source)
        {
            return Gl.CompileShaderAndGetError(shader, source);
        }

        public string LinkProgramAndGetError(int program)
        {
            return Gl.LinkProgramAndGetError(program);
        }
        
        public delegate void GlBlendFunc(BlendingFactorSrc src, BlendingFactorDest dst);
        [GlMinVersionEntryPoint("glBlendFunc", 2, 0)]
        public GlBlendFunc BlendFunc { get; }

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
            int[] temp = new int[1];
            Gl.GenTextures(1, temp);
            return temp[0];
        }
    }
}