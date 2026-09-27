using PS3Sharp.Cell;
using PS3Sharp.Common;
using System.Runtime.InteropServices;

namespace PS3Sharp.OS.Modules
{
    public static class CellSync2
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct CellSync2MutexAttribute : IPS3Struct
        {
            public BigEndian<uint> sdkVersion;
            public BigEndian<ushort> threadTypes;
            public BigEndian<ushort> maxWaiters;
            public InlineData.InlineArray2<bool> Padding;
            public InlineData.InlineArray32<byte> name;
            public InlineData.InlineArray86<byte> reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CellSync2SemaphoreAttribute : IPS3Struct
        {
            public BigEndian<uint> sdkVersion;
            public BigEndian<ushort> threadTypes;
            public BigEndian<ushort> maxWaiters;
            public InlineData.InlineArray32<byte> name;
            public InlineData.InlineArray88<byte> reserved;
        }
    }
}
