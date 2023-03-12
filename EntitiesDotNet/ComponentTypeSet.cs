using System.Collections.Immutable;
using System.Text;


namespace EntitiesDotNet;


public readonly partial struct ComponentTypeSet : IEquatable<ComponentTypeSet>
{
    private ComponentTypeSet(ReadOnlyMemory<int> memory)
    {
        this._memory = memory;
    }


    private readonly ReadOnlyMemory<int> _memory;


    public bool IsEmpty => this._memory.IsEmpty;


    public int Count()
    {
        var count = 0;
        var span = this._memory.Span;
        foreach (var intValue in span)
        {
            for (var bitIndex = 0; bitIndex < sizeof(int) * 8; ++bitIndex)
            {
                if ((intValue & (1 << bitIndex)) != 0) ++count;
            }
        }
        
        return count;
    }


    public bool Contains(ComponentTypeSet other)
    {
        if (this._memory.Length < other._memory.Length) return false;

        var count = this._memory.Length;
        var thisSpan = this._memory.Span;
        var otherSpan = other._memory.Span;

        for (var i = 0; i < count; ++i)
        {
            var thisValue = thisSpan[i];
            var otherValue = otherSpan[i];

            if ((thisValue & otherValue) != otherValue) return false;
        }

        return true;
    }


    public ComponentTypeSet Add(ComponentTypeSet components)
    {
        if (components == this) return this;

        lock (Locker)
        {
            DefaultBuilder.Clear();
            DefaultBuilder.Add(this);
            DefaultBuilder.Add(components);
            return DefaultBuilder.ComponentTypeSet;
        }
    }


    public ComponentTypeSet Remove(ComponentTypeSet components)
    {
        if (components == this) return this;

        lock (Locker)
        {
            DefaultBuilder.Clear();
            DefaultBuilder.Add(this);
            DefaultBuilder.Remove(components);
            return DefaultBuilder.ComponentTypeSet;
        }
    }


    public ImmutableArray<ComponentType> ToImmutableArray()
    {
        if (this._memory.IsEmpty)
        {
            return ImmutableArray<ComponentType>.Empty;
        }

        var components = ImmutableArray.CreateBuilder<ComponentType>(this.Count());

        var span = this._memory.Span;
        for (var intIndex = 0; intIndex < span.Length; ++intIndex)
        {
            var intValue = span[intIndex];
            for (var bitIndex = 0; bitIndex < sizeof(int) * 8; ++bitIndex)
            {
                if ((intValue & (1 << bitIndex)) == 0) continue;

                var componentTypeId = intIndex * sizeof(int) * 8 + bitIndex;
                var componentType = ComponentType.Instance(componentTypeId);
                components.Add(componentType);
            }
        }

        return components.MoveToImmutable();
    }


    public static bool operator ==(ComponentTypeSet a, ComponentTypeSet b)
    {
        return a.Equals(b);
    }


    public static bool operator !=(ComponentTypeSet a, ComponentTypeSet b)
    {
        return !a.Equals(b);
    }


    public override string ToString()
    {
        var builder = new StringBuilder();
        var span = this._memory.Span;
        foreach (var value in span)
        {
            for (var i = 0; i < sizeof(int) * 8; ++i)
            {
                var symbol = '0';
                if ((value & (1 << i)) != 0)
                {
                    symbol = '1';
                }

                builder.Append(symbol);
            }
        }

        return builder.ToString();
    }


    public bool Equals(ComponentTypeSet other)
    {
        return this._memory.Equals(other._memory);
    }


    public override bool Equals(object? obj)
    {
        return obj is ComponentTypeSet other && this.Equals(other);
    }


    public override int GetHashCode()
    {
        return this._memory.GetHashCode();
    }


    private static readonly object Locker = new();
    private static readonly Pool DefaultPool = new();
    private static readonly Builder DefaultBuilder = new();


    private class Pool
    {
        public ComponentTypeSet GetInstance(ReadOnlyMemory<int> components)
        {
            lock (this._dictionary)
            {
                if (this._dictionary.TryGetValue(components, out var instance)) return instance;

                var immutableMemoryCopy = components.ToArray().AsMemory();
                instance = new ComponentTypeSet(immutableMemoryCopy);
                this._dictionary[immutableMemoryCopy] = instance;

                return instance;
            }
        }


        private readonly Dictionary<ReadOnlyMemory<int>, ComponentTypeSet> _dictionary =
            new(new IntMemoryEqualityComparer());
    }


    public class Builder
    {
        public void Clear()
        {
            this._array.Clear();
        }


        public void Add(ComponentTypeSet id)
        {
            var span = id._memory.Span;
            this._array.EnsureCount(span.Length);
            for (var i = 0; i < span.Length; ++i)
            {
                this._array[i] |= span[i];
            }
        }


        public void Remove(ComponentTypeSet id)
        {
            var span = id._memory.Span;
            this._array.EnsureCount(span.Length);
            for (var i = 0; i < span.Length; ++i)
            {
                this._array[i] &= ~span[i];
            }
        }


        public void Add(ComponentType component)
        {
            var index = component.Id;
            var intIndex = index / (sizeof(int) * 8);
            var bitIndex = index - intIndex;

            this._array.EnsureCount(intIndex + 1);
            ref var value = ref this._array[intIndex];
            value |= 1 << bitIndex;
        }


        public void Remove(ComponentType component)
        {
            var index = component.Id;
            
            var intIndex = index / (sizeof(int) * 8);
            if (intIndex >= this._array.Count) return;
            
            var bitIndex = index - intIndex;

            ref var value = ref this._array[intIndex];
            value &= ~(1 << bitIndex);
        }


        public ComponentTypeSet ComponentTypeSet => DefaultPool.GetInstance(this._array.AsMemory());


        private readonly ResizableArray<int> _array = new();
    }
}