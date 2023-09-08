using OpenGLBindings;

namespace TheAvaloniaOpenGL
{
    public interface IDevice
    {
        void Begin();
        void DeleteVertexArrays(int count, int[] buffers);
        void BindVertexArray(int array);
        int GetInteger(GetPName n);
        void GenVertexArrays(int n, int[] rv);
        void GenerateMipmap(TextureTarget target);
        void Uniform1I(int location, int lalue);
        unsafe   void GetProgramiv(int program, GetProgramParameterName pname, int* prams);
        void GenSamplers(int len, int[] rv);
        void BindSampler(int unit, int sampler);
        void UniformBlockBinding(int program, int uniformBlockIndex, int uniformBlockBinding);
        void BindBufferBase(BufferRangeTarget target, int index, int buffer);
        void CullFace(CullFaceMode mode);
        void Disable(EnableCap mode);
        void Enable(EnableCap mode);
        void BindBuffer(BufferTarget target, int buffer);
        void BufferData(BufferTarget target, IntPtr size, IntPtr data, BufferUsageHint usage);
        void BufferSubData(BufferTarget target, IntPtr offset, IntPtr size, IntPtr data);
        void ActiveTexture(TextureUnit texture);
        void BindTexture(TextureTarget target, int fb);
        unsafe   void DeleteTextures(int count, int* textures);
        unsafe   void DeleteBuffers(int count, int* buffers);
        unsafe   void GenFramebuffers(int count, int* res);
        unsafe   void DeleteFramebuffers(int count, int* framebuffers);
        void BindFramebuffer(FramebufferTarget target, int fb);
        unsafe   void GenRenderbuffers(int count, int* res);
        unsafe   void DeleteRenderbuffers(int count, int* renderbuffers);
        void BindRenderbuffer(RenderbufferTarget target, int fb);
        void FramebufferTexture2D(FramebufferTarget target,FramebufferAttachment attachment,TextureTarget texTarget,int texture,int level);
        void RenderbufferStorage(RenderbufferTarget target, RenderbufferStorage internalFormat, int width, int height);
        void FramebufferRenderbuffer(FramebufferTarget target, FramebufferAttachment attachment, RenderbufferTarget renderbufferTarget, int renderbuffer);
        void ClearStencil(int buffer);
        void ClearColor(float r, float g, float b, float a);
        void Clear(ClearBufferMask bits);
        void Viewport(int x, int y, int width, int height);
        void BlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, ClearBufferMask mask, BlitFramebufferFilter filter);
        void TexImage3D(TextureTarget target, int level, InternalFormat internalFormat, int width, int height, int depth, int border, PixelFormat format, PixelType type, IntPtr data);
        void TexSubImage2D(TextureTarget target, int level, int xoffset, int yoffset, int width, int height, PixelFormat format, PixelType type, IntPtr data);
        void DrawElementsInstanced(PrimitiveType mode, int count, DrawElementsType type, IntPtr indices, int instancesCount);
        void ClampColor(ClampColorTarget target, ClampColorMode clamp);
        void TexBuffer(TextureBufferTarget target, SizedInternalFormat internalformat, int buffer);
        void ValidateProgram(int program);
        int GetAttribLocation(int program, string name);
        void VertexAttribPointer(int index, int size, VertexAttribPointerType type, bool normalized, int stride, IntPtr pointer);
        void EnableVertexAttribArray(int index);
        void UseProgram(int program);
        void ReadBuffer(ReadBufferMode buffer);
        void ReadPixels<T>(int x, int y, int width, int height, PixelFormat format, PixelType type, Span<T> data)  where T : unmanaged;
        void DrawBuffers(ReadOnlySpan<DrawBuffersEnum> buffers);
        void DrawArrays(PrimitiveType mode, int first, IntPtr count);
        void DrawElements(PrimitiveType mode, int count, DrawElementsType type, IntPtr startIndexLocation);
        void DrawElementsBaseVertex(PrimitiveType mode, int count, DrawElementsType type, IntPtr startIndexLocation, int startVertexLocationBytes);
        int GetUniformLocation(int program, string name);
        void Uniform1f(int location, float falue);
        void Uniform4f(int location, float a, float b, float c, float d);
        void Uniform3f(int loc, float a, float b, float c);
        void UniformMatrix4f(int location, ref Matrix m, bool transpose);
        void TexImage2D(TextureTarget target, int level, PixelInternalFormat internalFormat, int width, int height, int border, PixelFormat format, PixelType type, IntPtr data);
        void CopyTexSubImage2D(TextureTarget target, int level, int xoffset, int yoffset, int x, int y, int width, int height);
        void TexParameteri(TextureTarget target, TextureParameterName name, int value);
        unsafe   string GetProgramInfoLog(int program, int maxLength = 2048);
        void BlendEquation(BlendEquationMode mode);
        void BlendFuncSeparate(BlendingFactorSrc srcRGB, BlendingFactorDest dstRGB, BlendingFactorSrc srcAlpha, BlendingFactorDest dstAlpha);
        int CreateProgram();
        void AttachShader(int program, int shader);
        void LinkProgram(int program);
        int CreateShader(ShaderType shaderType);
        void BlendFunc(BlendingFactorSrc src, BlendingFactorDest dst);
        string CompileShaderAndGetError(int shader, string source);
        string LinkProgramAndGetError(int program);
        void CheckError(string what);
        int GenVertexArray();
        int GenBuffer();
        int GenTexture();
        void ActiveTextureUnit(uint slot);
        void ActiveTextureUnit(int slot);
        void DeleteProgram(int program);
        unsafe void DeleteTexture(int name);
        unsafe void DeleteBuffer(int name);
        unsafe int GenFramebuffer();
        unsafe void DeleteFramebuffer(int name);
        unsafe int GenRenderbuffer();
        unsafe void DeleteRenderbuffer(int name);
        int GetUniformBlockIndex(int program, string uniformBlockName);
        void TexParameter(TextureTarget target, TextureParameterName name, int value);
        unsafe int GetProgramParameter(int program, GetProgramParameterName pname);
        unsafe string GetActiveUniform(int unit, int index, int maxLength, out int length, out int size, out ActiveUniformType type);
        void DepthMask(bool on);
        void DepthFunction(DepthFunction func);
        void Scissor(int x, int y, int width, int height);
        void Flush();
        void Finish();
        void Debug(string msg);
    }

    public static class DeviceExtensions
    {
        public static void Toggle(this IDevice device, EnableCap enableCap, bool on)
        {
            if (on)
                device.Enable(enableCap);
            else
                device.Disable(enableCap);
        }
    }
}