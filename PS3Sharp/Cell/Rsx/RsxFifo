using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using PS3Sharp.Cell.PPU.Interpreter;
using PS3Sharp.Common;
using System.Runtime.CompilerServices;
using static PS3Sharp.Cell.RSX.RSXCommand;

namespace PS3Sharp.Cell.RSX
{
    internal sealed partial class RealitySynthesizer
    {
        internal void HandleInstruction(RSXCommand command, ref CellGcmCtrlRegister ctrl)
        {
            //PrintCommand(command);
            BigEndian<uint>[] param_array = _memoryManager.ReadArray<BigEndian<uint>>(_cellGcmSys.IoToEa(ctrl.Get), (int)command.MethodCount);
            Queue<BigEndian<uint>> param = new(param_array);   // this is very bad and slow

            ctrl.Get += command.MethodCount * 4;
            uint methodRegister = command.MethodRegister;

            while (param.Count > 0)
            {
                switch (methodRegister)
                {
                    case Nv40TclNop:                    /* NOP */
                    case Nv40TclVtxCacheInvalidate:   /* NOP?? */
                    case Nv40TclVtxCacheInvalidate2:  /* NOP?? */

                    case Nv40TclPolygonOffsetFillEnable:
                    case Nv40TclPolygonOffsetFactor:
                    case Nv40TclCullFace:
                    case Nv40TclCullFaceEnable:
                    case Nv40TclStencilBackEnable:
                    case Nv40TclFpPacker:
                    case Nv40TclPolygonModeFront:
                    case Nv40TclDepthBoundsMin:
                    case Nv40TclDepthBoundsTestEnable:
                    case Nv40TclAntiAliasingControl:
                    case Nv303dAlphaFuncEnable:
                    case Nv303dFogEnable:

                        param.Clear();
                        break;

                    case GcmFlipCommand:
                        Flip(param);
                        break;

                    case Nv406eTclSetRef:
                        SetRef(param);
                        break;

                    case Nv406eTclSemaphoreOffset:
                    case Nv40TclSemaphoreBackendwriteOffset:
                        SemaphoreBackendwriteOffset(param);
                        break;

                    case Nv406eTclSetCtxDmaSemaphore:
                        SET_CTX_DMA_SEMAPHORE(param);
                        break;

                    case Nv40TclSemaphoreBackendwriteRelease:
                        SemaphoreBackendwriteRelease(param);
                        break;

                    case Nv406eTclSemaphoreAcquire:
                        SemaphoreAcquire(param);
                        break;

                    case Nv406eTclSemaphoreRelease:
                    case Nv4097TextureReadSemaphoreRelease:
                        SemaphoreRelease(param);
                        break;

                    case Nv34TclPrimitive3dColorMask:
                        Primitive3DColorMask(param);
                        break;

                    case Nv34TclPrimitive3dFogColor:
                        Primitive3DFogColor(param);
                        break;

                    case Nv40TclViewportHoriz:
                        ViewPortHorizontal(param);
                        break;

                    case Nv40TclViewportVert:
                        ViewPortVertical(param);
                        break;

                    case Nv40TclDepthRange:
                        DepthRange(param);
                        break;

                    case Nv40TclViewportOffsetX:
                    case Nv40TclViewportOffsetY:
                    case Nv40TclViewportOffsetZ:
                    case Nv40TclViewportOffsetW:
                        ViewPortOffset(methodRegister, param);
                        break;

                    case Nv40TclViewportScaleX:
                    case Nv40TclViewportScaleY:
                    case Nv40TclViewportScaleZ:
                    case Nv40TclViewportScaleW:
                        ViewPortScale(methodRegister, param);
                        break;

                    case Nv40TclScissorHoriz:
                        ScissorHorizontal(param);
                        break;

                    case Nv40TclScissorVert:
                        ScissorVertical(param);
                        break;

                    case Nv40TclDepthTestEnable:
                        DepthTestEnable(param);
                        break;

                    case Nv40TclDepthFunc:
                        DepthFunction(param);
                        break;

                    case Nv40TclShadeModel:
                        ShadeModel(param);
                        break;

                    case Nv40TclDepthWriteEnable:
                        DepthWriteEnable(param);
                        break;

                    case Nv40TclFrontFace:
                        FrontFace(param);
                        break;

                    case Nv40TclDmaColor0:
                    case Nv40TclDmaColor1:
                    case Nv40TclDmaColor2:
                    case Nv40TclDmaColor3:
                        DMAColor(methodRegister, param);
                        break;

                    case Nv34TclPrimitive3dDmaInMemory0:
                    case Nv34TclPrimitive3dDmaInMemory1:
                    //   case NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY2:
                    //   case NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY3:
                    case Nv34TclPrimitive3dDmaInMemory4:
                    case Nv34TclPrimitive3dDmaInMemory5:
                    case Nv34TclPrimitive3dDmaInMemory6:
                    case Nv34TclPrimitive3dDmaInMemory7:
                    case Nv34TclPrimitive3dDmaInMemory8:
                        Primitive_3D_DMA_In_Memory(param);
                        break;

                    case Nv40TclRtFormat:
                        RTFormat(param);
                        break;

                    case Nv40TclZetaPitch:
                        ZetaPitch(param);
                        break;

                    case Nv40TclColor2Pitch:
                        Color2Pitch(param);
                        break;

                    case Nv40TclColor3Pitch:
                        Color3Pitch(param);
                        break;

                    case Nv40TclRtEnable:
                        RTEnable(param);
                        break;

                    case Nv40TclWindowOffset:
                        WindowOffset(param);
                        break;

                    case Nv40TclRtHoriz:
                        RTHorizontal(param);
                        break;

                    case Nv40TclRtVert:
                        RTVertical(param);
                        break;

                    case Nv40TclShaderWindow:
                        ShaderWindow(param);
                        break;

                    case Nv40TclClearValueColor:
                        ClearValueColor(param);
                        break;

                    case Nv40TclClearValueDepth:
                        ClearValueDepth(param);
                        break;

                    case Nv40TclClearBuffers:
                        ClearBuffers(param);
                        break;

                    case Nv40TclDepthControl:
                        DepthControl(param);
                        break;

                    case Nv40TclViewportClipHoriz0:
                    case Nv40TclViewportClipHoriz1:
                    case Nv40TclViewportClipHoriz2:
                    case Nv40TclViewportClipHoriz3:
                    case Nv40TclViewportClipHoriz4:
                    case Nv40TclViewportClipHoriz5:
                    case Nv40TclViewportClipHoriz6:
                    case Nv40TclViewportClipHoriz7:
                        ViewPortClipHorizontal(param);
                        break;

                    case Nv40TclVpUploadFromId:
                        VP_UploadFromID(param);
                        break;

                    case Nv40TclVpStartFromId:
                        VP_StartFromID(param);
                        break;

                    case Nv40TclVpUploadInst:
                        VP_UploadInstructions(param);
                        break;

                    case Nv40TclVpAttribEn:
                        VP_AttributeEnable(param);
                        break;

                    case Nv40TclVpResultEn:
                        VP_ResultEnable(param);
                        break;

                    case Nv40TclTransformTimeout:
                        TransformTimeout(param);
                        break;

                    case Nv40TclVpUploadConstId:
                        VP_UploadConstantID(param);
                        break;

                    case Nv40TclVpUploadConstX:
                        VP_UploadConstantX(param);
                        break;

                    case Nv40TclFpAddress:
                        FP_Address(param);
                        break;

                    case Nv40TclFpControl:
                        FP_Control(param);
                        break;

                    case Nv40TclClipPlaneEnable:
                        ClipPlaneEnable(param);
                        break;

                    case Nv40TclTexCoordControl:
                    case Nv40TclTexCoordControl + 4:
                        TexCoordControl(param);
                        break;

                    case Nv3062SetContextDmaImageDestin:
                        SetContextDMAImageDest(param);
                        break;

                    case Nv3062SetOffsetDestin:
                        SetOffsetDest(param);
                        break;

                    case Nv3062SetColorFormat:
                        SetColorFormat(param);
                        break;

                    case Nv308aPoint:
                        Point_Position(param);
                        break;

                    case Nv308aSizeOut:
                        SizeOut(param);
                        break;

                    case Nv308aColor:
                        Point_Color(param);
                        break;

                    case Nv40TclBlendEnable:
                        BlendEnable(param);
                        break;

                    case Nv40TclBlendFuncSrc:
                        BlendFunctionSRC(param);
                        break;

                    case Nv40TclBlendFuncDst:
                        BlendFunctionDST(param);
                        break;

                    case Nv40TclBlendColor:
                        BlendColor(param);
                        break;

                    case Nv40TclBlendEquation:
                        BlendEquation(param);
                        break;

                    case Nv40TclColorLogicOpEnable:
                        ColorLogicOPEnable(param);
                        break;

                    case Nv40TclColorLogicOp:
                        ColorLogicOP(param);
                        break;

                    case Nv40TclTexOffset0:
                    case Nv40TclTexOffset1:
                    case Nv40TclTexOffset2:
                    case Nv40TclTexOffset3:
                    case Nv40TclTexOffset4:
                    case Nv40TclTexOffset5:
                    case Nv40TclTexOffset6:
                    case Nv40TclTexOffset7:
                    case Nv40TclTexOffset8:
                    case Nv40TclTexOffset9:
                    case Nv40TclTexOffset10:
                    case Nv40TclTexOffset11:
                    case Nv40TclTexOffset12:
                    case Nv40TclTexOffset13:
                    case Nv40TclTexOffset14:
                    case Nv40TclTexOffset15:
                        TexOffset(methodRegister, param);
                        break;

                    case Nv40TclTexFormat0:
                    case Nv40TclTexFormat1:
                    case Nv40TclTexFormat2:
                    case Nv40TclTexFormat3:
                    case Nv40TclTexFormat4:
                    case Nv40TclTexFormat5:
                    case Nv40TclTexFormat6:
                    case Nv40TclTexFormat7:
                    case Nv40TclTexFormat8:
                    case Nv40TclTexFormat9:
                    case Nv40TclTexFormat10:
                    case Nv40TclTexFormat11:
                    case Nv40TclTexFormat12:
                    case Nv40TclTexFormat13:
                    case Nv40TclTexFormat14:
                    case Nv40TclTexFormat15:
                        TexFormat(methodRegister, param);
                        break;

                    case Nv40TclTexSwizzle0:
                    case Nv40TclTexSwizzle1:
                    case Nv40TclTexSwizzle2:
                    case Nv40TclTexSwizzle3:
                    case Nv40TclTexSwizzle4:
                    case Nv40TclTexSwizzle5:
                    case Nv40TclTexSwizzle6:
                    case Nv40TclTexSwizzle7:
                    case Nv40TclTexSwizzle8:
                    case Nv40TclTexSwizzle9:
                    case Nv40TclTexSwizzle10:
                    case Nv40TclTexSwizzle11:
                    case Nv40TclTexSwizzle12:
                    case Nv40TclTexSwizzle13:
                    case Nv40TclTexSwizzle14:
                    case Nv40TclTexSwizzle15:
                        TesSwizzle(methodRegister, param);
                        break;

                    case Nv40TclTexSize00:
                    case Nv40TclTexSize01:
                    case Nv40TclTexSize02:
                    case Nv40TclTexSize03:
                    case Nv40TclTexSize04:
                    case Nv40TclTexSize05:
                    case Nv40TclTexSize06:
                    case Nv40TclTexSize07:
                    case Nv40TclTexSize08:
                    case Nv40TclTexSize09:
                    case Nv40TclTexSize010:
                    case Nv40TclTexSize011:
                    case Nv40TclTexSize012:
                    case Nv40TclTexSize013:
                    case Nv40TclTexSize014:
                    case Nv40TclTexSize015:
                        TexSize0(methodRegister, param);
                        break;

                    case Nv40TclTexSize10:
                    case Nv40TclTexSize11:
                    case Nv40TclTexSize12:
                    case Nv40TclTexSize13:
                    case Nv40TclTexSize14:
                    case Nv40TclTexSize15:
                    case Nv40TclTexSize16:
                    case Nv40TclTexSize17:
                    case Nv40TclTexSize18:
                    case Nv40TclTexSize19:
                    case Nv40TclTexSize110:
                    case Nv40TclTexSize111:
                    case Nv40TclTexSize112:
                    case Nv40TclTexSize113:
                    case Nv40TclTexSize114:
                    case Nv40TclTexSize115:
                        TexSize1(methodRegister, param);
                        break;


                    case Nv40TclTexEnable0:
                    case Nv40TclTexEnable1:
                    case Nv40TclTexEnable2:
                    case Nv40TclTexEnable3:
                    case Nv40TclTexEnable4:
                    case Nv40TclTexEnable5:
                    case Nv40TclTexEnable6:
                    case Nv40TclTexEnable7:
                    case Nv40TclTexEnable8:
                    case Nv40TclTexEnable9:
                    case Nv40TclTexEnable10:
                    case Nv40TclTexEnable11:
                    case Nv40TclTexEnable12:
                    case Nv40TclTexEnable13:
                    case Nv40TclTexEnable14:
                    case Nv40TclTexEnable15:
                        TexEnable(methodRegister, param);
                        break;

                    case Nv40TclTexFilter0:
                    case Nv40TclTexFilter1:
                    case Nv40TclTexFilter2:
                    case Nv40TclTexFilter3:
                    case Nv40TclTexFilter4:
                    case Nv40TclTexFilter5:
                    case Nv40TclTexFilter6:
                    case Nv40TclTexFilter7:
                    case Nv40TclTexFilter8:
                    case Nv40TclTexFilter9:
                    case Nv40TclTexFilter10:
                    case Nv40TclTexFilter11:
                    case Nv40TclTexFilter12:
                    case Nv40TclTexFilter13:
                    case Nv40TclTexFilter14:
                    case Nv40TclTexFilter15:
                        TexFilter(methodRegister, param);
                        break;

                    case Nv40TclTexWrap0:
                    case Nv40TclTexWrap1:
                    case Nv40TclTexWrap2:
                    case Nv40TclTexWrap3:
                    case Nv40TclTexWrap4:
                    case Nv40TclTexWrap5:
                    case Nv40TclTexWrap6:
                    case Nv40TclTexWrap7:
                    case Nv40TclTexWrap8:
                    case Nv40TclTexWrap9:
                    case Nv40TclTexWrap10:
                    case Nv40TclTexWrap11:
                    case Nv40TclTexWrap12:
                    case Nv40TclTexWrap13:
                    case Nv40TclTexWrap14:
                    case Nv40TclTexWrap15:
                        TexWrap(methodRegister, param);
                        break;

                    case Nv40TclTexCacheCtl:
                        TexCacheCtl(param);
                        break;

                    case Nv40TclVtxfmt0:
                    case Nv40TclVtxfmt1:
                    case Nv40TclVtxfmt2:
                    case Nv40TclVtxfmt3:
                    case Nv40TclVtxfmt4:
                    case Nv40TclVtxfmt5:
                    case Nv40TclVtxfmt6:
                    case Nv40TclVtxfmt7:
                    case Nv40TclVtxfmt8:
                    case Nv40TclVtxfmt9:
                    case Nv40TclVtxfmt10:
                    case Nv40TclVtxfmt11:
                    case Nv40TclVtxfmt12:
                    case Nv40TclVtxfmt13:
                    case Nv40TclVtxfmt14:
                    case Nv40TclVtxfmt15:
                        VertexFormat(methodRegister, param);
                        break;

                    case Nv40TclVtxbufAddress0:
                    case Nv40TclVtxbufAddress1:
                    case Nv40TclVtxbufAddress2:
                    case Nv40TclVtxbufAddress3:
                    case Nv40TclVtxbufAddress4:
                    case Nv40TclVtxbufAddress5:
                    case Nv40TclVtxbufAddress6:
                    case Nv40TclVtxbufAddress7:
                    case Nv40TclVtxbufAddress8:
                    case Nv40TclVtxbufAddress9:
                    case Nv40TclVtxbufAddress10:
                    case Nv40TclVtxbufAddress11:
                    case Nv40TclVtxbufAddress12:
                    case Nv40TclVtxbufAddress13:
                    case Nv40TclVtxbufAddress14:
                    case Nv40TclVtxbufAddress15:
                        VertexBufferAddress(methodRegister, param);
                        break;

                    case Nv40TclVtxAttr2f0:
                    case Nv40TclVtxAttr2f1:
                    case Nv40TclVtxAttr2f2:
                    case Nv40TclVtxAttr2f3:
                    case Nv40TclVtxAttr2f4:
                    case Nv40TclVtxAttr2f5:
                    case Nv40TclVtxAttr2f6:
                    case Nv40TclVtxAttr2f7:
                    case Nv40TclVtxAttr2f8:
                    case Nv40TclVtxAttr2f9:
                    case Nv40TclVtxAttr2f10:
                    case Nv40TclVtxAttr2f11:
                    case Nv40TclVtxAttr2f12:
                    case Nv40TclVtxAttr2f13:
                    case Nv40TclVtxAttr2f14:
                        VertexImmediate2F(methodRegister, param);
                        break;

                    case Nv40TclVtxAttr4f0:
                    case Nv40TclVtxAttr4f1:
                    case Nv40TclVtxAttr4f2:
                    case Nv40TclVtxAttr4f3:
                    case Nv40TclVtxAttr4f4:
                    case Nv40TclVtxAttr4f5:
                    case Nv40TclVtxAttr4f6:
                    case Nv40TclVtxAttr4f7:
                    case Nv40TclVtxAttr4f8:
                    case Nv40TclVtxAttr4f9:
                    case Nv40TclVtxAttr4f10:
                    case Nv40TclVtxAttr4f11:
                    case Nv40TclVtxAttr4f12:
                    case Nv40TclVtxAttr4f13:
                    case Nv40TclVtxAttr4f14:
                        VertexImmediate4F(methodRegister, param);
                        break;

                    case Nv40TclVbIndexBatchOffset:
                        IndexBatchOffset(param);
                        break;

                    case Nv40TclBeginEnd:
                        BeginEnd(param);
                        break;

                    case Nv40TclVbIndexBatchDraw:
                        IndexBatchDraw(param);
                        break;

                    case Nv40TclVbVertexBatch:
                        VertexBatchDraw(param);
                        break;

                    case Nv40TclDividerFrequencyOp:
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

            if (!_cellGcmSys.FlipHandler.IsNullPtr)
            {
                PPUInterpreter.ExecuteFunction(_RSX_PPU_Thread, _cellGcmSys.FlipHandler, toc: true, [1, 0, 0, 0, 0]);
            }

            _memoryManager.Write<BigEndian<ulong>>(LabelsPtr + 0x10, 0);
            _memoryManager.Write<BigEndian<ulong>>(LabelsPtr + 0x10 + 0x8, 0);

            //Console.WriteLine($"[RSX] Flipped");

            LastFlipTime = Utility.ReadTime_Microseconds() * 8;
            Interlocked.Exchange<ulong>(ref _cellGcmSys.FlipStatus, 0);
        }

        private void Vblank()
        {
            if (!_cellGcmSys.VblankHandler.IsNullPtr)
            {
                PPUInterpreter.ExecuteFunction(_RSX_PPU_Thread, _cellGcmSys.VblankHandler, toc: true, [1, 0, 0, 0, 0]);
            }
        }

        private void SetRef(Queue<BigEndian<uint>> param)
        {
            uint ref_value = param.Dequeue();

            CellGcmCtrlRegister ctrl = _memoryManager.Read<CellGcmCtrlRegister>(CtrlPtr);
            ctrl.Ref = ref_value;
            _memoryManager.Write<CellGcmCtrlRegister>(CtrlPtr, ctrl);
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
            _memoryManager.Write<BigEndian<uint>>(LabelsPtr + CurrentState.CurrentSemaphoreOffset, value);
            //Console.WriteLine($"[RSX] SemaphoreBackendwriteRelease [{CurrentState.CurrentSemaphoreOffset}] = 0x{value:X8}");
        }

        private void SemaphoreAcquire(Queue<BigEndian<uint>> param)
        {
            BigEndian<uint> arg = param.Dequeue();

            // This command makes RSX waits until semaphore value == argument
            // We just compare the values as big endian instead of reversing every time

            GuestPtr<BigEndian<uint>> ptr = LabelsPtr + CurrentState.CurrentSemaphoreOffset;

            //while (Volatile.Read(ref be_val) != arg.RawBE) {
            //    Thread.Yield();
            //}
        }

        private void SemaphoreRelease(Queue<BigEndian<uint>> param)
        {
            uint value = param.Dequeue();
            _memoryManager.Write<BigEndian<uint>>(LabelsPtr + CurrentState.CurrentSemaphoreOffset, value);
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
            bool r = (colorMask & GcmColorMaskR) > 0;
            bool g = (colorMask & GcmColorMaskG) > 0;
            bool b = (colorMask & GcmColorMaskB) > 0;
            bool a = (colorMask & GcmColorMaskA) > 0;

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
            int index = (int)((methodRegister - Nv40TclViewportOffsetX) >> 2);
            uint raw = param.Dequeue();
            float value = Unsafe.BitCast<uint, float>(raw);

            if (CurrentState.ViewPortOffset[index] != value)
            {
                Vector4 viewportOffset = CurrentState.ViewPortOffset;
                viewportOffset[index] = value;
                CurrentState.ViewPortOffset = viewportOffset;
                CurrentState.ViewPortOffsetDirty = true;
            }
        }

        private void ViewPortScale(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            //TODO
            int index = (int)((methodRegister - Nv40TclViewportScaleX) >> 2);
            uint raw = param.Dequeue();
            float value = Unsafe.BitCast<uint, float>(raw);

            if (CurrentState.ViewPortScale[index] != value)
            {
                Vector4 viewporScale = CurrentState.ViewPortScale;
                viewporScale[index] = value;
                CurrentState.ViewPortScale = viewporScale;
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
            if (methodRegister == Nv40TclDmaColor0)
            {
                CurrentState.SurfaceALocation = param.Dequeue();
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
            CurrentState.SurfaceAOffset = param.Dequeue();

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
                Vector2i surfaceClip = CurrentState.SurfaceClip;
                surfaceClip[0] = width;
                CurrentState.SurfaceClip = surfaceClip;
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
                Vector2i surfaceClip = CurrentState.SurfaceClip;
                surfaceClip[1] = height;
                CurrentState.SurfaceClip = surfaceClip;
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
            _vertexShaderGenerator.VertexShaderLoadIndex = loadIndex;
        }

        private void VP_StartFromID(Queue<BigEndian<uint>> param)
        {
            uint startIndex = param.Dequeue() * 4; // Index of the first instruction in the program
            _vertexShaderGenerator.VertexShaderShaderStart = startIndex;
        }

        private void VP_UploadInstructions(Queue<BigEndian<uint>> param)
        {
            //Console.WriteLine($"RSX NV40TCL_VP_UPLOAD_INST param_count = {param.Count}");
            _vertexShaderGenerator.UploadInstructions(param);
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
            _fragmentShaderGenerator.FragmentShaderStartAddress = address;
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
            uint location = (methodRegister - Nv40TclTexOffset0) >> 5;
            uint offset = param.Dequeue();

            CurrentState.Textures[location].Offset = offset;
            //Console.WriteLine($"RSX NV40TCL_TEX_OFFSET: tex[{location}] offset = 0x{offset:X8}");
        }

        private void TexFormat(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - Nv40TclTexFormat0) >> 5;
            uint format = param.Dequeue();

            // Texture format contains a bunch of stuff
            uint memLocation = (format & 0x3) - 1;
            CurrentState.Textures[location].MemoryLocation = memLocation;
            CurrentState.Textures[location].Format = format;
        }

        private void TesSwizzle(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - Nv40TclTexSwizzle0) >> 5;
            uint swzzle = param.Dequeue();
            CurrentState.Textures[location].SwizzleValue = swzzle;
        }

        private void TexSize0(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - Nv40TclTexSize00) >> 5;
            uint size = param.Dequeue();
            uint height = size & 0xFFFF;
            uint width = (size >> 16) & 0xFFFF;
            CurrentState.Textures[location].Height = height;
            CurrentState.Textures[location].Width = width;
        }

        private void TexSize1(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - Nv40TclTexSize10) >> 2;
            uint pitch_depth = param.Dequeue();
            uint pitch = pitch_depth & 0xFFFF;
            uint depth = (pitch_depth >> 20) & 0xFFF;
            CurrentState.Textures[location].Pitch = pitch;
            CurrentState.Textures[location].Depth = depth;
        }

