namespace PS3Sharp.Cell.Rsx
{
    internal struct RsxCommand
    {

        //Constant masks/shifts based on PSL1GHT
        internal const uint RsxMethodFlagNoIncrement = 0x40000000;
        internal const uint RsxMethodFlagJump = 0x20000000;
        internal const uint RsxMethodFlagCall = 0x00000002;
        internal const uint RsxMethodFlagReturn = 0x00020000;
        internal const uint RsxMaxMethodCount = 0x000007FF;
        internal const int RsxMethodCountShift = 18;
        internal const int RsxSubchannelShift = 13;

        // Commands values:

        internal const uint Nv40TclNop = 0x00000100;
        internal const uint Nv40TclVtxCacheInvalidate = 0x00001714;
        internal const uint Nv40TclVtxCacheInvalidate2 = 0x00001710;

        internal const uint GcmFlipCommand = 0x0000FEAC;

        internal const uint Nv406eTclSetRef = 0x00000050;
        internal const uint Nv406eTclSetCtxDmaSemaphore = 0x00000060;
        internal const uint Nv406eTclSemaphoreOffset = 0x00000064;
        internal const uint Nv406eTclSemaphoreAcquire = 0x00000068;
        internal const uint Nv406eTclSemaphoreRelease = 0x0000006c;

        internal const uint Nv40TclSemaphoreBackendwriteOffset = 0x00001D6C; //it's named NV40TCL_SEMAPHORE_OFFSET	in PSL1GHT for some reason
        internal const uint Nv40TclSemaphoreBackendwriteRelease = 0x00001D70;
        internal const uint Nv4097TextureReadSemaphoreRelease = 0x00001D74;

        internal const uint Nv34TclPrimitive3dFogColor = 0x00000370;

        internal const uint Nv34TclPrimitive3dColorMask = 0x00000324;
        internal const uint GcmColorMaskB = 0x00000001;
        internal const uint GcmColorMaskG = 0x00000100;
        internal const uint GcmColorMaskR = 0x00010000;
        internal const uint GcmColorMaskA = 0x01000000;

        internal const uint Nv40TclViewportHoriz = 0x00000A00;
        internal const uint Nv40TclViewportVert = 0x00000A04;
        internal const uint Nv40TclDepthRange = 0x00000394;

        internal const uint Nv40TclViewportOffsetX = 0x00000A20 + 0;
        internal const uint Nv40TclViewportOffsetY = 0x00000A20 + 4;
        internal const uint Nv40TclViewportOffsetZ = 0x00000A20 + 8;
        internal const uint Nv40TclViewportOffsetW = 0x00000A20 + 12;

        internal const uint Nv40TclViewportScaleX = 0x00000A30 + 0;
        internal const uint Nv40TclViewportScaleY = 0x00000A30 + 4;
        internal const uint Nv40TclViewportScaleZ = 0x00000A30 + 8;
        internal const uint Nv40TclViewportScaleW = 0x00000A30 + 12;

        internal const uint Nv40TclScissorHoriz = 0x000008C0;
        internal const uint Nv40TclScissorVert = 0x000008C4;
        internal const uint Nv40TclDepthTestEnable = 0x00000A74;
        internal const uint Nv40TclDepthFunc = 0x00000A6C;
        internal const uint Nv40TclShadeModel = 0x00000368;
        internal const uint Nv40TclDepthWriteEnable = 0x00000A70;
        internal const uint Nv40TclFrontFace = 0x00001834;

        internal const uint Nv34TclPrimitive3dDmaInMemory0 = 0x00000184;
        internal const uint Nv34TclPrimitive3dDmaInMemory1 = 0x00000188;
        // internal const uint NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY2 = 0x0000018C;
        //internal const uint NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY3 = 0x00000194;
        internal const uint Nv34TclPrimitive3dDmaInMemory4 = 0x00000198;
        internal const uint Nv34TclPrimitive3dDmaInMemory5 = 0x000001A4;
        internal const uint Nv34TclPrimitive3dDmaInMemory6 = 0x000001A8;
        internal const uint Nv34TclPrimitive3dDmaInMemory7 = 0x000001AC;
        internal const uint Nv34TclPrimitive3dDmaInMemory8 = 0x000001B0;

