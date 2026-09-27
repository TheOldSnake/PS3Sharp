using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using PS3Sharp.Cell.PPU.PPUInterpreter;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.Modules;
using System.Runtime.CompilerServices;
using static PS3Sharp.Cell.RSX.RSXCommand;

namespace PS3Sharp.Cell.RSX
{
    internal sealed partial class RealitySynthesizer
    {
        internal unsafe void HandleInstruction(RSXCommand command, CellGcmCtrlRegister* ctrl)
        {
            //PrintCommand(command);
            Queue<BigEndian<uint>> param;

            unsafe
            {
                ReadOnlySpan<BigEndian<uint>> param_span = MemoryManager.CreateSpan<BigEndian<uint>>(CellGcmSys.IoToEa(ctrl->Get), (int)command.MethodCount);
                ctrl->Get += command.MethodCount * 4;

                // this is very bad and slow
                param = new Queue<BigEndian<uint>>(param_span.ToArray());
            }

            uint methodRegister = command.MethodRegister;

            while (param.Count > 0)
            {
                switch (methodRegister)
                {
                    case NV40TCL_NOP:                    /* NOP */
                    case NV40TCL_VTX_CACHE_INVALIDATE:   /* NOP?? */
                    case NV40TCL_VTX_CACHE_INVALIDATE2:  /* NOP?? */

                    case NV40TCL_POLYGON_OFFSET_FILL_ENABLE:
                    case NV40TCL_POLYGON_OFFSET_FACTOR:
                    case NV40TCL_CULL_FACE:
                    case NV40TCL_CULL_FACE_ENABLE:
                    case NV40TCL_STENCIL_BACK_ENABLE:
                    case NV40TCL_FP_PACKER:
                    case NV40TCL_POLYGON_MODE_FRONT:
                    case NV40TCL_DEPTH_BOUNDS_MIN:
                    case NV40TCL_DEPTH_BOUNDS_TEST_ENABLE:
                    case NV40TCL_ANTI_ALIASING_CONTROL:
                    case NV30_3D_ALPHA_FUNC_ENABLE:
                    case NV30_3D_FOG_ENABLE:

                        param.Clear();
                        break;

                    case GCM_FLIP_COMMAND:
                        Flip(param);
                        break;

                    case NV406ETCL_SET_REF:
                        SetRef(param);
                        break;

                    case NV406ETCL_SEMAPHORE_OFFSET:
                    case NV40TCL_SEMAPHORE_BACKENDWRITE_OFFSET:
                        SemaphoreBackendwriteOffset(param);
                        break;

                    case NV406ETCL_SET_CTX_DMA_SEMAPHORE:
                        SET_CTX_DMA_SEMAPHORE(param);
                        break;

                    case NV40TCL_SEMAPHORE_BACKENDWRITE_RELEASE:
                        SemaphoreBackendwriteRelease(param);
                        break;

                    case NV406ETCL_SEMAPHORE_ACQUIRE:
                        SemaphoreAcquire(param);
                        break;

                    case NV406ETCL_SEMAPHORE_RELEASE:
                        SemaphoreRelease(param);
                        break;

                    case NV34_TCL_PRIMITIVE_3D_COLOR_MASK:
                        Primitive3DColorMask(param);
                        break;

                    case NV34_TCL_PRIMITIVE_3D_FOG_COLOR:
                        Primitive3DFogColor(param);
                        break;

                    case NV40TCL_VIEWPORT_HORIZ:
                        ViewPortHorizontal(param);
                        break;

                    case NV40TCL_VIEWPORT_VERT:
                        ViewPortVertical(param);
                        break;

                    case NV40TCL_DEPTH_RANGE:
                        DepthRange(param);
                        break;

                    case NV40TCL_VIEWPORT_OFFSET_X:
                    case NV40TCL_VIEWPORT_OFFSET_Y:
                    case NV40TCL_VIEWPORT_OFFSET_Z:
                    case NV40TCL_VIEWPORT_OFFSET_W:
                        ViewPortOffset(methodRegister, param);
                        break;

                    case NV40TCL_VIEWPORT_SCALE_X:
                    case NV40TCL_VIEWPORT_SCALE_Y:
                    case NV40TCL_VIEWPORT_SCALE_Z:
                    case NV40TCL_VIEWPORT_SCALE_W:
                        ViewPortScale(methodRegister, param);
                        break;

                    case NV40TCL_SCISSOR_HORIZ:
                        ScissorHorizontal(param);
                        break;

                    case NV40TCL_SCISSOR_VERT:
                        ScissorVertical(param);
                        break;

                    case NV40TCL_DEPTH_TEST_ENABLE:
                        DepthTestEnable(param);
                        break;

                    case NV40TCL_DEPTH_FUNC:
                        DepthFunction(param);
                        break;

                    case NV40TCL_SHADE_MODEL:
                        ShadeModel(param);
                        break;

                    case NV40TCL_DEPTH_WRITE_ENABLE:
                        DepthWriteEnable(param);
                        break;

                    case NV40TCL_FRONT_FACE:
                        FrontFace(param);
                        break;

                    case NV40TCL_DMA_COLOR0:
                    case NV40TCL_DMA_COLOR1:
                    case NV40TCL_DMA_COLOR2:
                    case NV40TCL_DMA_COLOR3:
                        DMAColor(methodRegister, param);
                        break;

                    case NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY0:
                    case NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY1:
                    //   case NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY2:
                    //   case NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY3:
                    case NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY4:
                    case NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY5:
                    case NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY6:
                    case NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY7:
                    case NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY8:
                        Primitive_3D_DMA_In_Memory(param);
                        break;

                    case NV40TCL_RT_FORMAT:
                        RTFormat(param);
                        break;

                    case NV40TCL_ZETA_PITCH:
                        ZetaPitch(param);
                        break;

                    case NV40TCL_COLOR2_PITCH:
                        Color2Pitch(param);
                        break;

                    case NV40TCL_COLOR3_PITCH:
                        Color3Pitch(param);
                        break;

                    case NV40TCL_RT_ENABLE:
                        RTEnable(param);
                        break;

                    case NV40TCL_WINDOW_OFFSET:
                        WindowOffset(param);
                        break;

                    case NV40TCL_RT_HORIZ:
                        RTHorizontal(param);
                        break;

                    case NV40TCL_RT_VERT:
                        RTVertical(param);
                        break;

                    case NV40TCL_SHADER_WINDOW:
                        ShaderWindow(param);
                        break;

                    case NV40TCL_CLEAR_VALUE_COLOR:
                        ClearValueColor(param);
                        break;

                    case NV40TCL_CLEAR_VALUE_DEPTH:
                        ClearValueDepth(param);
                        break;

                    case NV40TCL_CLEAR_BUFFERS:
                        ClearBuffers(param);
                        break;

                    case NV40TCL_DEPTH_CONTROL:
                        DepthControl(param);
                        break;

                    case NV40TCL_VIEWPORT_CLIP_HORIZ_0:
                    case NV40TCL_VIEWPORT_CLIP_HORIZ_1:
                    case NV40TCL_VIEWPORT_CLIP_HORIZ_2:
                    case NV40TCL_VIEWPORT_CLIP_HORIZ_3:
                    case NV40TCL_VIEWPORT_CLIP_HORIZ_4:
                    case NV40TCL_VIEWPORT_CLIP_HORIZ_5:
                    case NV40TCL_VIEWPORT_CLIP_HORIZ_6:
                    case NV40TCL_VIEWPORT_CLIP_HORIZ_7:
                        ViewPortClipHorizontal(param);
                        break;

                    case NV40TCL_VP_UPLOAD_FROM_ID:
                        VP_UploadFromID(param);
                        break;

                    case NV40TCL_VP_START_FROM_ID:
                        VP_StartFromID(param);
                        break;

                    case NV40TCL_VP_UPLOAD_INST:
                        VP_UploadInstructions(param);
                        break;

                    case NV40TCL_VP_ATTRIB_EN:
                        VP_AttributeEnable(param);
                        break;

                    case NV40TCL_VP_RESULT_EN:
                        VP_ResultEnable(param);
                        break;

                    case NV40TCL_TRANSFORM_TIMEOUT:
                        TransformTimeout(param);
                        break;

                    case NV40TCL_VP_UPLOAD_CONST_ID:
                        VP_UploadConstantID(param);
                        break;

                    case NV40TCL_VP_UPLOAD_CONST_X:
                        VP_UploadConstantX(param);
                        break;

                    case NV40TCL_FP_ADDRESS:
                        FP_Address(param);
                        break;

                    case NV40TCL_FP_CONTROL:
                        FP_Control(param);
                        break;

                    case NV40TCL_CLIP_PLANE_ENABLE:
                        ClipPlaneEnable(param);
                        break;

                    case NV40TCL_TEX_COORD_CONTROL:
                    case NV40TCL_TEX_COORD_CONTROL + 4:
                        TexCoordControl(param);
                        break;

                    case NV3062_SET_CONTEXT_DMA_IMAGE_DESTIN:
                        SetContextDMAImageDest(param);
                        break;

                    case NV3062_SET_OFFSET_DESTIN:
                        SetOffsetDest(param);
                        break;

                    case NV3062_SET_COLOR_FORMAT:
                        SetColorFormat(param);
                        break;

                    case NV308A_POINT:
                        Point_Position(param);
                        break;

                    case NV308A_SIZE_OUT:
                        SizeOut(param);
                        break;

                    case NV308A_COLOR:
                        Point_Color(param);
                        break;

                    case NV40TCL_BLEND_ENABLE:
                        BlendEnable(param);
                        break;

                    case NV40TCL_BLEND_FUNC_SRC:
                        BlendFunctionSRC(param);
                        break;

                    case NV40TCL_BLEND_FUNC_DST:
                        BlendFunctionDST(param);
                        break;

                    case NV40TCL_BLEND_COLOR:
                        BlendColor(param);
                        break;

                    case NV40TCL_BLEND_EQUATION:
                        BlendEquation(param);
                        break;

                    case NV40TCL_COLOR_LOGIC_OP_ENABLE:
                        ColorLogicOPEnable(param);
                        break;

                    case NV40TCL_COLOR_LOGIC_OP:
                        ColorLogicOP(param);
                        break;

                    case NV40TCL_TEX_OFFSET_0:
                    case NV40TCL_TEX_OFFSET_1:
                    case NV40TCL_TEX_OFFSET_2:
                    case NV40TCL_TEX_OFFSET_3:
                    case NV40TCL_TEX_OFFSET_4:
                    case NV40TCL_TEX_OFFSET_5:
                    case NV40TCL_TEX_OFFSET_6:
                    case NV40TCL_TEX_OFFSET_7:
                    case NV40TCL_TEX_OFFSET_8:
                    case NV40TCL_TEX_OFFSET_9:
                    case NV40TCL_TEX_OFFSET_10:
                    case NV40TCL_TEX_OFFSET_11:
                    case NV40TCL_TEX_OFFSET_12:
                    case NV40TCL_TEX_OFFSET_13:
                    case NV40TCL_TEX_OFFSET_14:
                    case NV40TCL_TEX_OFFSET_15:
                        TexOffset(methodRegister, param);
                        break;

                    case NV40TCL_TEX_FORMAT_0:
                    case NV40TCL_TEX_FORMAT_1:
                    case NV40TCL_TEX_FORMAT_2:
                    case NV40TCL_TEX_FORMAT_3:
                    case NV40TCL_TEX_FORMAT_4:
                    case NV40TCL_TEX_FORMAT_5:
                    case NV40TCL_TEX_FORMAT_6:
                    case NV40TCL_TEX_FORMAT_7:
                    case NV40TCL_TEX_FORMAT_8:
                    case NV40TCL_TEX_FORMAT_9:
                    case NV40TCL_TEX_FORMAT_10:
                    case NV40TCL_TEX_FORMAT_11:
                    case NV40TCL_TEX_FORMAT_12:
                    case NV40TCL_TEX_FORMAT_13:
                    case NV40TCL_TEX_FORMAT_14:
                    case NV40TCL_TEX_FORMAT_15:
                        TexFormat(methodRegister, param);
                        break;

                    case NV40TCL_TEX_SWIZZLE_0:
                    case NV40TCL_TEX_SWIZZLE_1:
                    case NV40TCL_TEX_SWIZZLE_2:
                    case NV40TCL_TEX_SWIZZLE_3:
                    case NV40TCL_TEX_SWIZZLE_4:
                    case NV40TCL_TEX_SWIZZLE_5:
                    case NV40TCL_TEX_SWIZZLE_6:
                    case NV40TCL_TEX_SWIZZLE_7:
                    case NV40TCL_TEX_SWIZZLE_8:
                    case NV40TCL_TEX_SWIZZLE_9:
                    case NV40TCL_TEX_SWIZZLE_10:
                    case NV40TCL_TEX_SWIZZLE_11:
                    case NV40TCL_TEX_SWIZZLE_12:
                    case NV40TCL_TEX_SWIZZLE_13:
                    case NV40TCL_TEX_SWIZZLE_14:
                    case NV40TCL_TEX_SWIZZLE_15:
                        TesSwizzle(methodRegister, param);
                        break;

                    case NV40TCL_TEX_SIZE0_0:
                    case NV40TCL_TEX_SIZE0_1:
                    case NV40TCL_TEX_SIZE0_2:
                    case NV40TCL_TEX_SIZE0_3:
                    case NV40TCL_TEX_SIZE0_4:
                    case NV40TCL_TEX_SIZE0_5:
                    case NV40TCL_TEX_SIZE0_6:
                    case NV40TCL_TEX_SIZE0_7:
                    case NV40TCL_TEX_SIZE0_8:
                    case NV40TCL_TEX_SIZE0_9:
                    case NV40TCL_TEX_SIZE0_10:
                    case NV40TCL_TEX_SIZE0_11:
                    case NV40TCL_TEX_SIZE0_12:
                    case NV40TCL_TEX_SIZE0_13:
                    case NV40TCL_TEX_SIZE0_14:
                    case NV40TCL_TEX_SIZE0_15:
                        TexSize0(methodRegister, param);
                        break;

                    case NV40TCL_TEX_SIZE1_0:
                    case NV40TCL_TEX_SIZE1_1:
                    case NV40TCL_TEX_SIZE1_2:
                    case NV40TCL_TEX_SIZE1_3:
                    case NV40TCL_TEX_SIZE1_4:
                    case NV40TCL_TEX_SIZE1_5:
                    case NV40TCL_TEX_SIZE1_6:
                    case NV40TCL_TEX_SIZE1_7:
                    case NV40TCL_TEX_SIZE1_8:
                    case NV40TCL_TEX_SIZE1_9:
                    case NV40TCL_TEX_SIZE1_10:
                    case NV40TCL_TEX_SIZE1_11:
                    case NV40TCL_TEX_SIZE1_12:
                    case NV40TCL_TEX_SIZE1_13:
                    case NV40TCL_TEX_SIZE1_14:
                    case NV40TCL_TEX_SIZE1_15:
                        TexSize1(methodRegister, param);
                        break;


                    case NV40TCL_TEX_ENABLE_0:
                    case NV40TCL_TEX_ENABLE_1:
                    case NV40TCL_TEX_ENABLE_2:
                    case NV40TCL_TEX_ENABLE_3:
                    case NV40TCL_TEX_ENABLE_4:
                    case NV40TCL_TEX_ENABLE_5:
                    case NV40TCL_TEX_ENABLE_6:
                    case NV40TCL_TEX_ENABLE_7:
                    case NV40TCL_TEX_ENABLE_8:
                    case NV40TCL_TEX_ENABLE_9:
                    case NV40TCL_TEX_ENABLE_10:
                    case NV40TCL_TEX_ENABLE_11:
                    case NV40TCL_TEX_ENABLE_12:
                    case NV40TCL_TEX_ENABLE_13:
                    case NV40TCL_TEX_ENABLE_14:
                    case NV40TCL_TEX_ENABLE_15:
                        TexEnable(methodRegister, param);
                        break;

                    case NV40TCL_TEX_FILTER_0:
                    case NV40TCL_TEX_FILTER_1:
                    case NV40TCL_TEX_FILTER_2:
                    case NV40TCL_TEX_FILTER_3:
                    case NV40TCL_TEX_FILTER_4:
                    case NV40TCL_TEX_FILTER_5:
                    case NV40TCL_TEX_FILTER_6:
                    case NV40TCL_TEX_FILTER_7:
                    case NV40TCL_TEX_FILTER_8:
                    case NV40TCL_TEX_FILTER_9:
                    case NV40TCL_TEX_FILTER_10:
                    case NV40TCL_TEX_FILTER_11:
                    case NV40TCL_TEX_FILTER_12:
                    case NV40TCL_TEX_FILTER_13:
                    case NV40TCL_TEX_FILTER_14:
                    case NV40TCL_TEX_FILTER_15:
                        TexFilter(methodRegister, param);
                        break;

                    case NV40TCL_TEX_WRAP_0:
                    case NV40TCL_TEX_WRAP_1:
                    case NV40TCL_TEX_WRAP_2:
                    case NV40TCL_TEX_WRAP_3:
                    case NV40TCL_TEX_WRAP_4:
                    case NV40TCL_TEX_WRAP_5:
                    case NV40TCL_TEX_WRAP_6:
                    case NV40TCL_TEX_WRAP_7:
                    case NV40TCL_TEX_WRAP_8:
                    case NV40TCL_TEX_WRAP_9:
                    case NV40TCL_TEX_WRAP_10:
                    case NV40TCL_TEX_WRAP_11:
                    case NV40TCL_TEX_WRAP_12:
                    case NV40TCL_TEX_WRAP_13:
                    case NV40TCL_TEX_WRAP_14:
                    case NV40TCL_TEX_WRAP_15:
                        TexWrap(methodRegister, param);
                        break;

                    case NV40TCL_TEX_CACHE_CTL:
                        TexCacheCtl(param);
                        break;

                    case NV40TCL_VTXFMT_0:
                    case NV40TCL_VTXFMT_1:
                    case NV40TCL_VTXFMT_2:
                    case NV40TCL_VTXFMT_3:
                    case NV40TCL_VTXFMT_4:
                    case NV40TCL_VTXFMT_5:
                    case NV40TCL_VTXFMT_6:
                    case NV40TCL_VTXFMT_7:
                    case NV40TCL_VTXFMT_8:
                    case NV40TCL_VTXFMT_9:
                    case NV40TCL_VTXFMT_10:
                    case NV40TCL_VTXFMT_11:
                    case NV40TCL_VTXFMT_12:
                    case NV40TCL_VTXFMT_13:
                    case NV40TCL_VTXFMT_14:
                    case NV40TCL_VTXFMT_15:
                        VertexFormat(methodRegister, param);
                        break;

                    case NV40TCL_VTXBUF_ADDRESS_0:
                    case NV40TCL_VTXBUF_ADDRESS_1:
                    case NV40TCL_VTXBUF_ADDRESS_2:
                    case NV40TCL_VTXBUF_ADDRESS_3:
                    case NV40TCL_VTXBUF_ADDRESS_4:
                    case NV40TCL_VTXBUF_ADDRESS_5:
                    case NV40TCL_VTXBUF_ADDRESS_6:
                    case NV40TCL_VTXBUF_ADDRESS_7:
                    case NV40TCL_VTXBUF_ADDRESS_8:
                    case NV40TCL_VTXBUF_ADDRESS_9:
                    case NV40TCL_VTXBUF_ADDRESS_10:
                    case NV40TCL_VTXBUF_ADDRESS_11:
                    case NV40TCL_VTXBUF_ADDRESS_12:
                    case NV40TCL_VTXBUF_ADDRESS_13:
                    case NV40TCL_VTXBUF_ADDRESS_14:
                    case NV40TCL_VTXBUF_ADDRESS_15:
                        VertexBufferAddress(methodRegister, param);
                        break;



                    case NV40TCL_VTX_ATTR_2F_0:
                    case NV40TCL_VTX_ATTR_2F_1:
                    case NV40TCL_VTX_ATTR_2F_2:
                    case NV40TCL_VTX_ATTR_2F_3:
                    case NV40TCL_VTX_ATTR_2F_4:
                    case NV40TCL_VTX_ATTR_2F_5:
                    case NV40TCL_VTX_ATTR_2F_6:
                    case NV40TCL_VTX_ATTR_2F_7:
                    case NV40TCL_VTX_ATTR_2F_8:
                    case NV40TCL_VTX_ATTR_2F_9:
                    case NV40TCL_VTX_ATTR_2F_10:
                    case NV40TCL_VTX_ATTR_2F_11:
                    case NV40TCL_VTX_ATTR_2F_12:
                    case NV40TCL_VTX_ATTR_2F_13:
                    case NV40TCL_VTX_ATTR_2F_14:
                        VertexImmediate2F(methodRegister, param);
                        break;

                    case NV40TCL_VTX_ATTR_4F_0:
                    case NV40TCL_VTX_ATTR_4F_1:
                    case NV40TCL_VTX_ATTR_4F_2:
                    case NV40TCL_VTX_ATTR_4F_3:
                    case NV40TCL_VTX_ATTR_4F_4:
                    case NV40TCL_VTX_ATTR_4F_5:
                    case NV40TCL_VTX_ATTR_4F_6:
                    case NV40TCL_VTX_ATTR_4F_7:
                    case NV40TCL_VTX_ATTR_4F_8:
                    case NV40TCL_VTX_ATTR_4F_9:
                    case NV40TCL_VTX_ATTR_4F_10:
                    case NV40TCL_VTX_ATTR_4F_11:
                    case NV40TCL_VTX_ATTR_4F_12:
                    case NV40TCL_VTX_ATTR_4F_13:
                    case NV40TCL_VTX_ATTR_4F_14:
                        VertexImmediate4F(methodRegister, param);
                        break;

                    case NV40TCL_VB_INDEX_BATCH_OFFSET:
                        IndexBatchOffset(param);
                        break;

                    case NV40TCL_BEGIN_END:
                        BeginEnd(param);
                        break;

                    case NV40TCL_VB_INDEX_BATCH_DRAW:
                        IndexBatchDraw(param);
                        break;

                    case NV40TCL_VB_VERTEX_BATCH:
                        VertexBatchDraw(param);
                        break;

                    case NV40TCL_DIVIDER_FREQUENCY_OP:
                        DividerFrequencyOp(param);
                        break;

                    default:
                        //Console.WriteLine($"[RSX] Unknown MethodRegister: {methodRegister:X} raw = {command.Raw:X8} param_count = {param.Count}");
                        param.Clear();
                        break;

                        throw new Exception($"[RSX] Unknown MethodRegister: {methodRegister:X} base command raw = {command.Raw:X8} param_count = {param.Count}");
                }


                if (!command.NonIncrement)
                {
                    methodRegister += 4;
                }
            }

            if (param.Count > 0)
            {
                throw new Exception($"{command.Raw:X8} -- param: {param.Count}");
            }
        }

