namespace PS3Sharp.BEReader
{
    // Unused

    /*internal sealed class BigEndianReader : BinaryReader {
         internal BigEndianReader(System.IO.Stream stream) : base(stream) { }

         internal override int ReadInt32() {
             var data = base.ReadBytes(4);
             Array.Reverse(data);
             return BitConverter.ToInt32(data, 0);
         }

         internal uint ReadUInt32() {
             var data = base.ReadBytes(4);
             Array.Reverse(data);
             return BitConverter.ToUInt32(data, 0);
         }

         internal ushort ReadUInt16() {
             var data = base.ReadBytes(2);
             Array.Reverse(data);
             return BitConverter.ToUInt16(data, 0);
         }

         internal short ReadInt16() {
             var data = base.ReadBytes(2);
             Array.Reverse(data);
             return BitConverter.ToInt16(data, 0);
         }

         internal long ReadInt64() {
             var data = base.ReadBytes(8);
             Array.Reverse(data);
             return BitConverter.ToInt64(data, 0);
         }

         internal ulong ReadUInt64() {
             var data = base.ReadBytes(8);
             Array.Reverse(data);
             return BitConverter.ToUInt64(data, 0);
         }
     }*/

}
