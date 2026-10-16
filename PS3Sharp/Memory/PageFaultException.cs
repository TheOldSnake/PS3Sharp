using System.Runtime.Serialization;

namespace PS3Sharp.Memory
{
    internal sealed class PageFaultException : Exception
    {
        internal uint VirtualAddress { get; }

        internal PageFaultException(uint address)
            : base($"Unmapped virtual address: 0x{address:X8}")
        {
            VirtualAddress = address;
        }

        internal PageFaultException(uint address, Exception inner)
            : base($"Unmapped virtual address: 0x{address:X8}", inner)
        {
            VirtualAddress = address;
        }

        protected PageFaultException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            VirtualAddress = info.GetUInt32(nameof(VirtualAddress));
        }
    }
}