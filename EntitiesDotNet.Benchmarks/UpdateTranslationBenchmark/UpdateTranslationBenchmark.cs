using BenchmarkDotNet.Attributes;
using EntitiesDotNet.Benchmarks.UpdateTranslationBenchmark.Systems;


namespace EntitiesDotNet.Benchmarks.UpdateTranslationBenchmark;


[MemoryDiagnoser, RPlotExporter]
public class UpdateTranslationBenchmark
{
    // [Params(10_000, 100_000, 1_000_000)]
    [Params(100_000)]
    public int N { get; set; }


    public const float DeltaTime = 1f / 30f;


    [GlobalSetup]
    public void GlobalSetup()
    {
        this._entityManager = new EntityManager();
        this._enttSystem = new EnttSystem(this.N);
        this._nativeArrays = Native.arrays_new(2 * this.N);

        var random = new Random(0);

        float3 RandomFloat3() => new(
            random.NextSingle(),
            random.NextSingle(),
            random.NextSingle());

        SetRandomTranslation(SetRandomVelocity(
            CreateEntities(Archetype.Instance<Translation, Velocity>())));

        SetRandomVelocity(SetRandomAcceleration(
            CreateEntities(Archetype.Instance<Velocity, Acceleration>())));

        SetRandomTranslation(SetRandomVelocity(SetRandomAcceleration(
            CreateEntities(Archetype.Instance<Translation, Velocity, Acceleration>()))));


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
        Native.arrays_delete(this._nativeArrays);
    }


    [Benchmark(Baseline = true)]
    public void NativeArrays()
    {
        Native.arrays_update(this._nativeArrays, DeltaTime);
    }


    [Benchmark]
    public void ReadWrite_Native()
    {
        ReadWriteSystem.Execute_Native(this.Entities, DeltaTime);
    }


    [Benchmark]
    public void ReadWrite_Managed()
    {
        ReadWriteSystem.Execute_Managed(this.Entities, DeltaTime);
    }


    [Benchmark]
    public void EnTT()
    {
        this._enttSystem.UpdateVelocity(DeltaTime);
    }


    [Benchmark]
    public void ForEach_Inlined()
    {
        ForEachSystem.Execute_Inlined(this.Entities, DeltaTime);
    }


    [Benchmark]
    public void ForEach_Lambda()
    {
        ForEachSystem.Execute_Lambda(this.Entities, DeltaTime);
    }
    
    
    [Benchmark]
    public void EntityRef_From()
    {
        EntityRefSystem.Execute_From(this.Entities, DeltaTime);
    }


    [Benchmark]
    public void EntityRef_ForEach_Lambda()
    {
        EntityRefSystem.Execute_Lambda(this.Entities, DeltaTime);
    }


    [Benchmark]
    public void EntityRef_ForEach_Inlined()
    {
        EntityRefSystem.Execute_Inlined(this.Entities, DeltaTime);
    }


    private EntityManager _entityManager;
    private EnttSystem _enttSystem;
    private nint _nativeArrays;
    private EntityArrays Entities => this._entityManager.Entities;
}