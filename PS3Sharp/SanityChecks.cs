using PS3Sharp.Cell.PPU;
using PS3Sharp.Cell.Rsx;
using PS3Sharp.Cell.Spu;
using PS3Sharp.Common;
using System;
using System.Collections.Generic;
using System.Text;
using static PS3Sharp.Cell.PPU.PPUThread;
using static PS3Sharp.Cell.Rsx.FragmentShaderGenerator;
using static PS3Sharp.Cell.Rsx.VertexShaderGenerator;

namespace PS3Sharp
{
    internal static class SanityChecks
    {
        internal static void DoSanityChecks()
        {
            CheckPPURegisterSizes();
            CheckSPURegisterSize();
            CheckPointerSize();
            CheckBigEndianSize();
            CheckInstructionSize();
        }

        private static void CheckPPURegisterSizes()
        {
            // PPU GPR and FPR are 64-bit
            // PPU VR is 128-bit

            unsafe
            {
                if (sizeof(PPUGPR) != sizeof(ulong))
                {
                    throw new InvalidOperationException($"Unexpected PPU GPR Size = {sizeof(PPUGPR)}");
                }

                if (sizeof(PPUFPR) != sizeof(ulong))
                {
                    throw new InvalidOperationException($"Unexpected PPU FPR Size = {sizeof(PPUFPR)}");
                }

                if (sizeof(PPUVR) != sizeof(UInt128))
                {
                    throw new InvalidOperationException($"Unexpected PPU VR Size = {sizeof(PPUVR)}");
                }
            }
        }

        private static void CheckSPURegisterSize()
        {
            // SPU register is 128-bit

            unsafe
            {
                if (sizeof(SpuRegister) != sizeof(UInt128))
                {
                    throw new InvalidOperationException($"Unexpected SPU GPR Size = {sizeof(SpuRegister)}");
                }
            }
        }

        private static void CheckPointerSize()
        {
            // All pointers are 4 bytes
            // GuestPtr<T> and BigEndianGuestPtr<T> does not store T and should still be 4 bytes

            unsafe
            {
                if (sizeof(GuestVoidPtr) != sizeof(uint))
                {
                    throw new InvalidOperationException($"Unexpected void pointer Size = {sizeof(GuestVoidPtr)}");
                }

                if (sizeof(BigEndianGuestVoidPtr) != sizeof(uint))
                {
                    throw new InvalidOperationException($"Unexpected void pointer Size = {sizeof(BigEndianGuestVoidPtr)}");
                }

                if (sizeof(GuestPtr<byte>) != sizeof(uint))
                {
                    throw new InvalidOperationException($"Unexpected byte pointer Size = {sizeof(GuestPtr<byte>)}");
                }

                if (sizeof(BigEndianGuestPtr<byte>) != sizeof(uint))
                {
                    throw new InvalidOperationException($"Unexpected big endian byte pointer Size = {sizeof(BigEndianGuestPtr<byte>)}");
                }
            }
        }

        private static void CheckBigEndianSize()
        {
            // Check commonly used types and ensure that sizeof(BigEndian<T> == sizeof(T))

            unsafe
            {
                if (sizeof(BigEndian<byte>) != sizeof(byte))
                {
                    throw new InvalidOperationException($"Unexpected BigEndian<byte> Size = {sizeof(BigEndian<byte>)}");
                }

                if (sizeof(BigEndian<sbyte>) != sizeof(sbyte))
                {
                    throw new InvalidOperationException($"Unexpected BigEndian<sbyte> Size = {sizeof(BigEndian<sbyte>)}");
                }

                if (sizeof(BigEndian<short>) != sizeof(short))
                {
                    throw new InvalidOperationException($"Unexpected BigEndian<short> Size = {sizeof(BigEndian<short>)}");
                }

                if (sizeof(BigEndian<ushort>) != sizeof(ushort))
                {
                    throw new InvalidOperationException($"Unexpected BigEndian<ushort> Size = {sizeof(BigEndian<ushort>)}");
                }

                if (sizeof(BigEndian<int>) != sizeof(int))
                {
                    throw new InvalidOperationException($"Unexpected BigEndian<int> Size = {sizeof(BigEndian<int>)}");
                }

                if (sizeof(BigEndian<uint>) != sizeof(uint))
                {
                    throw new InvalidOperationException($"Unexpected BigEndian<uint> Size = {sizeof(BigEndian<uint>)}");
                }

                if (sizeof(BigEndian<long>) != sizeof(long))
                {
                    throw new InvalidOperationException($"Unexpected BigEndian<long> Size = {sizeof(BigEndian<long>)}");
                }

                if (sizeof(BigEndian<ulong>) != sizeof(ulong))
                {
                    throw new InvalidOperationException($"Unexpected BigEndian<ulong> Size = {sizeof(BigEndian<ulong>)}");
                }

                if (sizeof(BigEndian<Int128>) != sizeof(Int128))
                {
                    throw new InvalidOperationException($"Unexpected BigEndian<Int128> Size = {sizeof(BigEndian<Int128>)}");
                }

                if (sizeof(BigEndian<UInt128>) != sizeof(UInt128))
                {
                    throw new InvalidOperationException($"Unexpected BigEndian<UInt128> Size = {sizeof(BigEndian<UInt128>)}");
                }
            }
        }

        private static void CheckInstructionSize()
        {
            unsafe
            {
                if (sizeof(PPUInstruction) != sizeof(uint))
                {
                    throw new InvalidOperationException($"Unexpected PPUInstruction Size = {sizeof(PPUInstruction)}");
                }

                if (sizeof(SpuInstruction) != sizeof(uint))
                {
                    throw new InvalidOperationException($"Unexpected SpuInstruction Size = {sizeof(SpuInstruction)}");
                }

                if (sizeof(RealitySynthesizer.RsxCommand) != sizeof(uint))
                {
                    throw new InvalidOperationException($"Unexpected RSXCommand Size = {sizeof(RealitySynthesizer.RsxCommand)}");
                }

                if (sizeof(VertexShaderGenerator.VertexInstruction) != (sizeof(uint) * 4))
                {
                    throw new InvalidOperationException($"Unexpected VertexInstruction Size = {sizeof(VertexInstruction)}");
                }

                if (sizeof(FragmentShaderGenerator.FragmentInstruction) != (sizeof(uint) * 4))
                {
                    throw new InvalidOperationException($"Unexpected FragmentInstruction Size = {sizeof(FragmentInstruction)}");
                }
            }
        }
    }
}