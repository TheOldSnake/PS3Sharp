using PS3Sharp.Cell.PPU;
using PS3Sharp.Memory;
using PS3Sharp.Common;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal class CellSsl
    {
        internal const string CertificatesPath = @"dev_flash\data\cert";
        private readonly MemoryManager _memoryManager;
        private readonly Dictionary<string, byte[]> _certificatesData;

        internal CellSsl(MemoryManager memoryManager)
        {
            _memoryManager = memoryManager;
            _certificatesData = LoadCertificatesData();
        }

        internal ulong CellSslCertificateLoader(PPUThread ppu)
        {
            ulong flag = ppu.ReadArg<ulong>(Arg0);
            uint bufPtr = ppu.ReadArg<uint>(Arg1);
            uint size = ppu.ReadArg<uint>(Arg2);
            uint reqPtr = ppu.ReadArg<uint>(Arg3);

            // Calculate total size required for the requested certificates
            if (bufPtr == 0)
            {
                uint total = 0;

                for (int i = 0; i < 64; i++)
                {
                    if (((flag >> i) & 1) != 0)
                    {
                        total += (uint)GetCertificateSize(i + 1);
                    }
                }

                _memoryManager.Write<BigEndian<uint>>(reqPtr, total);
                return CellOk;
            }

            // Copy certificates to buffer
            _memoryManager.ClearMemory(bufPtr, (int)size);
            uint offset = 0;

            for (int i = 0; i < 64; i++)
            {
                if (((flag >> i) & 1) != 0)
                {
                    byte[] data = GetCertificateData(i + 1);
                    _memoryManager.CopyBufferToVirtualMemory(data, bufPtr + offset);
                    offset += (uint)data.Length;
                }
            }
            
            return CellOk;
        }

        private static Dictionary<string, byte[]> LoadCertificatesData()
        {
            Dictionary<string, byte[]> certificatesData = [];
            DirectoryInfo directoryInfo = new(CertificatesPath);
            IEnumerable<FileInfo> files = directoryInfo.EnumerateFiles();

            foreach (FileInfo file in files)
            {
                byte[] data = File.ReadAllBytes(file.FullName);
                certificatesData.Add(file.Name, data);
            }

            return certificatesData;
        }

        private int GetCertificateSize(int id)
        {
            string name = $"CA{id:D2}.cer";

            if (_certificatesData.TryGetValue(name, out byte[]? data))
            {
                return data.Length;
            }
            else
            {
                throw new InvalidOperationException($"[CellSsl] Can't find certificate {name}");
            }
        }

        private byte[] GetCertificateData(int id)
        {
            string name = $"CA{id:D2}.cer";

            if (_certificatesData.TryGetValue(name, out byte[]? data))
            {
                return data;
            }
            else
            {
                throw new InvalidOperationException($"[CellSsl] Can't find certificate {name}");
            }
        }
    }
}
