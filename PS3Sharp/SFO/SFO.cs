using System.Collections.ObjectModel;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PS3Sharp.SFOLoader
{
    public sealed class SFO
    {
        // Little Endian

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SFOHeader
        {
            public uint Magic;
            public uint Version;
            public uint KeyTableOffset;
            public uint DataTableOffset;
            public uint EntriesCount;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SFOIndexEntry
        {
            public ushort KeyOffset;
            public ushort ParamFormat;
            public uint ParamLength;
            public uint ParamMaxLength;
            public uint DataOffset;
        }

        public const uint MagicValue = 0x46535000; // "\0PSF"
        public const ushort ParamFormatString = 0x004;
        public const ushort ParamFormatArray = 0x204;
        public const ushort ParamFormatUInt = 0x404;

        private readonly Dictionary<string, uint> UIntsDictionary;
        private readonly Dictionary<string, string> StringsDictionary;

        private SFO(Dictionary<string, uint> uints, Dictionary<string, string> strings)
        {
            UIntsDictionary = uints;
            StringsDictionary = strings;
        }

        public string TitleID => StringsDictionary["TITLE_ID"];
        public string Title => StringsDictionary["TITLE"];
        public bool UIntLookup(string key, out uint value) => UIntsDictionary.TryGetValue(key, out value);
        public bool StringLookup(string key, out string value) => StringsDictionary.TryGetValue(key, out value);

        public static SFO Load(string path)
        {
            if (!File.Exists(path) || !path.EndsWith(".SFO"))
            {
                Console.WriteLine($"[SFO] Invalid Path: {path}");
                return null;
            }

            ReadOnlySpan<byte> sfo = File.ReadAllBytes(path);
            SFOHeader header = MemoryMarshal.Read<SFOHeader>(sfo);

            if (header.Magic != MagicValue)
            {
                throw new Exception($"[SFO] Magic value check failed");
            }

            Console.WriteLine($"[SFO] Version = 0x{header.Version:X8}");
            Console.WriteLine($"[SFO] KeyTableOffset = 0x{header.KeyTableOffset:X8}");
            Console.WriteLine($"[SFO] DataTableOffset = 0x{header.DataTableOffset:X8}");
            Console.WriteLine($"[SFO] EntriesCount = 0x{header.EntriesCount:X8}");

            Dictionary<string, uint> UIntsDictionary = [];
            Dictionary<string, string> StringsDictionary = [];

            int headerSize = Unsafe.SizeOf<SFOHeader>();
            int indexEntrySize = Unsafe.SizeOf<SFOIndexEntry>();

            for (int i = 0; i < header.EntriesCount; i++)
            {

                // Read entry
                int entryOffset = headerSize + (indexEntrySize * i);
                SFOIndexEntry entry = MemoryMarshal.Read<SFOIndexEntry>(sfo.Slice(entryOffset));

                // Read key
                int keyOffset = (int)(header.KeyTableOffset + entry.KeyOffset);
                string key = ReadString(sfo, keyOffset);

                // Read data, and add it to the dictionary
                int valueOffset = (int)(header.DataTableOffset + entry.DataOffset);

                switch (entry.ParamFormat)
                {
                    case ParamFormatArray:
                    case ParamFormatString:
                        string stringValue = ReadString(sfo, valueOffset);
                        StringsDictionary[key] = stringValue;
                        //Console.WriteLine($"Key = {key} value = {stringValue}");
                        break;

                    case ParamFormatUInt:
                        uint uintValue = MemoryMarshal.Read<uint>(sfo.Slice(valueOffset));
                        UIntsDictionary[key] = uintValue;
                        //Console.WriteLine($"Key = {key} value = 0x{uintValue:X8}");
                        break;
                }
            }

            return new SFO(UIntsDictionary, StringsDictionary);
        }

        private static string ReadString(ReadOnlySpan<byte> sfo, int offset)
        {
            ReadOnlySpan<byte> keyStart = sfo.Slice(offset);
            int keyLength = keyStart.IndexOf((byte)0);  //null terminator

            ReadOnlySpan<byte> key = keyStart.Slice(0, keyLength);
            string keyString = Encoding.UTF8.GetString(key);

            return keyString;
        }
    }
}
