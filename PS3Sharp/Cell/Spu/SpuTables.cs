using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.Spu
{
    internal static class SpuTables
    {
        [InlineArray(ShiftTableSize)]
        internal struct ShiftInlineArray
        {
            private byte _element;
        }

        [InlineArray(RotationTableSize)]
        internal struct RotationInlineArray
        {
            private byte _element;
        }

        [InlineArray(FsmbSelectorSize)]
        internal struct FsmbInlineArray
        {
            private UInt128 _element;
        }

        [InlineArray(FsmhSelectorSize)]
        internal struct FsmhInlineArray
        {
            private UInt128 _element;
        }

        [InlineArray(FsmSelectorSize)]
        internal struct FsmInlineArray
        {
            private UInt128 _element;
        }

        private const int ShiftTableSize = 16 * 16;       // 16 shifts of 16-bytes
        private const int RotationTableSize = 16 * 16;    // 16 rotations of 16-bytes

        private const int FsmbSelectorSize = 1 << 16;     // 16-bit selector
        private const int FsmhSelectorSize = 1 << 8;      // 8-bit selector
        private const int FsmSelectorSize = 1 << 4;       // 4-bit selector

        private static ShiftInlineArray _leftShiftTable;
        private static ShiftInlineArray _rightShiftTable;
        private static RotationInlineArray _leftRotationTable;
        private static RotationInlineArray _rightRotationTable;
        private static FsmbInlineArray _fsmbTable;
        private static FsmhInlineArray _fsmhTable;
        private static FsmInlineArray _fsmTable;

        // Exposed ROS
        internal static ReadOnlySpan<byte> LeftShiftTable => _leftShiftTable;
        internal static ReadOnlySpan<byte> RightShiftTable => _rightShiftTable;
        internal static ReadOnlySpan<byte> LeftRotationTable => _leftRotationTable;
        internal static ReadOnlySpan<byte> RightRotationTable => _rightRotationTable;

        // Exposed ref readonly byte (it is faster to pass the span in MemoryMarshal.GetReference())
        internal static ref readonly byte LeftShiftTableRef => ref MemoryMarshal.GetReference(LeftShiftTable);
        internal static ref readonly byte RightShiftTableRef => ref MemoryMarshal.GetReference(RightShiftTable);
        internal static ref readonly byte LeftRotationTableRef => ref MemoryMarshal.GetReference(LeftRotationTable);
        internal static ref readonly byte RightRotationTableRef => ref MemoryMarshal.GetReference(RightRotationTable);

        internal static ReadOnlySpan<UInt128> FsmbTable => _fsmbTable;
        internal static ReadOnlySpan<UInt128> FsmhTable => _fsmhTable;
        internal static ReadOnlySpan<UInt128> FsmTable => _fsmTable;

        internal static void InitializeStaticTables()
        {
            InitializeFsmTables();
            InitializeByteShuffleTables();
        }

        private static void InitializeFsmTables()
        {
            InitializeFsm<byte>(_fsmbTable);
            InitializeFsm<ushort>(_fsmhTable);
            InitializeFsm<uint>(_fsmTable);
        }

        private static void InitializeFsm<T>(Span<UInt128> fsmSpan) where T : unmanaged, IMinMaxValue<T>, IUnsignedNumber<T>
        {
            // Generate a 128-bit value for each selector
            Span<T> fsmT = MemoryMarshal.Cast<UInt128, T>(fsmSpan);

            int selectorBitCount = Unsafe.SizeOf<UInt128>() / Unsafe.SizeOf<T>();
            int selectorCount = 1 << selectorBitCount;
            int currentLocation = 0;

            if (fsmSpan.Length != selectorCount)
            {
                throw new ArgumentException($"Unexpected fsm span size = {fsmSpan.Length} for T: {typeof(T)}");
            }

            for (int selector = 0; selector < selectorCount; selector++)
            {
                // Each bit of the selector generates a T value
                for (int bit = 0; bit < selectorBitCount; bit++)
                {
                    bool isSet = ((selector >> bit) & 1) != 0;
                    fsmT[currentLocation++] = isSet ? T.MaxValue : T.Zero;
                }
            }
        }

        private static void InitializeByteShuffleTables()
        {
            // For each shift/rotation, generate a shuffle vector describing the new indices
            int shuffleCount = 16;
            int indexCount = 16;
            int currentLocation = 0;

            // Notably, this depends on x86 PSHUFB semantics
            Span<byte> leftShiftTable = _leftShiftTable;
            Span<byte> rightShiftTable = _rightShiftTable;

            Span<byte> leftRotationTable = _leftRotationTable;
            Span<byte> rightRotationTable = _rightRotationTable;

            for (int shuffle = 0; shuffle < shuffleCount; shuffle++)
            {
                for (int index = 0; index < indexCount; index++)
                {
                    int leftShiftedIndex = index - shuffle;
                    int rightShiftedIndex = index + shuffle;

                    int leftRotatedIndex = (index - shuffle) & 0xF;
                    int rightRotatedIndex = (index + shuffle) & 0xF;

                    if (leftShiftedIndex < 0)
                    {
                        leftShiftedIndex = 0xFF;
                    }

                    if (rightShiftedIndex >= 16)
                    {
                        rightShiftedIndex = 0xFF;
                    }
                    
                    leftShiftTable[currentLocation] = (byte)leftShiftedIndex;
                    rightShiftTable[currentLocation] = (byte)rightShiftedIndex;

                    leftRotationTable[currentLocation] = (byte)leftRotatedIndex;
                    rightRotationTable[currentLocation] = (byte)rightRotatedIndex;

                    currentLocation++;
                }
            }
        }
    }
}
