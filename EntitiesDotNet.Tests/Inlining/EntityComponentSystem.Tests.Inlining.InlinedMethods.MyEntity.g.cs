using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CSharp.SourceGen.Inlining;

#pragma warning disable CS0105    // disables warning about using the same namespaces several times

using CSharp.SourceGen.Inlining;
using EntitiesDotNet;
using Xunit.Abstractions;

namespace EntityComponentSystem.Tests.Inlining
{

public static partial class InlinedMethods
{
private ref partial struct MyEntity
{
    
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MyEntity.Array From(IComponentArray array) => new (array);


    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MyEntity.ArrayList From(IReadOnlyList<IComponentArray> arrayList) => new (arrayList);


    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MyEntity.ArrayEnumerable From(IEnumerable<IComponentArray> arrayEnumerable) => new (arrayEnumerable);


    public static class Delegates
    {
        public delegate void ForEach(MyEntity entity);
        public delegate void ForEachWithIndex(int index, MyEntity entity);
    }
    
    
    public static void ForEach(IComponentArray array, Delegates.ForEach action)
    {
        var (count, velocity, translation) = EntitiesDotNet.Read<EntityComponentSystem.Tests.Velocity>.Write<EntityComponentSystem.Tests.Translation>.From(array);
        
        if (count == 0) return;
        
        var entity = new MyEntity
        {
            velocity = ref MemoryMarshal.GetReference(velocity),
            translation = ref MemoryMarshal.GetReference(translation),
        };
        
        action(entity);
        
        for (var i = 1; i < count; ++i)
        {
            entity.velocity = ref Unsafe.Add(ref Unsafe.AsRef(entity.velocity), 1);
            entity.translation = ref Unsafe.Add(ref entity.translation, 1);
            
            action(entity);
        }
    }
    
    
    public static void ForEach(IComponentArray array, Delegates.ForEachWithIndex action)
    {
        var (count, velocity, translation) = EntitiesDotNet.Read<EntityComponentSystem.Tests.Velocity>.Write<EntityComponentSystem.Tests.Translation>.From(array);
        
        if (count == 0) return;
        
        var entity = new MyEntity
        {
            velocity = ref MemoryMarshal.GetReference(velocity),
            translation = ref MemoryMarshal.GetReference(translation),
        };
        
        action(0, entity);
        
        for (var i = 1; i < count; ++i)
        {
            entity.velocity = ref Unsafe.Add(ref Unsafe.AsRef(entity.velocity), 1);
            entity.translation = ref Unsafe.Add(ref entity.translation, 1);
            
            action(i, entity);
        }
    }
    
    
    [SupportsInlining("""
        MyEntity {action.arg0} = default;
        foreach (var __array in __arrays) {
            var (__count, __velocitySpan, __translationSpan) = EntitiesDotNet.Read<EntityComponentSystem.Tests.Velocity>.Write<EntityComponentSystem.Tests.Translation>.From(__array);
            if (__count == 0) continue;
            ref var __velocity = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(__velocitySpan);
            ref var __translation = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(__translationSpan);
            {action.arg0}.velocity = ref __velocity;
            {action.arg0}.translation = ref __translation;
            {action.body}
            
            for (var __i = 1; __i < __count; ++__i) {
                __velocity = ref System.Runtime.CompilerServices.Unsafe.Add(ref __velocity, 1);
                __translation = ref System.Runtime.CompilerServices.Unsafe.Add(ref __translation, 1);
                {action.arg0}.velocity = ref __velocity;
                {action.arg0}.translation = ref __translation;
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
            (this.Length, this.velocitySpan, this.translationSpan) = EntitiesDotNet.Read<EntityComponentSystem.Tests.Velocity>.Write<EntityComponentSystem.Tests.Translation>.From(array);
        }
        
        
        public readonly int Length;
        public readonly ReadOnlySpan<EntityComponentSystem.Tests.Velocity> velocitySpan;
        public readonly Span<EntityComponentSystem.Tests.Translation> translationSpan;
        
        
        public MyEntity this[int index] {
            get {
                MyEntity result = default;
                result.velocity = ref this.velocitySpan[index];
                result.translation = ref this.translationSpan[index];
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


            public MyEntity Current => this._array[this._index];
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
            private MyEntity.Array _currentArray;
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
            
            public MyEntity Current => this._currentArray[this._itemIndex];
            
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
            private MyEntity.Array _currentArray;
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
            
            public MyEntity Current => this._currentArray[this._index];
            
        }
    }

}
}
}

#pragma warning restore CS0104