        internal const uint Nv40TclDmaColor0 = 0x00000194;
        internal const uint Nv40TclDmaColor1 = 0x0000018C;
        internal const uint Nv40TclDmaColor2 = 0x000001B4;
        internal const uint Nv40TclDmaColor3 = 0x000001B8;

        internal const uint Nv40TclClearValueColor = 0x00001D90;
        internal const uint Nv40TclClearValueDepth = 0x00001D8C;
        internal const uint Nv40TclClearBuffers = 0x00001D94;

        internal const uint Nv40TclShaderWindow = 0x00001D88;

        internal const uint Nv40TclRtHoriz = 0x00000200;
        internal const uint Nv40TclRtVert = 0x00000204;
        internal const uint Nv40TclRtFormat = 0x00000208;
        internal const uint Nv40TclRtEnable = 0x00000220;
        internal const uint Nv40TclZetaPitch = 0x0000022c;
        internal const uint Nv40TclWindowOffset = 0x000002b8;

        internal const uint Nv40TclColor2Pitch = 0x00000280;
        internal const uint Nv40TclColor3Pitch = 0x00000284;

        internal const uint Nv40TclDepthControl = 0x00001D78;

        internal const uint Nv40TclViewportClipHoriz0 = 0x000002C0; // +0
        internal const uint Nv40TclViewportClipHoriz1 = 0x000002C8; // +8
        internal const uint Nv40TclViewportClipHoriz2 = 0x000002D0; // +16
        internal const uint Nv40TclViewportClipHoriz3 = 0x000002D8; // +24
        internal const uint Nv40TclViewportClipHoriz4 = 0x000002E0; // +32
        internal const uint Nv40TclViewportClipHoriz5 = 0x000002E8; // +40
        internal const uint Nv40TclViewportClipHoriz6 = 0x000002F0; // +48
        internal const uint Nv40TclViewportClipHoriz7 = 0x000002F8; // +56
        //internal const uint NV40TCL_VIEWPORT_CLIP_HORIZ_8 = 0x00000300; // +64

        internal const uint Nv40TclVpUploadFromId = 0x00001E9c;
        internal const uint Nv40TclVpStartFromId = 0x00001EA0;
        internal const uint Nv40TclVpUploadInst = 0x00000B80;
        internal const uint Nv40TclVpAttribEn = 0x00001FF0;
        internal const uint Nv40TclVpResultEn = 0x00001FF4;
        internal const uint Nv40TclTransformTimeout = 0x00001EF8;
        internal const uint Nv40TclVpUploadConstId = 0x00001EFC;
        internal const uint Nv40TclVpUploadConstX = 0x00001F00;

        internal const uint Nv40TclFpAddress = 0x000008E4;
        internal const uint Nv40TclFpControl = 0X00001D60;
        internal const uint Nv40TclClipPlaneEnable = 0x00001478;

        internal const uint Nv40TclVtxfmt0 = 0x00001740;
        internal const uint Nv40TclVtxfmt1 = 0x00001740 + (1 * 4);
        internal const uint Nv40TclVtxfmt2 = 0x00001740 + (2 * 4);
        internal const uint Nv40TclVtxfmt3 = 0x00001740 + (3 * 4);
        internal const uint Nv40TclVtxfmt4 = 0x00001740 + (4 * 4);
        internal const uint Nv40TclVtxfmt5 = 0x00001740 + (5 * 4);
        internal const uint Nv40TclVtxfmt6 = 0x00001740 + (6 * 4);
        internal const uint Nv40TclVtxfmt7 = 0x00001740 + (7 * 4);
        internal const uint Nv40TclVtxfmt8 = 0x00001740 + (8 * 4);
        internal const uint Nv40TclVtxfmt9 = 0x00001740 + (9 * 4);
        internal const uint Nv40TclVtxfmt10 = 0x00001740 + (10 * 4);
        internal const uint Nv40TclVtxfmt11 = 0x00001740 + (11 * 4);
        internal const uint Nv40TclVtxfmt12 = 0x00001740 + (12 * 4);
        internal const uint Nv40TclVtxfmt13 = 0x00001740 + (13 * 4);
        internal const uint Nv40TclVtxfmt14 = 0x00001740 + (14 * 4);
        internal const uint Nv40TclVtxfmt15 = 0x00001740 + (15 * 4);

