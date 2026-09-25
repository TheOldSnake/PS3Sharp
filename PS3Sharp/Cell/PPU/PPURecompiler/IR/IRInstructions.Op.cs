namespace PS3Sharp.Cell.PPU.PPURecompiler.IR {
    public static partial class IRInstructions {
        public enum Op {
            GPRLoad,
            GPRStore,

            FPRLoad,
            FPRStore,

            CRLoad,
            CRStore,

            CTRLoad,
            CTRStore,

            LRLoad,
            LRStore,

            XERLoad,
            XERStore,

            VRSaveLoad,
            VRSaveStore,

            PCStore,

            Constant,

            Add,
            Sub,

            IMulLD, // Signed 64x64 - takes low 64 of result
            IMulHD, // Signed 64x64 - takes high 64 of result
            IMulLW, // Signed 32x32 - takes low 32 of result
            IMulHW, // Signed 32x32 - takes high 32 of result
            MulHD,  // Unsigned 64x64 - takes high 64 of result
            MulHW,  // Unsigned 32x32 - takes high 32 of result


            // Checked divide instructions handle special cases
            CheckedIDivD,   // Signed 64/64 
            CheckedDivD,    // Unsigned 64/64 
            CheckedIDivW,   // Signed 32/32 
            CheckedDivW,    // Unsigned 32/32 

            And,
            Or,
            Xor,
            Nor,
            Not,

            ShiftLeft,
            ShiftRight,
            ShiftRightArithmetic,
            RotateLeft,
            RotateRight,

            SetEq,
            SetNotEq,
            SetLessThanUnsigned,
            SetLessThanEqUnsigned,
            SetGreaterThanUnsigned,
            SetGreaterThanEqUnsigned,
            SetLessThanSigned,
            SetLessThanEqSigned,
            SetGreaterThanSigned,
            SetGreaterThanEqSigned,

            SignExtend,
            ZeroExtend,
            Truncate,

            Select,

            Load8,
            Load16,
            Load16Rev,
            Load32,
            Load32Rev,
            Load32Rx,
            Load64,
            Load64Rx,
            Load128,

            Store8,
            Store16,
            Store16Rev,
            Store32,
            Store32Rev,
            Store32CX,
            Store64,
            Store64CX,
            Store128,

            CNTLZ,  

            Compare,

            //Internal control flow inside IR, these are not for PPC branches
            Branch,         
            BranchConditional,

            Syscall,
            Phi,
            InterpreterFallback,
            Ret,
        }
    }
}
