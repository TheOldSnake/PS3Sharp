namespace PS3Sharp.Cell
{
    /// <summary>
    /// Marker interface for structs that map directly to PS3 memory layouts.
    /// </summary>
    /// <remarks>
    /// Structs implementing this interface are expected to match the exact
    /// memory layout used by the PS3, and are expected to be implemented using BigEndian type.
    /// </remarks>
    public interface IPS3Struct
    {

    }
}