        internal const uint Nv40TclVtxbufAddress0 = 0x00001680;
        internal const uint Nv40TclVtxbufAddress1 = 0x00001680 + (1 * 4);
        internal const uint Nv40TclVtxbufAddress2 = 0x00001680 + (2 * 4);
        internal const uint Nv40TclVtxbufAddress3 = 0x00001680 + (3 * 4);
        internal const uint Nv40TclVtxbufAddress4 = 0x00001680 + (4 * 4);
        internal const uint Nv40TclVtxbufAddress5 = 0x00001680 + (5 * 4);
        internal const uint Nv40TclVtxbufAddress6 = 0x00001680 + (6 * 4);
        internal const uint Nv40TclVtxbufAddress7 = 0x00001680 + (7 * 4);
        internal const uint Nv40TclVtxbufAddress8 = 0x00001680 + (8 * 4);
        internal const uint Nv40TclVtxbufAddress9 = 0x00001680 + (9 * 4);
        internal const uint Nv40TclVtxbufAddress10 = 0x00001680 + (10 * 4);
        internal const uint Nv40TclVtxbufAddress11 = 0x00001680 + (11 * 4);
        internal const uint Nv40TclVtxbufAddress12 = 0x00001680 + (12 * 4);
        internal const uint Nv40TclVtxbufAddress13 = 0x00001680 + (13 * 4);
        internal const uint Nv40TclVtxbufAddress14 = 0x00001680 + (14 * 4);
        internal const uint Nv40TclVtxbufAddress15 = 0x00001680 + (15 * 4);

        internal const uint Nv40TclVbIndexBatchOffset = 0x0000181C;

        internal const uint Nv40TclBeginEnd = 0x00001808;
        internal const uint Nv40TclVbIndexBatchDraw = 0x00001824;
        internal const uint Nv40TclVbVertexBatch = 0x00001814;

        internal const uint Nv40TclTexCoordControl = 0x00000B40;

        internal const uint Nv3062SetObject = 0x00006000;
        internal const uint Nv3062SetContextDmaNotifies = 0x00006180;
        internal const uint Nv3062SetContextDmaImageSource = 0x00006184;
        internal const uint Nv3062SetContextDmaImageDestin = 0x00006188;
        internal const uint Nv3062SetColorFormat = 0x00006300;
        internal const uint Nv3062SetPitch = 0x00006304;
        internal const uint Nv3062SetOffsetSource = 0x00006308;
        internal const uint Nv3062SetOffsetDestin = 0x0000630C;

        internal const uint Nv308aPoint = 0x0000A304;
        internal const uint Nv308aSizeOut = 0x0000A308;
        internal const uint Nv308aColor = 0x0000A400;

        internal const uint Nv40TclBlendEnable = 0x00000310;
        internal const uint Nv40TclBlendFuncSrc = 0x00000314;
        internal const uint Nv40TclBlendFuncDst = 0x00000318;
        internal const uint Nv40TclBlendColor = 0x0000031C;
        internal const uint Nv40TclBlendEquation = 0x00000320;
        internal const uint Nv40TclColorLogicOpEnable = 0x00000374;
        internal const uint Nv40TclColorLogicOp = 0x00000378;


        // In PSL1GHT TEX_OFFSET and TEX_FORMAT are considered one command with 2 parameters
        // But we need to seperate them
        internal const uint Nv40TclTexOffset0 = 0x00001A00 + 32 * 0;
        internal const uint Nv40TclTexOffset1 = 0x00001A00 + 32 * 1;
        internal const uint Nv40TclTexOffset2 = 0x00001A00 + 32 * 2;
        internal const uint Nv40TclTexOffset3 = 0x00001A00 + 32 * 3;
        internal const uint Nv40TclTexOffset4 = 0x00001A00 + 32 * 4;
        internal const uint Nv40TclTexOffset5 = 0x00001A00 + 32 * 5;
        internal const uint Nv40TclTexOffset6 = 0x00001A00 + 32 * 6;
        internal const uint Nv40TclTexOffset7 = 0x00001A00 + 32 * 7;
        internal const uint Nv40TclTexOffset8 = 0x00001A00 + 32 * 8;
        internal const uint Nv40TclTexOffset9 = 0x00001A00 + 32 * 9;
        internal const uint Nv40TclTexOffset10 = 0x00001A00 + 32 * 10;
        internal const uint Nv40TclTexOffset11 = 0x00001A00 + 32 * 11;
        internal const uint Nv40TclTexOffset12 = 0x00001A00 + 32 * 12;
        internal const uint Nv40TclTexOffset13 = 0x00001A00 + 32 * 13;
        internal const uint Nv40TclTexOffset14 = 0x00001A00 + 32 * 14;
        internal const uint Nv40TclTexOffset15 = 0x00001A00 + 32 * 15;

