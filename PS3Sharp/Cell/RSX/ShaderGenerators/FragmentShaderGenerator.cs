using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace PS3Sharp.Cell.RSX
{
    public static partial class FragmentShaderGenerator
    {
        private static readonly string FragmentProgramBase = @"
            #version 460        

            //output color
            layout(location = 0) out vec4 out_col0; 

            //Inputs
            in vec4 fs_wpos;
            in vec4 fs_col0;
            in vec4 fs_col1;
            in vec4 fs_fog;
            in vec4 fs_tex0;
            in vec4 fs_tex1;
            in vec4 fs_tex2;
            in vec4 fs_tex3;
            in vec4 fs_tex4;
            in vec4 fs_tex5;
            in vec4 fs_tex6;
            in vec4 fs_tex7;
            in vec4 fs_tex8;
            in vec4 fs_tex9;
            in vec4 fs_ssa;
            in vec4 fs_bfc0;
            in vec4 fs_bfc1;
            in vec4 fs_point_size;
            in vec4 fs_unk15;

            // Condition registers
            vec4 cc0;
            vec4 cc1;

            // Outputs 
            vec4 r0 = vec4(0.0f);  // -> out_col0
            vec4 r1 = vec4(0.0f);  // unused
            vec4 r2 = vec4(0.0f);  // unused
            vec4 r3 = vec4(0.0f);  // unused
            vec4 r4 = vec4(0.0f);  // unused

            vec4 dummy;

            // Texture samplers
            uniform sampler2D u_tex0;
            uniform sampler2D u_tex1;
            uniform sampler2D u_tex2;
            uniform sampler2D u_tex3;
            uniform sampler2D u_tex4;
            uniform sampler2D u_tex5;
            uniform sampler2D u_tex6;
            uniform sampler2D u_tex7;
            uniform sampler2D u_tex8;
            uniform sampler2D u_tex9;
            uniform sampler2D u_tex10;
            uniform sampler2D u_tex11;
            uniform sampler2D u_tex12;
            uniform sampler2D u_tex13;
            uniform sampler2D u_tex14;
            uniform sampler2D u_tex15;

            // TODO
            uniform bool u_flip_tex_flags[16];";


        private static readonly string FragmentHelpers = @"
            const float FLOAT16_MAX = 65504.0;

            vec4 clampToFloat16(vec4 v) {
                return clamp(v, -FLOAT16_MAX, FLOAT16_MAX);
            }

            vec3 normalize3(vec3 x) {
                return length(x) > 0? normalize(x) : x;
            }

            vec2 getTexCoords(int textureIndex, vec2 originalCoords){
                bool flip = u_flip_tex_flags[textureIndex];
                float x = originalCoords.x;            
                float y = flip ? (1.0f - originalCoords.y) : originalCoords.y;
                return vec2(x, y);
            }
";

        private static readonly string FragmentProgramEnd = @"
            void main(){
                vec4 dbg = texture(u_tex0, fs_tex0.xy);
                main_cg();
                out_col0 = r0 + (dbg * 0.00001f);

               // out_col0 = texture(u_tex0, fs_tex0.xyzw.xy);
            }";


        private static string[] Globals = ["r0", "r1", "r2", "r3", "r4", "cc0", "cc1"];
        private static List<string> Locals = [];
        private static List<uint> CurrentUniforms = [];                  // uniformes names that are in use for this current shader
        private static string CurrentImmediate = string.Empty;

        private static readonly string WhiteSpace = @"                ";
        private static string CurrentFragmentShaderBody = string.Empty;

        public const uint INSTRUCTION_SIZE = 4 * sizeof(uint);          //4 32-bit words
        public static uint FragmentShaderStartAddress { get; set; }     // Address of the fragment shader program
        private static uint CurrentAddress;

        private static List<FragmentInstruction> Buf = new List<FragmentInstruction>();
        public static string AddressToUniform(uint address) => $"u_{address:x}";
        public static bool IsUniformInUse(uint address) => CurrentUniforms.Contains(address);

        //public static string AddUniform(uint address) {
        //    if (!CurrentUniforms.Contains(address)) {
        //        CurrentUniforms.Add(address);
        //    }

        //    return ToUniform(address);
        //}

        public static string FinalizeFragmentShader()
        {

            //Add the base
            string shader = FragmentProgramBase;

            //Add the helpers
            shader += FragmentHelpers;

            //Add the uniforms
            for (int i = 0; i < CurrentUniforms.Count; i++)
            {
                shader += $"            uniform vec4 {AddressToUniform(CurrentUniforms[i])};\n";
            }

            //Add the main_cg
            shader += @"
            void main_cg(){
";

            //Add local variables
            for (int i = 0; i < Locals.Count; i++)
            {
                shader += $"{WhiteSpace}vec4 {Locals[i]} = vec4(0.0f);\n";
            }

            //Add the body
            shader += CurrentFragmentShaderBody;

            //End of main_cg()
            shader += @"            }";

            //Add our main()
            shader += '\n';
            shader += FragmentProgramEnd;
            return shader;
        }

        private static uint Read32(uint address)
        {
            uint value = MemoryManager.Read<uint>(address);
            return (value >> 16) | (value << 16); // for some reason 16-bits are swapped
        }

        private static Vector4 ReadVector4(uint address)
        {
            Vector4 v = new Vector4();
            v.X = Unsafe.BitCast<uint, float>(Read32(address + 0));
            v.Y = Unsafe.BitCast<uint, float>(Read32(address + 4));
            v.Z = Unsafe.BitCast<uint, float>(Read32(address + 8));
            v.W = Unsafe.BitCast<uint, float>(Read32(address + 12));
            return v;
        }

        public static FragmentInstruction GetFragmentInstruction(uint address)
        {
            uint raw0 = Read32(address + 0);
            uint raw1 = Read32(address + 4);
            uint raw2 = Read32(address + 8);
            uint raw3 = Read32(address + 12);

            FragmentInstruction instruction = new FragmentInstruction
            {
                OPDEST_Raw = raw0,
                SRC0_Raw = raw1,
                SRC1_Raw = raw2,
                SRC2_Raw = raw3
            };

            return instruction;
        }

        public static ulong CalculateChecksum()
        {
            uint currentAddress = FragmentShaderStartAddress;
            int instructionCount = 0;
            ulong checksum = 0;

            while (true)
            {

                // I am trying to not include uniforms in the checksum

                if (!CurrentUniforms.Contains(currentAddress))
                {
                    FragmentInstruction instruction = GetFragmentInstruction(currentAddress);
                    instructionCount++;

                    checksum += instruction.OPDEST_Raw;
                    checksum += instruction.SRC0_Raw;
                    checksum += instruction.SRC1_Raw;
                    checksum += instruction.SRC2_Raw;

                    if (instruction.ProgramEnd)
                    {
                        break;
                    }
                }

                currentAddress += INSTRUCTION_SIZE;

                if (instructionCount > 512)
                {
                    throw new Exception($"Fragment program too large?");
                }
            }

            return checksum;
        }

        static bool stop = false;
        public static string GenerateFragmentShader()
        {
            CurrentAddress = FragmentShaderStartAddress;
            CurrentFragmentShaderBody = string.Empty;
            Locals.Clear();
            CurrentUniforms.Clear();

            int count = 0;
            while (true)
            {
                FragmentInstruction instruction = GetFragmentInstruction(CurrentAddress);
                CurrentAddress += INSTRUCTION_SIZE;
                CurrentFragmentShaderBody += HandleFragmentInstruction(instruction);

                if (instruction.ProgramEnd || stop)
                {
                    stop = false;
                    return FinalizeFragmentShader();
                }

                count++;
                if (count > 128)
                {
                    throw new Exception("Shader too large?");
                }
            }
        }

        public static string HandleFragmentInstruction(FragmentInstruction instruction)
        {

            if (instruction.Opcode == NVFX_FP_OP_OPCODE_NOP)
            {
                return string.Empty;
            }

            CurrentImmediate = string.Empty;

            string src0 = GetSource(0, instruction);
            string src1 = GetSource(1, instruction);
            string src2 = GetSource(2, instruction);

            (string destination, string destinationChannels) = GetDestination(instruction);
            string generatedSource = string.Empty;
            //string generatedSourceChannels = "xyzw".Substring(0, destinationChannels.Length);

            switch (instruction.Opcode)
            {
                case NVFX_FP_OP_OPCODE_NOP:
                case NVFX_FP_OP_OPCODE_FENCT:
                case NVFX_FP_OP_OPCODE_FENCB:
                    return string.Empty;

                case NVFX_FP_OP_OPCODE_MOV:
                    if (instruction.IsDestinationFp16)
                    {
                        generatedSource = $"clampToFloat16({src0})";
                    }
                    else
                    {
                        generatedSource = $"{src0}";
                    }
                    break;

                case NVFX_FP_OP_OPCODE_MUL:
                    if (instruction.IsDestinationFp16)
                    {
                        generatedSource = $"clampToFloat16(({src0} * {src1}))";
                    }
                    else
                    {
                        generatedSource = $"({src0} * {src1})";
                    }
                    break;

                case NVFX_FP_OP_OPCODE_ADD:
                    generatedSource = $"({src0} + {src1})";
                    break;


                case NVFX_FP_OP_OPCODE_MAD:
                    if (instruction.IsDestinationFp16)
                    {
                        generatedSource = $"clampToFloat16(fma({src0}, {src1}, {src2}))";
                    }
                    else
                    {
                        generatedSource = $"fma({src0}, {src1}, {src2})";
                    }
                    break;

                case NVFX_FP_OP_OPCODE_DP3:
                    if (instruction.IsDestinationFp16)
                    {
                        generatedSource = $"clampToFloat16(vec4(dot(vec3({src0}), vec3({src1}))))";
                    }
                    else
                    {
                        generatedSource = $"vec4(dot(vec3({src0}), vec3({src1})))";
                    }
                    break;

                case NVFX_FP_OP_OPCODE_DP4:
                    if (instruction.IsDestinationFp16)
                    {
                        generatedSource = $"clampToFloat16(vec4(dot(vec4({src0}), vec4({src1}))))";
                    }
                    else
                    {
                        generatedSource = $"vec4(dot(vec4({src0}), vec4({src1})))";
                    }
                    break;

                /* todo: NVFX_FP_OP_OPCODE_DST */

                case NVFX_FP_OP_OPCODE_MIN:
                    generatedSource = $"min({src0}, {src1})";
                    break;

                case NVFX_FP_OP_OPCODE_MAX:
                    generatedSource = $"max({src0}, {src1})";
                    break;


                case NVFX_FP_OP_OPCODE_SLT:
                    generatedSource = $"vec4(lessThan({src0}, {src1}))";
                    break;

                case NVFX_FP_OP_OPCODE_SGE:
                    generatedSource = $"vec4(greaterThanEqual({src0}, {src1}))";
                    break;

                case NVFX_FP_OP_OPCODE_SLE:
                    generatedSource = $"vec4(lessThanEqual({src0}, {src1}))";
                    break;

                case NVFX_FP_OP_OPCODE_SGT:
                    generatedSource = $"vec4(greaterThan({src0}, {src1}))";
                    break;

                case NVFX_FP_OP_OPCODE_SNE:
                    generatedSource = $"vec4(notEqual({src0}, {src1}))";
                    break;

                case NVFX_FP_OP_OPCODE_SEQ:
                    generatedSource = $"vec4(equal({src0}, {src1}))";
                    break;

                case NVFX_FP_OP_OPCODE_FRC:
                    generatedSource = $"fract({src0})";
                    break;

                case NVFX_FP_OP_OPCODE_FLR:
                    generatedSource = $"floor({src0})";
                    break;

                case NVFX_FP_OP_OPCODE_KIL:
                    generatedSource = $"discard";
                    break;

                /* NVFX_FP_OP_OPCODE_PK4B */

                /* NVFX_FP_OP_OPCODE_UP4B */

                case NVFX_FP_OP_OPCODE_DDX:
                    generatedSource = $"dFdx({src0})";
                    break;

                case NVFX_FP_OP_OPCODE_DDY:
                    generatedSource = $"dFdy({src0})";
                    break;

                case NVFX_FP_OP_OPCODE_TEX:
                    generatedSource = $"texture(u_tex{instruction.TextureIndex}, getTexCoords({instruction.TextureIndex}, {src0}.xy))";
                    break;


                // these instuctiosn only take the x part ?
                // need to check

                case NVFX_FP_OP_OPCODE_RCP:
                    generatedSource = $"(1.0f / {src0}).xxxx";
                    break;

                case NVFX_FP_OP_OPCODE_RSQ:
                    generatedSource = $"(1.0f / abs(sqrt({src0}.x))).xxxx";
                    break;


                case NVFX_FP_OP_OPCODE_EX2:
                    generatedSource = $"vec4(exp2({src0}.x))";
                    break;

                case NVFX_FP_OP_OPCODE_LG2:
                    generatedSource = $"vec4(log2({src0}.x))";
                    break;

                case NVFX_FP_OP_OPCODE_COS:
                    generatedSource = $"cos({src0}.xxxx)";
                    break;

                case NVFX_FP_OP_OPCODE_SIN:
                    generatedSource = $"sin({src0}.xxxx)";
                    break;


                case NVFX_FP_OP_OPCODE_DP2:
                    if (instruction.IsDestinationFp16)
                    {
                        generatedSource = $"clampToFloat16(vec4(dot(vec2({src0}), vec2({src1}))))";
                    }
                    else
                    {
                        generatedSource = $"vec4(dot(vec2({src0}), vec2({src1})))";
                    }
                    break;

                case NVFX_FP_OP_OPCODE_TXB:
                    //same as NVFX_FP_OP_OPCODE_TEX, but with a bias ?
                    generatedSource = $"/* TODO */ texture(u_tex{instruction.TextureIndex}, getTexCoords({instruction.TextureIndex}, {src0}.xy), {src1}.x)";
                    break;

                case NVFX_FP_OP_OPCODE_NRM:
                    //normalize the xyz part, and extend z from the output to make vec4
                    generatedSource = $"normalize3(vec3({src0})).xyzz";
                    break;

                case NVFX_FP_OP_OPCODE_DIV:
                    generatedSource = $"({src0} / {src1}.x)";
                    break;

                case NVFX_FP_OP_OPCODE_DIVSQ:
                    generatedSource = $"({src0} / abs(sqrt({src1})))";
                    break;

                default: throw new NotImplementedException($"unimplemented Fragment op = 0x{instruction.Opcode:X}");
            }

            if (instruction.Saturate)
            {
                generatedSource = $"clamp({generatedSource}, 0.0f, 1.0f)";
            }

            float scale = GetScale(instruction);
            if (scale != 1.000f)
            {
                generatedSource = $"({generatedSource} * {scale:f3}f)";
            }

            string output;
            if (instruction.IsConditional)
            {
                output = AssignConditionally(destination, destinationChannels, generatedSource, instruction);
            }
            else
            {
                if (instruction.Opcode != NVFX_FP_OP_OPCODE_KIL)
                {
                    output = $"{destination}.{destinationChannels} = {generatedSource}.{destinationChannels};\n";
                }
                else
                {
                    output = "discard;\n";
                }
            }


            //if (output.Contains("r0.xyz = fma(r1.wwww, r1.xyzw, r0.xyzw).xyz;")) {
            //    output += "r0 = texture(u_tex0, getTexCoords(0, fs_tex0.xyzw.xy), vec4(-0.5f, 0f, 0f, 0f).xxxx.x).xyzw;\n";
            //    stop = true;
            //}
            //Console.Write($"Line: {output}");
            return WhiteSpace + output;
        }

        public static string GetSource(int sourceNumber, FragmentInstruction instruction)
        {

            FragmentInstruction.SRC src;
            bool isAbsolute = false;

            switch (sourceNumber)
            {
                case 0:
                    src = instruction.SRC0;
                    isAbsolute = instruction.SRC0_Abs;
                    break;

                case 1:
                    src = instruction.SRC1;
                    isAbsolute = instruction.SRC1_Abs;
                    break;

                case 2:
                    src = instruction.SRC2;
                    isAbsolute = instruction.SRC2_Abs;
                    break;

                default:
                    throw new Exception($"[FragmentShaderGenerator] Invalid source = {sourceNumber}");
            }


            string srcName = string.Empty;
            switch (src.Type)
            {
                case 0:
                    srcName = $"{(src.IsFp16 ? "h" : "r")}{src.TempIndex}";
                    if (!Globals.Contains(srcName))
                    {
                        AddLocalVariable(srcName);
                    }
                    break;


                case 1:
                    srcName = $"{InputNames[instruction.SourceIndex]}";

                    //A bit hardcoding
                    if (srcName == "fs_wpos")
                    {
                        srcName = $"gl_FragCoord";
                        break;
                    }

                    if (srcName.StartsWith("fs_tex") && instruction.Perspective_Correction)
                    {
                        srcName = $"({srcName} * gl_FragCoord.w)";
                        break;
                    }

                    // From RPCS3: Reading from registers 1 and 2 (COL0 and COL1) is clamped to (0, 1)
                    if (instruction.SourceIndex == 1 || instruction.SourceIndex == 2)
                    {
                        srcName = $"clamp({srcName}, 0.0, 1.0)";
                        break;
                    }

                    break;


                case 2: //Immediate (constant) value
                    const uint sizeOfVector = (4 * sizeof(uint));

                    //Check if there's a uniform for this address
                    var uniformsDict = PS3Sharp.RSX.CurrentState.AllUniforms;
                    if (uniformsDict.ContainsKey(CurrentAddress))
                    {

                        // Add it to the currently used uniforms
                        if (!CurrentUniforms.Contains(CurrentAddress))
                        {
                            CurrentUniforms.Add(CurrentAddress);
                        }

                        srcName = AddressToUniform(CurrentAddress);
                        CurrentImmediate = srcName;
                        //CurrentAddress += sizeOfVector;
                        break;
                    }

                    //Check if we have an immediate from a previous SRC
                    if (CurrentImmediate != string.Empty)
                    {
                        srcName = CurrentImmediate;
                        break;
                    }

                    //Fetch vector4 from memory
                    Vector4 imm = ReadVector4(CurrentAddress);
                    CurrentAddress += sizeOfVector;

                    //Embed it as an immediate
                    srcName = $"vec4({imm.X}f, {imm.Y}f, {imm.Z}f, {imm.W}f)";
                    CurrentImmediate = srcName;
                    break;

                default:
                    throw new Exception($"Unknown src type = {src.Type}");
            }


            //Handle source swizzle
            string xyzw = "xyzw";
            string channels = string.Empty;

            //The XYZW values in the src can act as indices 
            channels += xyzw[(int)src.SwizzleX];
            channels += xyzw[(int)src.SwizzleY];
            channels += xyzw[(int)src.SwizzleZ];
            channels += xyzw[(int)src.SwizzleW];
            srcName += $".{channels}";


            if (isAbsolute)
            {
                srcName = $"abs({srcName})";
            }

            if (src.Negate)
            {
                srcName = $"-({srcName})";
            }

            //Console.WriteLine(srcName);

            return srcName;
        }

        public static (string, string) GetDestination(FragmentInstruction instruction)
        {
            string destName = string.Empty;

            if (instruction.NoDest)
            {
                if (instruction.SetCC)
                {
                    destName = $"cc{instruction.CC_Index}";
                }
                else
                {
                    destName = "dummy";
                }

            }
            else
            {
                destName = $"{(instruction.IsDestinationFp16 ? "h" : "r")}{instruction.DestinationIndex}";
                if (!Globals.Contains(destName))
                {
                    AddLocalVariable(destName);
                }
            }

            //Handle dest swizzle
            //string xyzw = "xyzw";
            string channels = string.Empty;
            int numberOfChannels = 0;

            if (instruction.X_Mask)
            {
                channels += "x";
                numberOfChannels++;
            }

            if (instruction.Y_Mask)
            {
                channels += "y";
                numberOfChannels++;
            }

            if (instruction.Z_Mask)
            {
                channels += "z";
                numberOfChannels++;
            }

            if (instruction.W_Mask)
            {
                channels += "w";
                numberOfChannels++;
            }

            return (destName, channels);
        }

        public static string AssignConditionally(string destination, string destinationChannels, string generatedSource, FragmentInstruction instruction)
        {
            // Generate the comparison operator
            string condition = string.Empty;
            if (instruction.ExecuteIfLess && instruction.ExecuteIfEqual)
            {
                condition = "<=";

            }
            else if (instruction.ExecuteIfGreater && instruction.ExecuteIfEqual)
            {
                condition = ">=";

            }
            else if (instruction.ExecuteIfGreater && instruction.ExecuteIfLess)
            {
                condition = "!=";

            }
            else if (instruction.ExecuteIfGreater)
            {
                condition = ">";

            }
            else if (instruction.ExecuteIfLess)
            {
                condition = "<";

            }
            else if (instruction.ExecuteIfEqual)
            {
                condition = "==";
            }

            // Generate the condition register
            string cc = $"cc{instruction.CC_Index}";

            // CC swizzle
            string xyzw = "xyzw";
            string channels = string.Empty;
            channels += xyzw[(int)instruction.CC_SwizzleX];
            channels += xyzw[(int)instruction.CC_SwizzleY];
            channels += xyzw[(int)instruction.CC_SwizzleZ];
            channels += xyzw[(int)instruction.CC_SwizzleW];
            cc += $".{channels}";

            string resultCode = string.Empty;

            int numberOfChannels = destinationChannels.Length;
            if (instruction.Opcode != NVFX_FP_OP_OPCODE_KIL)
            {

                // Assign based on channel-wise comparison (can we just use mix() ?)
                for (int i = 0; i < numberOfChannels; i++)
                {
                    resultCode += $@"if({cc}.{xyzw[i]} {condition} 0.0f){{
                          {destination}.{destinationChannels[i]} = {generatedSource}.{destinationChannels[i]};           
                    }}{"\n"}";
                }

            }
            else
            {

                // Discard if any check is true
                for (int i = 0; i < numberOfChannels; i++)
                {
                    resultCode += $@"if({cc}.{xyzw[i]} {condition} 0.0f){{
                          discard;           
                    }}{"\n"}";
                }
            }


            //Console.WriteLine(resultCode);

            //throw new Exception();
            return resultCode;
        }

        public static void AddLocalVariable(string name)
        {
            if (!Locals.Contains(name))
            {
                Locals.Add(name);
            }
        }

        public static float GetScale(FragmentInstruction instruction)
        {
            float scale = 1.000f;

            switch (instruction.Scale)
            {
                case 0: scale = 1.000f; break;
                case 1: scale = 2.000f; break;
                case 2: scale = 4.000f; break;
                case 3: scale = 8.000f; break;

                case 5: scale = 0.500f; break;
                case 6: scale = 0.250f; break;
                case 7: scale = 0.125f; break;
            }

            return scale;
        }

        public static string GetCast(int numberOfChannels)
        {
            switch (numberOfChannels)
            {
                //case 0: return "vec4";

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
