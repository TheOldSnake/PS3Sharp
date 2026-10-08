namespace PS3Sharp.Cell.RSX
{
    internal struct RSXCommand
    {

        //Constant masks/shifts based on PSL1GHT
        internal const uint RSX_METHOD_FLAG_NO_INCREMENT = 0x40000000;
        internal const uint RSX_METHOD_FLAG_JUMP = 0x20000000;
        internal const uint RSX_METHOD_FLAG_CALL = 0x00000002;
        internal const uint RSX_METHOD_FLAG_RETURN = 0x00020000;
        internal const uint RSX_MAX_METHOD_COUNT = 0x000007FF;
        internal const int RSX_METHOD_COUNT_SHIFT = 18;
        internal const int RSX_SUBCHANNEL_SHIFT = 13;

        // Commands values:

        internal const uint NV40TCL_NOP = 0x00000100;
        internal const uint NV40TCL_VTX_CACHE_INVALIDATE = 0x00001714;
        internal const uint NV40TCL_VTX_CACHE_INVALIDATE2 = 0x00001710;

        internal const uint GCM_FLIP_COMMAND = 0x0000FEAC;

        internal const uint NV406ETCL_SET_REF = 0x00000050;
        internal const uint NV406ETCL_SET_CTX_DMA_SEMAPHORE = 0x00000060;
        internal const uint NV406ETCL_SEMAPHORE_OFFSET = 0x00000064;
        internal const uint NV406ETCL_SEMAPHORE_ACQUIRE = 0x00000068;
        internal const uint NV406ETCL_SEMAPHORE_RELEASE = 0x0000006c;

        internal const uint NV40TCL_SEMAPHORE_BACKENDWRITE_OFFSET = 0x00001D6C; //it's named NV40TCL_SEMAPHORE_OFFSET	in PSL1GHT for some reason
        internal const uint NV40TCL_SEMAPHORE_BACKENDWRITE_RELEASE = 0x00001D70;

        internal const uint NV34_TCL_PRIMITIVE_3D_FOG_COLOR = 0x00000370;

        internal const uint NV34_TCL_PRIMITIVE_3D_COLOR_MASK = 0x00000324;
        internal const uint GCM_COLOR_MASK_B = 0x00000001;
        internal const uint GCM_COLOR_MASK_G = 0x00000100;
        internal const uint GCM_COLOR_MASK_R = 0x00010000;
        internal const uint GCM_COLOR_MASK_A = 0x01000000;

        internal const uint NV40TCL_VIEWPORT_HORIZ = 0x00000A00;
        internal const uint NV40TCL_VIEWPORT_VERT = 0x00000A04;
        internal const uint NV40TCL_DEPTH_RANGE = 0x00000394;

        internal const uint NV40TCL_VIEWPORT_OFFSET_X = 0x00000A20 + 0;
        internal const uint NV40TCL_VIEWPORT_OFFSET_Y = 0x00000A20 + 4;
        internal const uint NV40TCL_VIEWPORT_OFFSET_Z = 0x00000A20 + 8;
        internal const uint NV40TCL_VIEWPORT_OFFSET_W = 0x00000A20 + 12;

        internal const uint NV40TCL_VIEWPORT_SCALE_X = 0x00000A30 + 0;
        internal const uint NV40TCL_VIEWPORT_SCALE_Y = 0x00000A30 + 4;
        internal const uint NV40TCL_VIEWPORT_SCALE_Z = 0x00000A30 + 8;
        internal const uint NV40TCL_VIEWPORT_SCALE_W = 0x00000A30 + 12;

        internal const uint NV40TCL_SCISSOR_HORIZ = 0x000008C0;
        internal const uint NV40TCL_SCISSOR_VERT = 0x000008C4;
        internal const uint NV40TCL_DEPTH_TEST_ENABLE = 0x00000A74;
        internal const uint NV40TCL_DEPTH_FUNC = 0x00000A6C;
        internal const uint NV40TCL_SHADE_MODEL = 0x00000368;
        internal const uint NV40TCL_DEPTH_WRITE_ENABLE = 0x00000A70;
        internal const uint NV40TCL_FRONT_FACE = 0x00001834;

        internal const uint NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY0 = 0x00000184;
        internal const uint NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY1 = 0x00000188;
        // internal const uint NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY2 = 0x0000018C;
        //internal const uint NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY3 = 0x00000194;
        internal const uint NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY4 = 0x00000198;
        internal const uint NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY5 = 0x000001A4;
        internal const uint NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY6 = 0x000001A8;
        internal const uint NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY7 = 0x000001AC;
        internal const uint NV34_TCL_PRIMITIVE_3D_DMA_IN_MEMORY8 = 0x000001B0;

        internal const uint NV40TCL_DMA_COLOR0 = 0x00000194;
        internal const uint NV40TCL_DMA_COLOR1 = 0x0000018C;
        internal const uint NV40TCL_DMA_COLOR2 = 0x000001B4;
        internal const uint NV40TCL_DMA_COLOR3 = 0x000001B8;

        internal const uint NV40TCL_CLEAR_VALUE_COLOR = 0x00001D90;
        internal const uint NV40TCL_CLEAR_VALUE_DEPTH = 0x00001D8C;
        internal const uint NV40TCL_CLEAR_BUFFERS = 0x00001D94;

        internal const uint NV40TCL_SHADER_WINDOW = 0x00001D88;

        internal const uint NV40TCL_RT_HORIZ = 0x00000200;
        internal const uint NV40TCL_RT_VERT = 0x00000204;
        internal const uint NV40TCL_RT_FORMAT = 0x00000208;
        internal const uint NV40TCL_RT_ENABLE = 0x00000220;
        internal const uint NV40TCL_ZETA_PITCH = 0x0000022c;
        internal const uint NV40TCL_WINDOW_OFFSET = 0x000002b8;

        internal const uint NV40TCL_COLOR2_PITCH = 0x00000280;
        internal const uint NV40TCL_COLOR3_PITCH = 0x00000284;

        internal const uint NV40TCL_DEPTH_CONTROL = 0x00001D78;

        internal const uint NV40TCL_VIEWPORT_CLIP_HORIZ_0 = 0x000002C0; // +0
        internal const uint NV40TCL_VIEWPORT_CLIP_HORIZ_1 = 0x000002C8; // +8
        internal const uint NV40TCL_VIEWPORT_CLIP_HORIZ_2 = 0x000002D0; // +16
        internal const uint NV40TCL_VIEWPORT_CLIP_HORIZ_3 = 0x000002D8; // +24
        internal const uint NV40TCL_VIEWPORT_CLIP_HORIZ_4 = 0x000002E0; // +32
        internal const uint NV40TCL_VIEWPORT_CLIP_HORIZ_5 = 0x000002E8; // +40
        internal const uint NV40TCL_VIEWPORT_CLIP_HORIZ_6 = 0x000002F0; // +48
        internal const uint NV40TCL_VIEWPORT_CLIP_HORIZ_7 = 0x000002F8; // +56
        //internal const uint NV40TCL_VIEWPORT_CLIP_HORIZ_8 = 0x00000300; // +64

        internal const uint NV40TCL_VP_UPLOAD_FROM_ID = 0x00001E9c;
        internal const uint NV40TCL_VP_START_FROM_ID = 0x00001EA0;
        internal const uint NV40TCL_VP_UPLOAD_INST = 0x00000B80;
        internal const uint NV40TCL_VP_ATTRIB_EN = 0x00001FF0;
        internal const uint NV40TCL_VP_RESULT_EN = 0x00001FF4;
        internal const uint NV40TCL_TRANSFORM_TIMEOUT = 0x00001EF8;
        internal const uint NV40TCL_VP_UPLOAD_CONST_ID = 0x00001EFC;
        internal const uint NV40TCL_VP_UPLOAD_CONST_X = 0x00001F00;

        internal const uint NV40TCL_FP_ADDRESS = 0x000008E4;
        internal const uint NV40TCL_FP_CONTROL = 0X00001D60;
        internal const uint NV40TCL_CLIP_PLANE_ENABLE = 0x00001478;

        internal const uint NV40TCL_VTXFMT_0 = 0x00001740;
        internal const uint NV40TCL_VTXFMT_1 = 0x00001740 + (1 * 4);
        internal const uint NV40TCL_VTXFMT_2 = 0x00001740 + (2 * 4);
        internal const uint NV40TCL_VTXFMT_3 = 0x00001740 + (3 * 4);
        internal const uint NV40TCL_VTXFMT_4 = 0x00001740 + (4 * 4);
        internal const uint NV40TCL_VTXFMT_5 = 0x00001740 + (5 * 4);
        internal const uint NV40TCL_VTXFMT_6 = 0x00001740 + (6 * 4);
        internal const uint NV40TCL_VTXFMT_7 = 0x00001740 + (7 * 4);
        internal const uint NV40TCL_VTXFMT_8 = 0x00001740 + (8 * 4);
        internal const uint NV40TCL_VTXFMT_9 = 0x00001740 + (9 * 4);
        internal const uint NV40TCL_VTXFMT_10 = 0x00001740 + (10 * 4);
        internal const uint NV40TCL_VTXFMT_11 = 0x00001740 + (11 * 4);
        internal const uint NV40TCL_VTXFMT_12 = 0x00001740 + (12 * 4);
        internal const uint NV40TCL_VTXFMT_13 = 0x00001740 + (13 * 4);
        internal const uint NV40TCL_VTXFMT_14 = 0x00001740 + (14 * 4);
        internal const uint NV40TCL_VTXFMT_15 = 0x00001740 + (15 * 4);

        internal const uint NV40TCL_VTXBUF_ADDRESS_0 = 0x00001680;
        internal const uint NV40TCL_VTXBUF_ADDRESS_1 = 0x00001680 + (1 * 4);
        internal const uint NV40TCL_VTXBUF_ADDRESS_2 = 0x00001680 + (2 * 4);
        internal const uint NV40TCL_VTXBUF_ADDRESS_3 = 0x00001680 + (3 * 4);
        internal const uint NV40TCL_VTXBUF_ADDRESS_4 = 0x00001680 + (4 * 4);
        internal const uint NV40TCL_VTXBUF_ADDRESS_5 = 0x00001680 + (5 * 4);
        internal const uint NV40TCL_VTXBUF_ADDRESS_6 = 0x00001680 + (6 * 4);
        internal const uint NV40TCL_VTXBUF_ADDRESS_7 = 0x00001680 + (7 * 4);
        internal const uint NV40TCL_VTXBUF_ADDRESS_8 = 0x00001680 + (8 * 4);
        internal const uint NV40TCL_VTXBUF_ADDRESS_9 = 0x00001680 + (9 * 4);
        internal const uint NV40TCL_VTXBUF_ADDRESS_10 = 0x00001680 + (10 * 4);
        internal const uint NV40TCL_VTXBUF_ADDRESS_11 = 0x00001680 + (11 * 4);
        internal const uint NV40TCL_VTXBUF_ADDRESS_12 = 0x00001680 + (12 * 4);
        internal const uint NV40TCL_VTXBUF_ADDRESS_13 = 0x00001680 + (13 * 4);
        internal const uint NV40TCL_VTXBUF_ADDRESS_14 = 0x00001680 + (14 * 4);
        internal const uint NV40TCL_VTXBUF_ADDRESS_15 = 0x00001680 + (15 * 4);

        internal const uint NV40TCL_VB_INDEX_BATCH_OFFSET = 0x0000181C;

        internal const uint NV40TCL_BEGIN_END = 0x00001808;
        internal const uint NV40TCL_VB_INDEX_BATCH_DRAW = 0x00001824;
        internal const uint NV40TCL_VB_VERTEX_BATCH = 0x00001814;

        internal const uint NV40TCL_TEX_COORD_CONTROL = 0x00000B40;

        internal const uint NV3062_SET_OBJECT = 0x00006000;
        internal const uint NV3062_SET_CONTEXT_DMA_NOTIFIES = 0x00006180;
        internal const uint NV3062_SET_CONTEXT_DMA_IMAGE_SOURCE = 0x00006184;
        internal const uint NV3062_SET_CONTEXT_DMA_IMAGE_DESTIN = 0x00006188;
        internal const uint NV3062_SET_COLOR_FORMAT = 0x00006300;
        internal const uint NV3062_SET_PITCH = 0x00006304;
        internal const uint NV3062_SET_OFFSET_SOURCE = 0x00006308;
        internal const uint NV3062_SET_OFFSET_DESTIN = 0x0000630C;

        internal const uint NV308A_POINT = 0x0000A304;
        internal const uint NV308A_SIZE_OUT = 0x0000A308;
        internal const uint NV308A_COLOR = 0x0000A400;

        internal const uint NV40TCL_BLEND_ENABLE = 0x00000310;
        internal const uint NV40TCL_BLEND_FUNC_SRC = 0x00000314;
        internal const uint NV40TCL_BLEND_FUNC_DST = 0x00000318;
        internal const uint NV40TCL_BLEND_COLOR = 0x0000031C;
        internal const uint NV40TCL_BLEND_EQUATION = 0x00000320;
        internal const uint NV40TCL_COLOR_LOGIC_OP_ENABLE = 0x00000374;
        internal const uint NV40TCL_COLOR_LOGIC_OP = 0x00000378;


        // In PSL1GHT TEX_OFFSET and TEX_FORMAT are considered one command with 2 parameters
        // But we need to seperate them
        internal const uint NV40TCL_TEX_OFFSET_0 = 0x00001A00 + 32 * 0;
        internal const uint NV40TCL_TEX_OFFSET_1 = 0x00001A00 + 32 * 1;
        internal const uint NV40TCL_TEX_OFFSET_2 = 0x00001A00 + 32 * 2;
        internal const uint NV40TCL_TEX_OFFSET_3 = 0x00001A00 + 32 * 3;
        internal const uint NV40TCL_TEX_OFFSET_4 = 0x00001A00 + 32 * 4;
        internal const uint NV40TCL_TEX_OFFSET_5 = 0x00001A00 + 32 * 5;
        internal const uint NV40TCL_TEX_OFFSET_6 = 0x00001A00 + 32 * 6;
        internal const uint NV40TCL_TEX_OFFSET_7 = 0x00001A00 + 32 * 7;
        internal const uint NV40TCL_TEX_OFFSET_8 = 0x00001A00 + 32 * 8;
        internal const uint NV40TCL_TEX_OFFSET_9 = 0x00001A00 + 32 * 9;
        internal const uint NV40TCL_TEX_OFFSET_10 = 0x00001A00 + 32 * 10;
        internal const uint NV40TCL_TEX_OFFSET_11 = 0x00001A00 + 32 * 11;
        internal const uint NV40TCL_TEX_OFFSET_12 = 0x00001A00 + 32 * 12;
        internal const uint NV40TCL_TEX_OFFSET_13 = 0x00001A00 + 32 * 13;
        internal const uint NV40TCL_TEX_OFFSET_14 = 0x00001A00 + 32 * 14;
        internal const uint NV40TCL_TEX_OFFSET_15 = 0x00001A00 + 32 * 15;

        internal const uint NV40TCL_TEX_FORMAT_0 = 0x00001A04 + 32 * 0;
        internal const uint NV40TCL_TEX_FORMAT_1 = 0x00001A04 + 32 * 1;
        internal const uint NV40TCL_TEX_FORMAT_2 = 0x00001A04 + 32 * 2;
        internal const uint NV40TCL_TEX_FORMAT_3 = 0x00001A04 + 32 * 3;
        internal const uint NV40TCL_TEX_FORMAT_4 = 0x00001A04 + 32 * 4;
        internal const uint NV40TCL_TEX_FORMAT_5 = 0x00001A04 + 32 * 5;
        internal const uint NV40TCL_TEX_FORMAT_6 = 0x00001A04 + 32 * 6;
        internal const uint NV40TCL_TEX_FORMAT_7 = 0x00001A04 + 32 * 7;
        internal const uint NV40TCL_TEX_FORMAT_8 = 0x00001A04 + 32 * 8;
        internal const uint NV40TCL_TEX_FORMAT_9 = 0x00001A04 + 32 * 9;
        internal const uint NV40TCL_TEX_FORMAT_10 = 0x00001A04 + 32 * 10;
        internal const uint NV40TCL_TEX_FORMAT_11 = 0x00001A04 + 32 * 11;
        internal const uint NV40TCL_TEX_FORMAT_12 = 0x00001A04 + 32 * 12;
        internal const uint NV40TCL_TEX_FORMAT_13 = 0x00001A04 + 32 * 13;
        internal const uint NV40TCL_TEX_FORMAT_14 = 0x00001A04 + 32 * 14;
        internal const uint NV40TCL_TEX_FORMAT_15 = 0x00001A04 + 32 * 15;

        internal const uint NV40TCL_TEX_WRAP_0 = 0x00001A08 + 32 * 0;
        internal const uint NV40TCL_TEX_WRAP_1 = 0x00001A08 + 32 * 1;
        internal const uint NV40TCL_TEX_WRAP_2 = 0x00001A08 + 32 * 2;
        internal const uint NV40TCL_TEX_WRAP_3 = 0x00001A08 + 32 * 3;
        internal const uint NV40TCL_TEX_WRAP_4 = 0x00001A08 + 32 * 4;
        internal const uint NV40TCL_TEX_WRAP_5 = 0x00001A08 + 32 * 5;
        internal const uint NV40TCL_TEX_WRAP_6 = 0x00001A08 + 32 * 6;
        internal const uint NV40TCL_TEX_WRAP_7 = 0x00001A08 + 32 * 7;
        internal const uint NV40TCL_TEX_WRAP_8 = 0x00001A08 + 32 * 8;
        internal const uint NV40TCL_TEX_WRAP_9 = 0x00001A08 + 32 * 9;
        internal const uint NV40TCL_TEX_WRAP_10 = 0x00001A08 + 32 * 10;
        internal const uint NV40TCL_TEX_WRAP_11 = 0x00001A08 + 32 * 11;
        internal const uint NV40TCL_TEX_WRAP_12 = 0x00001A08 + 32 * 12;
        internal const uint NV40TCL_TEX_WRAP_13 = 0x00001A08 + 32 * 13;
        internal const uint NV40TCL_TEX_WRAP_14 = 0x00001A08 + 32 * 14;
        internal const uint NV40TCL_TEX_WRAP_15 = 0x00001A08 + 32 * 15;

        // Control 0
        internal const uint NV40TCL_TEX_ENABLE_0 = 0x00001A0C + 32 * 0;
        internal const uint NV40TCL_TEX_ENABLE_1 = 0x00001A0C + 32 * 1;
        internal const uint NV40TCL_TEX_ENABLE_2 = 0x00001A0C + 32 * 2;
        internal const uint NV40TCL_TEX_ENABLE_3 = 0x00001A0C + 32 * 3;
        internal const uint NV40TCL_TEX_ENABLE_4 = 0x00001A0C + 32 * 4;
        internal const uint NV40TCL_TEX_ENABLE_5 = 0x00001A0C + 32 * 5;
        internal const uint NV40TCL_TEX_ENABLE_6 = 0x00001A0C + 32 * 6;
        internal const uint NV40TCL_TEX_ENABLE_7 = 0x00001A0C + 32 * 7;
        internal const uint NV40TCL_TEX_ENABLE_8 = 0x00001A0C + 32 * 8;
        internal const uint NV40TCL_TEX_ENABLE_9 = 0x00001A0C + 32 * 9;
        internal const uint NV40TCL_TEX_ENABLE_10 = 0x00001A0C + 32 * 10;
        internal const uint NV40TCL_TEX_ENABLE_11 = 0x00001A0C + 32 * 11;
        internal const uint NV40TCL_TEX_ENABLE_12 = 0x00001A0C + 32 * 12;
        internal const uint NV40TCL_TEX_ENABLE_13 = 0x00001A0C + 32 * 13;
        internal const uint NV40TCL_TEX_ENABLE_14 = 0x00001A0C + 32 * 14;
        internal const uint NV40TCL_TEX_ENABLE_15 = 0x00001A0C + 32 * 15;

        // Control 1
        internal const uint NV40TCL_TEX_SWIZZLE_0 = 0x00001A10 + 32 * 0;
        internal const uint NV40TCL_TEX_SWIZZLE_1 = 0x00001A10 + 32 * 1;
        internal const uint NV40TCL_TEX_SWIZZLE_2 = 0x00001A10 + 32 * 2;
        internal const uint NV40TCL_TEX_SWIZZLE_3 = 0x00001A10 + 32 * 3;
        internal const uint NV40TCL_TEX_SWIZZLE_4 = 0x00001A10 + 32 * 4;
        internal const uint NV40TCL_TEX_SWIZZLE_5 = 0x00001A10 + 32 * 5;
        internal const uint NV40TCL_TEX_SWIZZLE_6 = 0x00001A10 + 32 * 6;
        internal const uint NV40TCL_TEX_SWIZZLE_7 = 0x00001A10 + 32 * 7;
        internal const uint NV40TCL_TEX_SWIZZLE_8 = 0x00001A10 + 32 * 8;
        internal const uint NV40TCL_TEX_SWIZZLE_9 = 0x00001A10 + 32 * 9;
        internal const uint NV40TCL_TEX_SWIZZLE_10 = 0x00001A10 + 32 * 10;
        internal const uint NV40TCL_TEX_SWIZZLE_11 = 0x00001A10 + 32 * 11;
        internal const uint NV40TCL_TEX_SWIZZLE_12 = 0x00001A10 + 32 * 12;
        internal const uint NV40TCL_TEX_SWIZZLE_13 = 0x00001A10 + 32 * 13;
        internal const uint NV40TCL_TEX_SWIZZLE_14 = 0x00001A10 + 32 * 14;
        internal const uint NV40TCL_TEX_SWIZZLE_15 = 0x00001A10 + 32 * 15;

        internal const uint NV40TCL_TEX_FILTER_0 = 0x00001A14 + 32 * 0;
        internal const uint NV40TCL_TEX_FILTER_1 = 0x00001A14 + 32 * 1;
        internal const uint NV40TCL_TEX_FILTER_2 = 0x00001A14 + 32 * 2;
        internal const uint NV40TCL_TEX_FILTER_3 = 0x00001A14 + 32 * 3;
        internal const uint NV40TCL_TEX_FILTER_4 = 0x00001A14 + 32 * 4;
        internal const uint NV40TCL_TEX_FILTER_5 = 0x00001A14 + 32 * 5;
        internal const uint NV40TCL_TEX_FILTER_6 = 0x00001A14 + 32 * 6;
        internal const uint NV40TCL_TEX_FILTER_7 = 0x00001A14 + 32 * 7;
        internal const uint NV40TCL_TEX_FILTER_8 = 0x00001A14 + 32 * 8;
        internal const uint NV40TCL_TEX_FILTER_9 = 0x00001A14 + 32 * 9;
        internal const uint NV40TCL_TEX_FILTER_10 = 0x00001A14 + 32 * 10;
        internal const uint NV40TCL_TEX_FILTER_11 = 0x00001A14 + 32 * 11;
        internal const uint NV40TCL_TEX_FILTER_12 = 0x00001A14 + 32 * 12;
        internal const uint NV40TCL_TEX_FILTER_13 = 0x00001A14 + 32 * 13;
        internal const uint NV40TCL_TEX_FILTER_14 = 0x00001A14 + 32 * 14;
        internal const uint NV40TCL_TEX_FILTER_15 = 0x00001A14 + 32 * 15;

        internal const uint NV40TCL_TEX_SIZE0_0 = 0x00001A18 + 32 * 0;
        internal const uint NV40TCL_TEX_SIZE0_1 = 0x00001A18 + 32 * 1;
        internal const uint NV40TCL_TEX_SIZE0_2 = 0x00001A18 + 32 * 2;
        internal const uint NV40TCL_TEX_SIZE0_3 = 0x00001A18 + 32 * 3;
        internal const uint NV40TCL_TEX_SIZE0_4 = 0x00001A18 + 32 * 4;
        internal const uint NV40TCL_TEX_SIZE0_5 = 0x00001A18 + 32 * 5;
        internal const uint NV40TCL_TEX_SIZE0_6 = 0x00001A18 + 32 * 6;
        internal const uint NV40TCL_TEX_SIZE0_7 = 0x00001A18 + 32 * 7;
        internal const uint NV40TCL_TEX_SIZE0_8 = 0x00001A18 + 32 * 8;
        internal const uint NV40TCL_TEX_SIZE0_9 = 0x00001A18 + 32 * 9;
        internal const uint NV40TCL_TEX_SIZE0_10 = 0x00001A18 + 32 * 10;
        internal const uint NV40TCL_TEX_SIZE0_11 = 0x00001A18 + 32 * 11;
        internal const uint NV40TCL_TEX_SIZE0_12 = 0x00001A18 + 32 * 12;
        internal const uint NV40TCL_TEX_SIZE0_13 = 0x00001A18 + 32 * 13;
        internal const uint NV40TCL_TEX_SIZE0_14 = 0x00001A18 + 32 * 14;
        internal const uint NV40TCL_TEX_SIZE0_15 = 0x00001A18 + 32 * 15;

        internal const uint NV40TCL_TEX_SIZE1_0 = 0x00001840 + 4 * 0;
        internal const uint NV40TCL_TEX_SIZE1_1 = 0x00001840 + 4 * 1;
        internal const uint NV40TCL_TEX_SIZE1_2 = 0x00001840 + 4 * 2;
        internal const uint NV40TCL_TEX_SIZE1_3 = 0x00001840 + 4 * 3;
        internal const uint NV40TCL_TEX_SIZE1_4 = 0x00001840 + 4 * 4;
        internal const uint NV40TCL_TEX_SIZE1_5 = 0x00001840 + 4 * 5;
        internal const uint NV40TCL_TEX_SIZE1_6 = 0x00001840 + 4 * 6;
        internal const uint NV40TCL_TEX_SIZE1_7 = 0x00001840 + 4 * 7;
        internal const uint NV40TCL_TEX_SIZE1_8 = 0x00001840 + 4 * 8;
        internal const uint NV40TCL_TEX_SIZE1_9 = 0x00001840 + 4 * 9;
        internal const uint NV40TCL_TEX_SIZE1_10 = 0x00001840 + 4 * 10;
        internal const uint NV40TCL_TEX_SIZE1_11 = 0x00001840 + 4 * 11;
        internal const uint NV40TCL_TEX_SIZE1_12 = 0x00001840 + 4 * 12;
        internal const uint NV40TCL_TEX_SIZE1_13 = 0x00001840 + 4 * 13;
        internal const uint NV40TCL_TEX_SIZE1_14 = 0x00001840 + 4 * 14;
        internal const uint NV40TCL_TEX_SIZE1_15 = 0x00001840 + 4 * 15;


        // Immediate vertex data - vec2
        internal const uint NV40TCL_VTX_ATTR_2F_0 = 0x00001880 + 0 * 8;
        internal const uint NV40TCL_VTX_ATTR_2F_1 = 0x00001880 + 1 * 8;
        internal const uint NV40TCL_VTX_ATTR_2F_2 = 0x00001880 + 2 * 8;
        internal const uint NV40TCL_VTX_ATTR_2F_3 = 0x00001880 + 3 * 8;
        internal const uint NV40TCL_VTX_ATTR_2F_4 = 0x00001880 + 4 * 8;
        internal const uint NV40TCL_VTX_ATTR_2F_5 = 0x00001880 + 5 * 8;
        internal const uint NV40TCL_VTX_ATTR_2F_6 = 0x00001880 + 6 * 8;
        internal const uint NV40TCL_VTX_ATTR_2F_7 = 0x00001880 + 7 * 8;
        internal const uint NV40TCL_VTX_ATTR_2F_8 = 0x00001880 + 8 * 8;
        internal const uint NV40TCL_VTX_ATTR_2F_9 = 0x00001880 + 9 * 8;
        internal const uint NV40TCL_VTX_ATTR_2F_10 = 0x00001880 + 10 * 8;
        internal const uint NV40TCL_VTX_ATTR_2F_11 = 0x00001880 + 11 * 8;
        internal const uint NV40TCL_VTX_ATTR_2F_12 = 0x00001880 + 12 * 8;
        internal const uint NV40TCL_VTX_ATTR_2F_13 = 0x00001880 + 13 * 8;
        internal const uint NV40TCL_VTX_ATTR_2F_14 = 0x00001880 + 14 * 8;

        // Immediate vertex data - vec4
        internal const uint NV40TCL_VTX_ATTR_4F_0 = 0x00001C00 + 0 * 16;
        internal const uint NV40TCL_VTX_ATTR_4F_1 = 0x00001C00 + 1 * 16;
        internal const uint NV40TCL_VTX_ATTR_4F_2 = 0x00001C00 + 2 * 16;
        internal const uint NV40TCL_VTX_ATTR_4F_3 = 0x00001C00 + 3 * 16;
        internal const uint NV40TCL_VTX_ATTR_4F_4 = 0x00001C00 + 4 * 16;
        internal const uint NV40TCL_VTX_ATTR_4F_5 = 0x00001C00 + 5 * 16;
        internal const uint NV40TCL_VTX_ATTR_4F_6 = 0x00001C00 + 6 * 16;
        internal const uint NV40TCL_VTX_ATTR_4F_7 = 0x00001C00 + 7 * 16;
        internal const uint NV40TCL_VTX_ATTR_4F_8 = 0x00001C00 + 8 * 16;
        internal const uint NV40TCL_VTX_ATTR_4F_9 = 0x00001C00 + 9 * 16;
        internal const uint NV40TCL_VTX_ATTR_4F_10 = 0x00001C00 + 10 * 16;
        internal const uint NV40TCL_VTX_ATTR_4F_11 = 0x00001C00 + 11 * 16;
        internal const uint NV40TCL_VTX_ATTR_4F_12 = 0x00001C00 + 12 * 16;
        internal const uint NV40TCL_VTX_ATTR_4F_13 = 0x00001C00 + 13 * 16;
        internal const uint NV40TCL_VTX_ATTR_4F_14 = 0x00001C00 + 14 * 16;


        internal const uint NV40TCL_TEX_CACHE_CTL = 0x00001FD8;

        internal const uint NV40TCL_TEX_FORMAT_FORMAT_L8 = 0x00000100;
        internal const uint NV40TCL_TEX_FORMAT_FORMAT_A1R5G5B5 = 0x00000200;
        internal const uint NV40TCL_TEX_FORMAT_FORMAT_A4R4G4B4 = 0x00000300;
        internal const uint NV40TCL_TEX_FORMAT_FORMAT_R5G6B5 = 0x00000400;
        internal const uint NV40TCL_TEX_FORMAT_FORMAT_A8R8G8B8 = 0x00000500;
        internal const uint NV40TCL_TEX_FORMAT_FORMAT_DXT1 = 0x00000600;
        internal const uint NV40TCL_TEX_FORMAT_FORMAT_DXT3 = 0x00000700;
        internal const uint NV40TCL_TEX_FORMAT_FORMAT_DXT5 = 0x00000800;
        internal const uint NV40TCL_TEX_FORMAT_FORMAT_G8B8 = 0x00000B00;
        internal const uint NV40TCL_TEX_FORMAT_FORMAT_A8L8 = 0x00001800;

        internal const uint NV40TCL_POLYGON_OFFSET_FILL_ENABLE = 0x00000A68;
        internal const uint NV40TCL_POLYGON_OFFSET_FACTOR = 0x00000A78;
        internal const uint NV40TCL_CULL_FACE = 0x00001830;
        internal const uint NV40TCL_CULL_FACE_ENABLE = 0x0000183C;
        internal const uint NV40TCL_STENCIL_BACK_ENABLE = 0x00000328;
        internal const uint NV40TCL_POLYGON_MODE_FRONT = 0x00001828;
        internal const uint NV40TCL_DEPTH_BOUNDS_MIN = 0x00000384;
        internal const uint NV40TCL_DEPTH_BOUNDS_TEST_ENABLE = 0x00000380;
        internal const uint NV40TCL_ANTI_ALIASING_CONTROL = 0x00001D7C;

        internal const uint NV30_3D_ALPHA_FUNC_ENABLE = 0x00000304;
        internal const uint NV30_3D_FOG_ENABLE = 0x0000036C;

        internal const uint NV40TCL_FP_PACKER = 0x00001fec;
        internal const uint NV40TCL_DIVIDER_FREQUENCY_OP = 0x00001fc0;

        internal uint Raw { get; set; }

        internal bool NonIncrement => (Raw & RSX_METHOD_FLAG_NO_INCREMENT) != 0;
        internal bool Jump => (Raw & RSX_METHOD_FLAG_JUMP) != 0;
        internal bool Call => (Raw & RSX_METHOD_FLAG_CALL) != 0;
        internal bool Return => (Raw & RSX_METHOD_FLAG_RETURN) != 0;

        internal uint MethodCount => (Raw >> RSX_METHOD_COUNT_SHIFT) & RSX_MAX_METHOD_COUNT;
        internal uint MethodRegister => Raw & 0xFFFF;
        internal uint MethodSubChannel => (Raw >> RSX_SUBCHANNEL_SHIFT) & 0x7;
        internal uint JumpOffset => Raw & 0x1FFFFFFF; // >> (31 - 31)
        internal uint CallOffset => Raw >> 2;         // >> (31 - 29)    
    }
}
