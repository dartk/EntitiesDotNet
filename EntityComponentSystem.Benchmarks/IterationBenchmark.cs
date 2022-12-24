using System.Numerics;
using BenchmarkDotNet.Attributes;


namespace EntityComponentSystem.Benchmarks;


[GenerateImplicitOperators]
public partial struct Velocity {
    public Vector3 Vector;
}


[GenerateImplicitOperators]
public partial struct Translation {
    public Vector3 Vector;
}


[Query]
internal ref partial struct Query {
    public ref readonly Velocity Velocity;
    public ref Translation Translation;
}


[SimpleJob(launchCount: 2, warmupCount: 5, targetCount: 5, invocationCount: 5)]
[MemoryDiagnoser]
public class IterationBenchmark {

    [Params(100000)] public int N;


    [GlobalSetup]
    public void Setup() {
        this._componentArray =
            new ComponentArray(Archetype<Velocity, Translation>.Instance, this.N);
    }


    [Benchmark]
    public void QueryForeach() {
        var deltaTime = 1f / 60f;

        foreach (var item in Query.Select(this._componentArray)) {
            item.Translation += deltaTime * item.Velocity.Vector;
        }
    }


    [Benchmark]
    public void ReadWrite() {
        var deltaTime = 1f / 60f;
        
        var (count, velocity, translation) =
            this._componentArray.Read<Velocity>().Write<Translation>();

        for (var i = 0; i < count; ++i) {
            translation[i] += deltaTime * velocity[i].Vector;
        }
    }


    private IComponentArray _componentArray;
}