        private void Flip(Queue<BigEndian<uint>> param)
        {
            uint bufferID = param.Dequeue(); // ?

            if (CurrentState.FrameDirty)
            {
                GLRenderer.BlitFBO();
                CurrentState.FrameDirty = false;
            }

            if (!CellGcmSys.FlipHandler.IsNullPtr)
            {
                PPUInterpreter.ExecuteFunction(_RSX_PPU_Thread, CellGcmSys.FlipHandler, toc: true, [1, 0, 0, 0, 0]);
            }

            MemoryManager.Write<ulong>(LabelsPtr + 0x10, 0);
            MemoryManager.Write<ulong>(LabelsPtr + 0x10 + 0x8, 0);

            //Console.WriteLine($"[RSX] Flipped");

            LastFlipTime = Utility.ReadTime_Microseconds() * 8;
            Interlocked.Exchange<ulong>(ref CellGcmSys.FlipStatus, 0);
        }

        private void Vblank()
        {
            if (!CellGcmSys.VblankHandler.IsNullPtr)
            {
                PPUInterpreter.ExecuteFunction(_RSX_PPU_Thread, CellGcmSys.VblankHandler, toc: true, [1, 0, 0, 0, 0]);
            }
        }

        private void SetRef(Queue<BigEndian<uint>> param)
        {
            uint ref_value = param.Dequeue();

            unsafe
            {
                CellGcmCtrlRegister* ctrl = CtrlPtr.AsNativePtr();
                ctrl->Ref = ref_value;
            }
        }

