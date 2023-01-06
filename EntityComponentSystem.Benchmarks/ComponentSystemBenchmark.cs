using System.Numerics;
using BenchmarkDotNet.Attributes;


namespace EntityComponentSystem.Benchmarks;


public partial class ComponentSystemBenchmark {

    [Params(10_000_000)]
    public int N { get; set; }


    [GlobalSetup]
    public void Setup() {
        var entityManager = new EntityManager();
        var array = entityManager.GetArray(Archetype<Translation, Velocity>.Instance);
        array.EnsureCapacity(this.N);
        
        for (var i = 0; i < this.N; ++i) {
            entityManager.CreateEntity(Archetype<Translation, Velocity>.Instance);
        }
        
        var random = new Random(0);
        var (count, translations, velocities) = array.Write<Translation, Velocity>();
        for (var i = 0; i < count; ++i) {
            translations[i].Vector = new Vector3(random.NextSingle());
            velocities[i].Vector = new Vector3(random.NextSingle());
        }

        this._system = entityManager.CreateSystem<BenchmarkSystem>();
    }


    [Benchmark]
    public void ExecuteSystem() {
        this._system.Execute();
    }


    private BenchmarkSystem _system;


    private partial class BenchmarkSystem : ComponentSystem {

        [GenerateOnExecute]
        protected override void OnExecute() {
            var deltaTime = 1f / 60f;
            this.Entities.ForEach((in Velocity velocity, ref Translation translation) => {
                translation += deltaTime * velocity.Vector;
            });
        }

    }
}