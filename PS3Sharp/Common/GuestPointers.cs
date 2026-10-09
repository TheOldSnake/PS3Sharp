using PS3Sharp.Cell;

namespace PS3Sharp.Common
{

    /// <summary>
    /// Represents a typed pointer (T*) to memory in the emulated guest address space.
    /// </summary>
    /// <typeparam name="T">The type the pointer refers to.</typeparam>
    /// <remarks>
    /// This is not a native pointer. It stores a virtual address that must be
    /// resolved through the memory manager.<br/>
    /// Important: The address is stored in Little Endian byte order. Therefore,
    /// this type should never be stored inside PS3 structs where Big Endian
    /// byte order is required.<br/>
    /// This type can be converted to/from BigEndianGuestPtr implicitly.
    /// </remarks>

    internal readonly struct GuestPtr<T> : IFormattable, ISpanFormattable where T : unmanaged
    {
        internal readonly uint Address;
        internal bool IsNullPtr => Address == 0;
        internal GuestPtr(uint address)
        {
            Address = address;
        }

        // Implicit conversions
        public static implicit operator uint(GuestPtr<T> ptr) => ptr.Address;
        public static implicit operator GuestPtr<T>(uint address) => new(address);
        public static implicit operator GuestPtr<T>(BigEndian<uint> address) => new(address);
        public static implicit operator GuestPtr<T>(BigEndianGuestPtr<T> ptr) => new(ptr.Address);
        public static implicit operator BigEndianGuestPtr<T>(GuestPtr<T> ptr) => new(ptr.Address);

        // Formatting
        public override string ToString() => Address.ToString();
        public string ToString(string? format, IFormatProvider? formatProvider) => Address.ToString(format, formatProvider);
        public bool TryFormat(
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format,
            IFormatProvider? provider)
            => Address.TryFormat(destination, out charsWritten, format, provider);
    }

    /// <summary>
    /// Represents a void pointer (void*) to memory in the emulated guest address space.
    /// </summary>
    /// <remarks>
    /// This is not a native pointer. It stores a virtual address that must be
    /// resolved through the memory manager.<br/>
    /// Important: The address is stored in Little Endian byte order. Therefore,
    /// this type should never be stored inside PS3 structs where Big Endian
    /// byte order is required.<br/>
    /// This type can be converted to/from BigEndianGuestVoidPtr implicitly.
    /// </remarks>
    internal readonly struct GuestVoidPtr : IFormattable, ISpanFormattable
    {
        internal readonly uint Address;
        internal bool IsNullPtr => Address == 0;
        internal GuestVoidPtr(uint address)
        {
            Address = address;
        }

        // Implicit conversions
        public static implicit operator uint(GuestVoidPtr ptr) => ptr.Address;
        public static implicit operator GuestVoidPtr(uint address) => new(address);
        public static implicit operator GuestVoidPtr(BigEndian<uint> address) => new(address);
        public static implicit operator GuestVoidPtr(BigEndianGuestVoidPtr ptr) => new(ptr.Address);
        public static implicit operator BigEndianGuestVoidPtr(GuestVoidPtr ptr) => new(ptr.Address);

        // Formatting
        public override string ToString() => Address.ToString();
        public string ToString(string? format, IFormatProvider? formatProvider) => Address.ToString(format, formatProvider);
        public bool TryFormat(
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format,
            IFormatProvider? provider)
            => Address.TryFormat(destination, out charsWritten, format, provider);
    }


    /// <summary>
    /// Represents a typed pointer (T*) to guest memory where the address is stored in Big Endian order.
    /// </summary>
    /// <typeparam name="T">The type the pointer refers to.</typeparam>
    /// <remarks>
    /// This is not a native pointer. It stores a virtual address that must be
    /// resolved through the memory manager.<br/>
    /// The internal address is converted to host endianness when accessed through
    /// the BigEndian wrapper.<br/>
    /// This type can be converted to/from GuestPtr implicitly.<br/>
    /// Used in PS3 structures where data must be stored in Big Endian byte order.
    /// </remarks>

    internal readonly struct BigEndianGuestPtr<T> : IPS3Struct, IFormattable, ISpanFormattable where T : unmanaged
    {
        internal readonly BigEndian<uint> Address;
        internal bool IsNullPtr => (uint)Address == 0;

        internal BigEndianGuestPtr(uint address)
        {
            Address = address;
        }

        internal BigEndianGuestPtr(BigEndian<uint> address)
        {
            Address = address;
        }

        // Implicit conversions
        public static implicit operator uint(BigEndianGuestPtr<T> ptr) => ptr.Address;
        public static implicit operator BigEndianGuestPtr<T>(uint address) => new(address);
        public static implicit operator BigEndianGuestPtr<T>(BigEndian<uint> address) => new(address);

        // Formatting
        public override string ToString() => Address.ToString();
        public string ToString(string? format, IFormatProvider? formatProvider) => Address.ToString(format, formatProvider);
        public bool TryFormat(
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format,
            IFormatProvider? provider)
            => Address.TryFormat(destination, out charsWritten, format, provider);
    }

    /// <summary>
    /// Represents a void pointer (void*) to guest memory where the address is stored in Big Endian order.
    /// </summary>
    /// <remarks>
    /// This is not a native pointer. It stores a virtual address that must be
    /// resolved through the memory manager.<br/>
    /// The internal address is converted to host endianness when accessed through
    /// the BigEndian wrapper.<br/>
    /// This type can be converted to/from GuestVoidPtr implicitly.<br/>
    /// Used in PS3 structures where data must be stored in Big Endian byte order.
    /// </remarks>

    internal readonly struct BigEndianGuestVoidPtr : IPS3Struct, IFormattable, ISpanFormattable
    {
        internal readonly BigEndian<uint> Address;

        internal bool IsNullPtr => (uint)Address == 0;

        internal BigEndianGuestVoidPtr(uint address)
        {
            Address = address;
        }

        internal BigEndianGuestVoidPtr(BigEndian<uint> address)
        {
            Address = address;
        }

        // Implicit conversions
        public static implicit operator uint(BigEndianGuestVoidPtr ptr) => ptr.Address;
        public static implicit operator BigEndianGuestVoidPtr(uint address) => new(address);
        public static implicit operator BigEndianGuestVoidPtr(BigEndian<uint> address) => new(address);

        // Formatting
        public override string ToString() => Address.ToString();
        public string ToString(string? format, IFormatProvider? formatProvider) => Address.ToString(format, formatProvider);
        public bool TryFormat(
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format,
            IFormatProvider? provider)
            => Address.TryFormat(destination, out charsWritten, format, provider);
    }

    internal static class PS3PointersExtensions
    {
        //// Gets a native pointer to a struct in guest memory using the GuestPtr
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //internal static unsafe T* AsNativePtr<T>(this GuestPtr<T> ptr) where T : unmanaged, IPS3Struct
        //{
        //    return MemoryManager.GetPtr(ptr);
        //}

        //// Gets a managed reference to a struct in guest memory using the GuestPtr -- Avoid using this and prefer explicit T* with unsafe block
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //internal static ref T AsRef<T>(this GuestPtr<T> ptr) where T : unmanaged, IPS3Struct
        //{
        //    return ref MemoryManager.GetRef(ptr);
        //}

        //// Gets the struct by value from guest memory using the GuestPtr - This is an atomic read.
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //internal static T Read<T>(this GuestPtr<T> ptr) where T : unmanaged, IPS3Struct
        //{
        //    return MemoryManager.ReadStruct<T>(ptr);
        //}
    }
}
