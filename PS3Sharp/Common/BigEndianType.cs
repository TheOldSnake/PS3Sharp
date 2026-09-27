using PS3Sharp.Cell;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PS3Sharp.Common
{
    /// <summary>
    /// Represents a value stored in memory using Big Endian byte order.
    /// </summary>
    /// <typeparam name="T">An unmanaged value type.</typeparam>
    /// <remarks>
    /// The value is automatically converted to/from host endianness when read/written using the implicit operators.<br/>
    /// Useful for creating structs identical to those used by the PS3.<br/>
    /// </remarks>

    [StructLayout(LayoutKind.Sequential)]
    public struct BigEndian<T> : IPS3Struct, IFormattable, ISpanFormattable where T : unmanaged
    {
        private T _raw;

        // Gets the raw Big Endian value
        public T RawBE => _raw;

        // Converts from BigEndian<T> to the native integer type T automatically
        // (e.g., allows: int x = bigEndianValue;)
        public static implicit operator T(BigEndian<T> value) => Reverse(value._raw);

        // Converts from the native integer type T to BigEndian<T> automatically
        // (e.g., allows: BigEndian<int> bigEndianValue = 123;)
        public static implicit operator BigEndian<T>(T value) => new BigEndian<T>
        {
            _raw = Reverse(value)
        };

        private static T Reverse(T value)
        {
            // TODO: Find the most efficient way to do this
            int size = Unsafe.SizeOf<T>();

            T result = default;

            ref byte src = ref Unsafe.As<T, byte>(ref value);
            ref byte dst = ref Unsafe.As<T, byte>(ref result);

            switch (size)
            {
                // byte / sbyte, and bool
                // Nothing to reverse
                case 1:
                    return value;

                // short / ushort
                case 2:
                    Unsafe.WriteUnaligned(ref dst,
                        BinaryPrimitives.ReverseEndianness(
                            Unsafe.ReadUnaligned<ushort>(ref src)));
                    break;

                // int / uint
                case 4:
                    Unsafe.WriteUnaligned(ref dst,
                        BinaryPrimitives.ReverseEndianness(
                            Unsafe.ReadUnaligned<uint>(ref src)));
                    break;

                // long / ulong
                case 8:
                    Unsafe.WriteUnaligned(ref dst,
                        BinaryPrimitives.ReverseEndianness(
                            Unsafe.ReadUnaligned<ulong>(ref src)));
                    break;

                // Int128 / UInt128
                case 16:
                    Unsafe.WriteUnaligned(ref dst,
                        BinaryPrimitives.ReverseEndianness(
                            Unsafe.ReadUnaligned<UInt128>(ref src)));
                    break;

                default:
                    throw new NotSupportedException($"[BigEndian] Type {typeof(T)} not supported.");
            }

            return result;
        }

        public override string ToString()
        {
            T native = Reverse(_raw);
            return native.ToString() ?? string.Empty;
        }

        //All the supported types already implement IFormattable
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            T native = Reverse(_raw);
            return ((IFormattable)(object)native).ToString(format, formatProvider);
        }

        //All the supported types already implement ISpanFormattable
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            T native = Reverse(_raw);
            return ((ISpanFormattable)(object)native).TryFormat(destination, out charsWritten, format, provider);
        }
    }
}