        internal const uint Nv40TclTexFormat0 = 0x00001A04 + 32 * 0;
        internal const uint Nv40TclTexFormat1 = 0x00001A04 + 32 * 1;
        internal const uint Nv40TclTexFormat2 = 0x00001A04 + 32 * 2;
        internal const uint Nv40TclTexFormat3 = 0x00001A04 + 32 * 3;
        internal const uint Nv40TclTexFormat4 = 0x00001A04 + 32 * 4;
        internal const uint Nv40TclTexFormat5 = 0x00001A04 + 32 * 5;
        internal const uint Nv40TclTexFormat6 = 0x00001A04 + 32 * 6;
        internal const uint Nv40TclTexFormat7 = 0x00001A04 + 32 * 7;
        internal const uint Nv40TclTexFormat8 = 0x00001A04 + 32 * 8;
        internal const uint Nv40TclTexFormat9 = 0x00001A04 + 32 * 9;
        internal const uint Nv40TclTexFormat10 = 0x00001A04 + 32 * 10;
        internal const uint Nv40TclTexFormat11 = 0x00001A04 + 32 * 11;
        internal const uint Nv40TclTexFormat12 = 0x00001A04 + 32 * 12;
        internal const uint Nv40TclTexFormat13 = 0x00001A04 + 32 * 13;
        internal const uint Nv40TclTexFormat14 = 0x00001A04 + 32 * 14;
        internal const uint Nv40TclTexFormat15 = 0x00001A04 + 32 * 15;

        internal const uint Nv40TclTexWrap0 = 0x00001A08 + 32 * 0;
        internal const uint Nv40TclTexWrap1 = 0x00001A08 + 32 * 1;
        internal const uint Nv40TclTexWrap2 = 0x00001A08 + 32 * 2;
        internal const uint Nv40TclTexWrap3 = 0x00001A08 + 32 * 3;
        internal const uint Nv40TclTexWrap4 = 0x00001A08 + 32 * 4;
        internal const uint Nv40TclTexWrap5 = 0x00001A08 + 32 * 5;
        internal const uint Nv40TclTexWrap6 = 0x00001A08 + 32 * 6;
        internal const uint Nv40TclTexWrap7 = 0x00001A08 + 32 * 7;
        internal const uint Nv40TclTexWrap8 = 0x00001A08 + 32 * 8;
        internal const uint Nv40TclTexWrap9 = 0x00001A08 + 32 * 9;
        internal const uint Nv40TclTexWrap10 = 0x00001A08 + 32 * 10;
        internal const uint Nv40TclTexWrap11 = 0x00001A08 + 32 * 11;
        internal const uint Nv40TclTexWrap12 = 0x00001A08 + 32 * 12;
        internal const uint Nv40TclTexWrap13 = 0x00001A08 + 32 * 13;
        internal const uint Nv40TclTexWrap14 = 0x00001A08 + 32 * 14;
        internal const uint Nv40TclTexWrap15 = 0x00001A08 + 32 * 15;

