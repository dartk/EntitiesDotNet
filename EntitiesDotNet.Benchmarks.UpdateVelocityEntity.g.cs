using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CSharp.SourceGen.Inlining;

#pragma warning disable CS0105    // disables warning about using the same namespaces several times

using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using CSharp.SourceGen.Inlining;
using static EntitiesDotNet.Benchmarks.Functions;

namespace EntitiesDotNet.Benchmarks
{

public ref partial struct UpdateVelocityEntity
{
    
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UpdateVelocityEntity.Array From(IComponentArray array) => new (array);


    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UpdateVelocityEntity.ArrayList From(IReadOnlyList<IComponentArray> arrayList) => new (arrayList);


    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UpdateVelocityEntity.ArrayEnumerable From(IEnumerable<IComponentArray> arrayEnumerable) => new (arrayEnumerable);


    public static class Delegates
    {
        public delegate void ForEach(UpdateVelocityEntity entity);
        public delegate void ForEachWithIndex(int index, UpdateVelocityEntity entity);
    }
    
    
    public static void ForEach(IComponentArray array, Delegates.ForEach action)
    {
        var (count, Acceleration, Velocity) = EntitiesDotNet.Read<EntitiesDotNet.Benchmarks.Acceleration>.Write<EntitiesDotNet.Benchmarks.Velocity>.From(array);
        
        if (count == 0) return;
        
        var entity = new UpdateVelocityEntity
        {
            Acceleration = ref MemoryMarshal.GetReference(Acceleration),
            Velocity = ref MemoryMarshal.GetReference(Velocity),
        };
        
        action(entity);
        
        for (var i = 1; i < count; ++i)
        {
            entity.Acceleration = ref Unsafe.Add(ref Unsafe.AsRef(entity.Acceleration), 1);
            entity.Velocity = ref Unsafe.Add(ref entity.Velocity, 1);
            
            action(entity);
        }
    }
    
    
    public static void ForEach(IComponentArray array, Delegates.ForEachWithIndex action)
    {
        var (count, Acceleration, Velocity) = EntitiesDotNet.Read<EntitiesDotNet.Benchmarks.Acceleration>.Write<EntitiesDotNet.Benchmarks.Velocity>.From(array);
        
        if (count == 0) return;
        
        var entity = new UpdateVelocityEntity
        {
            Acceleration = ref MemoryMarshal.GetReference(Acceleration),
            Velocity = ref MemoryMarshal.GetReference(Velocity),
        };
        
        action(0, entity);
        
        for (var i = 1; i < count; ++i)
        {
            entity.Acceleration = ref Unsafe.Add(ref Unsafe.AsRef(entity.Acceleration), 1);
            entity.Velocity = ref Unsafe.Add(ref entity.Velocity, 1);
            
            action(i, entity);
        }
    }
    
    
    [SupportsInlining("""
        UpdateVelocityEntity {action.arg0} = default;
        foreach (var __array in __arrays) {
            var (__count, __AccelerationSpan, __VelocitySpan) = EntitiesDotNet.Read<EntitiesDotNet.Benchmarks.Acceleration>.Write<EntitiesDotNet.Benchmarks.Velocity>.From(__array);
            if (__count == 0) continue;
            ref var __Acceleration = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(__AccelerationSpan);
            ref var __Velocity = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(__VelocitySpan);
            {action.arg0}.Acceleration = ref __Acceleration;
            {action.arg0}.Velocity = ref __Velocity;
            {action.body}
            
            for (var __i = 1; __i < __count; ++__i) {
                __Acceleration = ref System.Runtime.CompilerServices.Unsafe.Add(ref __Acceleration, 1);
                __Velocity = ref System.Runtime.CompilerServices.Unsafe.Add(ref __Velocity, 1);
                {action.arg0}.Acceleration = ref __Acceleration;
                {action.arg0}.Velocity = ref __Velocity;
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
            (this.Length, this.AccelerationSpan, this.VelocitySpan) = EntitiesDotNet.Read<EntitiesDotNet.Benchmarks.Acceleration>.Write<EntitiesDotNet.Benchmarks.Velocity>.From(array);
        }
        
        
        public readonly int Length;
        public readonly ReadOnlySpan<EntitiesDotNet.Benchmarks.Acceleration> AccelerationSpan;
        public readonly Span<EntitiesDotNet.Benchmarks.Velocity> VelocitySpan;
        
        
        public UpdateVelocityEntity this[int index] {
            get {
                UpdateVelocityEntity result = default;
                result.Acceleration = ref this.AccelerationSpan[index];
                result.Velocity = ref this.VelocitySpan[index];
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


            public UpdateVelocityEntity Current => this._array[this._index];
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
            private UpdateVelocityEntity.Array _currentArray;
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
            
            public UpdateVelocityEntity Current => this._currentArray[this._itemIndex];
            
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
            private UpdateVelocityEntity.Array _currentArray;
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
            
            public UpdateVelocityEntity Current => this._currentArray[this._index];
            
        }
    }

}
}

#pragma warning restore CS0104

