using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using PS3Sharp.Cell.RSX;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PS3Sharp
{
    internal class GLRenderer : IDisposable
    {
        // Default shaders that sample a given texture and draw it on screen - unused -
        private const string DefaultVertexShader = @"
            #version 460
            out vec2 uv;

            const vec2 positions[4] = vec2[](
                vec2(-1.0, -1.0),
                vec2( 1.0, -1.0),
                vec2(-1.0,  1.0),
                vec2( 1.0,  1.0)
            );

            const vec2 texcoords[4] = vec2[](
                vec2(0.0, 0.0),
                vec2(1.0, 0.0),
                vec2(0.0, 1.0),
                vec2(1.0, 1.0)
            );

            void main() {
                gl_Position = vec4(positions[gl_VertexID], 0.0, 1.0);
                uv = texcoords[gl_VertexID];
            }";

        private const string DefaultFragmentShader = @"
            #version 460

            in vec2 uv;
            uniform sampler2D sourceTexture;
            out vec4 FragColor;

            void main() {
                FragColor = texture(sourceTexture, uv);
            }";

        internal const int FboTextureUnit = 20; // or anything more than 15

        private readonly GameWindow _gameWindow;

        // Handles for VAO / EBO / Depth texture
        internal int VertexArrayObject { get; private set; }
        internal int ElementBufferObject { get; private set; }
        internal int DepthTexture { get; private set; }

        // Current shaders / program handles
        internal int DefaultGLProgram { get; private set; }
        internal int CurrentProgramHandle { get; private set; }
        internal int CurrentVertexShaderHandle { get; private set; }
        internal int CurrentFragmentShaderHandle { get; private set; }

        // Dictionaries probable need their own cache sealed class

        // Shader checksum to handle lookup dictionaries
        internal Dictionary<(ulong vertexChecksum, ulong fragmentChecksum), int> CompiledPrograms { get; private set; } = [];
        internal Dictionary<ulong, int> CompiledVertexShaders { get; private set; } = [];
        internal Dictionary<ulong, int> CompiledFragmentShaders { get; private set; } = [];

        // Surface to fbo/texture lookup dictionary
        internal Dictionary<uint, (int fbo, int texture)> Surfaces { get; private set; } = [];

        // Texture cache lookup dictionary
        internal Dictionary<RSXTexture.TextureCacheKey, int> TextureCache { get; private set; } = [];

        // Texture swizzle cache lookup dictionary (lookup by gl tex handle)
        internal Dictionary<int, RSXTexture.Swizzle> TextureSwizzleCache { get; private set; } = [];

        // Vertex buffer cache lookup dictionary
        internal Dictionary<RSXVertexBuffer.VertexCacheKey, int> VertexCache { get; private set; } = [];

        // Last framebuffer that was bound
        internal int LastBoundFBO { get; private set; }

        // Global GL states (probaly need their own sealed class)
        internal float ClearColorR { get; private set; } = 0.0f;
        internal float ClearColorG { get; private set; } = 0.0f;
        internal float ClearColorB { get; private set; } = 0.0f;
        internal float ClearColorA { get; private set; } = 1.0f;

        internal float BlendColorR { get; private set; } = 0.0f;
        internal float BlendColorG { get; private set; } = 0.0f;
        internal float BlendColorB { get; private set; } = 0.0f;
        internal float BlendColorA { get; private set; } = 1.0f;

        internal bool ColorMaskR { get; private set; } = false;
        internal bool ColorMaskG { get; private set; } = false;
        internal bool ColorMaskB { get; private set; } = false;
        internal bool ColorMaskA { get; private set; } = false;

        internal GLRenderer(GameWindow gameWindow)
        {
            _gameWindow = gameWindow;

            CreateGLObjects();

            // Compile the default program
            int vertex = CompileShader(ShaderType.VertexShader, DefaultVertexShader);
            int fragment = CompileShader(ShaderType.FragmentShader, DefaultFragmentShader);
            DefaultGLProgram = CreateAndUseProgram(vertex, fragment);

            // Bind VAO
            GL.BindVertexArray(VertexArrayObject);

            // Set default viewport
            GL.Viewport(0, 0, 1280, 720);

            // Enable depth function
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);

            // Disable scissor test by default
            GL.Disable(EnableCap.ScissorTest);

            // Set front face to clockwise by default
            GL.FrontFace(FrontFaceDirection.Cw);

            // Clear the screen
            GL.ClearColor(ClearColorR, ClearColorG, ClearColorB, ClearColorA);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            _gameWindow.SwapBuffers();
        }

        internal void CreateGLObjects()
        {
            // Create VAO and EBO
            VertexArrayObject = GL.GenVertexArray();
            ElementBufferObject = GL.GenBuffer();

            // Global parameters
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            GL.PixelStore(PixelStoreParameter.PackAlignment, 1);

            // Create depth texture
            DepthTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, DepthTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent24, 1280, 720, 0, PixelFormat.DepthComponent, PixelType.Float, 0);
        }

        internal int CreateAndUseProgram(int vertexHandle, int fragmentHandle)
        {
            CurrentVertexShaderHandle = vertexHandle;
            CurrentFragmentShaderHandle = fragmentHandle;

            // Create a new program and link these shaders
            GL.UseProgram(0);
            CurrentProgramHandle = GL.CreateProgram();

            GL.AttachShader(CurrentProgramHandle, CurrentVertexShaderHandle);
            GL.AttachShader(CurrentProgramHandle, CurrentFragmentShaderHandle);
            Link(CurrentProgramHandle);
            GL.UseProgram(CurrentProgramHandle);
            return CurrentProgramHandle;
        }

        internal void SwitchProgram(int handle)
        {
            if (CurrentProgramHandle != handle)
            {
                CurrentProgramHandle = handle;
                GL.UseProgram(CurrentProgramHandle);
            }
        }

        internal int CompileShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int code);  //Check for compilation errors
            if (code != (int)All.True)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"Error occurred whilst compiling Shader({shader}).\n\n{infoLog}");
            }
            else
            {
                Console.WriteLine("[OpenGL] Shader compiled!");
            }

            return shader;
        }

        private void Link(int program)
        {
            //Link the program
            GL.LinkProgram(program);
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int code);    // Check for linking errors
            if (code != (int)All.True)
            {
                throw new Exception($"Error occurred whilst linking Program({program})");
            }
        }

        internal void BlitFBO()
        {
            // Just blit the last draw FBO into the screen, this is probably inaccurate
            // we can also draw a square instrad of GL.BlitFramebuffer
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, LastBoundFBO);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

            GL.BlitFramebuffer(0, 0, 1280, 720, 0, 0, 1280, 720, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
            SwapBuffers();
        }

        internal void DrawFrame(uint surface_a)
        {          // unused
            // Draw a quad that uses surface A as a texture
            int surface_a_tex = GetSurfaceTexture(surface_a);

            int currentProgram = CurrentProgramHandle;
            SwitchProgram(DefaultGLProgram);

            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

            GL.BindVertexArray(VertexArrayObject); // do we need to rebind?
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, surface_a_tex);
            GL.Uniform1(GL.GetUniformLocation(DefaultGLProgram, "sourceTexture"), 0);

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
            SwapBuffers();

            SwitchProgram(currentProgram);
            SetSurface(surface_a); // Rebinds surface a fbo
        }

        internal void SetSurface(uint address)
        {
            // Activate the fbo texture unit, we don't want to bind to one of the 16 units accidentally
            GL.ActiveTexture(TextureUnit.Texture0 + FboTextureUnit);

            // If we created it before we can bind directly 
            if (Surfaces.TryGetValue(address, out (int fbo, int texture) surface))
            {
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, surface.fbo);
                GL.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, surface.texture, 0);
                LastBoundFBO = surface.fbo;

            }
            else
            {

                // Create a framebuffer / texture 
                int tex = GL.GenTexture();
                int fbo = GL.GenFramebuffer();

                GL.BindTexture(TextureTarget.Texture2D, tex);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1280, 720, 0, PixelFormat.Bgra, PixelType.UnsignedByte, 0);

                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, fbo);
                GL.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, tex, 0);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

                Surfaces.Add(address, (fbo, tex));
                LastBoundFBO = fbo;
            }

            // Bind our depth texture (I don't know if we need to create one per target)
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, DepthTexture, 0);

            // Reset to texture unit 0, this is probably not needed
            GL.ActiveTexture(TextureUnit.Texture0);
        }

        internal int GetSurfaceTexture(uint address)
        {
            int handle = -1;

            if (Surfaces.TryGetValue(address, out (int fbo, int texture) surface))
            {
                handle = surface.texture;
            }

            return handle;
        }

        internal bool AddressIsSurface(uint address)
        {
            return Surfaces.ContainsKey(address);
        }

        internal bool UploadConstant(float[] values)
        {
            int c_location = GL.GetUniformLocation(CurrentProgramHandle, $"c[0]");
            const int count = 468;

            if (c_location != -1)
            {
                GL.Uniform4(c_location, count, values);
                return true;
            }

            return false;
        }

        internal bool UploadUniform4(string name, Vector4 value)
        {
            int location = GL.GetUniformLocation(CurrentProgramHandle, name);

            if (location != -1)
            {
                GL.Uniform4(location, value);
                return true;
            }

            return false;
        }

        internal void UploadUniform3(string name, Vector3 value)
        {

            int location = GL.GetUniformLocation(CurrentProgramHandle, name);
            if (location == -1)
            {
                return;
            }

            GL.Uniform3(location, value);
        }

        internal void UploadUniform2I(string name, Vector2i value)
        {
            int location = GL.GetUniformLocation(CurrentProgramHandle, name);

            if (location == -1)
            {
                return;
            }

            GL.Uniform2(location, value);
        }

        internal int CreateGLTexture()
        {
            return GL.GenTexture();
        }

        internal void SetTextureView(int tex, PixelInternalFormat pixelInternalFormat, int numLevels, int numLayers)
        {
            GL.TextureView(tex, TextureTarget.Texture2D, 0, pixelInternalFormat, 0, numLevels, 0, numLayers);
        }

        internal void BindTextureByHandle(int textureIndex, int glTexHandle, int flip)
        {
            int samplerLocation = GL.GetUniformLocation(CurrentProgramHandle, $"u_tex{textureIndex}");
            int flipBoolLocation = GL.GetUniformLocation(CurrentProgramHandle, $"u_flip_tex_flags[{textureIndex}]");

            // Activate the texture unit and bind the texture object to it
            TextureUnit textureUnit = TextureUnit.Texture0 + textureIndex;
            GL.ActiveTexture(textureUnit);
            GL.BindTexture(TextureTarget.Texture2D, glTexHandle);

            // Assign the sampler to the texture unit index
            GL.Uniform1(samplerLocation, textureIndex);

            // Assign the flip flag
            GL.Uniform1(flipBoolLocation, flip);
        }

        internal void UploadTextureData<T>(PixelInternalFormat internalFormat, int width, int height, int pitch_pixels,
            PixelFormat format, PixelType type, ReadOnlySpan<T> data, bool swapBytes) where T : unmanaged
        {

            if (data.IsEmpty)
            {
                return;
            }

            int swapParameter = swapBytes ? 1 : 0;
            int unpackRowLength = pitch_pixels == width ? 0 : pitch_pixels;
            SetPixelStoreParameters(swapParameter, unpackRowLength);

            ref T dataReference = ref MemoryMarshal.GetReference(data);
            GL.TexImage2D<T>(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, format, type, ref dataReference);
        }

        internal void UploadCompressedTextureData<T>(InternalFormat internalFormat, int width, int height, ReadOnlySpan<T> data) where T : unmanaged
        {

            if (data.IsEmpty)
            {
                return;
            }

            SetPixelStoreParameters(0, 0);

            ref T dataReference = ref MemoryMarshal.GetReference(data);
            GL.CompressedTexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, data.Length, ref dataReference);
        }

        internal void GenerateMipMap()
        {
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        internal void SetPixelStoreParameters(int swapBytes, int unpackRowLength)
        {
            GL.PixelStore(PixelStoreParameter.UnpackSwapBytes, swapBytes);
            GL.PixelStore(PixelStoreParameter.UnpackRowLength, unpackRowLength);

            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.MirroredRepeat);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.MirroredRepeat);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        }

        internal void SetWrapParameters(TextureWrapMode wrapS, TextureWrapMode wrapT)
        {
            //Console.WriteLine($"wrapS = {wrapS}");
            //Console.WriteLine($"wrapT = {wrapT}");
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapS);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrapT);
        }

        internal void SetFilterParameters(TextureMinFilter minFilter, TextureMagFilter magFilter)
        {
            //Console.WriteLine($"minFilter = {minFilter}");
            //Console.WriteLine($"magFilter = {magFilter}");
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);
        }

        internal void SetTextureSwizzle(RSXTexture.Swizzle swizzle)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleA, swizzle.A);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleR, swizzle.R);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleG, swizzle.G);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleB, swizzle.B);
        }

        internal void SetTextureSwizzleA(int value)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleA, value);
        }

        internal void SetTextureSwizzleR(int value)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleR, value);
        }

        internal void SetTextureSwizzleG(int value)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleG, value);
        }

        internal void SetTextureSwizzleB(int value)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleB, value);
        }

        internal void ResetTextureSwizzle()
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleA, (int)All.Alpha);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleR, (int)All.Red);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleG, (int)All.Green);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleB, (int)All.Blue);
        }

        internal void ClearColorBuffer()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
        }

        internal void ClearDepthBuffer()
        {
            GL.Clear(ClearBufferMask.DepthBufferBit);
        }

        internal void ClearStencilBuffer()
        {
            GL.Clear(ClearBufferMask.StencilBufferBit);
        }

        internal void SetClearColor(float r, float g, float b, float a)
        {
            ClearColorR = r;
            ClearColorG = g;
            ClearColorB = b;
            ClearColorA = a;
            GL.ClearColor(r, g, b, a);
        }

        internal void SetBlendColor(float r, float g, float b, float a)
        {
            BlendColorR = r;
            BlendColorG = g;
            BlendColorB = b;
            BlendColorA = a;
            GL.BlendColor(r, g, b, a);
        }

        internal void SetColorMask(bool r, bool g, bool b, bool a)
        {
            ColorMaskR = r;
            ColorMaskG = g;
            ColorMaskB = b;
            ColorMaskA = a;
            GL.ColorMask(r, g, b, a);
        }

        internal void EnableAttrib(int index)
        {
            GL.EnableVertexAttribArray(index);
        }

        internal void DisableAttrib(int index)
        {
            GL.DisableVertexAttribArray(index);
        }

        internal void EnableBlending()
        {
            GL.Enable(EnableCap.Blend);
        }

        internal void DisableBlending()
        {
            GL.Disable(EnableCap.Blend);
        }

        internal void SetBlendFactors(BlendingFactorSrc srcFactor, BlendingFactorDest dstFactor, BlendingFactorSrc srcAlpha, BlendingFactorDest dstAlpha)
        {
            GL.BlendFuncSeparate(srcFactor, dstFactor, srcAlpha, dstAlpha);
        }

        internal void SetBlendEquation(BlendEquationMode rgb, BlendEquationMode alpha)
        {
            GL.BlendEquationSeparate(rgb, alpha);
        }

        internal void EnabelDepthTest()
        {
            GL.Enable(EnableCap.DepthTest);
        }

        internal void DisableDepthTest()
        {
            GL.Disable(EnableCap.DepthTest);
        }

        internal void SetDepthFunction(DepthFunction function)
        {
            GL.DepthFunc(function);
        }

        internal void SetDepthMask(bool mask)
        {
            GL.DepthMask(mask);
        }

        internal void SetScissor(int x, int y, int width, int height)
        {
            GL.Scissor(x, y, width, height);
        }

        internal int CreateGLVertexBuffer()
        {
            return GL.GenBuffer();
        }

        internal void SetIndexList<T>(Span<T> indices) where T : unmanaged
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * Unsafe.SizeOf<T>(), ref indices[0], BufferUsageHint.StreamDraw);
        }

        internal void SetIndexList<T>(List<T> indices) where T : unmanaged
        {
            Span<T> indicesAsSpan = CollectionsMarshal.AsSpan<T>(indices);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indicesAsSpan.Length * Unsafe.SizeOf<T>(), ref indicesAsSpan[0], BufferUsageHint.StreamDraw);
        }

        internal void SetVertexAttrib(int location, int size, VertexAttribPointerType dtype, bool isNormalized)
        {
            GL.VertexAttribPointer(
               index: location,          // layout(location = X)
               size: size,
               type: dtype,
               normalized: isNormalized,
               stride: 0,
               offset: 0
            );
        }

        internal void BindBuffer(int handle)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, handle);
        }

        internal void UploadBuffer<T>(Span<T> data) where T : unmanaged
        {
            GL.BufferData(BufferTarget.ArrayBuffer, data.Length * Unsafe.SizeOf<T>(), ref data[0], BufferUsageHint.StreamDraw);
        }

        internal void DrawElements(BeginMode mode, int count, DrawElementsType drawElementsType)
        {
            GL.DrawElements(mode, count, drawElementsType, 0);
        }

        internal void DrawArrays(PrimitiveType mode, int first, int count)
        {
            GL.DrawArrays(mode, first, count);
        }

        internal void SwapBuffers()
        {
            _gameWindow.SwapBuffers();
        }

        public void Dispose()
        {
            // TODO
            Console.WriteLine($"[GLRenderer] Disposing GL objects");

            GL.DeleteVertexArray(VertexArrayObject);
            GL.DeleteTexture(DepthTexture);
            GL.DeleteBuffer(ElementBufferObject);

            foreach (int vertexBuffer in VertexCache.Values)
            {
                GL.DeleteBuffer(vertexBuffer);
            }

            foreach (int texture in TextureCache.Values)
            {
                GL.DeleteTexture(texture);
            }

            foreach ((int fbo, int texture) in Surfaces.Values)
            {
                GL.DeleteFramebuffer(fbo);
                GL.DeleteTexture(texture);
            }

            foreach (int vertexShader in CompiledVertexShaders.Values)
            {
                GL.DeleteShader(vertexShader);
            }

            foreach (int fragmentShader in CompiledFragmentShaders.Values)
            {
                GL.DeleteShader(fragmentShader);
            }

            foreach (int program in CompiledPrograms.Values)
            {
                GL.DeleteProgram(program);
            }
        }
    }
}