        // Control 0
        internal const uint Nv40TclTexEnable0 = 0x00001A0C + 32 * 0;
        internal const uint Nv40TclTexEnable1 = 0x00001A0C + 32 * 1;
        internal const uint Nv40TclTexEnable2 = 0x00001A0C + 32 * 2;
        internal const uint Nv40TclTexEnable3 = 0x00001A0C + 32 * 3;
        internal const uint Nv40TclTexEnable4 = 0x00001A0C + 32 * 4;
        internal const uint Nv40TclTexEnable5 = 0x00001A0C + 32 * 5;
        internal const uint Nv40TclTexEnable6 = 0x00001A0C + 32 * 6;
        internal const uint Nv40TclTexEnable7 = 0x00001A0C + 32 * 7;
        internal const uint Nv40TclTexEnable8 = 0x00001A0C + 32 * 8;
        internal const uint Nv40TclTexEnable9 = 0x00001A0C + 32 * 9;
        internal const uint Nv40TclTexEnable10 = 0x00001A0C + 32 * 10;
        internal const uint Nv40TclTexEnable11 = 0x00001A0C + 32 * 11;
        internal const uint Nv40TclTexEnable12 = 0x00001A0C + 32 * 12;
        internal const uint Nv40TclTexEnable13 = 0x00001A0C + 32 * 13;
        internal const uint Nv40TclTexEnable14 = 0x00001A0C + 32 * 14;
        internal const uint Nv40TclTexEnable15 = 0x00001A0C + 32 * 15;

        // Control 1
        internal const uint Nv40TclTexSwizzle0 = 0x00001A10 + 32 * 0;
        internal const uint Nv40TclTexSwizzle1 = 0x00001A10 + 32 * 1;
        internal const uint Nv40TclTexSwizzle2 = 0x00001A10 + 32 * 2;
        internal const uint Nv40TclTexSwizzle3 = 0x00001A10 + 32 * 3;
        internal const uint Nv40TclTexSwizzle4 = 0x00001A10 + 32 * 4;
        internal const uint Nv40TclTexSwizzle5 = 0x00001A10 + 32 * 5;
        internal const uint Nv40TclTexSwizzle6 = 0x00001A10 + 32 * 6;
        internal const uint Nv40TclTexSwizzle7 = 0x00001A10 + 32 * 7;
        internal const uint Nv40TclTexSwizzle8 = 0x00001A10 + 32 * 8;
        internal const uint Nv40TclTexSwizzle9 = 0x00001A10 + 32 * 9;
        internal const uint Nv40TclTexSwizzle10 = 0x00001A10 + 32 * 10;
        internal const uint Nv40TclTexSwizzle11 = 0x00001A10 + 32 * 11;
        internal const uint Nv40TclTexSwizzle12 = 0x00001A10 + 32 * 12;
        internal const uint Nv40TclTexSwizzle13 = 0x00001A10 + 32 * 13;
        internal const uint Nv40TclTexSwizzle14 = 0x00001A10 + 32 * 14;
        internal const uint Nv40TclTexSwizzle15 = 0x00001A10 + 32 * 15;

        internal const uint Nv40TclTexFilter0 = 0x00001A14 + 32 * 0;
        internal const uint Nv40TclTexFilter1 = 0x00001A14 + 32 * 1;
        internal const uint Nv40TclTexFilter2 = 0x00001A14 + 32 * 2;
        internal const uint Nv40TclTexFilter3 = 0x00001A14 + 32 * 3;
        internal const uint Nv40TclTexFilter4 = 0x00001A14 + 32 * 4;
        internal const uint Nv40TclTexFilter5 = 0x00001A14 + 32 * 5;
        internal const uint Nv40TclTexFilter6 = 0x00001A14 + 32 * 6;
        internal const uint Nv40TclTexFilter7 = 0x00001A14 + 32 * 7;
        internal const uint Nv40TclTexFilter8 = 0x00001A14 + 32 * 8;
        internal const uint Nv40TclTexFilter9 = 0x00001A14 + 32 * 9;
        internal const uint Nv40TclTexFilter10 = 0x00001A14 + 32 * 10;
        internal const uint Nv40TclTexFilter11 = 0x00001A14 + 32 * 11;
        internal const uint Nv40TclTexFilter12 = 0x00001A14 + 32 * 12;
        internal const uint Nv40TclTexFilter13 = 0x00001A14 + 32 * 13;
        internal const uint Nv40TclTexFilter14 = 0x00001A14 + 32 * 14;
        internal const uint Nv40TclTexFilter15 = 0x00001A14 + 32 * 15;