        private void SemaphoreBackendwriteOffset(Queue<BigEndian<uint>> param)
        {
            CurrentState.CurrentSemaphoreOffset = param.Dequeue();
            //Console.WriteLine($"[RSX] SemaphoreBackendwriteOffset = [{CurrentState.CurrentSemaphoreOffset}]");
        }

        private void SET_CTX_DMA_SEMAPHORE(Queue<BigEndian<uint>> param)
        {
            CurrentState.CurrentSemaphoreLocation = param.Dequeue() & 1;
        }

        private void SemaphoreBackendwriteRelease(Queue<BigEndian<uint>> param)
        {
            // wvalue is: u32 wvalue = (value&0xff00ff00) | ((value>>16)&0xff) | ((value&0xff)<<16);
            // Reverse it
            uint wvalue = param.Dequeue();
            uint value = (wvalue & 0xff00ff00) | ((wvalue & 0xff) << 16) | ((wvalue >> 16) & 0xff);
            MemoryManager.Write<uint>(LabelsPtr + CurrentState.CurrentSemaphoreOffset, value);
            //Console.WriteLine($"[RSX] SemaphoreBackendwriteRelease [{CurrentState.CurrentSemaphoreOffset}] = 0x{value:X8}");
        }

        private void SemaphoreAcquire(Queue<BigEndian<uint>> param)
        {
            BigEndian<uint> arg = param.Dequeue();

            // This command makes RSX waits until semaphore value == argument
            // We just compare the values as big endian instead of reversing every time

            // Get a reference to the (big endian) value, so we don't spam locking the memory

            // GuestPtr<uint> ptr = OffsetToAddress(CurrentState.CurrentSemaphoreOffset, CurrentState.CurrentSemaphoreLocation);
            GuestPtr<uint> ptr = LabelsPtr + CurrentState.CurrentSemaphoreOffset;
            ref uint be_val = ref MemoryManager.DangerousBigEndianRef<uint>(ptr);

            //while (Volatile.Read(ref be_val) != arg.RawBE) {
            //    Thread.Yield();
            //}
        }

