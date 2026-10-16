using System.Collections.Immutable;
using System.Globalization;

namespace PS3Sharp.OS.NID
{
    internal static class NIDManager
    {
        private static bool _nidInitialized;
        internal static ImmutableDictionary<uint, string> NIDList { get; } = ParseNIDsFile(@"OS\NID\NIDs.txt").ToImmutableDictionary();

        internal static Dictionary<uint, string> ParseNIDsFile(string path)
        {
            Dictionary<uint, string> dict = new();

            foreach (string line in File.ReadLines(path))
            {
                string trimmed = line.Trim();

                // Skip empty lines or comments
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                {
                    continue;
                }

                // Split on space
                string[] parts = trimmed.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    continue;
                }

                // Parse hex uint (with/without 0x prefix)
                string hexPart = parts[0].StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? parts[0].Substring(2) : parts[0];
                uint key = uint.Parse(hexPart, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                // Everything after the number is the name
                string value = parts[1];

                dict[key] = value;
            }

            _nidInitialized = true;
            return dict;
        }

        internal static uint GetNID(string name)
        {
            if (NIDList == null)
            {
                throw new Exception($"NID file was not parsed");
            }

            // Search 
            foreach (KeyValuePair<uint, string> pair in NIDList)
            {
                if (pair.Value == name)
                {
                    return pair.Key;
                }
            }

            throw new Exception($"Cannot find: {name} in NIDList");
        }
    }
}