        internal const uint Nv40TclTexSize00 = 0x00001A18 + 32 * 0;
        internal const uint Nv40TclTexSize01 = 0x00001A18 + 32 * 1;
        internal const uint Nv40TclTexSize02 = 0x00001A18 + 32 * 2;
        internal const uint Nv40TclTexSize03 = 0x00001A18 + 32 * 3;
        internal const uint Nv40TclTexSize04 = 0x00001A18 + 32 * 4;
        internal const uint Nv40TclTexSize05 = 0x00001A18 + 32 * 5;
        internal const uint Nv40TclTexSize06 = 0x00001A18 + 32 * 6;
        internal const uint Nv40TclTexSize07 = 0x00001A18 + 32 * 7;
        internal const uint Nv40TclTexSize08 = 0x00001A18 + 32 * 8;
        internal const uint Nv40TclTexSize09 = 0x00001A18 + 32 * 9;
        internal const uint Nv40TclTexSize010 = 0x00001A18 + 32 * 10;
        internal const uint Nv40TclTexSize011 = 0x00001A18 + 32 * 11;
        internal const uint Nv40TclTexSize012 = 0x00001A18 + 32 * 12;
        internal const uint Nv40TclTexSize013 = 0x00001A18 + 32 * 13;
        internal const uint Nv40TclTexSize014 = 0x00001A18 + 32 * 14;
        internal const uint Nv40TclTexSize015 = 0x00001A18 + 32 * 15;

        internal const uint Nv40TclTexSize10 = 0x00001840 + 4 * 0;
        internal const uint Nv40TclTexSize11 = 0x00001840 + 4 * 1;
        internal const uint Nv40TclTexSize12 = 0x00001840 + 4 * 2;
        internal const uint Nv40TclTexSize13 = 0x00001840 + 4 * 3;
        internal const uint Nv40TclTexSize14 = 0x00001840 + 4 * 4;
        internal const uint Nv40TclTexSize15 = 0x00001840 + 4 * 5;
        internal const uint Nv40TclTexSize16 = 0x00001840 + 4 * 6;
        internal const uint Nv40TclTexSize17 = 0x00001840 + 4 * 7;
        internal const uint Nv40TclTexSize18 = 0x00001840 + 4 * 8;
        internal const uint Nv40TclTexSize19 = 0x00001840 + 4 * 9;
        internal const uint Nv40TclTexSize110 = 0x00001840 + 4 * 10;
        internal const uint Nv40TclTexSize111 = 0x00001840 + 4 * 11;
        internal const uint Nv40TclTexSize112 = 0x00001840 + 4 * 12;
        internal const uint Nv40TclTexSize113 = 0x00001840 + 4 * 13;
        internal const uint Nv40TclTexSize114 = 0x00001840 + 4 * 14;
        internal const uint Nv40TclTexSize115 = 0x00001840 + 4 * 15;


        // Immediate vertex data - vec2
        internal const uint Nv40TclVtxAttr2f0 = 0x00001880 + 0 * 8;
        internal const uint Nv40TclVtxAttr2f1 = 0x00001880 + 1 * 8;
        internal const uint Nv40TclVtxAttr2f2 = 0x00001880 + 2 * 8;
        internal const uint Nv40TclVtxAttr2f3 = 0x00001880 + 3 * 8;
        internal const uint Nv40TclVtxAttr2f4 = 0x00001880 + 4 * 8;
        internal const uint Nv40TclVtxAttr2f5 = 0x00001880 + 5 * 8;
        internal const uint Nv40TclVtxAttr2f6 = 0x00001880 + 6 * 8;
        internal const uint Nv40TclVtxAttr2f7 = 0x00001880 + 7 * 8;
        internal const uint Nv40TclVtxAttr2f8 = 0x00001880 + 8 * 8;
        internal const uint Nv40TclVtxAttr2f9 = 0x00001880 + 9 * 8;
        internal const uint Nv40TclVtxAttr2f10 = 0x00001880 + 10 * 8;
        internal const uint Nv40TclVtxAttr2f11 = 0x00001880 + 11 * 8;
        internal const uint Nv40TclVtxAttr2f12 = 0x00001880 + 12 * 8;
        internal const uint Nv40TclVtxAttr2f13 = 0x00001880 + 13 * 8;
        internal const uint Nv40TclVtxAttr2f14 = 0x00001880 + 14 * 8;