        private void SemaphoreRelease(Queue<BigEndian<uint>> param)
        {
            uint value = param.Dequeue();
            MemoryManager.Write<uint>(LabelsPtr + CurrentState.CurrentSemaphoreOffset, value);
            //Console.WriteLine($"[RSX] SemaphoreRelease [{CurrentState.CurrentSemaphoreOffset}] = 0x{value:X8}");
        }

        internal void DividerFrequencyOp(Queue<BigEndian<uint>> param)
        {
            uint op = param.Dequeue();
            if (op != 0)
            {
                Console.WriteLine($"[RSX] NV40TCL_DIVIDER_FREQUENCY_OP = 0x{op:X8}");
            }
        }

        private void Primitive3DColorMask(Queue<BigEndian<uint>> param)
        {
            uint colorMask = param.Dequeue();
            bool r = (colorMask & GCM_COLOR_MASK_R) > 0;
            bool g = (colorMask & GCM_COLOR_MASK_G) > 0;
            bool b = (colorMask & GCM_COLOR_MASK_B) > 0;
            bool a = (colorMask & GCM_COLOR_MASK_A) > 0;

            bool hasChanged =
                GLRenderer.ColorMaskR != r ||
                GLRenderer.ColorMaskG != g ||
                GLRenderer.ColorMaskB != b ||
                GLRenderer.ColorMaskA != a;

            if (hasChanged)
            {
                GLRenderer.SetColorMask(r, g, b, a);
            }
        }

        private void Primitive3DFogColor(Queue<BigEndian<uint>> param)
        {
            uint fogColor = param.Dequeue();
        }

        private void ViewPortHorizontal(Queue<BigEndian<uint>> param)
        {
            uint w = param.Dequeue() >> 16;
            uint h = param.Dequeue() >> 16;
        }

