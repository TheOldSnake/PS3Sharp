using PS3Sharp.Cell;
using PS3Sharp.Common;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace PS3Sharp.OS.Modules {
    public static class CellSync2 {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CellSync2MutexAttribute : IPS3Struct {
            public BigEndian<uint> sdkVersion;
            public BigEndian<ushort> threadTypes;
            public BigEndian<ushort>  maxWaiters;
            public InlineData.InlineArray2<bool> Padding;
            public InlineData.InlineArray32<byte> name;
            public InlineData.InlineArray86<byte> reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CellSync2SemaphoreAttribute : IPS3Struct {
            public BigEndian<uint> sdkVersion;
            public BigEndian<ushort> threadTypes;
            public BigEndian<ushort>  maxWaiters;
            public InlineData.InlineArray32<byte> name;
            public InlineData.InlineArray88<byte> reserved;
        }
    }
}
