using System.Collections;


namespace EntitiesDotNet;


public static class ResizableArray
{
    public static T[] InternalArray<T>(ResizableArray<T> array) =>
        ResizableArray<T>.InternalArray(array);
}


public class ReadOnlyArray<T> : IReadOnlyList<T>
{


    public ReadOnlyArray(ResizableArray<T> array)
    {
        this._array = array;
    }


    public int Count => this._array.Count;


    public bool IsEmpty => this._array.IsEmpty;
    public bool IsNotEmpty => this._array.IsNotEmpty;


    public ref readonly T this[int index] => ref this._array[index];


    public ReadOnlySpan<T> AsSpan() => this._array.AsSpan();


    T IReadOnlyList<T>.this[int index] => this[index];


    public ReadOnlySpan<T>.Enumerator GetEnumerator()
    {
        return this.AsSpan().GetEnumerator();
    }


    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return ((IEnumerable<T>)this._array).GetEnumerator();
    }


    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)this._array).GetEnumerator();
    }


    public static implicit operator ReadOnlySpan<T>(ReadOnlyArray<T> @this) =>
        @this.AsSpan();


    private readonly ResizableArray<T> _array;

}


/// <summary>
/// Класс, аналогичный <see cref="List{T}"/>, но предоставляющий доступ к Span.
/// </summary>
public class ResizableArray<T> : IList<T>
{

    public ResizableArray()
    {
        this._items = Array.Empty<T>();
        this._readOnly = new ReadOnlyArray<T>(this);
    }


    public ResizableArray(int capacity)
    {
        this._items = new T[capacity];
        this._readOnly = new ReadOnlyArray<T>(this);
    }


    public bool Remove(T item)
    {
        var index = this.IndexOf(item);
        if (index < 0)
        {
            return false;
        }

        this.RemoveAt(index);
        return true;
    }


    public int Count => this._size;


    bool ICollection<T>.IsReadOnly { get; } = false;


    /// <summary>
    /// Gets or sets the total number of elements the internal data structure can
    /// hold without resizing.
    /// </summary>
    /// <returns>
    /// The number of elements that the <see cref="T:System.Collections.Generic.List`1" />
    /// can contain before resizing is required.</returns>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <see cref="Capacity" /> is set to a value that
    /// is less than <see cref="Count" />.
    /// </exception>
    /// <exception cref="T:System.OutOfMemoryException">
    /// There is not enough memory available on the system.
    /// </exception>
    public int Capacity
    {
        get => this._items.Length;
        set
        {
            if (value < this._size)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (value == this._items.Length)
            {
                return;
            }

            if (value == 0)
            {
                this._items = Array.Empty<T>();
                return;
            }

            var destinationArray = new T[value];
            if (this._size > 0)
            {
                Array.Copy(this._items, 0, destinationArray, 0, this._size);
            }

            this._items = destinationArray;
        }
    }


    public bool IsEmpty => this._size == 0;
    public bool IsNotEmpty => this._size > 0;