        private void ViewPortVertical(Queue<BigEndian<uint>> param)
        {
            uint p1 = param.Dequeue();
            uint p2 = param.Dequeue();
        }

        private void DepthRange(Queue<BigEndian<uint>> param)
        {
            //TODO
            float min = Unsafe.BitCast<uint, float>(param.Dequeue());
            float max = Unsafe.BitCast<uint, float>(param.Dequeue());
        }

        private void ViewPortOffset(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            //TODO
            int index = (int)((methodRegister - NV40TCL_VIEWPORT_OFFSET_X) >> 2);
            uint raw = param.Dequeue();
            float value = Unsafe.BitCast<uint, float>(raw);

            if (CurrentState.ViewPortOffset[index] != value)
            {
                CurrentState.ViewPortOffset[index] = value;
                CurrentState.ViewPortOffsetDirty = true;
            }
        }

        private void ViewPortScale(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            //TODO
            int index = (int)((methodRegister - NV40TCL_VIEWPORT_SCALE_X) >> 2);
            uint raw = param.Dequeue();
            float value = Unsafe.BitCast<uint, float>(raw);

            if (CurrentState.ViewPortScale[index] != value)
            {
                CurrentState.ViewPortScale[index] = value;
                CurrentState.ViewPortScaleDirty = true;
            }
        }

        private void ScissorHorizontal(Queue<BigEndian<uint>> param)
        {
            uint value = param.Dequeue();
            CurrentState.ScissorX = value & 0xFFFF;
            CurrentState.ScissorWidth = (value >> 16) & 0xFFFF;
            GLRenderer.SetScissor((int)CurrentState.ScissorX, 720 - (int)(CurrentState.ScissorY + CurrentState.ScissorHeight), (int)CurrentState.ScissorWidth, (int)CurrentState.ScissorHeight);
        }

        private void ScissorVertical(Queue<BigEndian<uint>> param)
        {
            uint value = param.Dequeue();
            CurrentState.ScissorY = value & 0xFFFF;
            CurrentState.ScissorHeight = (value >> 16) & 0xFFFF;
            GLRenderer.SetScissor((int)CurrentState.ScissorX, 720 - (int)(CurrentState.ScissorY + CurrentState.ScissorHeight), (int)CurrentState.ScissorWidth, (int)CurrentState.ScissorHeight);
        }

        private void DepthTestEnable(Queue<BigEndian<uint>> param)
        {
            uint value = param.Dequeue();
            if (value > 0)
            {
                CurrentState.DepthTestEnabled = true;
                GLRenderer.EnabelDepthTest();
            }
            else
            {
                CurrentState.DepthTestEnabled = false;
                GLRenderer.DisableDepthTest();
            }
        }

        private void DepthFunction(Queue<BigEndian<uint>> param)
        {
            //They use the same values as opengl -- we can cast directly
            uint function = param.Dequeue();
            GLRenderer.SetDepthFunction((DepthFunction)function);
        }

        private void ShadeModel(Queue<BigEndian<uint>> param)
        {
            //TODO
            param.Clear();
        }

        private void DepthWriteEnable(Queue<BigEndian<uint>> param)
        {
            //Depth Mask
            uint value = param.Dequeue();
            CurrentState.DepthMask = value > 0;
            GLRenderer.SetDepthMask(CurrentState.DepthMask);
        }

        private void FrontFace(Queue<BigEndian<uint>> param)
        {
            //TODO
            param.Clear();
        }

        private void Primitive_3D_DMA_In_Memory(Queue<BigEndian<uint>> param)
        {
            //TODO
            param.Clear();
        }

        private void DMAColor(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            if (methodRegister == NV40TCL_DMA_COLOR0)
            {
                CurrentState.Surface_A_Location = param.Dequeue();
            }

            //todo
            param.Clear();
        }

        private void RTFormat(Queue<BigEndian<uint>> param)
        {
            // TODO
            //Console.WriteLine($"RSX NV40TCL_RT_FORMAT");

            // format ?
            uint format = param.Dequeue();

            // pitch ?
            uint pitch = param.Dequeue();

            // Render surface coloroffset[0]
            CurrentState.Surface_A_Offset = param.Dequeue();

            // other stuff
            param.Clear();
        }

        private void ZetaPitch(Queue<BigEndian<uint>> param)
        {
            //TODO
            param.Clear();
        }

        private void Color2Pitch(Queue<BigEndian<uint>> param)
        {
            //TODO
            param.Clear();
        }


        private void Color3Pitch(Queue<BigEndian<uint>> param)
        {
            //TODO
            param.Clear();
        }

        private void RTEnable(Queue<BigEndian<uint>> param)
        {
            //TODO
            param.Clear();
        }

        private void WindowOffset(Queue<BigEndian<uint>> param)
        {
            //TODO
            param.Clear();
        }

        private void RTHorizontal(Queue<BigEndian<uint>> param)
        {
            // Surface clip X
            uint clip = param.Dequeue();
            uint x = clip & 0xFFFF;            // todo
            int width = (int)((clip >> 16) & 0xFFFF);

            if (width != CurrentState.SurfaceClip[0])
            {
                CurrentState.SurfaceClip[0] = width;
                CurrentState.SurfaceClipDirty = true;
            }

            //Console.WriteLine($"[RSX] Surface Clip width = {width}");
        }

        private void RTVertical(Queue<BigEndian<uint>> param)
        {
            // Surface clip Y
            uint clip = param.Dequeue();
            uint y = clip & 0xFFFF;             // todo
            int height = (int)((clip >> 16) & 0xFFFF);

            if (height != CurrentState.SurfaceClip[1])
            {
                CurrentState.SurfaceClip[1] = height;
                CurrentState.SurfaceClipDirty = true;
            }
            //Console.WriteLine($"[RSX] Surface Clip height = {height}");
        }

        private void ShaderWindow(Queue<BigEndian<uint>> param)
        {
            //TODO
            param.Clear();
        }

        private void ClearValueColor(Queue<BigEndian<uint>> param)
        {
            uint color = param.Dequeue();

            float r = (color & 0xFF) / 255.0f;
            float g = ((color >> 8) & 0xFF) / 255.0f;
            float b = ((color >> 16) & 0xFF) / 255.0f;
            float a = ((color >> 24) & 0xFF) / 255.0f;

            bool hasChanged =
                GLRenderer.ClearColorR != r ||
                GLRenderer.ClearColorG != g ||
                GLRenderer.ClearColorB != b ||
                GLRenderer.ClearColorA != a;

            if (hasChanged)
            {
                GLRenderer.SetClearColor(r, g, b, a);
            }
        }

        private void ClearValueDepth(Queue<BigEndian<uint>> param)
        {
            //Console.WriteLine($"RSX NV40TCL_CLEAR_VALUE_DEPTH param_count = {command.MethodCount}");
            param.Clear();
        }

