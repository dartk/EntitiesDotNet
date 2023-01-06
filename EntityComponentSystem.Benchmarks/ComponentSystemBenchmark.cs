using System.Numerics;
using BenchmarkDotNet.Attributes;


namespace EntityComponentSystem.Benchmarks;


public partial class ComponentSystemBenchmark {

    [Params(100_000, 10_000_000)]
    public int N { get; set; }


    [GlobalSetup]
    public void Setup() {
        var entityManager = new EntityManager();
        var random = new Random(0);
        for (var i = 0; i < this.N; ++i) {
            entityManager.CreateEntity(
                new Translation { Vector = new Vector3(random.NextSingle()) },
                new Velocity { Vector = new Vector3(random.NextSingle()) }
            );
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