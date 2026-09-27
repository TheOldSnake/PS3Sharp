using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using PS3Sharp.Common;
using System.Runtime.CompilerServices;

namespace PS3Sharp.Cell.RSX {
    public sealed partial class RSXInternalState {
        public uint CurrentSemaphoreOffset { get; set; }
        public uint CurrentSemaphoreLocation { get; set; }
        public RSXVertexBuffer[] VertexBuffers { get; } = new RSXVertexBuffer[16];
        public RSXTexture[] Textures { get; } = new RSXTexture[16];
        public RSXElementBuffer ElementBuffer { get; } = new RSXElementBuffer();

        //public uint ElementBufferAddress { get; set; }          // Indices list address
        //public DrawElementsType ElementBufferType { get; set; } // Indices list type

        public uint PointX { get; set; }
        public uint PointY { get; set; }
        public uint DestOffset { get; set; }

        public BeginMode BeginMode { get; set; }
        public bool Begin { get; set; }

        public bool BlendingEnabled { get; set; }
        public bool DepthTestEnabled { get; set; }
        public bool DepthMask { get; set; }
        public BlendEquationMode CurrentBlendMode { get; set; }
        public BlendingFactorSrc CurrentBlendSrcFactor { get; set; }
        public BlendingFactorSrc CurrentBlendSrcAlphaFactor { get; set; }
        public BlendingFactorDest CurrentBlendDestFactor { get; set; }
        public BlendingFactorDest CurrentBlendDestAlphaFactor { get; set; }
        public uint ScissorX { get; set; }
        public uint ScissorY { get; set; }
        public uint ScissorWidth { get; set; }
        public uint ScissorHeight { get; set; }
        public float[] Constants { get; } = new float[468 * 4]; // 468 of vec4
        public bool ConstantsDirty { get; set; }
        public bool ShaderProgramHasChanged { get; set; }
        public Dictionary<uint, Vector4> AllUniforms { get; } = [];   // Dictionary of all uniforms and their values

        // Don't use { get; set; } on these structs
        public Vector2i SurfaceClip = new Vector2i();       
        public bool SurfaceClipDirty { get; set; }

        public Vector4 ViewPortOffset = new Vector4();       
        public bool ViewPortOffsetDirty { get; set; }

        public Vector4 ViewPortScale = new Vector4();    
        public bool ViewPortScaleDirty { get; set; }


        public uint Surface_A_Offset;
        public uint Surface_A_Location;
        public bool FrameDirty { get; set; }

        public static readonly BeginMode[] BeginModeLookup = [
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

        public static readonly PrimitiveType[] PrimitiveTypeLookup = [
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


        public static readonly Dictionary<uint, BlendingFactorSrc> BlendSrcFactors = new() {
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


        public static readonly Dictionary<uint, BlendingFactorDest> BlendDestFactors = new() {
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


        public static readonly Dictionary<uint, BlendEquationMode> BlendEquationModes = new() {
            { 0x8006, BlendEquationMode.FuncAdd },
            { 0x8007, BlendEquationMode.Min },
            { 0x8008, BlendEquationMode.Max },
            { 0x800A, BlendEquationMode.FuncSubtract },
            { 0x800B, BlendEquationMode.FuncReverseSubtract },

            // Few more
        };
    }
}