        private void ClearBuffers(Queue<BigEndian<uint>> param)
        {
            //Console.WriteLine($"RSX NV40TCL_CLEAR_BUFFERS param_count = {command.MethodCount}");

            // ensure that we're clearing the target buffer
            BindSurfaceA();

            uint value = param.Dequeue();
            if ((value >> 4) > 0)
            {                 //should & 0xf0
                GLRenderer.ClearColorBuffer();
            }

            if (((value >> 1) & 1) == 1)
            {
                GLRenderer.ClearStencilBuffer();
            }

            if ((value & 1) == 1)
            {
                GLRenderer.SetDepthMask(true);
                GLRenderer.ClearDepthBuffer();
                GLRenderer.SetDepthMask(CurrentState.DepthMask);
            }

            CurrentState.FrameDirty = true;
        }

        private void DepthControl(Queue<BigEndian<uint>> param)
        {
            param.Clear();
        }

        private void ViewPortClipHorizontal(Queue<BigEndian<uint>> param)
        {
            param.Clear();
        }

        //-----------------------------------------------Shader stuff-------------------------------------------------
        private void VP_UploadFromID(Queue<BigEndian<uint>> param)
        {
            uint loadIndex = param.Dequeue() * 4; // Index of instruction to load at in the buffer
            VertexShaderGenerator.VertexShaderLoadIndex = loadIndex;
        }

        private void VP_StartFromID(Queue<BigEndian<uint>> param)
        {
            uint startIndex = param.Dequeue() * 4; // Index of the first instruction in the program
            VertexShaderGenerator.VertexShaderShaderStart = startIndex;
        }

        private void VP_UploadInstructions(Queue<BigEndian<uint>> param)
        {
            //Console.WriteLine($"RSX NV40TCL_VP_UPLOAD_INST param_count = {param.Count}");
            VertexShaderGenerator.UploadInstructions(param);
        }

        private void VP_AttributeEnable(Queue<BigEndian<uint>> param)
        {
            uint mask = param.Dequeue();
        }

        private void VP_ResultEnable(Queue<BigEndian<uint>> param)
        {
            uint mask = param.Dequeue();
        }

        private void TransformTimeout(Queue<BigEndian<uint>> param)
        {
            param.Clear();
        }

        private void VP_UploadConstantID(Queue<BigEndian<uint>> param)
        {
            uint loadPosition = (uint)param.Dequeue(); //Get load position (first arg)

            //Console.WriteLine($"RSX NV40TCL_VP_UPLOAD_CONST_ID param_count = {command.MethodCount} load at: {loadPosition}");
            //Console.WriteLine($"Expected count: {CurrentCommand.MethodCount}, actual params: {param.Count}");

            uint count = (uint)param.Count;

            if ((count % 4) != 0)
            {
                //Console.WriteLine("[RSX] NV40TCL_VP_UPLOAD_CONST_ID: warning values count is not a multiple of 4");
                //throw new Exception();
            }

            for (uint i = 0; i < count; i++)
            {
                uint index = (loadPosition * 4) + i;

                uint raw = param.Dequeue();
                float value = Unsafe.BitCast<uint, float>(raw);

                if (value != CurrentState.Constants[index])
                {
                    CurrentState.Constants[index] = value;
                    CurrentState.ConstantsDirty = true;


                    //// just for logging
                    //string channel = string.Empty;
                    //uint vectorNumber = loadPosition + (i / 4);
                    //switch (i % 4) {
                    //    case 0: channel = "x"; break;
                    //    case 1: channel = "y"; break;
                    //    case 2: channel = "z"; break;
                    //    case 3: channel = "w"; break;
                    //}

                    //Console.WriteLine($"c[{vectorNumber}].{channel} = {value}");
                }
            }
        }

        private void VP_UploadConstantX(Queue<BigEndian<uint>> param)
        {
            //uint id = (CurrentCommand.Raw - NV40TCL_VP_UPLOAD_CONST_X) / 16;
            param.Clear();

            // todo
            throw new Exception($"NV40TCL_VP_UPLOAD_CONST_X");
        }

        // Sends the address of the fragment shader program 
        private void FP_Address(Queue<BigEndian<uint>> param)
        {
            uint offset_loc = param.Dequeue();
            uint offset = offset_loc & ~3U;
            uint loc = (offset_loc & 3) - 1;

            uint address = OffsetToAddress(offset, loc);
            FragmentShaderGenerator.FragmentShaderStartAddress = address;
        }

        private void FP_Control(Queue<BigEndian<uint>> param)
        {
            uint fp_control = param.Dequeue();
        }

        private void ClipPlaneEnable(Queue<BigEndian<uint>> param)
        {
            param.Clear();
        }

        private void TexCoordControl(Queue<BigEndian<uint>> param)
        {
            param.Clear();
        }

        private void SetContextDMAImageDest(Queue<BigEndian<uint>> param)
        {
            param.Clear();
        }

        private void SetOffsetDest(Queue<BigEndian<uint>> param)
        {
            CurrentState.DestOffset = param.Dequeue();
        }

        private void SetColorFormat(Queue<BigEndian<uint>> param)
        {
            param.Clear();
        }

        // Sets the position for NV308A_COLOR
        private void Point_Position(Queue<BigEndian<uint>> param)
        {
            //Still needs to be figured out
            uint point = param.Dequeue();
            CurrentState.PointX = point & 0xFFFF;
            CurrentState.PointY = point >> 16;
        }

        private void SizeOut(Queue<BigEndian<uint>> param)
        {
            param.Clear();
        }

        // Sends a fragment uniform at the positions set by NV308A_POINT
        private void Point_Color(Queue<BigEndian<uint>> param)
        {
            //Still needs to be figured out

            int count = param.Count;
            if (count > 4)
            {
                throw new ArgumentException($"[RSX] NV308A_COLOR got more than 4 parameters");
            }

            uint address = OffsetToAddress((CurrentState.PointX << 2) + CurrentState.DestOffset, 0);
            //string uniform = FragmentShaderGenerator.AddUniform(address);

            Span<float> values = [0.0f, 0.0f, 0.0f, 0.0f];

            for (int i = 0; i < count; i++)
            {
                uint raw = param.Dequeue();

                // Reverse the swapF32_16 function
                raw = (raw >> 16) | (raw << 16);

                values[i] = Unsafe.BitCast<uint, float>(raw);
            }

            // save the name and value to be uploaded later
            Vector4 vector = new(values[0], values[1], values[2], values[3]);
            CurrentState.AllUniforms[address] = vector;
        }

        private void BlendEnable(Queue<BigEndian<uint>> param)
        {
            bool isEnabled = param.Dequeue() != 0;
            CurrentState.BlendingEnabled = isEnabled;

            if (isEnabled)
            {
                GLRenderer.EnableBlending();
            }
            else
            {
                GLRenderer.DisableBlending();
            }
        }

        private void BlendFunctionSRC(Queue<BigEndian<uint>> param)
        {
            uint value = param.Dequeue();
            (BlendingFactorSrc srcFactor, BlendingFactorSrc srcAlphaFactor) = GetSrcBlendFactors(value);

            CurrentState.CurrentBlendSrcFactor = srcFactor;
            CurrentState.CurrentBlendSrcAlphaFactor = srcAlphaFactor;

            GLRenderer.SetBlendFactors(
                CurrentState.CurrentBlendSrcFactor,
                CurrentState.CurrentBlendDestFactor,
                CurrentState.CurrentBlendSrcAlphaFactor,
                CurrentState.CurrentBlendDestAlphaFactor
            );
        }