        private void TexEnable(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - Nv40TclTexEnable0) >> 5;
            uint value = param.Dequeue();
            bool isEnabled = (value >> 31) == 1;
            CurrentState.Textures[location].IsEnabled = isEnabled;
            // Console.WriteLine($"RSX NV40TCL_TEX_ENABLE {(isEnabled? "enabled" : "disabled")} tex: {location} address = 0x{CurrentState.TextureAttributes[location].Address:X8}");
        }


        private void TexFilter(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - Nv40TclTexFilter0) >> 5;
            uint filter = param.Dequeue();
            CurrentState.Textures[location].Filter = filter;
        }

        private void TexWrap(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - Nv40TclTexWrap0) >> 5;
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

            uint location = (methodRegister - Nv40TclVtxfmt0) >> 2;

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
            uint location = (methodRegister - Nv40TclVtxbufAddress0) >> 2;
            uint offset = param.Dequeue();
            uint isEA = offset >> 31;
            CurrentState.VertexBuffers[location].BufferAddress = OffsetToAddress(offset & 0x7FFFFFFF, isEA);

            //Console.WriteLine($"RSX NV40TCL_VTXBUF_ADDRESS location = {location} - offset = 0x{offset:X8} address = 0x{CurrentState.VertexAttributes[location].BufferAddress:X}");
        }

        private void VertexImmediate4F(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - Nv40TclVtxAttr4f0) >> 4;

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

        private void VertexImmediate2F(uint methodRegister, Queue<BigEndian<uint>> param)
        {
            uint location = (methodRegister - Nv40TclVtxAttr2f0) >> 3;

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
                    CurrentState.BeginMode = RSXInternalState.BeginModeLookup[(int)(mode - 1)];
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
