> [!WARNING]
>  This project exists for educational purposes only. Do not use it for anything else.
 
# EntitiesDotNet

A fast and ergonomic general purpose Entity Component System library for .NET
inspired
by [Unity Entities](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/index.html).

- [Installation](#installation)
- [Entity Component System concepts](#entity-component-system-concepts)
    - [Entity](#entity)
    - [Component](#component)
    - [System](#system)
- [EntitiesDotNet types](#entitiesdotnet-types)
    - [EntityId](#entityid)
    - [IComponentArray](#icomponentarray)
    - [EntityArrays](#entityarrays)
    - [EntityManager](#entitymanager)
    - [Entity](#entity)
- [Iterating over components](#iterating-over-components)
    - [foreach loop](#foreach-loop)
    - [foreach loop with unmanaged function call](#foreach-loop-with-unmanaged-function-call)
    - [ForEach extensions](#foreach-extensions)
    - [ForEach extensions inlining](#foreach-extensions-inlining)
- [Iterating over components using EntityRef](#iterating-over-components-using-entityref)
    - [foreach loop](#foreach-loop)
    - [ForEach extensions](#foreach-extensions-1)
    - [ForEach extensions inlining](#foreach-extensions-inlining-1)
- [Inlining source generator](#inlining-source-generator)
- [Performance](#performance)

## Installation

Add prerelease package [Dartk.EntitiesDotNet](https://www.nuget.org/packages/Dartk.EntitiesDotNet/).

```
dotnet add package Dartk.EntitiesDotNet --prerelease
```

(Optional) For [`EntityRef`](#iterating-over-components-using-entityref)
and [`Inline`](#inlining-source-generator) source generators add prerelease
package [Dartk.EntitiesDotNet.Generators](https://www.nuget.org/packages/Dartk.EntitiesDotNet.Generators/)

```
dotnet add package Dartk.EntitiesDotNet.Generators --prerelease
```

## Entity Component System concepts

### Entity

Entities in ECS are basic units. They are similar to objects in OOP, but unlike objects entities do
not store any values themselves. Values are stored in components that are layed out in parallel
arrays for efficient processing of components of the same type. Entity is just a lightweight key
that is used to identify a position of components in the arrays.

### Component

Components are what holds the data. They are stored in parallel arrays and can be efficiently
processed sequentially or accessed randomly using [Entity](#entity) as a key.

### System

Any function that manipulates entities.

## EntitiesDotNet types

### EntityId

A structure that represents an Entity and used as a key to index entity's components
by [`EntityManager`](#entitymanager).

```c#
public readonly record struct EntityId(int Id, int Version);
```

### Archetype

A combination of component types. Every unique combination is represented by a single instance
of `Archetype` class.

```c#
var archetype0 = Archetype<Translation, Velocity, Acceleration>.Instance;
var archetype1 = Archetype.Instance<Translation, Velocity, Acceleration>();

Assert.IsTrue(object.ReferenceEquals(archetype0, archetype1));
```

### IComponentArray

An interface that provides access to parallel arrays of components.

```c#
void UpdateTranslation(IComponentArray components, float deltaTime)
{
    if (!components.Archetype.Contains<Velocity, Translation>()) return;

    int count = components.Count;
    ReadOnlySpan<Velocity> velocities = components.GetReadOnlySpan<Velocity>();
    Span<Translation> translations = components.GetSpan<Translation>();
    
    for (var i = 0; i < count; ++i)
    {
        translations[i] += velocities[i] * deltaTime;
    }
}
```

An equivalent method using [Read<...>.Write<...>.From](#foreach-loop):

```c#
void UpdateTranslation(IComponentArray components, float deltaTime)
{
    var (count, velocities, translations) = Read<Velocity>.Write<Translation>.From(components);
    for (var i = 0; i < count; ++i)
    {
        translations[i] += velocities[i] * deltaTime;
    }
}
```

### EntityArrays

A collection of `IComponentArray` objects.

```c#
void UpdateTranslation(EntityArrays entities, float deltaTime)
{
	entities.ForEach((in Velocity velocity, ref Translation translation) =>
		translation += velocity * deltaTime);
}
```

### EntityManager

Creates and destroys entities, stores components.

Components are stored in a dictionary of `IComponentArray` objects indexed by `Archetype`.
`Entities` property returns an [`EntityArrays`](#entityarrays) collection that holds all entities'
components owned by the `EntityManager`.

```c#
var entityManager = new EntityManager();

// creates entity with Velocity and Translation components
var entity0 = entityManager.CreateEntity(Archetype<Velocity, Translation>.Instance);

// creates entity with Velocity and Translation components and sets their values
var entity1 = entityManager.CreateEntity(new Velocity(10), new Translation(0));

// updates translation
var deltaTime = 1f / 60f;
entityManager.Entities.ForEach((in Velocity velocity, ref Translation translation) =>
	translation += velocity * deltaTime);
```

### Entity

A convenience structure, that holds `EntityId` and `EntityManager` and provides easy access to
components.

```c#
public readonly record struct Entity : IDisposable
{
    public Entity(EntityManager entityManager, EntityId id);
    
    public readonly EntityId Id;
    public readonly EntityManager EntityManager;
    public ref T RefRW<T>();            // mutable component reference
    public ref readonly T RefRO<T>();   // read-only component reference
    public void Dispose();              // Destroys entity
}
```

## Iterating over components

All of the examples below declare a static class `UpdateTranslationSystem`
with a method `Execute(EntityArrays entityArrays, float deltaTime)`
that reads `Velocity` and updates `Translation` components for every entity in
the `entityArrays` argument.

```c#
static class UpdateTranslationSystem
{
    public static void Execute(EntityArrays arrays, float deltaTime);
}
```

### foreach loop

`Read<TR0, TR1, ...>.Write<TW0, TW1, ...>.From()` methods can be used to access spans of
components (`Span<T>` or `ReadOnlySpan<T>`) from `EntityArrays` and `IComponentArray`. The result
can be deconstructed into a count of elements followed by spans of the specified components.

```c#
static class UpdateTranslationSystem
{
    public static void Execute(EntityArrays entities, float deltaTime)
    {
        foreach (var (count, velocities, translations) in
            Read<Velocity>.Write<Translation>.From(entities))
        {
            // velocities : ReadOnlySpan<Velocity>
            // translations : Span<Translation>
            
            for (var i = 0; i < count; ++i)
            {
                translations[i] += velocities[i] * deltaTime;
            }
        }
    }
}
```

Or

```c#
static class UpdateTranslationSystem
{
    public void Excecute(EntityArrays entities, float deltaTime)
    {
        foreach (IComponentArray components in entities)
        {
            var (count, v, t) = Read<Velocity>.Write<Translation>.From(components);
            // velocities : ReadOnlySpan<Velocity>
            // translations : Span<Translation>
            
            for (var i = 0; i < count; ++i)
            {
                translations[i] += velocities[i] * deltaTime;
            }
        }
    }
}
```

### foreach loop with unmanaged function call

Spans of components that are returned from `Read<TR0, TR1, ...>.Write<TW0, TW1, ...>.From()` methods
can be passed to an unmanaged function.

```c#
static class UpdateTranslationSystem
{
    public static unsafe void loop_native(EntityArrays entities, float deltaTime)
    {
        foreach (var (count, velocities, translations) in
            Read<Velocity>.Write<Translation>.From(entities))
        {
            fixed (Velocity* velocitiesPtr = velocities)
            fixed (Translation* translationsPtr = translations)
            {
                update_translation(count, velocitiesPtr, translationsPtr, deltaTime);
            }
        }
    }
    
    [DllImport("native_library.dll")]
    private static extern void update_translation(
        int count,
        Velocity* velocities,
        Translation* translations,
        float deltaTime);
}
```

### ForEach extensions

`ForEach` extension methods for `EntityArrays` and `IComponentArray` take one of
the `ForEach_RR..WW..<T0, T1, ...>` delegates to iterate over components.

```c#
namespace EntitiesDotNet.Delegates;

public delegate void Func_RW<T0, T1>(in T0 arg0, ref T1 arg1);
public delegate void Func_RW_I<T0, T1>(in T0 arg0, ref T1 arg1, int index);
```

```c#
static class UpdateTranslationSystem
{
    public static void Execute(EntityArrays entities, float deltaTime)
    {
        entities.ForEach((in Velocity v, ref Translation t) => t += v * deltaTime);
    }
}
```

### ForEach extensions inlining

`ForEach` extension method calls can be [inlined](#inlining-source-generator).

> **Info**: This is one of the [fastest](#performance) ways to iterate over components

```c#
static partial class UpdateTranslationSystem
{
    [Inline] static void _Execute(EntityArrays entities, float deltaTime)
    {
        entities.ForEach((in Velocity v, ref Translation t) => t += v * deltaTime);
    }
}
```

<details>

<summary>Generated <code>UpdateTranslationSystem.Execute</code></summary>

```c#
#define SOURCEGEN
#pragma warning disable CS0105 // disables warning about using the same namespaces several times

using EntitiesDotNet;

static partial class UpdateTranslationSystem
{
    [EntitiesDotNet.GeneratedFrom(nameof(_Execute))]
    public static void Execute(EntityArrays entities, float deltaTime)
    {
        {
            var @this = entities;
            foreach (var __array in @this)
            {
                if (__array.Count == 0 || !__array.Archetype.Contains<Velocity, Translation>())
                {
                    continue;
                }

                var __count = __array.Count;
                ref var v = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(__array.GetSpan<Velocity>());
                ref var t = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(__array.GetSpan<Translation>());
                for (var __i = 0; __i < __count; v = ref System.Runtime.CompilerServices.Unsafe.Add(ref v, 1), t = ref System.Runtime.CompilerServices.Unsafe.Add(ref t, 1), ++__i)
                {
                    t += v * deltaTime;
                }
            }
        }
    }
}
#pragma warning restore CS0104
```

</details>

## Iterating over components using EntityRef

.NET 7 introduced
[ref fields](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/ref#ref-fields)
that can be used to access entity's components.

> **Warning**: `ref fields` are not supported for runtime targets lower than .NET 7.

Create a `ref partial struct` with mutable `ref` fields of component types (`ref` itself can
be readonly but the field must be mutable). Mark the struct with `[EntityRef]` attribute. The struct
must be public or internal to support [`ForEach` extension methods](#foreach-extensions-1)
for `EntityArrays` and `IComponentArray`.

```c#
static partial class UpdateTranslationSystem
{
    [EntityRef]
    public ref partial struct ThisEntity
    {
        public ref Translation Translation;
        public ref readonly Velocity Velocity;   // mutable field, readonly reference
    }
}
```

## foreach loop

EntityRef generator creates static method `From(IComponentArray)` that
can be used with `foreach` loop:

> **Info**: This is one of the [fastest](#performance) ways to iterate over components

```c#
static partial class UpdateTranslationSystem
{
    [EntityRef]
    ref partial struct ThisEntity
    {
        public ref Translation Translation;
        public ref readonly Velocity Velocity;
    }
    
    public static void Execute(EntityArrays entityArrays, float deltaTime)
    {
        foreach (var array in entityArrays)
        foreach (var entity in ThisEntity.From(array))
        {
            entity.Translation += entity.Velocity * deltaTime;
        }
    }
}
```

### ForEach extensions

If the `EntityRef` struct is public or internal, then `ForEach` extension methods for `EntityArrays`
and `IComponentArray` are generated:

```c#
static partial class UpdateTranslationSystem
{
    [EntityRef]
    public ref partial struct ThisEntity
    {
        public ref Translation Translation;
        public ref readonly Velocity Velocity;
    }

    public static void Execute(EntityArrays entities, float deltaTime)
    {
        entities.ForEach((in ThisEntity entity) =>
            entity.Translation += entity.Velocity * deltaTime);
    }
}
```

### ForEach extensions inlining

`ForEach` extension method calls can be [inlined](#inlining-source-generator).

> **Info**: This is one of the [fastest](#performance) ways to iterate over components

```c#
static partial class UpdateTranslationSystem
{
    [EntityRef]
    public ref partial struct ThisEntity
    {
        public ref Translation Translation;
        public ref readonly Velocity Velocity;
    }

    [Inline] static void _Execute(EntityArrays entities, float deltaTime)
    {
        entities.ForEach((in ThisEntity entity) =>
            entity.Translation += entity.Velocity * deltaTime);
    }
}
```

<details>

<summary>Generated method <code>UpdateTranslationSystem.Execute</code></summary>

```c#
#define SOURCEGEN
#pragma warning disable CS0105 // disables warning about using the same namespaces several times

using EntitiesDotNet;

static partial class UpdateTranslationSystem
{
    [EntitiesDotNet.GeneratedFrom(nameof(_Execute))]
    public static void Execute(EntityArrays entities, float deltaTime)
    {
        {
            var @this = entities;
            UpdateTranslationSystem.ThisEntity entity = default;
            foreach (var __array in @this)
            {
                var(__count, __VelocitySpan, __TranslationSpan) = EntitiesDotNet.Read<Velocity>.Write<Translation>.From(__array);
                if (__count == 0)
                    continue;
                ref var __Velocity = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(__VelocitySpan);
                ref var __Translation = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(__TranslationSpan);
                for (var __i = 0; __i < __count; __Velocity = ref System.Runtime.CompilerServices.Unsafe.Add(ref __Velocity, 1), __Translation = ref System.Runtime.CompilerServices.Unsafe.Add(ref __Translation, 1), ++__i)
                {
                    entity.Velocity = ref __Velocity;
                    entity.Translation = ref __Translation;
                    entity.Translation += entity.Velocity * deltaTime;
                }
            }
        }
    }
}
#pragma warning restore CS0104
```

</details>

## Inlining source generator

`EntitiesDotNet.Generators` includes a source generator that inlines a body of a lambda that is
passed to `ForEach` extension method ([for individual components](#foreach-extensions-inlining)
and [EntityRefs](#foreach-extensions-inlining-1)). The inlined methods do not allocate memory on the
heap and iterate over components with [near-native performance](#performance).

Given the following code:

```c#
static partial class InliningExample
{
    [Inline] static int _SumPositiveIntegers(EntityArrays entities)
    {
        var sum = 0;
        entities.ForEach((in int i) =>
        {
            if (i <= 0) return;
            sum += i;
        });

        return sum;
    }
}
```

Inlining generator will create a method called `SumPositiveIntegers` that will

* iterate over component spans from `EntityArrays`
  using [InteropServices.MemoryMarshal.GetReference](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.memorymarshal.getreference)
  and [CompilerServices.Unsafe.Add](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.unsafe.add)
  methods from `System.Runtime` namespace

* inline the lambda body, substituting `return` statement for `continue` statement.

Generated code:

```c#
#define SOURCEGEN
#pragma warning disable CS0105 // disables warning about using the same namespaces several times

using EntitiesDotNet;

static partial class InliningExample
{
    [EntitiesDotNet.GeneratedFrom(nameof(_SumPositiveIntegers))]
    public static int SumPositiveIntegers(EntityArrays entities)
    {
        var sum = 0;
        {
            var @this = entities;
            foreach (var __array in @this)
            {
                if (__array.Count == 0 || !__array.Archetype.Contains<int>())
                {
                    continue;
                }

                var __count = __array.Count;
                ref var i = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(__array.GetSpan<int>());
                for (var __i = 0; __i < __count; i = ref System.Runtime.CompilerServices.Unsafe.Add(ref i, 1), ++__i)
                {
                    {
                        if (i <= 0)
                            continue;
                        sum += i;
                    }
                }
            }
        }

        return sum;
    }
}
#pragma warning restore CS0104
```

### Requirements

For an inlined method to be generated following conditions must be met:

1) Containing type must be `partial`

   ```c#
   static partial class InliningExample
   ```

2) The original method must be marked with one of the attributes:

    * `[Inline]`
    * `[Inline.Private]`
    * `[Inline.Protected]`
    * `[Inline.Internal]`
    * `[Inline.Public]`

   These attributes take a `string? name = null` argument that defines a name for a method that will
   be generated.

    ```c#
    [Inline.Public("InlinedSumPositiveIntegers")] static int SumPositiveIntegers(EntityArrays entities)
    // Inlined method name: 'InlinedSumPositiveIntegers'
    ```

   If the attribute's argument `name` is null then the generated method name will be determined by
   the following rule:

   If the original method's name starts with an underscore, then the name without an underscore will
   be used.

    ```c#
    [Inline.Public] static int _SumPositiveIntegers(EntityArrays entities)
    // Inlined method name: 'SumPositiveIntegers'
    ```

   If the original method's name does not start with an underscore, then the name with `_Inlined` at
   the end will be used.

    ```c#
    [Inline.Public] static int SumPositiveIntegers(EntityArrays entities)
    // Inlined method name: 'SumPositiveIntegers_Inlined'
    ```

## Performance

The following benchmark measured a performance of updating `Translation` from `Velocity`
for `2 x N` entities.

```c#
struct float3
{
    public float X;
    public float Y;
    public float Z;
}

struct Velocity
{
    public float3 Float3;
}

struct Translation {
    public float3 Float3;
}

void UpdateTranslation(in Velocity v, ref Translation t, float deltaTime)
{
    t.Float3.X += v.Float3.X * deltaTime;
    t.Float3.Y += v.Float3.Y * deltaTime;
    t.Float3.Z += v.Float3.Z * deltaTime;
}
```

Component systems included in the benchmark:

* `native` - uses native C++ arrays
* `loop_native` -
  uses [foreach loop that calls unmanaged C++ function](#foreach-loop-with-unmanaged-function-call)
* `loop` - uses [foreach loop](#foreach-loop)
* `ext` - uses [ForEach extensions](#foreach-extensions)
* `ext_inl` - uses [inlined ForEach extensions](#foreach-extensions-inlining)
* `ER_loop` - uses [EntityRef's](#iterating-over-components-using-entityref)
  [foreach loop](#foreach-loop-1)
* `ER_ext` - uses [EntityRef's](#iterating-over-components-using-entityref)
  [ForEach extensions](#foreach-extensions-1)
* `ER_ext_inl` - uses [EntityRef's](#iterating-over-components-using-entityref)
  inlined [ForEach extensions](#foreach-extensions-1)

Full code of the component
systems: [ComponentSystems.cs](./EntitiesDotNet.Benchmarks/ComponentSystems.cs)

The benchmark was run on a system:

```
BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19045.2728)
AMD Ryzen 5 5600U with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK=7.0.101
[Host]     : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
DefaultJob : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
```

### 2 x 1 000 Entities

|      Method |     Mean |     Error |    StdDev | Ratio | Allocated |
|------------ |---------:|----------:|----------:|------:|----------:|
|      native | 1.612 μs | 0.0036 μs | 0.0032 μs |  1.00 |         - |
| loop_native | 1.858 μs | 0.0019 μs | 0.0018 μs |  1.15 |         - |
|     ext_inl | 1.963 μs | 0.0381 μs | 0.0408 μs |  1.22 |         - |
|  ER_ext_inl | 1.926 μs | 0.0029 μs | 0.0027 μs |  1.19 |         - |
|     ER_loop | 1.931 μs | 0.0070 μs | 0.0065 μs |  1.20 |         - |
|        loop | 2.217 μs | 0.0072 μs | 0.0060 μs |  1.38 |         - |
|         ext | 3.784 μs | 0.0480 μs | 0.0513 μs |  2.35 |     120 B |
|      ER_ext | 4.028 μs | 0.0079 μs | 0.0070 μs |  2.50 |     120 B |

![](./Docs/1_000.png)

### 2 x 100 000 Entities

|      Method |     Mean |   Error |  StdDev | Ratio | Allocated |
|------------ |---------:|--------:|--------:|------:|----------:|
|      native | 173.3 μs | 0.58 μs | 0.49 μs |  1.00 |         - |
| loop_native | 168.4 μs | 0.40 μs | 0.37 μs |  0.97 |         - |
|     ext_inl | 174.4 μs | 1.76 μs | 1.37 μs |  1.01 |         - |
|  ER_ext_inl | 174.7 μs | 0.53 μs | 0.44 μs |  1.01 |         - |
|     ER_loop | 174.9 μs | 0.64 μs | 0.60 μs |  1.01 |         - |
|        loop | 206.8 μs | 0.71 μs | 0.66 μs |  1.19 |         - |
|         ext | 361.5 μs | 1.58 μs | 1.47 μs |  2.09 |     120 B |
|      ER_ext | 415.8 μs | 1.01 μs | 0.94 μs |  2.40 |     120 B |

![](./Docs/100_000.png)