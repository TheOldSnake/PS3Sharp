[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]

namespace PS3Sharp
{
    internal sealed class PS3Sharp
    {
        internal static void Main()
        {
            SanityChecks.DoSanityChecks();
            using Emulator emulator = new();
            emulator.RunEmulator();
        }
    }
}