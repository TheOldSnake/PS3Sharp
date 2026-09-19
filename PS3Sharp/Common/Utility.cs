using System.Diagnostics;
using System.Text;
using System.IO.Hashing;
using SkiaSharp;
using PS3Sharp.Memory_Manager;

namespace PS3Sharp.Common {
    public static class Utility {
        public static long TimeBaseStart = Stopwatch.GetTimestamp();
        public static double TimeBaseFreq = Stopwatch.Frequency;
        //public const ulong TB_FREQUENCY = 80000000; //80 Mhz

        // todo clean this
        public static ulong ReadTime_Microseconds() { 

            return GetCurrentMicrosecondSince1970();

            long ticks = Stopwatch.GetTimestamp();
            long microseconds = ticks * 1_000_000 / Stopwatch.Frequency;
            return (ulong)microseconds;

            //return (ulong)((Stopwatch.GetTimestamp() - TimeBaseStart) * (TB_FREQUENCY / TimeBaseFreq));
        }

        public static string ToString(ReadOnlySpan<byte> c_str) { 
            return Encoding.UTF8.GetString(c_str);
        }

        public static Span<byte> ToCString(string str) {

            if (!str.EndsWith('\0')) {
                str += '\0';
            }

            return Encoding.UTF8.GetBytes(str);
        }

        public static ulong GetCurrentTickSince1970() {
            long ticks = DateTimeOffset.UtcNow.UtcTicks - 621355968000000000L; //in 100-nanosecond ticks
            return (ulong)ticks;    
        }

        public static ulong GetCurrentNanosecondsSince1970() {
            ulong ticks = GetCurrentTickSince1970();
            return ticks * 100;    
        }

        public static ulong GetCurrentMicrosecondSince1970() {
            ulong ticks = GetCurrentTickSince1970();
            return ticks / 10;    
        }

        public static ulong GetCurrentMillisecondSince1970() {
            //We can get it directly
            long value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return (ulong)value;    
        }

        public static ulong GetCurrentSecondsSince1970() {
            //We can get it directly
            long value = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return (ulong)value;    
        }

        public static string ToHostPath(string guestPath) {
            //Assuming absolute paths
            if (!guestPath.StartsWith('/')) {
                throw new Exception($"[FileSystem] path does not start with / -- path: {guestPath}");
            }

            //Swap '/' with '\' and remove the first one
            return guestPath.Replace($"/", $"\\").Substring(1);
        }

        public static ulong Hash(Span<byte> data) {
            return BitConverter.ToUInt64(XxHash64.Hash(data));
        }

        public static void DumpMemoryAsImage(uint address, int width, int height, string path) { // As BRGA8888
            var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

            unsafe {
                Span<byte> data = MemoryManager.CreateSpan<byte>(address, width * height * 4);

                for (int y = 0; y < height; y++) {
                    for (int x = 0; x < width; x++) {

                        int offset = (y * width + x) * 4;

                        byte a = data[offset + 0];
                        byte r = data[offset + 1];
                        byte g = data[offset + 2];
                        byte b = data[offset + 3];

                        bitmap.SetPixel(x, y, new SKColor(r, g, b, a));
                    }
                }
            }

            var image = SKImage.FromBitmap(bitmap);
            var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(path, encoded.ToArray());
        }
    }
}
