using System.Globalization;

namespace PS3Sharp.OS.NID {
    public static class NIDManager {
        public static Dictionary<uint, string> NIDList = ParseNIDsFile(@"OS\NID\NIDs.txt");
        public static bool NIDInitialized;

        public static Dictionary<uint, string> ParseNIDsFile(string path) {
            var dict = new Dictionary<uint, string>();

            foreach (var line in File.ReadLines(path)) {
                var trimmed = line.Trim();

                // Skip empty lines or comments
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) {
                    continue;
                }
                    
                // Split on space
                var parts = trimmed.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) {
                    continue;
                }

                // Parse hex uint (with/without 0x prefix)
                string hexPart = parts[0].StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? parts[0].Substring(2) : parts[0];
                uint key = uint.Parse(hexPart, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                // Everything after the number is the name
                string value = parts[1];

                dict[key] = value;
            }

            NIDInitialized = true;
            return dict;
        }

        public static uint GetNID(string name) {
            if (NIDList == null) {
                throw new Exception($"NID file was not parsed");
            }

            // Search 
            foreach (KeyValuePair<uint, string> pair in NIDList) {
                if (pair.Value == name) {
                    return pair.Key;
                }
            }

            throw new Exception($"Cannot find: {name} in NIDList");
        }
    }
}
