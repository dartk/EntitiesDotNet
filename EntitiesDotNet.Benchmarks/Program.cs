using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using EntitiesDotNet;
using EntitiesDotNet.Benchmarks;


BenchmarkRunner.Run(typeof(UpdateTranslationBenchmark));


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
            arrays.ForEach((ref Translation value) => value.Float3 = RandomFloat3());
            return arrays;
        }


        IEnumerable<IComponentArray> SetRandomVelocity(IEnumerable<IComponentArray> arrays)
        {
            arrays.ForEach((ref Translation value) => value.Float3 = RandomFloat3());
            return arrays;
        }


        IEnumerable<IComponentArray> SetRandomAcceleration(IEnumerable<IComponentArray> arrays)
        {
            arrays.ForEach((ref Translation value) => value.Float3 = RandomFloat3());
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
        Native.arrays_delete(this._nativeArrays);
    }


    [Benchmark(Baseline = true)]
    public void native()
    {
        Native.arrays_update(this._nativeArrays, DeltaTime);
    }


    [Benchmark]
    public void loop_native()
    {
        ComponentSystems.loop_native(this.Entities, DeltaTime);
    }


    [Benchmark]
    public void ext_inl()
    {
        ComponentSystems.ext_inl(this.Entities, DeltaTime);
    }


    [Benchmark]
    public void ER_ext_inl()
    {
        ComponentSystems.ER_ext_inl(this.Entities, DeltaTime);
    }


    [Benchmark]
    public void loop()
    {
        ComponentSystems.loop(this.Entities, DeltaTime);
    }


    [Benchmark]
    public void ext()
    {
        ComponentSystems.ext(this.Entities, DeltaTime);
    }


    [Benchmark]
    public void ER_ext()
    {
        ComponentSystems.ER_ext(this.Entities, DeltaTime);
    }


    [Benchmark]
    public void ER_loop()
    {
        ComponentSystems.ER_loop(this.Entities, DeltaTime);
    }


    [Benchmark]
    public void GenerateSystem()
    {
        ComponentSystems.UpdateTranslationSystem(this.Entities);
    }


    private EntityManager _entityManager;
    private nint _nativeArrays;
    private EntityArrays Entities => this._entityManager.Entities;
}
