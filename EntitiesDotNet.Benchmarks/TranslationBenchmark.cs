using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using static EntitiesDotNet.Benchmarks.Functions;


namespace EntitiesDotNet.Benchmarks;


[MemoryDiagnoser]
public class TranslationBenchmark
{
    // [Params(10_000, 100_000, 1_000_000)]
    [Params(10_000)]
    public int N { get; set; }


    public const float DeltaTime = 1f / 30f;


    [GlobalSetup]
    public void GlobalSetup()
    {
        this._entityManager = new EntityManager();
        this._entityRefSystem = new EntityRefSystem(this.Entities);
        this._readWriteSystem = new ReadWriteSystem(this.Entities);
        this._readWriteNativeSystem = new ReadWriteNativeSystem(this.Entities);
        this._readWriteUnsafeSystem = new ReadWriteUnsafeSystem(this.Entities);
        this._foreachSystem = new ForEachSystem(this.Entities);
        this._foreachGeneratedSystem = new ForEachGeneratedSystem(this.Entities);
        this._enttSystem = new EnttSystem(this.N);

        var random = new Random(0);

        float3 RandomFloat3() => new(
            random.NextSingle(),
            random.NextSingle(),
            random.NextSingle());

        SetRandomTranslation(SetRandomVelocity(
            CreateEntities(Archetype<Translation, Velocity>.Instance)));

        SetRandomVelocity(SetRandomAcceleration(
            CreateEntities(Archetype<Velocity, Acceleration>.Instance)));

        SetRandomTranslation(SetRandomVelocity(SetRandomAcceleration(
            CreateEntities(Archetype<Translation, Velocity, Acceleration>.Instance))));


        return;


        IEnumerable<IComponentArray> SetRandomTranslation(IEnumerable<IComponentArray> arrays)
        {
            arrays.ForEach((ref Translation value) => value = RandomFloat3());
            return arrays;
        }


        IEnumerable<IComponentArray> SetRandomVelocity(IEnumerable<IComponentArray> arrays)
        {
            arrays.ForEach((ref Translation value) => value = RandomFloat3());
            return arrays;
        }


        IEnumerable<IComponentArray> SetRandomAcceleration(IEnumerable<IComponentArray> arrays)
        {
            arrays.ForEach((ref Translation value) => value = RandomFloat3());
            return arrays;
        }

        IEnumerable<IComponentArray> CreateEntities(Archetype archetype)
        {
            for (var i = 0; i < this.N; ++i)
            {
                this._entityManager.CreateEntity(archetype);
            }

            return this.Entities.Where(x => x.Archetype == archetype);
        }
    }


    [GlobalCleanup]
    public void GlobalCleanup()
    {
        this._enttSystem.Dispose();
    }


    // [Benchmark]
    public void EntityRef()
    {
        this._entityRefSystem.DeltaTime = DeltaTime;
        this._entityRefSystem.Execute();
    }


    // [Benchmark]
    public void ReadWrite()
    {
        this._readWriteSystem.DeltaTime = DeltaTime;
        this._readWriteSystem.Execute();
    }


    [Benchmark]
    public void ReadWriteNative()
    {
        this._readWriteNativeSystem.DeltaTime = DeltaTime;
        this._readWriteNativeSystem.Execute();
    }


    // [Benchmark]
    public void ReadWriteUnsafe()
    {
        this._readWriteUnsafeSystem.DeltaTime = DeltaTime;
        this._readWriteUnsafeSystem.Execute();
    }


    // [Benchmark]
    public void ReadWriteUnsafe2()
    {
        this._readWriteUnsafeSystem.DeltaTime = DeltaTime;
        this._readWriteUnsafeSystem.Execute2();
    }


    // [Benchmark]
    public void ForEach()
    {
        this._foreachSystem.DeltaTime = DeltaTime;
        this._foreachSystem.Execute();
    }


    [Benchmark(Baseline = true)]
    public void ForEachGenerated()
    {
        this._foreachGeneratedSystem.DeltaTime = DeltaTime;
        this._foreachGeneratedSystem.Execute();
    }


    [Benchmark]
    public void EnTT()
    {
        this._enttSystem.UpdateVelocity(DeltaTime);
    }


    private EntityManager _entityManager;
    private EntityRefSystem _entityRefSystem;
    private ReadWriteSystem _readWriteSystem;
    private ReadWriteNativeSystem _readWriteNativeSystem;
    private ReadWriteUnsafeSystem _readWriteUnsafeSystem;
    private ForEachSystem _foreachSystem;
    private ForEachGeneratedSystem _foreachGeneratedSystem;
    private EnttSystem _enttSystem;
    private EntityArrays Entities => this._entityManager.Entities;
}


public class EntityRefSystem : ComponentSystem
{
    public EntityRefSystem(EntityArrays entities) : base(entities)
    {
    }


    protected override void OnExecute()
    {
        foreach (var entity in AccelerationAndVelocity.From(this.Entities))
        {
            UpdateVelocity(entity.Acceleration, ref entity.Velocity, this.DeltaTime);
        }

        // foreach (var entity in VelocityAndTranslation.From(this.Entities))
        // {
        //     UpdateTranslation(entity.Velocity, ref entity.Translation, this.DeltaTime);
        // }
    }


    public float DeltaTime = 1f / 30f;
}


public class ReadWriteSystem : ComponentSystem
{
    public ReadWriteSystem(EntityArrays entities) : base(entities)
    {
    }


