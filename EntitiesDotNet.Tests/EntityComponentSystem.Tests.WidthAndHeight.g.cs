using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CSharp.SourceGen.Inlining;

#pragma warning disable CS0105    // disables warning about using the same namespaces several times

using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using EntitiesDotNet;
using Xunit.Abstractions;

namespace EntityComponentSystem.Tests
{

public ref partial struct WidthAndHeight
{
    
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WidthAndHeight.Array From(IComponentArray array) => new (array);


    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WidthAndHeight.ArrayList From(IReadOnlyList<IComponentArray> arrayList) => new (arrayList);


    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WidthAndHeight.ArrayEnumerable From(IEnumerable<IComponentArray> arrayEnumerable) => new (arrayEnumerable);


    public static class Delegates
    {
        public delegate void ForEach(WidthAndHeight entity);
        public delegate void ForEachWithIndex(int index, WidthAndHeight entity);
    }
    
    
    public static void ForEach(IComponentArray array, Delegates.ForEach action)
    {
        var (count, Height, Width) = EntitiesDotNet.Write<EntityComponentSystem.Tests.Height, EntityComponentSystem.Tests.Width>.From(array);
        
        if (count == 0) return;
        
        var entity = new WidthAndHeight
        {
            Height = ref MemoryMarshal.GetReference(Height),
            Width = ref MemoryMarshal.GetReference(Width),
        };
        
        action(entity);
        
        for (var i = 1; i < count; ++i)
        {
            entity.Height = ref Unsafe.Add(ref entity.Height, 1);
            entity.Width = ref Unsafe.Add(ref entity.Width, 1);
            
            action(entity);
        }
    }
    
    
    public static void ForEach(IComponentArray array, Delegates.ForEachWithIndex action)
    {
        var (count, Height, Width) = EntitiesDotNet.Write<EntityComponentSystem.Tests.Height, EntityComponentSystem.Tests.Width>.From(array);
        
        if (count == 0) return;
        
        var entity = new WidthAndHeight
        {
            Height = ref MemoryMarshal.GetReference(Height),
            Width = ref MemoryMarshal.GetReference(Width),
        };
        
        action(0, entity);
        
        for (var i = 1; i < count; ++i)
        {
            entity.Height = ref Unsafe.Add(ref entity.Height, 1);
            entity.Width = ref Unsafe.Add(ref entity.Width, 1);
            
            action(i, entity);
        }
    }
    
    
    [SupportsInlining("""
        WidthAndHeight {action.arg0} = default;
        foreach (var __array in __arrays) {
            var (__count, __HeightSpan, __WidthSpan) = EntitiesDotNet.Write<EntityComponentSystem.Tests.Height, EntityComponentSystem.Tests.Width>.From(__array);
            if (__count == 0) continue;
            ref var __Height = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(__HeightSpan);
            ref var __Width = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(__WidthSpan);
            {action.arg0}.Height = ref __Height;
            {action.arg0}.Width = ref __Width;
            {action.body}
            
            for (var __i = 1; __i < __count; ++__i) {
                __Height = ref System.Runtime.CompilerServices.Unsafe.Add(ref __Height, 1);
                __Width = ref System.Runtime.CompilerServices.Unsafe.Add(ref __Width, 1);
                {action.arg0}.Height = ref __Height;
                {action.arg0}.Width = ref __Width;
                {action.body}
            }
        }
        """)]
    public static void ForEach_inlining(EntityArrays arrays, Delegates.ForEach action)
    {
        foreach (var array in arrays)
        {
            ForEach(array, action);
        }
    }
    
    
    public readonly ref struct Array {

        public Array(IComponentArray array) {
            (this.Length, this.HeightSpan, this.WidthSpan) = EntitiesDotNet.Write<EntityComponentSystem.Tests.Height, EntityComponentSystem.Tests.Width>.From(array);
        }
        
        
        public readonly int Length;
        public readonly Span<EntityComponentSystem.Tests.Height> HeightSpan;
        public readonly Span<EntityComponentSystem.Tests.Width> WidthSpan;
        
        
        public WidthAndHeight this[int index] {
            get {
                WidthAndHeight result = default;
                result.Height = ref this.HeightSpan[index];
                result.Width = ref this.WidthSpan[index];
                return result;
            }
        }
        

        public Enumerator GetEnumerator() {
            return new Enumerator(this);
        }


        public ref struct Enumerator {
            public Enumerator(Array array) {
                this._index = -1;
                this._array = array;
            }


            private Array _array;
            private int _index;


            public bool MoveNext() {
                return ++this._index < this._array.Length;
            }


            public WidthAndHeight Current => this._array[this._index];
        }
        
    }
    
    
    public readonly struct ArrayList {
    
        public ArrayList(IReadOnlyList<IComponentArray> arrayList) {
            this._arrayList = arrayList;
        }
        
        private readonly IReadOnlyList<IComponentArray> _arrayList;
        
        public Enumerator GetEnumerator() => new (this);
        
        
        public ref struct Enumerator {
        
            public Enumerator(ArrayList arrayList) {
                this._arrayList = arrayList._arrayList;
                this._arrayIndex = -1;
                this._itemIndex = -1;
            }
        
            private readonly IReadOnlyList<IComponentArray> _arrayList;
            private WidthAndHeight.Array _currentArray;
            private int _arrayIndex;
            private int _itemIndex;
            
            public bool MoveNext() {
                if (++this._itemIndex < this._currentArray.Length) {
                    return true;
                }
                
                while (this._itemIndex >= this._currentArray.Length) {
                    ++this._arrayIndex;
                    
                    if (this._arrayIndex >= this._arrayList.Count) {
                        return false;
                    }
                    
                    this._currentArray = From(_arrayList[this._arrayIndex]);
                    this._itemIndex = 0;
                }
                
                return true;
            }
            
            public WidthAndHeight Current => this._currentArray[this._itemIndex];
            
        }
        
    }
    
    
    public readonly struct ArrayEnumerable {
        public ArrayEnumerable(IEnumerable<IComponentArray> arraySeq) {
            this._arraySeq = arraySeq;
        }
        
        private readonly IEnumerable<IComponentArray> _arraySeq;
        
        public Enumerator GetEnumerator() => new (this);
        
        
        public ref struct Enumerator {
        
            public Enumerator(ArrayEnumerable arrayEnumerable) {
                this._enumerator = arrayEnumerable._arraySeq.GetEnumerator();
                this._index = -1;
            }
        
            private readonly IEnumerator<IComponentArray> _enumerator;
            private WidthAndHeight.Array _currentArray;
            private int _index;
            
            public bool MoveNext() {
                if (++this._index < this._currentArray.Length) {
                    return true;
                }
                
                while (this._index >= this._currentArray.Length) {
                    if (!this._enumerator.MoveNext()) {
                        return false;
                    }
                    
                    this._currentArray = From(this._enumerator.Current);
                    this._index = 0;
                }
                
                return true;
            }
            
            public WidthAndHeight Current => this._currentArray[this._index];
            
        }
    }

}
}

#pragma warning restore CS0104

