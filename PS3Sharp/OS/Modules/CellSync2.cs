using PS3Sharp.Cell;
using PS3Sharp.Common;
using System.Runtime.InteropServices;

namespace PS3Sharp.OS.Modules
{
    internal static class CellSync2
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct CellSync2MutexAttribute : IPS3Struct
        {
            internal BigEndian<uint> sdkVersion;
            internal BigEndian<ushort> threadTypes;
            internal BigEndian<ushort> maxWaiters;
            internal InlineData.InlineArray2<bool> Padding;
            internal InlineData.InlineArray32<byte> name;
            internal InlineData.InlineArray86<byte> reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CellSync2SemaphoreAttribute : IPS3Struct
        {
            internal BigEndian<uint> sdkVersion;
            internal BigEndian<ushort> threadTypes;
            internal BigEndian<ushort> maxWaiters;
            internal InlineData.InlineArray32<byte> name;
            internal InlineData.InlineArray88<byte> reserved;
        }
    }
}
