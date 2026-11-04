using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.Spu
{
    internal static class SpuTables
    {
        [InlineArray(ShiftCount)]
        internal struct ShiftInlineArray
        {
            private byte _element;
        }

        [InlineArray(RotationCount)]
        internal struct RotationiInlineArray
        {
            private byte _element;
        }

        [InlineArray(FsmbSelectorSize)]
        internal struct FsmbInlineArray
        {
            private UInt128 _element;
        }

        [InlineArray(FsmSelectorSize)]
        internal struct FsmInlineArray
        {
            private UInt128 _element;
        }

        private const int ShiftCount = 16 * 16;       // 16 shifts of 16-bytes
        private const int RotationCount = 16 * 16;    // 16 rotations of 16-bytes
        private const int FsmbSelectorSize = 0x10000; // 16-bit selector
        private const int FsmSelectorSize = 16;       // 4-bit selector

        private static ShiftInlineArray _leftShiftTable;
        private static ShiftInlineArray _rightShiftTable;
        private static RotationiInlineArray _leftRotationTable;
        private static RotationiInlineArray _rightRotationTable;
        private static FsmInlineArray _fsmTable;
        private static FsmbInlineArray _fsmbTable;

        // Exposed ROS
        internal static ReadOnlySpan<byte> LeftShiftTable => _leftShiftTable;
        internal static ReadOnlySpan<byte> RightShiftTable => _rightShiftTable;
        internal static ReadOnlySpan<byte> LeftRotationTable => _leftRotationTable;
        internal static ReadOnlySpan<byte> RightRotationTable => _rightRotationTable;

        // Exposed ref byte (it is faster to pass the span in MemoryMarshal.GetReference())
        internal static ref byte LeftShiftTableRef => ref MemoryMarshal.GetReference(LeftShiftTable);
        internal static ref byte RightShiftTableRef => ref MemoryMarshal.GetReference(RightShiftTable);
        internal static ref byte LeftRotationTableRef => ref MemoryMarshal.GetReference(LeftRotationTable);
        internal static ref byte RightRotationTableRef => ref MemoryMarshal.GetReference(RightRotationTable);

        internal static ReadOnlySpan<UInt128> FsmbTable => _fsmbTable;
        internal static ReadOnlySpan<UInt128> FsmTable => _fsmTable;

        internal static void InitializeStaticTables()
        {
            InitializeFsmbTable();
            InitializeFsmTable();
            InitializeLeftShiftTable();
            InitializeRightShiftTable();
            InitializeLeftRotationTable();
            InitializeRightRotationTable();
        }

        private static void InitializeFsmbTable()
        {
            Span<UInt128> fsmbTable = _fsmbTable;
            Span<byte> fsmbBytes = MemoryMarshal.Cast<UInt128, byte>(fsmbTable);
            int currentLocation = 0;

            // Generate a 128-bit value for each selector
            for (int selector = 0; selector < FsmbSelectorSize; selector++)
            {
                for (int bit = 0; bit < 16; bit++)
                {
                    bool isSet = ((selector >> bit) & 1) != 0;
                    byte value = (byte)(isSet ? 0xFF : 0x00);
                    fsmbBytes[currentLocation++] = value;
                }
            }
        }

        private static void InitializeFsmTable()
        {
            Span<UInt128> fsmTable = _fsmTable;
            Span<uint> fsmWords = MemoryMarshal.Cast<UInt128, uint>(fsmTable);
            int currentLocation = 0;

            // Generate a 128-bit value for each selector
            for (int selector = 0; selector < FsmSelectorSize; selector++)
            {
                for (int bit = 0; bit < 4; bit++)
                {
                    bool isSet = ((selector >> bit) & 1) != 0;
                    uint value = isSet ? 0xFFFFFFFF : 0x00000000;
                    fsmWords[currentLocation++] = value;
                }
            }
        }

        private static void InitializeLeftShiftTable()
        {
            // For each shift, generate a shuffle vector describing the new indices
            int shiftCount = 16;
            int indexCount = 16;
            int currentLocation = 0;

            // Notably, this table depends on x86 PSHUFB semantics
            Span<byte> table = _leftShiftTable;

            for (int shift = 0; shift < shiftCount; shift++)
            {
                for (int index = 0; index < indexCount; index++)
                {
                    int newIndex = index - shift;

                    if (newIndex < 0)
                    {
                        newIndex = 0xFF;
                    }

                    table[currentLocation++] = (byte)newIndex;
                }
            }
        }

        private static void InitializeRightShiftTable()
        {
            // For each shift, generate a shuffle vector describing the new indices
            int shiftCount = 16;
            int indexCount = 16;
            int currentLocation = 0;

            // Notably, this table depends on x86 PSHUFB semantics
            Span<byte> table = _rightShiftTable;

            for (int shift = 0; shift < shiftCount; shift++)
            {
                for (int index = 0; index < indexCount; index++)
                {
                    int newIndex = index + shift;

                    if (newIndex >= 16)
                    {
                        newIndex = 0xFF;
                    }

                    table[currentLocation++] = (byte)newIndex;
                }
            }
        }

        private static void InitializeLeftRotationTable()
        {
            // For each rotation, generate a shuffle vector describing the new indices
            int rotationsCount = 16;
            int indexCount = 16;
            int currentLocation = 0;

            for (int rotation = 0; rotation < rotationsCount; rotation++)
            {
                for (int index = 0; index < indexCount; index++)
                {
                    byte value = (byte)((index + rotation) & 0xF);
                    _leftRotationTable[currentLocation++] = value;
                }
            }
        }

        private static void InitializeRightRotationTable()
        {
            // For each rotation, generate a shuffle vector describing the new indices
            int rotationsCount = 16;
            int indexCount = 16;
            int currentLocation = 0;

            for (int rotation = 0; rotation < rotationsCount; rotation++)
            {
                for (int index = 0; index < indexCount; index++)
                {
                    byte value = (byte)((index - rotation) & 0xF);
                    _rightRotationTable[currentLocation++] = value;
                }
            }
        }
    }
}