        private void BlendFunctionDST(Queue<BigEndian<uint>> param)
        {
            uint value = param.Dequeue();
            (BlendingFactorDest destFactor, BlendingFactorDest destAlphaFactor) = GetDestBlendFactors(value);

            CurrentState.CurrentBlendDestFactor = destFactor;
            CurrentState.CurrentBlendDestAlphaFactor = destAlphaFactor;

            GLRenderer.SetBlendFactors(
                CurrentState.CurrentBlendSrcFactor,
                CurrentState.CurrentBlendDestFactor,
                CurrentState.CurrentBlendSrcAlphaFactor,
                CurrentState.CurrentBlendDestAlphaFactor
            );
        }

        private void BlendColor(Queue<BigEndian<uint>> param)
        {
            uint color = param.Dequeue();

            float r = (color & 0xFF) / 255.0f;
            float g = ((color >> 8) & 0xFF) / 255.0f;
            float b = ((color >> 16) & 0xFF) / 255.0f;
            float a = ((color >> 24) & 0xFF) / 255.0f;

            bool hasChanged =
                GLRenderer.BlendColorR != r ||
                GLRenderer.BlendColorG != g ||
                GLRenderer.BlendColorB != b ||
                GLRenderer.BlendColorA != a;

            if (hasChanged)
            {
                GLRenderer.SetBlendColor(r, g, b, a);
            }
        }

        private void BlendEquation(Queue<BigEndian<uint>> param)
        {
            uint value = param.Dequeue();
            (BlendEquationMode rgb, BlendEquationMode alpha) = GetBlendEquation(value);

            GLRenderer.SetBlendEquation(rgb, alpha);
        }

        private void ColorLogicOPEnable(Queue<BigEndian<uint>> param)
        {
            param.Clear();
        }

        private void ColorLogicOP(Queue<BigEndian<uint>> param)
        {
            param.Clear();
        }

        // Sets texture offset
        private void TexOffset(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - NV40TCL_TEX_OFFSET_0) >> 5;
            uint offset = param.Dequeue();

            CurrentState.Textures[location].Offset = offset;
            //Console.WriteLine($"RSX NV40TCL_TEX_OFFSET: tex[{location}] offset = 0x{offset:X8}");
        }

        private void TexFormat(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - NV40TCL_TEX_FORMAT_0) >> 5;
            uint format = param.Dequeue();

