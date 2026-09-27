using OpenTK.Graphics.OpenGL4;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace PS3Sharp.Cell.RSX
{
    internal sealed partial class RealitySynthesizer
    {
        internal void BindSurfaceA()
        {
            // Bind Surface A
            uint surface_a_address = OffsetToAddress(CurrentState.Surface_A_Offset, CurrentState.Surface_A_Location & 1);
            GLRenderer.SetSurface(surface_a_address);
        }

        internal void SetupRenderer(uint vertexCount, uint startOffset = 0)
        {
            BindSurfaceA();

            SetupShaders();
            SetVertexData(vertexCount, startOffset);
            UploadConstants();

            //UploadViewportTransforms(); // todo

            UploadUniforms();
            UploadTextures();
        }

        internal void SetupRendererImmediateMode()
        {
            // Does not setup vao
            BindSurfaceA();

            SetupShaders();
            UploadConstants();

            //UploadViewportTransforms(); // todo

            UploadUniforms();
            UploadTextures();
        }

        // Temporary
        private const bool LogShaders = false;
        private const string LogShadersDirectory = @"C:\Users\Old Snake\Desktop\shaders";
        private static int VertexShadersCount = 0;
        private static int FragmentShadersCount = 0;

        internal void SetupShaders()
        {
            ulong vertexChecksum = 0;
            ulong fragmentChecksum = 0;

            // Calculate checksums in parallel 
            Parallel.For(0, 2, i =>
            {
                if (i == 0)
                {
                    vertexChecksum = VertexShaderGenerator.CalculateChecksum();
                }
                else
                {
                    fragmentChecksum = FragmentShaderGenerator.CalculateChecksum();
                }
            });


            // Check if we already compiled this program before

            // Fast path, if we have a ready program with these shaders we switch to it
            if (GLRenderer.CompiledPrograms.TryGetValue((vertexChecksum, fragmentChecksum), out int handle))
            {
                //Console.WriteLine($"using old program: {handle}");
                CurrentState.ShaderProgramHasChanged = handle != GLRenderer.CurrentProgramHandle;
                GLRenderer.SwitchProgram(handle);
                return;
            }

            int vertexHandle;
            int fragmentHandle;

            // Check if we can compiled this vertex shader before
            if (!GLRenderer.CompiledVertexShaders.TryGetValue(vertexChecksum, out vertexHandle))
            {
                // Generate and compile vertex shader
                string vertexSource = VertexShaderGenerator.GenerateVertexShader();
                Console.WriteLine($"vertex shader source: \n{vertexSource}");

                if (LogShaders && Directory.Exists(LogShadersDirectory))
                {
                    File.WriteAllText(LogShadersDirectory + $@"\VertexShader{VertexShadersCount++}.txt", vertexSource);
                }

                vertexHandle = GLRenderer.CompileShader(ShaderType.VertexShader, vertexSource);
                GLRenderer.CompiledVertexShaders.Add(vertexChecksum, vertexHandle);
                Console.WriteLine($"Compiled a new vertex shader:{vertexHandle}");

            }
            else
            {
                //Already compiled - We need to clear the buffer
                //VertexShaderGenerator.ClearBuffer();
                Console.WriteLine($"using old vertex: {vertexHandle}");
            }

            // Check if we compiled this fragment shader before
            if (!GLRenderer.CompiledFragmentShaders.TryGetValue(fragmentChecksum, out fragmentHandle))
            {

                // Generate and compile fragment shader
                string fragmentSource = FragmentShaderGenerator.GenerateFragmentShader();
                Console.WriteLine($"fragment shader source: \n{fragmentSource}");


                if (LogShaders && Directory.Exists(LogShadersDirectory))
                {
                    File.WriteAllText(LogShadersDirectory + $@"\FragmentShader{FragmentShadersCount++}.txt", fragmentSource);
                }

                fragmentHandle = GLRenderer.CompileShader(ShaderType.FragmentShader, fragmentSource);
                GLRenderer.CompiledFragmentShaders.Add(fragmentChecksum, fragmentHandle);
                Console.WriteLine($"Compiled a new fragment shader: {fragmentHandle}");

            }
            else
            {
                Console.WriteLine($"using old fragment: {fragmentHandle}");
            }

            // Create a new program using these handles, and cache it
            int newProgram = GLRenderer.CreateAndUseProgram(vertexHandle, fragmentHandle);
            GLRenderer.CompiledPrograms.Add((vertexChecksum, fragmentChecksum), newProgram);
            CurrentState.ShaderProgramHasChanged = true;

            Console.WriteLine($"Created a new program: {newProgram}");
        }

        internal void UploadConstants()
        {
            if (CurrentState.ConstantsDirty || CurrentState.ShaderProgramHasChanged)
            {
                GLRenderer.UploadConstant(CurrentState.Constants);
                CurrentState.ConstantsDirty = false;
            }
        }

        internal void UploadUniforms()
        {
            // Upload uniforms

            // uniforms needs to be uploaded again if new shaders use them,
            // we cannot assume that the game will always re upload them via NV308A_COLOR

            foreach (KeyValuePair<uint, OpenTK.Mathematics.Vector4> uniform_pair in CurrentState.AllUniforms)
            {
                uint address = uniform_pair.Key;
                if (FragmentShaderGenerator.IsUniformInUse(address))
                {
                    OpenTK.Mathematics.Vector4 value = uniform_pair.Value;
                    string name = FragmentShaderGenerator.AddressToUniform(address);
                    GLRenderer.UploadUniform4(name, value);
                }
            }
        }

        internal void UploadViewportTransforms()
        {
            if (CurrentState.SurfaceClipDirty)
            {
                GLRenderer.UploadUniform2I("surface_clip", CurrentState.SurfaceClip);
                CurrentState.SurfaceClipDirty = false;
            }

            if (CurrentState.ViewPortOffsetDirty)
            {
                GLRenderer.UploadUniform3("viewport_offset", CurrentState.ViewPortOffset.Xyz);
                CurrentState.ViewPortOffsetDirty = false;
            }

            if (CurrentState.ViewPortScaleDirty)
            {
                GLRenderer.UploadUniform3("viewport_scale", CurrentState.ViewPortScale.Xyz);
                CurrentState.ViewPortScaleDirty = false;
            }
        }

        internal (T, uint) SetupIndexList<T>(Queue<BigEndian<uint>> param) where T : unmanaged, INumber<T>
        {
            // TODO: Optimize this with scratch buffers and caches  

            // Build the index list from NV40TCL_VB_INDEX_BATCH_DRAW parameters
            List<T> indexList = new();
            T maxIndex = T.Zero;

            while (param.Count > 0)
            {
                uint info = param.Dequeue();
                uint startIndex = info & 0xFFFFFF;
                uint indicesCount = (info >> 24) + 1;

                T batchMax = FillIndexList<T>(indexList, CurrentState.ElementBuffer.BufferAddress, startIndex, indicesCount);

                if (batchMax > maxIndex)
                {
                    maxIndex = batchMax;
                }
            }

            GLRenderer.SetIndexList<T>(indexList);
            return (maxIndex, (uint)indexList.Count);
        }

        internal T FillIndexList<T>(List<T> indexList, uint elementBufferAddress, uint indexStart, uint indicesCount) where T : unmanaged, INumber<T>
        {
            uint sizeOfT = (uint)Unsafe.SizeOf<T>();
            T max = T.Zero;

            elementBufferAddress += (indexStart * sizeOfT);

            for (uint i = 0; i < indicesCount; i++)
            {
                uint address = elementBufferAddress + (i * sizeOfT);
                T data = MemoryManager.Read<T>(address);
                indexList.Add(data);

                if (i == 0 || data > max)
                {
                    max = data;
                }
            }

            return max;
        }

        internal void SetVertexData(uint vertexCount, uint startOffset)
        {

            // Fetch vertex buffers from memory in parallel
            Parallel.For(0, CurrentState.VertexBuffers.Length, location =>
            {
                RSXVertexBuffer vertexBuffer = CurrentState.VertexBuffers[location];
                if (vertexBuffer.IsEnabled && vertexBuffer.NumberOfElements > 0)
                {
                    vertexBuffer.FetchVertexData(vertexCount, startOffset);
                    vertexBuffer.UpdateHashKey();
                }
            });


            for (int location = 0; location < CurrentState.VertexBuffers.Length; location++)
            {
                RSXVertexBuffer vertexBuffer = CurrentState.VertexBuffers[location];

                // Check normal vertex binding 
                if (vertexBuffer.IsEnabled && vertexBuffer.NumberOfElements > 0)
                {
                    GLRenderer.EnableAttrib(location);

                    // Check if we created this vertex buffer before
                    int handle = TryBindVertexFromCache(location, vertexBuffer);

                    if (handle == -1)
                    {
                        // Create a vertex buffer and upload data
                        CreateAndUploadVertexBuffer(location, vertexBuffer);
                    }


                }
                else
                {

                    // Check if there is any immediate data that was pushed - we only do floats currently 
                    // This is supposed to be used for Begin/End blocks

                    if (vertexBuffer.HasImmediateData && CurrentState.Begin)
                    {
                        // TODO handle caching here somehow
                        SetVertexDataImmediate(location, vertexBuffer, vertexCount);

                    }
                    else
                    {
                        GLRenderer.DisableAttrib(location);
                    }
                }
            }
        }

        internal void CreateAndUploadVertexBuffer(int location, RSXVertexBuffer vertexBuffer)
        {
            // Data is ready in the scratch buffer
            Span<byte> vertexData = vertexBuffer.GetLastScratchData();
            int handle = GLRenderer.CreateGLVertexBuffer();
            GLRenderer.BindBuffer(handle);
            GLRenderer.SetVertexAttrib(location, (int)vertexBuffer.NumberOfElements, vertexBuffer.AttribType, vertexBuffer.IsNormalized);
            GLRenderer.UploadBuffer<byte>(vertexData);

            // Add a cache entry
            GLRenderer.VertexCache.Add(vertexBuffer.CacheKey, handle);
        }

        internal void DrawArrays(uint vertexCount, uint first)
        {
            if (CurrentState.BeginMode != BeginMode.QuadStrip && CurrentState.BeginMode != BeginMode.Polygon)
            {
                PrimitiveType type = RSXInternalState.PrimitiveTypeLookup[(int)CurrentState.BeginMode];

                //Console.WriteLine($"Draw arrays type = {type} vertexCount = {vertexCount}");

                if (type != PrimitiveType.Quads)
                {
                    GLRenderer.DrawArrays(type, (int)first, (int)vertexCount);

                }
                else
                {
                    //For quads we need to use an index array since GL_QUADS is deprecated/removed
                    uint[] quad_indices = BuildQuadIndices(vertexCount / 4);
                    GLRenderer.SetIndexList<uint>(quad_indices);
                    GLRenderer.DrawElements(BeginMode.Triangles, quad_indices.Length, DrawElementsType.UnsignedInt);
                }

            }
            else
            {
                throw new Exception($"RSX: Unhandled Draw Arrays begin mode: {CurrentState.BeginMode}");
            }
        }

        internal void SetVertexDataImmediate(int location, RSXVertexBuffer vertexBuffer, uint vertexCount)
        {
            // This is very slow and hacky

            Console.WriteLine("[RSX] TODO: VertexDataImmediate");

            Span<float> immFinal;

            int totalNeededFloats = (int)(vertexBuffer.NumberOfElements * vertexCount);
            Span<float> imm = vertexBuffer.ImmediateDataSpan;

            // We should check NV40TCL_DIVIDER_FREQUENCY_OP or the vertex attributes and handle this

            if (totalNeededFloats > imm.Length)
            {
                immFinal = new float[vertexCount * vertexBuffer.NumberOfElements];

                for (int i = 0; i < immFinal.Length; i++)
                {
                    immFinal[i] = imm[(int)(i % imm.Length)];
                }

            }
            else
            {
                immFinal = imm;
            }


            int handle = GLRenderer.CreateGLVertexBuffer();
            GLRenderer.BindBuffer(handle);
            GLRenderer.SetVertexAttrib(location, (int)vertexBuffer.NumberOfElements, vertexBuffer.AttribType, vertexBuffer.IsNormalized);
            GLRenderer.UploadBuffer<float>(immFinal);
        }

        internal uint[] BuildQuadIndices(uint quadCount)
        {
            uint[] indices = new uint[quadCount * 6];

            for (uint i = 0; i < quadCount; i++)
            {
                uint v = i * 4;     // first vertex of this quad
                uint idx = i * 6;   // first index slot

                indices[idx + 0] = v + 0;
                indices[idx + 1] = v + 1;
                indices[idx + 2] = v + 2;

                indices[idx + 3] = v + 2;
                indices[idx + 4] = v + 3;
                indices[idx + 5] = v + 0;
            }

            return indices;
        }

        internal (BlendingFactorSrc, BlendingFactorSrc) GetSrcBlendFactors(uint value)
        {
            uint src = value & 0xFFFF;
            uint alpha = (value >> 16) & 0xFFFF;

            if (!RSXInternalState.BlendSrcFactors.TryGetValue(src, out BlendingFactorSrc srcFactor))
            {
                throw new NotSupportedException($"Unknown src blend factor: 0x{value:X8}");
            }

            if (!RSXInternalState.BlendSrcFactors.TryGetValue(alpha, out BlendingFactorSrc alphaFactor))
            {
                throw new NotSupportedException($"Unknown alpha blend factor: 0x{value:X8}");
            }

            return (srcFactor, alphaFactor);
        }

        internal (BlendingFactorDest, BlendingFactorDest) GetDestBlendFactors(uint value)
        {
            uint src = value & 0xFFFF;
            uint alpha = (value >> 16) & 0xFFFF;

            if (!RSXInternalState.BlendDestFactors.TryGetValue(src, out BlendingFactorDest srcFactor))
            {
                throw new NotSupportedException($"Unknown src blend factor: 0x{value:X8}");
            }

            if (!RSXInternalState.BlendDestFactors.TryGetValue(alpha, out BlendingFactorDest alphaFactor))
            {
                throw new NotSupportedException($"Unknown alpha blend factor: 0x{value:X8}");
            }

            return (srcFactor, alphaFactor);
        }

        internal (BlendEquationMode, BlendEquationMode) GetBlendEquation(uint value)
        {
            uint rgb = value & 0xFFFF;
            uint alpha = (value >> 16) & 0xFFFF;

            if (!RSXInternalState.BlendEquationModes.TryGetValue(rgb, out BlendEquationMode rgbMode))
            {
                throw new NotSupportedException($"Unknown rgb blend equation: 0x{value:X8}");
            }

            if (!RSXInternalState.BlendEquationModes.TryGetValue(alpha, out BlendEquationMode alphaMode))
            {
                throw new NotSupportedException($"Unknown alpha blend equation: 0x{value:X8}");
            }

            return (rgbMode, alphaMode);
        }


        internal void UploadTextures()
        {

            // Fetch the data and compute hashes in parallel 

            Parallel.For(0, CurrentState.Textures.Length, location =>
            {
                RSXTexture texture = CurrentState.Textures[location];

                // Ignore disabled textures as well as render surfaces
                if (!texture.IsEnabled || texture.IsSurface)
                {
                    return;
                }

                texture.FetchTextureData();
                texture.UpdateHashKey();
            });

            for (int location = 0; location < CurrentState.Textures.Length; location++)
            {
                RSXTexture texture = CurrentState.Textures[location];

                if (!texture.IsEnabled)
                {
                    continue;
                }

                int glTexHandle;

                // check if it's a render surface
                if (texture.IsSurface)
                {
                    glTexHandle = TryBindSurface(location, texture);

                }
                else
                {
                    // check if we already created this general texture before
                    glTexHandle = TryBindTextureFromCache(location, texture);
                }

                // if both failed create a new texture and upload data
                if (glTexHandle == -1)
                {
                    glTexHandle = CreateAndUploadTexture(location, texture);
                }

                SetSwizzle(glTexHandle, texture);
            }
        }

        internal int CreateAndUploadTexture(int location, RSXTexture texture)
        {
            // Data should be ready in the scratch buffer
            ReadOnlySpan<byte> data = texture.GetLastScratchData();

            // Create GL texture and bind it
            int glTexHandle = GLRenderer.CreateGLTexture();
            GLRenderer.BindTextureByHandle(location, glTexHandle, flip: 0);

            int width = (int)texture.Width;
            int height = (int)texture.Height;
            PixelInternalFormat pixelInternalFormat = texture.GetPixelInternalFormat();

            // Upload data, this is probably the most expensive operation
            if (texture.IsCompressed())
            {
                InternalFormat internalFormat = texture.GetCompressedInternalFormat();
                GLRenderer.UploadCompressedTextureData(internalFormat, width, height, data);

            }
            else
            {
                int pitchInBytes = (int)texture.Pitch;
                int bytesPerPixel = texture.GetBytesPerPixel();
                int pitchInPixels = pitchInBytes / bytesPerPixel;

                PixelType pixelType = texture.GetPixelType();
                PixelFormat pixelFormat = texture.GetPixelFormat();

                // For NV40TCL_TEX_FORMAT_FORMAT_R5G6B5
                bool needsSwapBytes = pixelType == PixelType.UnsignedShort565;
                GLRenderer.UploadTextureData<byte>(pixelInternalFormat, width, height, pitchInPixels, pixelFormat, pixelType, data, needsSwapBytes);
            }

            // Set wrap mode and min/mag filters
            TextureWrapMode wrapS = texture.GetWrapS();
            TextureWrapMode wrapT = texture.GetWrapT();
            TextureMinFilter minFilter = texture.GetMinFilter();
            TextureMagFilter magFilter = texture.GetMagFilter();

            GLRenderer.SetWrapParameters(wrapS, wrapT);
            GLRenderer.SetFilterParameters(minFilter, magFilter);

            // Generate all MipMap levels
            GLRenderer.GenerateMipMap();

            // Add the texture to the cache
            GLRenderer.TextureCache.Add(texture.CacheKey, glTexHandle);
            return glTexHandle;
        }

        internal int TryBindVertexFromCache(int location, RSXVertexBuffer vertexBuffer)
        {
            if (GLRenderer.VertexCache.TryGetValue(vertexBuffer.CacheKey, out int glvertexHandle))
            {
                GLRenderer.BindBuffer(glvertexHandle);
                GLRenderer.SetVertexAttrib(location, (int)vertexBuffer.NumberOfElements, vertexBuffer.AttribType, vertexBuffer.IsNormalized);
                return glvertexHandle;
            }

            return -1;
        }

        internal int TryBindTextureFromCache(int location, RSXTexture texture)
        {
            if (GLRenderer.TextureCache.TryGetValue(texture.CacheKey, out int glTexHandle))
            {
                GLRenderer.BindTextureByHandle(location, glTexHandle, flip: 0);
                return glTexHandle;
            }

            return -1;
        }

        internal int TryBindSurface(int location, RSXTexture texture)
        {
            int handle = GLRenderer.GetSurfaceTexture(texture.Address);

            if (handle != -1)
            {
                GLRenderer.BindTextureByHandle(location, handle, flip: 1);
            }

            return handle;
        }

        internal void SetSwizzle(int glTexHandle, RSXTexture texture)
        {
            // This code assumes that the texture is already bound

            RSXTexture.Swizzle currentSwizzle = texture.GetSwizzle();

            // Look up swizzle cache for this texture
            if (GLRenderer.TextureSwizzleCache.TryGetValue(glTexHandle, out RSXTexture.Swizzle lastSwizzle))
            {
                bool swizzleChanged = false;

                if (lastSwizzle.R != currentSwizzle.R)
                {
                    GLRenderer.SetTextureSwizzleR(currentSwizzle.R);
                    swizzleChanged = true;
                }

                if (lastSwizzle.G != currentSwizzle.G)
                {
                    GLRenderer.SetTextureSwizzleG(currentSwizzle.G);
                    swizzleChanged = true;
                }

                if (lastSwizzle.B != currentSwizzle.B)
                {
                    GLRenderer.SetTextureSwizzleB(currentSwizzle.B);
                    swizzleChanged = true;
                }

                if (lastSwizzle.A != currentSwizzle.A)
                {
                    GLRenderer.SetTextureSwizzleA(currentSwizzle.A);
                    swizzleChanged = true;
                }

                if (swizzleChanged)
                {
                    GLRenderer.TextureSwizzleCache[glTexHandle] = currentSwizzle;
                }

            }
            else
            {
                // Set the swizzle parameter and create a cache entry
                GLRenderer.SetTextureSwizzle(currentSwizzle);
                GLRenderer.TextureSwizzleCache.Add(glTexHandle, currentSwizzle);
            }
        }
    }
}
