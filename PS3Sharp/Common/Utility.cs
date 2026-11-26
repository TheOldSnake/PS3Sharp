using PS3Sharp.GuestMemory;
using SkiaSharp;
using System.Diagnostics;
using System.IO.Hashing;
using System.Text;

namespace PS3Sharp.Common
{
    internal static class Utility
    {
        internal static long TimeBaseStart = Stopwatch.GetTimestamp();
        internal static double TimeBaseFreq = Stopwatch.Frequency;
        //internal const ulong TB_FREQUENCY = 80000000; //80 Mhz

        // todo clean this
        internal static ulong ReadTime_Microseconds()
        {

            return GetCurrentMicrosecondSince1970();

            long ticks = Stopwatch.GetTimestamp();
            long microseconds = ticks * 1_000_000 / Stopwatch.Frequency;
            return (ulong)microseconds;

            //return (ulong)((Stopwatch.GetTimestamp() - TimeBaseStart) * (TB_FREQUENCY / TimeBaseFreq));
        }

        internal static ulong AlignPowerOfTwo(ulong value, ulong alignment)
        {
            bool isPowerOf2 = alignment != 0 && (alignment & (alignment - 1)) == 0;

            if (!isPowerOf2)
            {
                throw new Exception($"Alignment is not power of 2");
            }

            return (value + alignment - 1) & ~(alignment - 1);
        }

        internal static uint Align(uint value, uint alignment)
        {
            uint remainder = value % alignment;
            return remainder == 0 ? value : (value + alignment - remainder);
        }

        internal static string ToString(ReadOnlySpan<byte> c_str)
        {
            return Encoding.UTF8.GetString(c_str);
        }

        internal static Span<byte> ToCString(string str)
        {

            if (!str.EndsWith('\0'))
            {
                str += '\0';
            }

            return Encoding.UTF8.GetBytes(str);
        }

        internal static ulong GetCurrentTickSince1970()
        {
            long ticks = DateTimeOffset.UtcNow.UtcTicks - 621355968000000000L; //in 100-nanosecond ticks
            return (ulong)ticks;
        }

        internal static ulong GetCurrentNanosecondsSince1970()
        {
            ulong ticks = GetCurrentTickSince1970();
            return ticks * 100;
        }

        internal static ulong GetCurrentMicrosecondSince1970()
        {
            ulong ticks = GetCurrentTickSince1970();
            return ticks / 10;
        }

        internal static ulong GetCurrentMillisecondSince1970()
        {
            //We can get it directly
            long value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            return (ulong)value;
        }

        internal static ulong GetCurrentSecondsSince1970()
        {
            //We can get it directly
            long value = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return (ulong)value;
        }

        internal static string ToHostPath(string guestPath)
        {
            //Assuming absolute paths
            if (!guestPath.StartsWith('/'))
            {
                throw new Exception($"[FileSystem] path does not start with / -- path: {guestPath}");
            }

            //Swap '/' with '\' and remove the first one
            return guestPath.Replace($"/", $"\\").Substring(1);
        }

        internal static ulong Hash(Span<byte> data)
        {
            return BitConverter.ToUInt64(XxHash64.Hash(data));
        }

        internal static void DumpMemoryAsImage(uint address, int width, int height, string path, MemoryManager memoryManager)
        { 
            SKBitmap bitmap = new(width, height, SKColorType.Bgra8888, SKAlphaType.Premul); // As BRGA8888

            unsafe
            {
                Span<byte> data = memoryManager.CreateByteSpan(address, width * height * 4);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {

                        int offset = (y * width + x) * 4;

                        byte a = data[offset + 0];
                        byte r = data[offset + 1];
                        byte g = data[offset + 2];
                        byte b = data[offset + 3];

                        bitmap.SetPixel(x, y, new SKColor(r, g, b, a));
                    }
                }
            }

            SKImage image = SKImage.FromBitmap(bitmap);
            SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(path, encoded.ToArray());
        }
    }
}
