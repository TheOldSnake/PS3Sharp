using PS3Sharp.Common;

namespace PS3Sharp.Cell.RSX {
    public static partial class VertexShaderGenerator {
        private static readonly string VertexProgramBase = @"
            #version 460        

            // temporary registers
            vec4 r[32];         

            // constants
            uniform vec4 c[468];

            // Condition Registers
            vec4 cc0;
            vec4 cc1;

            // Index registers
            ivec4 addr0;
            ivec4 addr1;

            // scale matrix   
            // uniform vec4 scale = vec4(1.0f, 1.0f, 1.0f, 1.0f);    

            // offset matrix   
            // uniform vec4 offset = vec4(0.0f, 0.0f, 0.0f, 0.0f);  

            // Viewport transformation
            uniform ivec2 surface_clip;
            uniform vec3 viewport_offset;
            uniform vec3 viewport_scale;

            // Inputs
            layout(location = 0) in vec4 vs_pos;
            layout(location = 1) in vec4 vs_weight;
            layout(location = 2) in vec4 vs_normal;
            layout(location = 3) in vec4 vs_diff_color;
            layout(location = 4) in vec4 vs_spec_color;
            layout(location = 5) in vec4 vs_fog;
            layout(location = 6) in vec4 vs_point_size;
            layout(location = 7) in vec4 vs_unk7;
            layout(location = 8) in vec4 vs_tex0;
            layout(location = 9) in vec4 vs_tex1;
            layout(location = 10) in vec4 vs_tex2;
            layout(location = 11) in vec4 vs_tex3;
            layout(location = 12) in vec4 vs_tex4;
            layout(location = 13) in vec4 vs_tex5;
            layout(location = 14) in vec4 vs_tex6;
            layout(location = 15) in vec4 vs_tex7;

            // Outputs
            out vec4 fs_wpos;       //dst_reg0
            out vec4 fs_col0;       //dst_reg1
            out vec4 fs_col1;       //dst_reg2
            out vec4 fs_bfc0;       //dst_reg3
            out vec4 fs_bfc1;       //dst_reg4
            out vec4 fs_fog;        //dst_reg5
            out vec4 fs_point_size; //dst_reg6
            out vec4 fs_tex0;       //dst_reg7
            out vec4 fs_tex1;       //dst_reg8
            out vec4 fs_tex2;       //dst_reg9
            out vec4 fs_tex3;       //dst_reg10
            out vec4 fs_tex4;       //dst_reg11
            out vec4 fs_tex5;       //dst_reg12
            out vec4 fs_tex6;       //dst_reg13
            out vec4 fs_tex7;       //dst_reg14
            out vec4 fs_tex8;       //dst_reg15
            out vec4 fs_tex9;       //dst_reg16
            out vec4 fs_ssa;        //dst_reg17
            out vec4 fs_unk15;      //dst_reg18";    

        private static readonly string VertexProgramEnd = @"
            void main(){
                main_cg();

                //TODO
                //vec3 scale = viewport_scale;
                //vec3 offs  = viewport_offset;
                //vec2 half_clip = surface_clip / 2.0f;

                //scale.x /=  half_clip.x;
                //scale.y /= -half_clip.y;

                //offs.x  -= half_clip.x;
                //offs.x  /= half_clip.x;
                //offs.y  -= half_clip.y;
                //offs.y  /= half_clip.y;
                //offs.z -= 0.5f;

                //Output position
                gl_Position = fs_wpos;
            }";


        private static readonly string WhiteSpace = @"                ";
        private static string CurrentVetrexShader = VertexProgramBase;
        public static string LastVertexShader;
        public static uint VertexShaderShaderStart { get; set; }   // Index of the first instruction
        public static uint VertexShaderLoadIndex { get;  set; }    // Index to load instructions from

        private static readonly uint[] VertexShaderBuffer = new uint[512 * 4];  // 512 qwords

