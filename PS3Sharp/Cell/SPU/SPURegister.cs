using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PS3Sharp.Cell.SPU
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SPURegister
    {
        internal UInt128 Value128;
        internal Span<byte> Bytes => MemoryMarshal.AsBytes(Quads);
        internal Span<short> Shorts => MemoryMarshal.Cast<byte, short>(Bytes);
        internal Span<ushort> UShorts => MemoryMarshal.Cast<byte, ushort>(Bytes);
        internal Span<int> Ints => MemoryMarshal.Cast<byte, int>(Bytes);
        internal Span<uint> UInts => MemoryMarshal.Cast<byte, uint>(Bytes);
        internal Span<float> Floats => MemoryMarshal.Cast<byte, float>(Bytes);
        internal Span<long> Longs => MemoryMarshal.Cast<byte, long>(Bytes);
        internal Span<ulong> ULongs => MemoryMarshal.Cast<byte, ulong>(Bytes);
        internal Span<double> Doubles => MemoryMarshal.Cast<byte, double>(Bytes);
        internal Span<UInt128> Quads => MemoryMarshal.CreateSpan(ref Value128, 1);
    }

    [InlineArray(128)]
    internal struct SPURegisterArray
    {
        private SPURegister _element;
    }
}