        // Immediate vertex data - vec4
        internal const uint Nv40TclVtxAttr4f0 = 0x00001C00 + 0 * 16;
        internal const uint Nv40TclVtxAttr4f1 = 0x00001C00 + 1 * 16;
        internal const uint Nv40TclVtxAttr4f2 = 0x00001C00 + 2 * 16;
        internal const uint Nv40TclVtxAttr4f3 = 0x00001C00 + 3 * 16;
        internal const uint Nv40TclVtxAttr4f4 = 0x00001C00 + 4 * 16;
        internal const uint Nv40TclVtxAttr4f5 = 0x00001C00 + 5 * 16;
        internal const uint Nv40TclVtxAttr4f6 = 0x00001C00 + 6 * 16;
        internal const uint Nv40TclVtxAttr4f7 = 0x00001C00 + 7 * 16;
        internal const uint Nv40TclVtxAttr4f8 = 0x00001C00 + 8 * 16;
        internal const uint Nv40TclVtxAttr4f9 = 0x00001C00 + 9 * 16;
        internal const uint Nv40TclVtxAttr4f10 = 0x00001C00 + 10 * 16;
        internal const uint Nv40TclVtxAttr4f11 = 0x00001C00 + 11 * 16;
        internal const uint Nv40TclVtxAttr4f12 = 0x00001C00 + 12 * 16;
        internal const uint Nv40TclVtxAttr4f13 = 0x00001C00 + 13 * 16;
        internal const uint Nv40TclVtxAttr4f14 = 0x00001C00 + 14 * 16;


        internal const uint Nv40TclTexCacheCtl = 0x00001FD8;

        internal const uint Nv40TclTexFormatFormatL8 = 0x00000100;
        internal const uint Nv40TclTexFormatFormatA1r5g5b5 = 0x00000200;
        internal const uint Nv40TclTexFormatFormatA4r4g4b4 = 0x00000300;
        internal const uint Nv40TclTexFormatFormatR5g6b5 = 0x00000400;
        internal const uint Nv40TclTexFormatFormatA8r8g8b8 = 0x00000500;
        internal const uint Nv40TclTexFormatFormatDxt1 = 0x00000600;
        internal const uint Nv40TclTexFormatFormatDxt3 = 0x00000700;
        internal const uint Nv40TclTexFormatFormatDxt5 = 0x00000800;
        internal const uint Nv40TclTexFormatFormatG8b8 = 0x00000B00;
        internal const uint Nv40TclTexFormatFormatA8l8 = 0x00001800;

        internal const uint Nv40TclPolygonOffsetFillEnable = 0x00000A68;
        internal const uint Nv40TclPolygonOffsetFactor = 0x00000A78;
        internal const uint Nv40TclCullFace = 0x00001830;
        internal const uint Nv40TclCullFaceEnable = 0x0000183C;
        internal const uint Nv40TclStencilBackEnable = 0x00000328;
        internal const uint Nv40TclPolygonModeFront = 0x00001828;
        internal const uint Nv40TclDepthBoundsMin = 0x00000384;
        internal const uint Nv40TclDepthBoundsTestEnable = 0x00000380;
        internal const uint Nv40TclAntiAliasingControl = 0x00001D7C;

        internal const uint Nv303dAlphaFuncEnable = 0x00000304;
        internal const uint Nv303dFogEnable = 0x0000036C;

        internal const uint Nv40TclFpPacker = 0x00001fec;
        internal const uint Nv40TclDividerFrequencyOp = 0x00001fc0;

        internal uint Raw { get; set; }

        internal bool NonIncrement => (Raw & RsxMethodFlagNoIncrement) != 0;
        internal bool Jump => (Raw & RsxMethodFlagJump) != 0;
        internal bool Call => (Raw & RsxMethodFlagCall) != 0;
        internal bool Return => (Raw & RsxMethodFlagReturn) != 0;

        internal uint MethodCount => (Raw >> RsxMethodCountShift) & RsxMaxMethodCount;
        internal uint MethodRegister => Raw & 0xFFFF;
        internal uint MethodSubChannel => (Raw >> RsxSubchannelShift) & 0x7;
        internal uint JumpOffset => Raw & 0x1FFFFFFF; // >> (31 - 31)
        internal uint CallOffset => Raw >> 2;         // >> (31 - 29)    
    }
}
