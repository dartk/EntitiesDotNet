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
    public void EntityRef_ForEach()
    {
        this._entityRefSystem.DeltaTime = DeltaTime;
        // this._entityRefSystem.OnExecuteForEach();
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


    [Benchmark(Baseline = true)]
    public void ForEachInlined()
    {
        ForEachInlinedSystem.Execute_Inlined(this.Entities, DeltaTime);
    }
    
    
    // [Benchmark]
    // public void ForEachInlined_EntityRef()
    // {
    //     ForEachInlinedSystem_EntityRef.Execute_Inlined(this.Entities, DeltaTime);
    // }


    [Benchmark]
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


    [Benchmark]
    public void ForEach()
    {
        ForEachInlinedSystem.Execute_Regular(this.Entities, DeltaTime);
    }
    
    
    // [Benchmark]
    // public void ForEach_EntityRef()
    // {
    //     ForEachInlinedSystem_EntityRef.Execute_Regular(this.Entities, DeltaTime);
    // }


    private EntityManager _entityManager;
    private EntityRefSystem _entityRefSystem;
    private ReadWriteSystem _readWriteSystem;
    private ReadWriteNativeSystem _readWriteNativeSystem;
    private ForEachSystem _foreachSystem;
    private ForEachGeneratedSystem _foreachGeneratedSystem;
    private EnttSystem _enttSystem;
    private EntityArrays Entities => this._entityManager.Entities;
}



[EntityRefStruct]
public ref partial struct UpdateVelocityEntity
{
    public ref readonly Acceleration Acceleration;
    public ref Velocity Velocity;
}


public partial class EntityRefSystem : ComponentSystem
{
    public EntityRefSystem(EntityArrays entities) : base(entities)
    {
    }


    [EntityRefStruct]
    private ref partial struct UpdateTranslationEntity
    {
        public ref readonly Velocity Velocity;
        public ref Translation Translation;
    }


    protected override void OnExecute()
    {
        foreach (var entity in UpdateVelocityEntity.From(this.Entities))
        {
            UpdateVelocity(entity.Acceleration, ref entity.Velocity, this.DeltaTime);
        }

        // foreach (var entity in VelocityAndTranslation.From(this.Entities))
        // {
        //     UpdateTranslation(entity.Velocity, ref entity.Translation, this.DeltaTime);
        // }
    }


    // public void OnExecuteForEach()
    // {
    //     UpdateVelocityEntity.ForEach_inlining(this.Entities,
    //         static entity => UpdateVelocity(entity.Acceleration, ref entity.Velocity, 1f / 30f));
    // }


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
        this.Entities.ForEach([Inline](in Acceleration a, ref Velocity v) =>
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


public static partial class ForEachInlinedSystem
{
    [Inline.Public(nameof(Execute_Inlined))]
    public static void Execute_Regular(EntityArrays entities, float deltaTime)
    {
        entities.ForEach([Inline](in Acceleration a, ref Velocity v) =>
        {
            UpdateVelocity(a, ref v, deltaTime);
        });
    }
}


public static partial class ForEachInlinedSystem_EntityRef
{
    [Inline.Public(nameof(Execute_Inlined))]
    public static void Execute_Regular(EntityArrays entities, float deltaTime)
    {
        UpdateVelocityEntity.ForEach_inlining(entities, [Inline](entity) =>
        {
            UpdateVelocity(entity.Acceleration, ref entity.Velocity, deltaTime);
        });
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