    protected override void OnExecute()
    {
        foreach (var (count, acceleration, velocity) in
            Read<Acceleration>.Write<Velocity>.From(this.Entities))
        {
            for (var i = 0; i < count; ++i)
            {
                UpdateVelocity(acceleration[i], ref velocity[i], this.DeltaTime);
            }
        }

        // foreach (var (count, velocity, translation) in
        //     Read<Velocity>.Write<Translation>.From(this.Entities))
        // {
        //     for (var i = 0; i < count; ++i)
        //     {
        //         UpdateTranslation(velocity[i], ref translation[i], this.DeltaTime);
        //     }
        // }
    }


    public float DeltaTime = 1f / 30f;
}


public class ReadWriteUnsafeSystem : ComponentSystem
{
    public ReadWriteUnsafeSystem(EntityArrays entities) : base(entities)
    {
    }


    protected override unsafe void OnExecute()
    {
        foreach (var (count, acceleration, velocity) in
            Read<Acceleration>.Write<Velocity>.From(this.Entities))
        {
            fixed (Acceleration* accelerationPtr = acceleration)
            fixed (Velocity* velocityPtr = velocity)
            {
                var accelerationPtr2 = accelerationPtr;
                var velocityPtr2 = velocityPtr;
                for (var i = 0; i < count; ++i)
                {
                    UpdateVelocity(*accelerationPtr2, ref *velocityPtr2, this.DeltaTime);
                    ++accelerationPtr2;
                    ++velocityPtr2;
                }
            }
        }
    }


    public unsafe void Execute2()
    {
        foreach (var (count, acceleration, velocity) in
            Read<Acceleration>.Write<Velocity>.From(this.Entities))
        {
            fixed (Acceleration* accelerationPtr = acceleration)
            fixed (Velocity* velocityPtr = velocity)
            {
                var accelerationPtr2 = (float3*)accelerationPtr;
                var velocityPtr2 = (float3*)velocityPtr;
                
                for (var i = 0; i < count; ++i)
                {
                    UpdateVelocity(*accelerationPtr2++, ref *velocityPtr2++, this.DeltaTime);
                }
            }
        }
    }


    public float DeltaTime;
}


public class ReadWriteNativeSystem : ComponentSystem
{
    public ReadWriteNativeSystem(EntityArrays entities) : base(entities)
    {
    }


    protected override unsafe void OnExecute()
    {
        foreach (var (count, acceleration, velocity) in
            Read<Acceleration>.Write<Velocity>.From(this.Entities))
        {
            fixed (Acceleration* accelerationPtr = acceleration)
            fixed (Velocity* velocityPtr = velocity)
            {
                Native.update_velocity(count, accelerationPtr, velocityPtr, this.DeltaTime);
            }
        }

        // foreach (var (count, velocity, translation) in
        //     Read<Velocity>.Write<Translation>.From(this.Entities))
        // {
        //     fixed (void* velocityPtr = velocity, translationPtr = translation)
        //     {
        //         Native.update_translation(count, velocityPtr, translationPtr, this.DeltaTime);
        //     }
        // }
    }


    public float DeltaTime = 1f / 30f;
}


public class ForEachSystem : ComponentSystem
{
    public ForEachSystem(EntityArrays entities) : base(entities)
    {
    }


    public float DeltaTime = 1f / 30f;


    protected override void OnExecute()
    {
        this.Entities.ForEach((in Acceleration a, ref Velocity v) =>
            UpdateVelocity(a, ref v, this.DeltaTime));
        // this.Entities.ForEach((in Velocity v, ref Translation t) =>
        //     UpdateTranslation(v, ref t, this.DeltaTime));
    }
}


public partial class ForEachGeneratedSystem : ComponentSystem
{
    public ForEachGeneratedSystem(EntityArrays entities) : base(entities)
    {
    }


    public float DeltaTime = 1f / 30f;


    [GenerateOnExecute]
    protected override void OnExecute()
    {
        this.Entities.ForEach((in Acceleration a, ref Velocity v) =>
            UpdateVelocity(a, ref v, this.DeltaTime));

        // this.Entities.ForEach((in Velocity v, ref Translation t) =>
        //     UpdateTranslation(v, ref t, this.DeltaTime));
    }
}


public static class Functions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateVelocity(in Acceleration a, ref Velocity v, float deltaTime)
    {
        v.Float3.X += a.Float3.X * deltaTime;
        v.Float3.Y += a.Float3.Y * deltaTime;
        v.Float3.Z += a.Float3.Z * deltaTime;
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateVelocity(in float3 a, ref float3 v, float deltaTime)
    {
        v.X += a.X * deltaTime;
        v.Y += a.Y * deltaTime;
        v.Z += a.Z * deltaTime;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateTranslation(in Velocity v, ref Translation t, float deltaTime)
    {
        t.Float3.X += v.Float3.X * deltaTime;
        t.Float3.Y += v.Float3.Y * deltaTime;
        t.Float3.Z += v.Float3.Z * deltaTime;
    }
}


[EntityRefStruct]
public ref partial struct VelocityAndTranslation
{
    public ref readonly Velocity Velocity;
    public ref Translation Translation;
}


[EntityRefStruct]
public ref partial struct AccelerationAndVelocity
{
    public ref readonly Acceleration Acceleration;
    public ref Velocity Velocity;
}


[GenerateImplicitOperators]
public partial record struct Velocity
{
    public float3 Float3;
}


[GenerateImplicitOperators]
public partial record struct Translation
{
    public float3 Float3;
}


[GenerateImplicitOperators]
public partial record struct Acceleration
{
    public float3 Float3;
}