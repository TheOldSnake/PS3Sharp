namespace PS3Sharp.Cell.Ppu.Recompiler.IR
{
    internal sealed partial class IRGenerator
    {
        internal readonly struct Value : IEquatable<Value>
        {
            internal enum ValueType
            {
                None,
                I8,
                I16,
                I32,
                I64,
                I128,
                F32,
                F64,
                Vector128,
            }

            internal readonly int Id = -1;
            internal readonly ValueType Type;

            internal bool IsValid => Id > 0;

            internal static Value Invalid => new(-1, ValueType.None);

            internal Value(int id, ValueType type)
            {
                Id = id;
                Type = type;
            }

            internal static int GetValueSize(ValueType type)
            {
                switch (type)
                {
                    case ValueType.None:
                        return 0;

                    case ValueType.I8:
                        return 1;

                    case ValueType.I16:
                        return 2;

                    case ValueType.I32:
                    case ValueType.F32:
                        return 4;

                    case ValueType.I64:
                    case ValueType.F64:
                        return 8;

                    case ValueType.I128:
                    case ValueType.Vector128:
                        return 16;

                    default:
                        return -1;
                }
            }

            internal static int GetBitCount(ValueType type)
            {
                switch (type)
                {
                    case ValueType.None:
                        return 0;

                    case ValueType.I8:
                        return 8;

                    case ValueType.I16:
                        return 16;

                    case ValueType.I32:
                    case ValueType.F32:
                        return 32;

                    case ValueType.I64:
                    case ValueType.F64:
                        return 64;

                    case ValueType.I128:
                    case ValueType.Vector128:
                        return 128;

                    default:
                        return -1;
                }
            }

            public bool Equals(Value other) => Id == other.Id && Type == other.Type;
            public override bool Equals(object? obj) => obj is Value other && Equals(other);
            public override int GetHashCode() => Id;
            public override string ToString() => $"v{Id}";


            public static bool operator ==(Value left, Value right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Value left, Value right)
            {
                return !(left == right);
            }
        }
    }
}
