using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PS3Sharp.SFOLoader
{
    internal sealed class SFO
    {
        // Little Endian

        [StructLayout(LayoutKind.Sequential)]
        internal struct SFOHeader
        {
            internal uint Magic;
            internal uint Version;
            internal uint KeyTableOffset;
            internal uint DataTableOffset;
            internal uint EntriesCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SFOIndexEntry
        {
            internal ushort KeyOffset;
            internal ushort ParamFormat;
            internal uint ParamLength;
            internal uint ParamMaxLength;
            internal uint DataOffset;
        }

        internal const uint MagicValue = 0x46535000; // "\0PSF"
        internal const ushort ParamFormatString = 0x004;
        internal const ushort ParamFormatArray = 0x204;
        internal const ushort ParamFormatUInt = 0x404;

        private readonly Dictionary<string, uint> _uintDictionary;
        private readonly Dictionary<string, string> _stringsDictionary;

        internal string TitleID => _stringsDictionary["TITLE_ID"];
        internal string Title => _stringsDictionary["TITLE"];

        private SFO(Dictionary<string, uint> uints, Dictionary<string, string> strings)
        {
            _uintDictionary = uints;
            _stringsDictionary = strings;
        }

        internal bool UIntLookup(string key, out uint value) => _uintDictionary.TryGetValue(key, out value);

        internal bool StringLookup(string key, out string value) => _stringsDictionary.TryGetValue(key, out value);

        internal static SFO? LoadSFO(string path)
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