        private static string FinalizeVertexShader() {
            //Add the base
            string shader = VertexProgramBase;

            //Add the main_cg
            shader += @"
            void main_cg(){
";

            //Add the body
            shader += CurrentVetrexShader;

            //End of main_cg()
            shader += @"            }";

            //Add our main()
            shader += '\n';
            shader += VertexProgramEnd;
            return shader;
        }

        public static VertexInstruction GetVertexInstruction(ReadOnlySpan<uint> data) {
            VertexInstruction v;
            v.Raw_0 = data[0];
            v.Raw_1 = data[1];
            v.Raw_2 = data[2];
            v.Raw_3 = data[3];
            return v;
        }

        public static void UploadInstructions(Queue<BigEndian<uint>> data) {
            while (data.Count > 0) {
                BigEndian<uint> value = data.Dequeue();
                VertexShaderBuffer[VertexShaderLoadIndex] = value;
                VertexShaderLoadIndex++;
            }
        }

        public static ulong CalculateChecksum() {
            ulong checksum = 0;
            for (uint i = VertexShaderShaderStart; i < VertexShaderBuffer.Length; i += 4) {
                checksum += VertexShaderBuffer[i + 0];
                checksum += VertexShaderBuffer[i + 1];
                checksum += VertexShaderBuffer[i + 2];
                checksum += VertexShaderBuffer[i + 3];

                if ((VertexShaderBuffer[i + 3] & 1) == 1) { // End bit
                    break;
                }
            }

            return checksum;
        }

        public static string GenerateVertexShader() {
            CurrentVetrexShader = string.Empty;
            
            for (uint i = VertexShaderShaderStart; i < VertexShaderBuffer.Length; i += 4) {
                VertexInstruction instruction = new VertexInstruction { // Or we can do casts
                    Raw_0 = VertexShaderBuffer[i + 0],
                    Raw_1 = VertexShaderBuffer[i + 1],
                    Raw_2 = VertexShaderBuffer[i + 2],
                    Raw_3 = VertexShaderBuffer[i + 3],
                };

                CurrentVetrexShader += HandleVectorInstruction(instruction);
                CurrentVetrexShader += HandleScalarInstruction(instruction);

                if (instruction.End) {
                    break;
                }
            }

            LastVertexShader = FinalizeVertexShader(); 
            return LastVertexShader;
        }

        public static string HandleVectorInstruction(VertexInstruction instruction) {
            string src0 = GetSource(0, instruction);
            string src1 = GetSource(1, instruction);
            string src2 = GetSource(2, instruction);

            (string vectorDest, string vectorChannels) = GetDestination(instruction, false);

            string generatedSource = string.Empty;
            string generatedSourceChannels = "xyzw".Substring(0, vectorChannels.Length);

            switch (instruction.VectorOp) {
                case NVFX_VP_INST_VEC_OP_NOP:
                    return string.Empty;

                case NVFX_VP_INST_VEC_OP_MOV:
                    generatedSource = $"{src0}";
                    break;

                case NVFX_VP_INST_VEC_OP_MUL:
                    generatedSource = $"({src0} * {src1})";
                    break;

                case NVFX_VP_INST_VEC_OP_ADD:
                    // For some reason ADD works with src0 and src2, so this is not a bug.
                    generatedSource = $"({src0} + {src2})";
                    break;

                case NVFX_VP_INST_VEC_OP_MAD:
                    generatedSource = $"fma({src0}, {src1}, {src2})";
                    break;

                case NVFX_VP_INST_VEC_OP_DP3:
                    generatedSource = $"vec4(dot(vec3({src0}), vec3({src1})))";
                    break;

                case NVFX_VP_INST_VEC_OP_DPH:
                    generatedSource = $"vec4(dot(vec4({src0}.xyz, 1.0), vec4({src1})))";
                    break;

                case NVFX_VP_INST_VEC_OP_DP4:
                    generatedSource = $"vec4(dot(vec4({src0}), vec4({src1})))";
                    break;


                /* todo: NVFX_VP_INST_VEC_OP_DST */

                case NVFX_VP_INST_VEC_OP_MIN:
                    generatedSource = $"min({src0}, {src1})";
                    break;

                case NVFX_VP_INST_VEC_OP_MAX:
                    generatedSource = $"max({src0}, {src1})";
                    break;

                case NVFX_VP_INST_VEC_OP_SLT:
                    generatedSource = $"vec4(lessThan({src0}, {src1}))";
                    break;

                case NVFX_VP_INST_VEC_OP_SGE:
                    generatedSource = $"vec4(greaterThanEqual({src0}, {src1}))";
                    break;

                case NVFX_VP_INST_VEC_OP_ARL: 
                    generatedSource = $"ivec4({src0})";
                    break;

                case NVFX_VP_INST_VEC_OP_FRC:
                    generatedSource = $"fract({src0})";
                    break;

                case NVFX_VP_INST_VEC_OP_FLR:
                    generatedSource = $"floor({src0})";
                    break;

                case NVFX_VP_INST_VEC_OP_SEQ:
                    generatedSource = $"vec4(equal({src0}, {src1}))";
                    break;

                /* todo: NVFX_VP_INST_VEC_OP_SFL */

                case NVFX_VP_INST_VEC_OP_SGT:
                    generatedSource = $"vec4(greaterThan({src0}, {src1}))";
                    break;

                case NVFX_VP_INST_VEC_OP_SLE:
                    generatedSource = $"vec4(lessThanEqual({src0}, {src1}))";
                    break;

                case NVFX_VP_INST_VEC_OP_SNE:
                    generatedSource = $"vec4(notEqual({src0}, {src1}))";
                    break;

                /* todo: NVFX_VP_INST_VEC_OP_STR */
                /* todo: NVFX_VP_INST_VEC_OP_SSG */
                /* todo: NVFX_VP_INST_VEC_OP_ARR */
                /* todo: NVFX_VP_INST_VEC_OP_ARA */

                default:
                    throw new NotImplementedException($"unimplemented vector op = {instruction.VectorOp:X}");
            }


            // assign "vectorChannels" from the source,
            // i.e. if destination is z we take the z component from the final source
            string fullSource = $"{generatedSource}.{vectorChannels}";
            string fullDest = $"{vectorDest}.{vectorChannels}";

            // apply saturation if needed
            if (instruction.Saturate) {
                fullSource = $"clamp({fullSource}, 0.0f, 1.0f)";
            }

            string output;
            if (instruction.CondEnabled) {
                string selectionStatment = ConditionalSelect(instruction, fullDest, vectorChannels, fullSource);
                output = $"{fullDest} = {selectionStatment};\n";

            } else {
                output = $"{fullDest} = {fullSource};\n";
            }

            //Console.Write($"line: {output}");
            return WhiteSpace + output;
        }

        public static string HandleScalarInstruction(VertexInstruction instruction) {
            // Not sure how scalar work
            // Big TODO

            (string scalarDest, string scalarChannels) = GetDestination(instruction, true);
            string src2 = GetSource(2, instruction);

            string generatedSource = string.Empty;

            switch (instruction.ScalarOp) {
                case NVFX_VP_INST_SCA_OP_NOP: 
                    /* NOP */  
                    return string.Empty;

                case NVFX_VP_INST_SCA_OP_RCP:
                    generatedSource = $"vec4(1.0f / {src2})";
                    break;

                case NVFX_VP_INST_SCA_OP_RSQ: // was using inversesqrt
                    generatedSource = $"(1.0f / sqrt(max({src2}, 0.0000000001))).xxxx"; 
                    break;

                case NVFX_VP_INST_SCA_OP_EXP:
                    generatedSource = $"exp({src2})";
                    break;

                case NVFX_VP_INST_SCA_OP_LOG:
                    generatedSource = $"log({src2})";
                    break;

                case NVFX_VP_INST_SCA_OP_LG2:
                    generatedSource = $"log2({src2})";
                    break;

                case NVFX_VP_INST_SCA_OP_EX2:
                    generatedSource = $"exp2({src2})";
                    break;


                default:
                    Console.WriteLine($"unimplemented scalar op = {instruction.ScalarOp:X}");
                    return string.Empty;

                    //throw new NotImplementedException($"unimplemented scalar op = {instruction.ScalarOp:X}");
            }

            string fullSource = $"{generatedSource}.{scalarChannels}";
            string fullDest = $"{scalarDest}.{scalarChannels}";

            string output = $"{fullDest} = {fullSource};\n";
            return WhiteSpace + output;
        }

        public static string GetSource(int sourceNumber, VertexInstruction instruction) {
            //some information like "abs" are not in the common "src" fields 

            VertexSrc src;
            bool isAbsolute = false;

            switch (sourceNumber) {
                case 0:
                    src = new VertexSrc { Raw = instruction.Src0 };
                    isAbsolute = instruction.Src0Abs;
                    break;

                case 1:
                    src = new VertexSrc { Raw = instruction.Src1 };
                    isAbsolute = instruction.Src1Abs;
                    break;

                case 2:
                    src = new VertexSrc { Raw = instruction.Src2 };
                    isAbsolute = instruction.Src2Abs;
                    break;

                default:
                    throw new Exception($"[VertexShaderGenerator] Invalid Source number: {sourceNumber}");
            }


            const string xyzw = "xyzw";     // we index into this string for swizzling
            string srcName = string.Empty;

            switch (src.Type) {

                // temorary register
                case 1: 
                    srcName = $"r[{src.TempID}]"; 
                    break;

                // input
                case 2: 
                    srcName = $"{InputNames[instruction.InputSrc]}";
                    break;

                // constant
                case 3:
                    uint const_src = instruction.ConstSrc;
                    string c_index = string.Empty;

                    if (instruction.IsIndexedConst) {
                        // index is offseted by addr0 / addr1
                        // the swizzle selects a single channel from the vector
                        char addr_reg_swizzle = xyzw[(int)instruction.Addr_Reg_Swizzle];
                        c_index = $"{const_src} + addr{instruction.Addr_Reg_Sel}.{addr_reg_swizzle}";

                    } else {
                        // direct index 
                        c_index = $"{const_src}";
                    }

                    srcName = $"c[{c_index}]";
                    break;

                default:
                    throw new Exception($"Unknown src type = {src.Type}");
            }

            //Handle source swizzle
            string channels = string.Empty;

            //The XYZW values in the src can act as indices 
            channels += xyzw[(int)src.Src_Swizzle_X];
            channels += xyzw[(int)src.Src_Swizzle_Y];
            channels += xyzw[(int)src.Src_Swizzle_Z];
            channels += xyzw[(int)src.Src_Swizzle_W];

            srcName += $".{channels}";

            if (isAbsolute) {
                srcName = $"abs({srcName})";
            }

            if (src.Negate) {
                srcName = $"(-{srcName})";
            }

            return srcName;
        }

        public static (string, string) GetDestination(VertexInstruction instruction, bool isScalar = false) {
            string destName = string.Empty;

            bool isDestAddr = instruction.VectorOp == NVFX_VP_INST_VEC_OP_ARL; // Load to address register
            bool isOutput = isScalar ? !instruction.IsVectorResult : instruction.IsVectorResult;
            uint temp_index = isScalar ? instruction.Sca_Dest_Temp : instruction.Vec_Dest_Temp;
            uint output_index = instruction.Dest;

            if (isOutput && output_index != 0x1F) {
                // Output
                destName = OutputNames[instruction.Dest];

            } else {

                if (isDestAddr) {
                    destName = $"addr{instruction.Vec_Dest_Temp}";   // always vec_dest_temp ?

                } else {
                    if (temp_index == 0x3F) {
                        //CC
                        destName = $"cc{instruction.Cond_Reg_Sel}";
                    } else {
                        // temporary register
                        destName = $"r[{temp_index}]";
                    }
                }            
            }

            //Handle dest swizzle
            //string xyzw = "xyzw";
            string channels = string.Empty;
            int numberOfChannels = 0;

            bool x_mask;
            bool y_mask;
            bool z_mask;
            bool w_mask;

            if (isScalar) {
                x_mask = instruction.X_Sca_Mask;
                y_mask = instruction.Y_Sca_Mask;
                z_mask = instruction.Z_Sca_Mask;
                w_mask = instruction.W_Sca_Mask;
            } else {
                x_mask = instruction.X_Vec_Mask;
                y_mask = instruction.Y_Vec_Mask;
                z_mask = instruction.Z_Vec_Mask;
                w_mask = instruction.W_Vec_Mask;
            }


            if (x_mask) {
                channels += "x";
                numberOfChannels++;
            }

            if (y_mask) {
                channels += "y";
                numberOfChannels++;
            }

            if (z_mask) {
                channels += "z";
                numberOfChannels++;
            }

            if (w_mask) {
                channels += "w";
                numberOfChannels++;
            }

            //destName += $".{channels}";

            return (destName, channels);
        }

        // GL functions to be used based on the condition field 
        private static readonly string[] Condition_GL_Functions = [
            "invalidCond",
            "lessThan",
            "equal",
            "lessThanEqual",
            "greaterThan",
            "notEqual",
            "greaterThanEqual"
        ];

        const string xyzw = "xyzw";

        public static string ConditionalSelect(VertexInstruction instruction, string destination, string destinationMask, string source) { // dest and source are already masked
            // Select CC
            string cc = $"cc{instruction.Cond_Reg_Sel}";

            // Handle CC swizzle
            string channels = string.Empty;
            channels += xyzw[(int)instruction.Cond_Swizzle_X];
            channels += xyzw[(int)instruction.Cond_Swizzle_Y];
            channels += xyzw[(int)instruction.Cond_Swizzle_Z];
            channels += xyzw[(int)instruction.Cond_Swizzle_W];
            cc += $".{channels}";

            // this line will evaluate the condition register agains 0, with the selected function
            string condition_function = $"{Condition_GL_Functions[instruction.Cond]}({cc}, vec4(0.0f))";

            // we need to use the result to select between the original destination (which means no change), or the source
            // this works because mix returns x * (1 - a) + y * (a), and a is either 0 or 1 making it select either x or y
            // alternatively, we can also generate an if statment for each channel

            string final = $"mix({destination}, {source}, {condition_function}.{destinationMask})"; // use the destinaion mask on the bvec 

            return final;
        }

        public static string GetCast(int numberOfChannels) {
            switch (numberOfChannels) {
                case 1: return "float";
                case 2: return "vec2";
                case 3: return "vec3";
                case 4: return "vec4";
                default:
                    throw new Exception($"Invalid number of channels: {numberOfChannels}");
            }
        }
    }
}
