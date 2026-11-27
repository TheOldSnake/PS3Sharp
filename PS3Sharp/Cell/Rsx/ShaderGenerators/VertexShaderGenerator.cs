using PS3Sharp.Common;

namespace PS3Sharp.Cell.RSX
{
    internal sealed partial class VertexShaderGenerator
    {
        private static readonly string _vertexProgramBase = @"
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

        private static readonly string _vertexProgramEnd = @"
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


        private static readonly string _whiteSpace = @"                ";
        private string _currentVetrexShader = _vertexProgramBase;
        private string _lastVertexShader;
        private readonly uint[] _vertexShaderBuffer = new uint[512 * 4];  // 512 qwords

        internal uint VertexShaderShaderStart { get; set; }   // Index of the first instruction
        internal uint VertexShaderLoadIndex { get; set; }    // Index to load instructions from


        private string FinalizeVertexShader()
        {
            //Add the base
            string shader = _vertexProgramBase;

            //Add the main_cg
            shader += @"
            void main_cg(){
";

            //Add the body
            shader += _currentVetrexShader;

            //End of main_cg()
            shader += @"            }";

            //Add our main()
            shader += '\n';
            shader += _vertexProgramEnd;
            return shader;
        }

        internal VertexInstruction GetVertexInstruction(ReadOnlySpan<uint> data)
        {
            VertexInstruction v;
            v.Raw0 = data[0];
            v.Raw1 = data[1];
            v.Raw2 = data[2];
            v.Raw3 = data[3];
            return v;
        }

        internal void UploadInstructions(Queue<BigEndian<uint>> data)
        {
            while (data.Count > 0)
            {
                BigEndian<uint> value = data.Dequeue();
                _vertexShaderBuffer[VertexShaderLoadIndex] = value;
                VertexShaderLoadIndex++;
            }
        }

        internal ulong CalculateChecksum()
        {
            ulong checksum = 0;
            for (uint i = VertexShaderShaderStart; i < _vertexShaderBuffer.Length; i += 4)
            {
                checksum += _vertexShaderBuffer[i + 0];
                checksum += _vertexShaderBuffer[i + 1];
                checksum += _vertexShaderBuffer[i + 2];
                checksum += _vertexShaderBuffer[i + 3];

                if ((_vertexShaderBuffer[i + 3] & 1) == 1)
                { // End bit
                    break;
                }
            }

            return checksum;
        }

        internal string GenerateVertexShader()
        {
            _currentVetrexShader = string.Empty;

            for (uint i = VertexShaderShaderStart; i < _vertexShaderBuffer.Length; i += 4)
            {
                VertexInstruction instruction = new()
                { // Or we can do casts
                    Raw0 = _vertexShaderBuffer[i + 0],
                    Raw1 = _vertexShaderBuffer[i + 1],
                    Raw2 = _vertexShaderBuffer[i + 2],
                    Raw3 = _vertexShaderBuffer[i + 3],
                };

                _currentVetrexShader += HandleVectorInstruction(instruction);
                _currentVetrexShader += HandleScalarInstruction(instruction);

                if (instruction.End)
                {
                    break;
                }
            }

            _lastVertexShader = FinalizeVertexShader();
            return _lastVertexShader;
        }

        internal string HandleVectorInstruction(VertexInstruction instruction)
        {
            string src0 = GetSource(0, instruction);
            string src1 = GetSource(1, instruction);
            string src2 = GetSource(2, instruction);

            (string vectorDest, string vectorChannels) = GetDestination(instruction, false);

            string generatedSource = string.Empty;
            string generatedSourceChannels = "xyzw".Substring(0, vectorChannels.Length);

            switch (instruction.VectorOp)
            {
                case NvfxVpInstVecOpNop:
                    return string.Empty;

                case NvfxVpInstVecOpMov:
                    generatedSource = $"{src0}";
                    break;

                case NvfxVpInstVecOpMul:
                    generatedSource = $"({src0} * {src1})";
                    break;

                case NvfxVpInstVecOpAdd:
                    // For some reason ADD works with src0 and src2, so this is not a bug.
                    generatedSource = $"({src0} + {src2})";
                    break;

                case NvfxVpInstVecOpMad:
                    generatedSource = $"fma({src0}, {src1}, {src2})";
                    break;

                case NvfxVpInstVecOpDp3:
                    generatedSource = $"vec4(dot(vec3({src0}), vec3({src1})))";
                    break;

                case NvfxVpInstVecOpDph:
                    generatedSource = $"vec4(dot(vec4({src0}.xyz, 1.0), vec4({src1})))";
                    break;

                case NvfxVpInstVecOpDp4:
                    generatedSource = $"vec4(dot(vec4({src0}), vec4({src1})))";
                    break;


                /* todo: NVFX_VP_INST_VEC_OP_DST */

                case NvfxVpInstVecOpMin:
                    generatedSource = $"min({src0}, {src1})";
                    break;

                case NvfxVpInstVecOpMax:
                    generatedSource = $"max({src0}, {src1})";
                    break;

                case NvfxVpInstVecOpSlt:
                    generatedSource = $"vec4(lessThan({src0}, {src1}))";
                    break;

                case NvfxVpInstVecOpSge:
                    generatedSource = $"vec4(greaterThanEqual({src0}, {src1}))";
                    break;

                case NvfxVpInstVecOpArl:
                    generatedSource = $"ivec4({src0})";
                    break;

                case NvfxVpInstVecOpFrc:
                    generatedSource = $"fract({src0})";
                    break;

                case NvfxVpInstVecOpFlr:
                    generatedSource = $"floor({src0})";
                    break;

                case NvfxVpInstVecOpSeq:
                    generatedSource = $"vec4(equal({src0}, {src1}))";
                    break;

                /* todo: NVFX_VP_INST_VEC_OP_SFL */

                case NvfxVpInstVecOpSgt:
                    generatedSource = $"vec4(greaterThan({src0}, {src1}))";
                    break;

                case NvfxVpInstVecOpSle:
                    generatedSource = $"vec4(lessThanEqual({src0}, {src1}))";
                    break;

                case NvfxVpInstVecOpSne:
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
            if (instruction.Saturate)
            {
                fullSource = $"clamp({fullSource}, 0.0f, 1.0f)";
            }

            string output;
            if (instruction.CondEnabled)
            {
                string selectionStatment = ConditionalSelect(instruction, fullDest, vectorChannels, fullSource);
                output = $"{fullDest} = {selectionStatment};\n";

            }
            else
            {
                output = $"{fullDest} = {fullSource};\n";
            }

            //Console.Write($"line: {output}");
            return _whiteSpace + output;
        }

        internal string HandleScalarInstruction(VertexInstruction instruction)
        {
            // Not sure how scalar work
            // Big TODO

            (string scalarDest, string scalarChannels) = GetDestination(instruction, true);
            string src2 = GetSource(2, instruction);

            string generatedSource = string.Empty;

            switch (instruction.ScalarOp)
            {
                case NvfxVpInstScaOpNop:
                    /* NOP */
                    return string.Empty;

                case NvfxVpInstScaOpRcp:
                    generatedSource = $"vec4(1.0f / {src2})";
                    break;

                case NvfxVpInstScaOpRsq: // was using inversesqrt
                    generatedSource = $"(1.0f / sqrt(max({src2}, 0.0000000001))).xxxx";
                    break;

                case NvfxVpInstScaOpExp:
                    generatedSource = $"exp({src2})";
                    break;

                case NvfxVpInstScaOpLog:
                    generatedSource = $"log({src2})";
                    break;

                case NvfxVpInstScaOpLg2:
                    generatedSource = $"log2({src2})";
                    break;

                case NvfxVpInstScaOpEx2:
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
            return _whiteSpace + output;
        }

        internal string GetSource(int sourceNumber, VertexInstruction instruction)
        {
            //some information like "abs" are not in the common "src" fields 

            VertexSrc src;
            bool isAbsolute = false;

            switch (sourceNumber)
            {
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

            switch (src.Type)
            {

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

                    if (instruction.IsIndexedConst)
                    {
                        // index is offseted by addr0 / addr1
                        // the swizzle selects a single channel from the vector
                        char addr_reg_swizzle = xyzw[(int)instruction.AddrRegSwizzle];
                        c_index = $"{const_src} + addr{instruction.AddrRegSel}.{addr_reg_swizzle}";

                    }
                    else
                    {
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
            channels += xyzw[(int)src.SrcSwizzleX];
            channels += xyzw[(int)src.SrcSwizzleY];
            channels += xyzw[(int)src.SrcSwizzleZ];
            channels += xyzw[(int)src.SrcSwizzleW];

            srcName += $".{channels}";

            if (isAbsolute)
            {
                srcName = $"abs({srcName})";
            }

            if (src.Negate)
            {
                srcName = $"(-{srcName})";
            }

            return srcName;
        }

        internal (string, string) GetDestination(VertexInstruction instruction, bool isScalar = false)
        {
            string destName = string.Empty;

            bool isDestAddr = instruction.VectorOp == NvfxVpInstVecOpArl; // Load to address register
            bool isOutput = isScalar ? !instruction.IsVectorResult : instruction.IsVectorResult;
            uint temp_index = isScalar ? instruction.Sca_Dest_Temp : instruction.VecDestTemp;
            uint output_index = instruction.Dest;

            if (isOutput && output_index != 0x1F)
            {
                // Output
                destName = OutputNames[instruction.Dest];

            }
            else
            {

                if (isDestAddr)
                {
                    destName = $"addr{instruction.VecDestTemp}";   // always vec_dest_temp ?

                }
                else
                {
                    if (temp_index == 0x3F)
                    {
                        //CC
                        destName = $"cc{instruction.CondRegSel}";
                    }
                    else
                    {
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

            if (isScalar)
            {
                x_mask = instruction.XScaMask;
                y_mask = instruction.YScaMask;
                z_mask = instruction.ZScaMask;
                w_mask = instruction.WScaMask;
            }
            else
            {
                x_mask = instruction.XVecMask;
                y_mask = instruction.YVecMask;
                z_mask = instruction.ZVecMask;
                w_mask = instruction.WVecMask;
            }


            if (x_mask)
            {
                channels += "x";
                numberOfChannels++;
            }

            if (y_mask)
            {
                channels += "y";
                numberOfChannels++;
            }

            if (z_mask)
            {
                channels += "z";
                numberOfChannels++;
            }

            if (w_mask)
            {
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

        internal string ConditionalSelect(VertexInstruction instruction, string destination, string destinationMask, string source)
        { // dest and source are already masked
            // Select CC
            string cc = $"cc{instruction.CondRegSel}";

            // Handle CC swizzle
            string channels = string.Empty;
            channels += xyzw[(int)instruction.CondSwizzleX];
            channels += xyzw[(int)instruction.CondSwizzleY];
            channels += xyzw[(int)instruction.CondSwizzleZ];
            channels += xyzw[(int)instruction.CondSwizzleW];
            cc += $".{channels}";

            // this line will evaluate the condition register agains 0, with the selected function
            string condition_function = $"{Condition_GL_Functions[instruction.Cond]}({cc}, vec4(0.0f))";

            // we need to use the result to select between the original destination (which means no change), or the source
            // this works because mix returns x * (1 - a) + y * (a), and a is either 0 or 1 making it select either x or y
            // alternatively, we can also generate an if statment for each channel

            string final = $"mix({destination}, {source}, {condition_function}.{destinationMask})"; // use the destinaion mask on the bvec 

            return final;
        }

        internal string GetCast(int numberOfChannels)
        {
            switch (numberOfChannels)
            {
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