            // Texture format contains a bunch of stuff
            uint memLocation = (format & 0x3) - 1;
            CurrentState.Textures[location].MemoryLocation = memLocation;
            CurrentState.Textures[location].Format = format;
        }

        private void TesSwizzle(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - NV40TCL_TEX_SWIZZLE_0) >> 5;
            uint swzzle = param.Dequeue();
            CurrentState.Textures[location].SwizzleValue = swzzle;
        }

        private void TexSize0(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - NV40TCL_TEX_SIZE0_0) >> 5;
            uint size = param.Dequeue();
            uint height = size & 0xFFFF;
            uint width = (size >> 16) & 0xFFFF;
            CurrentState.Textures[location].Height = height;
            CurrentState.Textures[location].Width = width;
        }

        private void TexSize1(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - NV40TCL_TEX_SIZE1_0) >> 2;
            uint pitch_depth = param.Dequeue();
            uint pitch = pitch_depth & 0xFFFF;
            uint depth = (pitch_depth >> 20) & 0xFFF;
            CurrentState.Textures[location].Pitch = pitch;
            CurrentState.Textures[location].Depth = depth;
        }

        private void TexEnable(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - NV40TCL_TEX_ENABLE_0) >> 5;
            uint value = param.Dequeue();
            bool isEnabled = (value >> 31) == 1;
            CurrentState.Textures[location].IsEnabled = isEnabled;
            // Console.WriteLine($"RSX NV40TCL_TEX_ENABLE {(isEnabled? "enabled" : "disabled")} tex: {location} address = 0x{CurrentState.TextureAttributes[location].Address:X8}");
        }


        private void TexFilter(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - NV40TCL_TEX_FILTER_0) >> 5;
            uint filter = param.Dequeue();
            CurrentState.Textures[location].Filter = filter;
        }

        private void TexWrap(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - NV40TCL_TEX_WRAP_0) >> 5;
            uint wrap = param.Dequeue();
            CurrentState.Textures[location].Wrap = wrap;
        }

        private void TexCacheCtl(Queue<BigEndian<uint>> param)
        {
            param.Clear();
        }

        private void VertexFormat(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            // Verex Buffer format

            uint location = (methodRegister - NV40TCL_VTXFMT_0) >> 2;

            uint format = param.Dequeue();
            uint type = format & 0xF;
            uint elements = (format >> 4) & 0xF;
            uint stride = (format >> 8) & 0xFF;
            uint frequency = (format >> 16) & 0xFFFFF;

            VertexAttribPointerType dtype;
            bool isNormalized = type == 1 || type == 4;

            switch (type)
            {
                case 1: dtype = VertexAttribPointerType.Short; break;
                case 2: dtype = VertexAttribPointerType.Float; break;
                case 3: dtype = VertexAttribPointerType.HalfFloat; break;
                case 4: dtype = VertexAttribPointerType.UnsignedByte; break;
                case 5: dtype = VertexAttribPointerType.Short; break;
                case 7: dtype = VertexAttribPointerType.UnsignedByte; break;

                case 6: //??

                // default to float
                default:
                    dtype = VertexAttribPointerType.Float;
                    //throw new NotImplementedException($"[RSX] vertex buffer type = 0x{type:X}");
                    break;
            }

            CurrentState.VertexBuffers[location].AttribType = dtype;
            CurrentState.VertexBuffers[location].IsNormalized = isNormalized;
            CurrentState.VertexBuffers[location].NumberOfElements = elements;
            CurrentState.VertexBuffers[location].Stride = stride;
            CurrentState.VertexBuffers[location].IsEnabled = elements > 0;

            //GLRenderer.SetVertexAttrib(location, (int)elements, dtype, isNormalized);
            //Console.WriteLine($"RSX NV40TCL_VTXFMT type = {dtype} elements = {elements} stride = {stride} frequency = {frequency} isNormalized: {isNormalized} location = {location}");
        }

        private void VertexBufferAddress(uint methodRegister, Queue<BigEndian<uint>> param)
        {   //Vertex Buffer Address
            uint location = (methodRegister - NV40TCL_VTXBUF_ADDRESS_0) >> 2;
            uint offset = param.Dequeue();
            uint isEA = offset >> 31;
            CurrentState.VertexBuffers[location].BufferAddress = OffsetToAddress(offset & 0x7FFFFFFF, isEA);

            //Console.WriteLine($"RSX NV40TCL_VTXBUF_ADDRESS location = {location} - offset = 0x{offset:X8} address = 0x{CurrentState.VertexAttributes[location].BufferAddress:X}");
        }

        internal void VertexImmediate4F(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - NV40TCL_VTX_ATTR_4F_0) >> 4;

            // Assuming that X, Y, Z, W are always provided. Othwerise we need to split this into 4 commands.
            float x = Unsafe.BitCast<uint, float>(param.Dequeue());
            float y = Unsafe.BitCast<uint, float>(param.Dequeue());
            float z = Unsafe.BitCast<uint, float>(param.Dequeue());
            float w = Unsafe.BitCast<uint, float>(param.Dequeue());

            CurrentState.VertexBuffers[location].NumberOfElements = 4; // vec4
            CurrentState.VertexBuffers[location].AttribType = VertexAttribPointerType.Float;
            CurrentState.VertexBuffers[location].IsNormalized = false;

            CurrentState.VertexBuffers[location].ImmediateData.Add(x);
            CurrentState.VertexBuffers[location].ImmediateData.Add(y);
            CurrentState.VertexBuffers[location].ImmediateData.Add(z);
            CurrentState.VertexBuffers[location].ImmediateData.Add(w);

            //Console.WriteLine($"vertex buffer {location} imm: ({x}. {y}, {z}, {w})");
        }

        internal void VertexImmediate2F(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - NV40TCL_VTX_ATTR_2F_0) >> 3;

            // Assuming that X, Y are always provided. Othwerise we need to split this into 2 commands.
            float x = Unsafe.BitCast<uint, float>(param.Dequeue());
            float y = Unsafe.BitCast<uint, float>(param.Dequeue());

            CurrentState.VertexBuffers[location].NumberOfElements = 2; // vec2
            CurrentState.VertexBuffers[location].AttribType = VertexAttribPointerType.Float;
            CurrentState.VertexBuffers[location].IsNormalized = false;

            CurrentState.VertexBuffers[location].ImmediateData.Add(x);
            CurrentState.VertexBuffers[location].ImmediateData.Add(y);
        }

        private void IndexBatchOffset(Queue<BigEndian<uint>> param)
        {
            uint offset = param.Dequeue();
            uint loc_type = param.Dequeue();

            uint loc = loc_type & 0xF;
            uint type = loc_type >> 4;

            CurrentState.ElementBuffer.BufferAddress = OffsetToAddress(offset, loc);

            switch (type)
            {
                case 1: CurrentState.ElementBuffer.ElementBufferType = DrawElementsType.UnsignedShort; break;
                default: throw new Exception($"unimplemented ElementBufferType = {type}");
            }

            //Console.WriteLine($"RSX NV40TCL_VB_INDEX_BATCH_OFFSET param_count = {command.MethodCount} offset = {offset:X} loc = {loc:X} type = {type:X} ");
        }

        private void BeginEnd(Queue<BigEndian<uint>> param)
        {
            // TODO: figure out immediate mode

            uint mode = param.Dequeue();
            CurrentState.Begin = mode != 0;

            if (CurrentState.Begin)
            {
                if (mode >= 1 && mode <= RSXInternalState.BeginModeLookup.Length)
                {
                    CurrentState.BeginMode = RSXInternalState.BeginModeLookup[mode - 1];
                }

            }
            else
            {

                // Mode 0 == End
                // Dispatch the last bound mode..?

                //SetupRendererImmediateMode();

                //// Manually setup vertex buffers from immediate data
                //int vertexCount = 0;
                //for (int location = 0; location < CurrentState.VertexAttributes.Length; location++) {
                //    var vertexAttribute = CurrentState.VertexAttributes[location];
                //    if (vertexAttribute.HasImmediateData) {
                //        vertexCount = Math.Max(vertexCount, vertexAttribute.ImmediateData.Count);

                //        GLRenderer.EnableAttrib(location);
                //        GLRenderer.SetVertexAttrib(location, (int)vertexAttribute.NumberOfElements, VertexAttribPointerType.Float, false); // this needs to handle other types
                //        GLRenderer.BindBuffer(location, vertexAttribute.ImmediateDataSpan);

                //    } else {
                //        GLRenderer.DisableAttrib(location);
                //    }
                //}

                //DrawArrays((uint)vertexCount, 0);

                for (int location = 0; location < CurrentState.VertexBuffers.Length; location++)
                {
                    RSXVertexBuffer vertexAttribute = CurrentState.VertexBuffers[location];
                    if (vertexAttribute.HasImmediateData)
                    {
                        vertexAttribute.ImmediateData.Clear();
                    }
                }
            }

            //Console.WriteLine($"[RSX] TODO NV40TCL_BEGIN_END Begin = {CurrentState.Begin} Mode = {CurrentState.BeginMode}");
        }

        private void IndexBatchDraw(Queue<BigEndian<uint>> param)
        {
            // Draw indexed -> GL DrawElements
            // Each param is a draw call

            //uint maxIndex = 0;
            //uint totalCount = 0;
            //switch (CurrentState.ElementBuffer.ElementBufferType) {
            //    case DrawElementsType.UnsignedShort:
            //        (maxIndex, totalCount) = SetupIndexList<ushort>(param);
            //        break;
            //}

            CurrentState.ElementBuffer.Reset();
            uint maxIndex = CurrentState.ElementBuffer.SetupElementBuffer(param);
            Span<byte> data = CurrentState.ElementBuffer.GetLastScratchData();

            // Set up vertex array and draw
            // we don't know the total vertices in the vertex buffer,
            // but we know that we need to read (maxIndex + 1) vertices   
            uint vertexCount = maxIndex + 1;
            SetupRenderer(vertexCount);

            // Send raw bytes anyways, the type is an argument of the draw call
            GLRenderer.SetIndexList<byte>(data);

            // Draw
            GLRenderer.DrawElements(CurrentState.BeginMode, (int)CurrentState.ElementBuffer.TotalElements, CurrentState.ElementBuffer.ElementBufferType);
            CurrentState.FrameDirty = true;

            //Console.WriteLine($"RSX NV40TCL_VB_INDEX_BATCH_DRAW count = {indicesTotalCount}");
        }

        private void VertexBatchDraw(Queue<BigEndian<uint>> param)
        {
            // Draw Array

            int count = param.Count;
            PrimitiveType type = RSXInternalState.PrimitiveTypeLookup[(int)CurrentState.BeginMode];

            //Console.WriteLine($"[RSX] Draw arrays command count = {count}");

            for (int i = 0; i < count; i++)
            {
                uint drawCall = param.Dequeue();
                uint start = drawCall & 0xFFFFFF;
                uint vertexCount = (drawCall >> 24) + 1;
                SetupRenderer(vertexCount, start);
                DrawArrays(vertexCount, 0);

                //Console.WriteLine($"[RSX] Draw arrays command {i}, vcount = {vertexCount}, start = {start} type = {type}");
            }

            CurrentState.FrameDirty = true;

            //// this code assumes that the draw commands are sequential,
            //// which might not be true

            //uint firstDrawCall = param.Dequeue();
            //uint start = firstDrawCall & 0xFFFFFF;
            //uint vertexCount = (firstDrawCall >> 24) + 1;

            ////Get the total vertex count
            //while (param.Count > 0) {
            //    vertexCount += ((uint)param.Dequeue()) >> 24;
            //}

            ////Console.WriteLine($"RSX NV40TCL_VB_VERTEX_BATCH vertexCount = {vertexCount} start = {start}");

            //SetupRenderer(vertexCount, start);     
            //DrawArrays(vertexCount, start);      
        }
    }
}
