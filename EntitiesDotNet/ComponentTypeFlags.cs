using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;


namespace EntitiesDotNet;


public unsafe struct ComponentTypeFlags : IEquatable<ComponentTypeFlags>
{
    private fixed ulong _buffer[4];


    public bool IsEmpty =>
        this._buffer[0] == 0
        && this._buffer[1] == 0
        && this._buffer[2] == 0
        && this._buffer[3] == 0;


    public bool IsNotEmpty => !this.IsEmpty;


    public ComponentTypeFlags(byte flag)
    {
        this.SetFlag(flag);
    }


    public bool Contains(ComponentTypeFlags flags)
    {
        return (this & flags) == flags;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFlag(byte flag)
    {
        var index = flag / 64;
        var itemBit = flag - index * 64;
        this._buffer[index] = 1ul << itemBit;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BitwiseAnd(in ComponentTypeFlags other)
    {
        this._buffer[0] &= other._buffer[0];
        this._buffer[1] &= other._buffer[1];
        this._buffer[2] &= other._buffer[2];
        this._buffer[3] &= other._buffer[3];
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BitwiseOr(in ComponentTypeFlags other)
    {
        this._buffer[0] |= other._buffer[0];
        this._buffer[1] |= other._buffer[1];
        this._buffer[2] |= other._buffer[2];
        this._buffer[3] |= other._buffer[3];
    }


    public byte ComponentCount()
    {
        byte count = 0;
        Count(this._buffer[0], ref count);
        Count(this._buffer[1], ref count);
        Count(this._buffer[2], ref count);
        Count(this._buffer[3], ref count);

        return count;

        static void Count(ulong value, ref byte count)
        {
            if (value == 0) return;

            for (var bitIndex = 0; bitIndex < 64; ++bitIndex)
            {
                if ((value & (1ul << bitIndex)) != 0) ++count;
            }
        }
    }


    public ImmutableArray<ComponentType> GetComponentTypeArray()
    {
        if (this.IsEmpty)
        {
            return ImmutableArray<ComponentType>.Empty;
        }

        var components = ImmutableArray.CreateBuilder<ComponentType>(this.ComponentCount());
        Add(0, this._buffer[0], components);
        Add(1, this._buffer[1], components);
        Add(2, this._buffer[2], components);
        Add(3, this._buffer[3], components);

        return components.MoveToImmutable();

        static void Add(byte valueIndex, ulong value, ImmutableArray<ComponentType>.Builder builder)
        {
            if (value == 0) return;

            for (byte bitIndex = 0; bitIndex < 64; ++bitIndex)
            {
                if ((value & (1ul << bitIndex)) == 0) continue;

                var id = unchecked(valueIndex + bitIndex);
                builder.Add(ComponentType.Instance((byte)id));
            }
        }
    }


    public static ComponentTypeFlags operator &(in ComponentTypeFlags a, in ComponentTypeFlags b)
    {
        var result = a;
        result.BitwiseAnd(b);
        return result;
    }


    public static ComponentTypeFlags operator |(in ComponentTypeFlags a, in ComponentTypeFlags b)
    {
        var result = a;
        result.BitwiseOr(b);
        return result;
    }


    public static ComponentTypeFlags operator ~(in ComponentTypeFlags flags)
    {
        var result = new ComponentTypeFlags();
        result._buffer[0] = ~flags._buffer[0];
        result._buffer[1] = ~flags._buffer[1];
        result._buffer[2] = ~flags._buffer[2];
        result._buffer[3] = ~flags._buffer[3];
        return result;
    }


    public override string ToString()
    {
        var builder = new StringBuilder(256);
        AppendULong(this._buffer[0]);
        builder.AppendLine();
        AppendULong(this._buffer[1]);
        builder.AppendLine();
        AppendULong(this._buffer[2]);
        builder.AppendLine();
        AppendULong(this._buffer[3]);

        return builder.ToString();

        void AppendULong(ulong value)
        {
            for (var i = 0; i < 64; ++i)
            {
                if (i is > 0 and < 64 && i % 8 == 0)
                {
                    builder.Append('_');
                }

                builder.Append((value & (1ul << i)) == 0ul ? '0' : '1');
            }
        }
    }


    public bool Equals(ComponentTypeFlags other)
    {
        return this._buffer[0] == other._buffer[0]
            && this._buffer[1] == other._buffer[1]
            && this._buffer[2] == other._buffer[2]
            && this._buffer[3] == other._buffer[3];
    }


    public override bool Equals(object? obj)
    {
        return obj is ComponentTypeFlags other && this.Equals(other);
    }


    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this._buffer[0];
            hashCode = hashCode * 397 ^ this._buffer[1];
            hashCode = hashCode * 397 ^ this._buffer[2];
            hashCode = hashCode * 397 ^ this._buffer[3];
            return hashCode.GetHashCode();
        }
    }


    public static bool operator ==(ComponentTypeFlags left, ComponentTypeFlags right)
    {
        return left.Equals(right);
    }


    public static bool operator !=(ComponentTypeFlags left, ComponentTypeFlags right)
    {
        return !left.Equals(right);
    }
}