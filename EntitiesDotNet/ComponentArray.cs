namespace EntitiesDotNet;


public partial class ComponentArray : IComponentArray
{


    #region Public


    public ComponentArray(int capacity = DefaultCapacity) : this(Archetype.Empty, capacity) { }


    public ComponentArray(Archetype archetype, int capacity = DefaultCapacity)
    {
        this._capacity = capacity;
        this._archetype = archetype;

        var components = archetype.Components;
        this._arrays = new Array[components.Length];
        for (var i = 0; i < components.Length; ++i)
        {
            this._arrays[i] = Array.CreateInstance(components[i], capacity);
        }
    }


    public int Count { get; private set; }


    public int Capacity
    {
        get => this._capacity;
        set
        {
            var newCapacity = value;
            if (newCapacity == this.Capacity)
            {
                return;
            }

            if (newCapacity < this.Count)
            {
                throw new ArgumentOutOfRangeException(
                    $"Cannot set capacity to {value}. {nameof(ComponentArray)} already contains {this.Count} elements."
                );
            }

            var components = this.Archetype.Components;
            for (var i = 0; i < components.Length; ++i)
            {
                ref var array = ref this._arrays[i];
                var newArray = Array.CreateInstance(components[i], newCapacity);
                array.CopyTo(newArray, 0);
                array = newArray;
            }

            this._capacity = newCapacity;
        }
    }


    public Archetype Archetype
    {
        get => this._archetype;
        set
        {
            var newArchetype = value;
            if (this.Archetype == newArchetype)
            {
                return;
            }

            var newArrays = new Array[newArchetype.Components.Length];
            for (var i = 0; i < newArrays.Length; ++i)
            {
                var component = newArchetype.Components[i];
                if (!this.TryGetArray(component, out var array))
                {
                    array = Array.CreateInstance(component, this.Count);
                }

                newArrays[i] = array;
            }

            this._arrays = newArrays;
            this._archetype = newArchetype;
        }
    }


    /// <summary>
    /// Adds the specified number of elements.
    /// </summary>
    /// <param name="count">Number of elements to add.</param>
    public void Add(int count = 1)
    {
        var newCount = this.Count + count;
        this.EnsureCapacity(newCount);
        this.Count += count;
    }


    public void Remove(int count = 1)
    {
        if (this.Count <= count)
        {
            throw new ArgumentOutOfRangeException();
        }

        var newCount = this.Count - count;
        foreach (var array in this._arrays)
        {
            Array.Clear(array, newCount, count);
        }

        this.Count = newCount;
    }


    public ReadOnlySpan<T> GetReadOnlySpan<T>()
    {
        return this.GetArray<T>().AsSpan(0, this.Count);
    }


    public Span<T> GetSpan<T>()
    {
        return this.GetArray<T>().AsSpan(0, this.Count);
    }


    public bool TryGetReadOnlySpan<T>(out ReadOnlySpan<T> span)
    {
        if (this.TryGetArray<T>(out var array))
        {
            span = array;
            return true;
        }

        span = default;
        return false;
    }


    public bool TryGetSpan<T>(out Span<T> span)
    {
        if (this.TryGetArray<T>(out var array))
        {
            span = array;
            return true;
        }

        span = default;
        return false;
    }


    public object? GetValue(ComponentType component, int index)
    {
        return this.GetArray(component).GetValue(index);
    }


    public void SetValue(ComponentType component, int index, object? value)
    {
        this.GetArray(component).SetValue(value, index);
    }


    /// <summary>
    /// Copies a range of elements starting from the specified source index to another <see cref="ComponentArray"/>
    /// starting from the specified destination index, without adding types.
    /// </summary>
    /// <param name="src"><see cref="ComponentArray"/> to copy elements from.</param>
    /// <param name="srcIndex">Start index of the range in src <see cref="ComponentArray"/>.</param>
    /// <param name="dest"><see cref="ComponentArray"/> to copy elements to.</param>
    /// <param name="destIndex">Start index of the range in dest <see cref="ComponentArray"/>.</param>
    /// <param name="count">Number of elements to copy.</param>
    public static void CopyTo(ComponentArray src, int srcIndex, ComponentArray dest,
        int destIndex, int count)
    {
        foreach (var field in src.Archetype.Components)
        {
            var srcArray = src.GetArray(field);
            if (dest.TryGetArray(field, out var destArray))
            {
                Array.Copy(srcArray, srcIndex, destArray, destIndex, count);
            }
        }
    }


    /// <summary>
    /// Copies all the elements to another <see cref="ComponentArray"/> starting from the specified index,
    /// without adding types.
    /// </summary>
    /// <param name="dest">The <see cref="ComponentArray"/> to copy values to.</param>
    /// <param name="destIndex">Index in the destination <see cref="ComponentArray"/> at which copying begins.</param>
    public void CopyTo(ComponentArray dest, int destIndex)
    {
        CopyTo(this, 0, dest, destIndex, this.Count);
    }


    /// <summary>
    /// Copies all the elements to another <see cref="ComponentArray"/> without adding types.
    /// </summary>
    /// <param name="dest">The <see cref="ComponentArray"/> to copy values to.</param>
    public void CopyTo(ComponentArray dest) => this.CopyTo(dest, 0);


    /// <summary>
    /// Sets range of elements to default values.
    /// </summary>
    /// <param name="index">The starting index of the range to clear.</param>
    /// <param name="length">The number of elements to clear.</param>
    public void Clear(int index, int length)
    {
        foreach (var field in this.Archetype.Components)
        {
            var array = this.GetArray(field);
            Array.Clear(array, index, length);
        }
    }


    public void EnsureCapacity(int min)
    {
        if (this.Capacity >= min)
        {
            return;
        }

        var newCapacity = this.Capacity != 0 ? this.Capacity * 2 : DefaultCapacity;
        newCapacity = Math.Max(Math.Min(newCapacity, MaxArrayLength), min);

        this.Capacity = newCapacity;
    }


    #endregion


    #region Private


    private const int DefaultCapacity = 4;
    private const int MaxArrayLength = 2 * 1024 * 1024;


    private Archetype _archetype;
    private Array[] _arrays;
    private int _capacity;


    internal bool TryGetArray(ComponentType component, out Array components)
    {
        var index = this.Archetype.GetIndex(component);
        if (index < 0)
        {
            components = null!;
            return false;
        }

        components = this._arrays[index];
        return true;
    }


    internal Array GetArray(ComponentType component)
    {
        if (!this.TryGetArray(component, out var components))
        {
            throw new ArgumentOutOfRangeException(
                $"IComponentArray does not contain component '{component}'.");
        }

        return components;
    }


    internal bool TryGetArray<T>(out T[] array)
    {
        var result = this.TryGetArray(ComponentType<T>.Instance, out var arrayRaw);
        array = (T[])arrayRaw;
        return result;
    }


    internal T[] GetArray<T>() => (T[])this.GetArray(ComponentType<T>.Instance);


    #endregion


}