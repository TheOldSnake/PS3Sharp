using PS3Sharp.Memory_Manager;
using System.Text;

namespace PS3Sharp.Common
{
    internal struct GuestString
    {
        internal readonly uint Address;   // char* -- native endianness
        internal bool IsNullString => Address == 0;
        internal GuestString(uint address)
        {
            Address = address;
        }

        // Implicit conversions

        // To address
        public static implicit operator uint(GuestString ptr) => ptr.Address;

        // From address
        public static implicit operator GuestString(uint address) => new(address);

        // To C# string
        public static implicit operator string(GuestString ptr) => MemoryManager.ReadString(ptr.Address);

        // To byte[]
        public static implicit operator byte[](GuestString ptr) => Encoding.UTF8.GetBytes(MemoryManager.ReadString(ptr.Address));
    }
}
