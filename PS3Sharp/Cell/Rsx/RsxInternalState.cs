using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using PS3Sharp.GuestMemory;

namespace PS3Sharp.Cell.RSX
{
    internal sealed partial class RSXInternalState
    {
        private readonly MemoryManager _memoryManager;

        internal uint CurrentSemaphoreOffset { get; set; }
        internal uint CurrentSemaphoreLocation { get; set; }
        internal RSXVertexBuffer[] VertexBuffers { get; } = new RSXVertexBuffer[16];
        internal RSXTexture[] Textures { get; } = new RSXTexture[16];
        internal RSXElementBuffer ElementBuffer { get; }

        internal uint PointX { get; set; }
        internal uint PointY { get; set; }
        internal uint DestOffset { get; set; }

        internal BeginMode BeginMode { get; set; }
        internal bool Begin { get; set; }

        internal bool BlendingEnabled { get; set; }
        internal bool DepthTestEnabled { get; set; }
        internal bool DepthMask { get; set; }
        internal BlendEquationMode CurrentBlendMode { get; set; }
        internal BlendingFactorSrc CurrentBlendSrcFactor { get; set; }
        internal BlendingFactorSrc CurrentBlendSrcAlphaFactor { get; set; }
        internal BlendingFactorDest CurrentBlendDestFactor { get; set; }
        internal BlendingFactorDest CurrentBlendDestAlphaFactor { get; set; }
        internal uint ScissorX { get; set; }
        internal uint ScissorY { get; set; }
        internal uint ScissorWidth { get; set; }
        internal uint ScissorHeight { get; set; }
        internal float[] Constants { get; } = new float[468 * 4]; // 468 of vec4
        internal bool ConstantsDirty { get; set; }
        internal bool ShaderProgramHasChanged { get; set; }
        internal Dictionary<uint, Vector4> AllUniforms { get; } = [];   // Dictionary of all uniforms and their values

        internal Vector2i SurfaceClip { get; set; }
        internal bool SurfaceClipDirty { get; set; }

        internal Vector4 ViewPortOffset { get; set; }
        internal bool ViewPortOffsetDirty { get; set; }

        internal Vector4 ViewPortScale { get; set; }
        internal bool ViewPortScaleDirty { get; set; }

        internal uint SurfaceAOffset;
        internal uint SurfaceALocation;
        internal bool FrameDirty { get; set; }

        internal static ReadOnlySpan<BeginMode> BeginModeLookup => [
            BeginMode.Points,        // 1
            BeginMode.Lines,         // 2
            BeginMode.LineLoop,      // 3
            BeginMode.LineStrip,     // 4
            BeginMode.Triangles,     // 5
            BeginMode.TriangleStrip, // 6
            BeginMode.TriangleFan,   // 7
            BeginMode.Quads,         // 8
            BeginMode.QuadStrip,     // 9
            BeginMode.Polygon        // 10
        ];

        internal static ReadOnlySpan<PrimitiveType> PrimitiveTypeLookup => [
            PrimitiveType.Points,        // 1
            PrimitiveType.Lines,         // 2
            PrimitiveType.LineLoop,      // 3
            PrimitiveType.LineStrip,     // 4
            PrimitiveType.Triangles,     // 5
            PrimitiveType.TriangleStrip, // 6
            PrimitiveType.TriangleFan,   // 7
            PrimitiveType.Quads,         // 8
            //PrimitiveType.QuadStrip,     // 9
            //PrimitiveType.Polygon        // 10
        ];


        internal static readonly Dictionary<uint, BlendingFactorSrc> BlendSrcFactors = new() {
            { 0x00000000, BlendingFactorSrc.Zero },
            { 0x00000001, BlendingFactorSrc.One },

            { 0x00000300, BlendingFactorSrc.SrcColor },
            { 0x00000301, BlendingFactorSrc.OneMinusSrcColor },
            { 0x00000302, BlendingFactorSrc.SrcAlpha },
            { 0x00000303, BlendingFactorSrc.OneMinusSrcAlpha },

            { 0x00000304, BlendingFactorSrc.DstAlpha },
            { 0x00000305, BlendingFactorSrc.OneMinusDstAlpha },
            { 0x00000306, BlendingFactorSrc.DstColor },
            { 0x00000307, BlendingFactorSrc.OneMinusDstColor },
            { 0x00000308, BlendingFactorSrc.SrcAlphaSaturate },

            { 0x00008001, BlendingFactorSrc.ConstantColor },
            { 0x00008002, BlendingFactorSrc.OneMinusConstantColor },
            { 0x00008003, BlendingFactorSrc.ConstantAlpha },
            { 0x00008004, BlendingFactorSrc.OneMinusConstantAlpha },
        };


        internal static readonly Dictionary<uint, BlendingFactorDest> BlendDestFactors = new() {
            { 0x00000000, BlendingFactorDest.Zero },
            { 0x00000001, BlendingFactorDest.One },

            { 0x00000300, BlendingFactorDest.SrcColor },
            { 0x00000301, BlendingFactorDest.OneMinusSrcColor },
            { 0x00000302, BlendingFactorDest.SrcAlpha },
            { 0x00000303, BlendingFactorDest.OneMinusSrcAlpha },

            { 0x00000304, BlendingFactorDest.DstAlpha },
            { 0x00000305, BlendingFactorDest.OneMinusDstAlpha },
            { 0x00000306, BlendingFactorDest.DstColor },
            { 0x00000307, BlendingFactorDest.OneMinusDstColor },
            { 0x00000308, BlendingFactorDest.SrcAlphaSaturate },

            { 0x00008001, BlendingFactorDest.ConstantColor },
            { 0x00008002, BlendingFactorDest.OneMinusConstantColor },
            { 0x00008003, BlendingFactorDest.ConstantAlpha },
            { 0x00008004, BlendingFactorDest.OneMinusConstantAlpha },
        };


        internal static readonly Dictionary<uint, BlendEquationMode> BlendEquationModes = new() {
            { 0x8006, BlendEquationMode.FuncAdd },
            { 0x8007, BlendEquationMode.Min },
            { 0x8008, BlendEquationMode.Max },
            { 0x800A, BlendEquationMode.FuncSubtract },
            { 0x800B, BlendEquationMode.FuncReverseSubtract },

            // Few more
        };

        internal RSXInternalState(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
            ElementBuffer = new RSXElementBuffer(_memoryManager);
        }
    }
}
