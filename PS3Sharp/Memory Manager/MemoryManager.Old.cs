using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace PS3Sharp.Memory_Manager {
    public  static partial class MemoryManager {
        // Unused functions that depends on the older paging system 
        
        /* 
         
         public static bool IsWithinSingleBlock(uint virtualAddress, uint length) {
            // Check if "virtualAddress + length" exceeds the block end
            MemoryBlock ownerBlock = GetBlockInfo(virtualAddress);
            return ownerBlock.End > virtualAddress + length;
        }


          public static void ClearMemory(uint virtualAddress, uint count, byte value = 0) {
            if (count == 0) {
                return;
            }


            if (IsWithinSingleBlock(virtualAddress, count)) {
                // Direct path
                void* dest = GetNativeAddress((uint)virtualAddress);
                NativeMemory.Fill(dest, count, value);

            } else {
                // Clear is across multiple blocks
                int remainingBytes = (int)count;
                uint currentAddress = virtualAddress;
                while (remainingBytes > 0) {

                    // Get the current block info
                    MemoryBlock currentBlock = GetBlockInfo(currentAddress);

                    void* dest = (byte*)GetNativeAddress(currentAddress);

                    // Calculate how much we can clear
                    int bytesUntilBlockEnd = (int)(currentBlock.End - currentAddress);
                    int bytesToClear = Math.Min(remainingBytes, bytesUntilBlockEnd);

                    // Clear
                    NativeMemory.Fill(dest, (uint)bytesToClear, value);

                    // Advance the address
                    currentAddress += (uint)bytesToClear;
                    remainingBytes -= bytesToClear;
                }
            }
        } 

        public static void CopyBufferToVirtualMemory(ReadOnlySpan<byte> source, uint destinationVirtualAddress) {
            // A virtual memory block is contiguous in native memory,
            // but adjacent virtual blocks are not necessarily contiguous.

            uint currentAddress = destinationVirtualAddress;

            while (!source.IsEmpty) {
                // Get the current block info
                MemoryBlock currentBlock = GetBlockInfo(currentAddress);

                byte* nativeDestination = (byte*)GetNativeAddress(currentAddress);

                // Calculate how much we can copy. MemoryBlock.End is exclusive.
                int bytesUntilBlockEnd = checked((int)(currentBlock.End - currentAddress));
                int bytesToCopy = Math.Min(source.Length, bytesUntilBlockEnd);

                // Copy
                Span<byte> destSpan = new Span<byte>(nativeDestination, bytesToCopy);
                source[..bytesToCopy].CopyTo(destSpan);

                // Advance the span and the address
                source = source[bytesToCopy..];
                currentAddress += (uint)bytesToCopy;
            }
        }

        public static void ReadBufferFromVirtualMemory(uint sourceVirtualAddress, Span<byte> destination) {
            // A virtual memory block is contiguous in native memory,
            // but adjacent virtual blocks are not necessarily contiguous.

            uint currentAddress = sourceVirtualAddress;

            while (!destination.IsEmpty) {

                // Get the current block info
                MemoryBlock currentBlock = GetBlockInfo(currentAddress);

                byte* nativeSource = (byte*)GetNativeAddress(currentAddress);

                // Calculate how much we can copy. MemoryBlock.End is exclusive.
                int bytesUntilBlockEnd = (int)(currentBlock.End - currentAddress);
                int bytesToCopy = Math.Min(destination.Length, bytesUntilBlockEnd);

                // Copy
                ReadOnlySpan<byte> sourceSpan = new ReadOnlySpan<byte>(nativeSource, bytesToCopy);
                sourceSpan.CopyTo(destination[..bytesToCopy]);

                // Advance the span and the address
                destination = destination[bytesToCopy..];
                currentAddress += (uint)bytesToCopy;
            }
        }

        public static void CopyVirtualMemory(uint sourceVirtualAddress, uint destinationVirtualAddress, int byteCount) {
            // A virtual memory block is contiguous in native memory,
            // but adjacent virtual blocks are not necessarily contiguous.

            uint currentSourceAddress = sourceVirtualAddress;
            uint currentDestinationAddress = destinationVirtualAddress;
            int remainingBytes = byteCount;

            while (remainingBytes > 0) {

                // Get the source and dest blocks info
                MemoryBlock sourceBlock = GetBlockInfo(currentSourceAddress);
                MemoryBlock destinationBlock = GetBlockInfo(currentDestinationAddress);

                byte* nativeSource = (byte*)GetNativeAddress(currentSourceAddress);
                byte* nativeDestination = (byte*)GetNativeAddress(currentDestinationAddress);

                // Calculate how much we can copy. MemoryBlock.End is exclusive.
                int sourceBytesAvailable = (int)(sourceBlock.End - currentSourceAddress);
                int destinationBytesAvailable = (int)(destinationBlock.End - currentDestinationAddress);

                int copyAmount = Math.Min(
                    remainingBytes,
                    Math.Min(sourceBytesAvailable, destinationBytesAvailable));

                // Copy
                ReadOnlySpan<byte> sourceSpan = new ReadOnlySpan<byte>(nativeSource, copyAmount);
                Span<byte> destSpan = new Span<byte>(nativeDestination, copyAmount);
                sourceSpan.CopyTo(destSpan);

                // Advance source and dest 
                currentSourceAddress += (uint)copyAmount;
                currentDestinationAddress += (uint)copyAmount;
                remainingBytes -= copyAmount;
            }
        }    


         public static ReadOnlySpan<T> ReadSpanFromVirtualMemory<T>(uint virtualAddress, int length, out bool wasCopied) where T : unmanaged, IPS3Struct {
            int byteSize = length * sizeof(T);
            Span<byte> data;

            // Get data
            if (IsWithinSingleBlock(virtualAddress, (uint)byteSize)) {
                void* nativeAddress = GetNativeAddress(virtualAddress);
                data = new Span<byte>(nativeAddress, byteSize);
                wasCopied = false;

            } else {
                data = new byte[byteSize];
                ReadBufferFromVirtualMemory(virtualAddress, data);
                wasCopied = true;
            }            

            // Cast
            return MemoryMarshal.Cast<byte, T>(data);
        }


         */

    }
}
