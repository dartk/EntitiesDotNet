using System.Numerics;
using BenchmarkDotNet.Attributes;


namespace EntitiesDotNet.Benchmarks;


[MemoryDiagnoser]
public partial class ComponentSystemBenchmark
{
    [Params(1_000, 10_000, 100_000)]
    public int N { get; set; }


    [GlobalSetup]
    public void Setup()
    {
        this._entityManager = new EntityManager();
        var array = this._entityManager.GetArray(Archetype<Translation, Velocity>.Instance);
        array.EnsureCapacity(this.N);

        for (var i = 0; i < this.N; ++i)
        {
            this._entityManager.CreateEntity(Archetype<Translation, Velocity>.Instance);
        }

        var random = new Random(0);
        var (count, translations, velocities) =
            array.Select(Selector.Write<Translation, Velocity>());
        for (var i = 0; i < count; ++i)
        {
            translations[i].Vector = new Vector3(random.NextSingle());
            velocities[i].Vector = new Vector3(random.NextSingle());
        }

        this._system = new BenchmarkSystem(this._entityManager.Entities);
    }


    [Benchmark(Baseline = true)]
    public void ExecuteReadWrite()
    {
        var deltaTime = 1f / 60f;
        foreach (var array in this._entityManager.Entities)
        {
            var (count, velocities, translations) =
                array.Select(Selector.Read<Velocity>().Write<Translation>());
            for (var i = 0; i < count; ++i)
            {
                ref var translation = ref translations[i];
                ref readonly var velocity = ref velocities[i];
                translation.Vector += deltaTime * velocity.Vector;
            }
        }
    }


    [Benchmark]
    public void ExecuteGenerated()
    {
        this._system.Execute();
    }


    [Benchmark]
    public void ExecuteOriginal()
    {
        this._system.Execute(false);
    }


    private BenchmarkSystem _system;
    private EntityManager _entityManager;


    private partial record BenchmarkSystem(EntityArrays Entities)
        : ComponentSystem(Entities)
    {
        [GenerateOnExecute]
        protected override void OnExecute()
        {
            var deltaTime = 1f / 60f;
            this.Entities.ForEach((in Velocity velocity, ref Translation translation) =>
            {
                translation += deltaTime * velocity.Vector;
            });
        }
    }
}