    public ref T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)this._size)
            {
                throw new IndexOutOfRangeException();
            }

            return ref this._items[index];
        }
    }


    T IList<T>.this[int index]
    {
        get => this._items[index];
        set => this._items[index] = value;
    }


    public Span<T> AsSpan() => this._items.AsSpan(0, this._size);


    public ReadOnlyArray<T> AsReadOnly() => this._readOnly;


    /// <summary>
    /// Adds an object to the end of the <see cref="ResizableArray{T}" />.
    /// </summary>
    public void Add(T item)
    {
        this.EnsureCapacity(this._size + 1);

        this._items[this._size++] = item;
    }


    /// <summary>
    /// Adds the elements of the specified collection to the end of the
    /// <see cref="ResizableArray{T}"/>.
    /// </summary>
    /// <param name="collection">The collection whose elements should be added to the end
    /// of the <see cref="ResizableArray{T}" />.
    /// The collection itself cannot be <see langword="null" />, but it can contain
    /// elements that are <see langword="null" />, if type <paramref name="T" />
    /// is a reference type.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="collection" /> is <see langword="null" />.</exception>
    public void AddRange(IEnumerable<T> collection) =>
        this.InsertRange(this._size, collection);


    /// <summary>
    /// Inserts the elements of a collection into the
    /// <see cref="ResizableArray{T}" /> at the specified index.
    /// </summary>
    /// <param name="index">
    /// The zero-based index at which the new elements should be inserted.
    /// </param>
    /// <param name="collection">
    /// The collection whose elements should be inserted into the
    /// <see cref="ResizableArray{T}" />. The collection itself cannot be
    /// <see langword="null" />, but it can contain elements that are <see langword="null" />,
    /// if type <paramref name="T" /> is a reference type.
    /// </param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than 0.
    /// -or-
    /// <paramref name="index" /> is greater than <see cref="Count" />.
    /// </exception>
    public void InsertRange(int index, IEnumerable<T> collection)
    {
        if ((uint)index > (uint)this._size)
        {
            throw new ArgumentOutOfRangeException();
        }

        if (collection is ICollection<T> objs)
        {
            var count = objs.Count;
            if (count > 0)
            {
                this.EnsureCapacity(this._size + count);
                if (index < this._size)
                {
                    Array.Copy(this._items, index, this._items,
                        index + count, this._size - index);
                }

                if (this.Equals(objs))
                {
                    Array.Copy(this._items, 0, this._items, index, index);
                    Array.Copy(this._items, index + count, this._items,
                        index * 2, this._size - index);
                }
                else
                {
                    var array = new T[count];
                    objs.CopyTo(array, 0);
                    array.CopyTo(this._items, index);
                }

                this._size += count;
            }
        }
        else
        {
            foreach (var obj in collection)
            {
                this.Insert(index++, obj);
            }
        }
    }


    public int IndexOf(T item)
    {
        return Array.IndexOf(this._items, item, 0, this._size);
    }


    /// <summary>Inserts an element into the <see cref="ResizableArray{T}" /> at the specified index.</summary>
    /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
    /// <param name="item">The object to insert. The value can be <see langword="null" /> for reference types.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///         <paramref name="index" /> is less than 0.
    /// -or-
    /// <paramref name="index" /> is greater than <see cref="Count" />.
    /// </exception>
    public void Insert(int index, T item)
    {
        if ((uint)index > (uint)this._size)
        {
            throw new ArgumentOutOfRangeException();
        }

        if (this._size == this._items.Length)
        {
            this.EnsureCapacity(this._size + 1);
        }

        if (index < this._size)
        {
            Array.Copy(this._items, index, this._items, index + 1, this._size - index);
        }

        this._items[index] = item;
        ++this._size;
    }


    public void RemoveAt(int index)
    {
        if ((uint)index >= (uint)this._size)
        {
            throw new ArgumentOutOfRangeException();
        }

        --this._size;
        if (index < this._size)
        {
            Array.Copy(this._items, index + 1, this._items, index, this._size - index);
        }

        this._items[this._size] = default!;
    }


    /// <summary>
    /// Removes all elements from the <see cref="ResizableArray{T}" />.
    /// </summary>
    public void Clear()
    {
        if (this.IsEmpty)
        {
            return;
        }

        Array.Clear(this._items, 0, this._size);
        this._size = 0;
    }


    bool ICollection<T>.Contains(T item)
    {
        return this.IndexOf(item) >= 0;
    }


    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
    {
        if (array.Rank != 1)
        {
            throw new ArgumentException(
                "Cannot copy to array. Array cannot be multidimensional."
            );
        }

        try
        {
            Array.Copy(this._items, 0, array, arrayIndex, this._size);
        }
        catch (ArrayTypeMismatchException)
        {
            throw new ArgumentException(
                "Cannot copy to array. Array has mismatched value type."
            );
        }
    }


    public Span<T>.Enumerator GetEnumerator() => this.AsSpan().GetEnumerator();


    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return new Enumerator(this);
    }


    IEnumerator IEnumerable.GetEnumerator()
    {
        return new Enumerator(this);
    }


    public static T[] InternalArray(ResizableArray<T> array) => array._items;


    public static implicit operator Span<T>(ResizableArray<T> array) =>
        array.AsSpan();


    public static implicit operator ReadOnlySpan<T>(ResizableArray<T> array) =>
        array.AsSpan();


    public static implicit operator ReadOnlyArray<T>(ResizableArray<T> array) =>
        array.AsReadOnly();


    #region Private


    private const int DEFAULT_CAPACITY = 4;

    private int _size;
    private T[] _items;

    private readonly ReadOnlyArray<T> _readOnly;


    private void EnsureCapacity(int min)
    {
        if (this._items.Length >= min)
        {
            return;
        }

        var num = this._items.Length == 0 ? DEFAULT_CAPACITY : this._items.Length * 2;
        if ((uint)num > 2146435071U)
        {
            num = 2146435071;
        }

        if (num < min)
        {
            num = min;
        }

        this.Capacity = num;
    }


    #endregion


    private class Enumerator : IEnumerator<T>
    {

        public Enumerator(ResizableArray<T> array)
        {
            this._array = array;
            this._index = -1;
        }


        public void Dispose()
        {
        }


        public bool MoveNext()
        {
            ++this._index;
            return (uint)this._index < (uint)this._array._size;
        }


        public void Reset()
        {
            this._index = -1;
        }


        public T Current => this._array[this._index];


        object? IEnumerator.Current => this.Current;


        private readonly ResizableArray<T> _array;
        private int _index;

    }


}