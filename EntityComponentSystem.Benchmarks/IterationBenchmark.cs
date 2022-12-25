using System.Numerics;
using BenchmarkDotNet.Attributes;


namespace EntityComponentSystem.Benchmarks;


[GenerateImplicitOperators]
public partial record struct Velocity {
    public Vector3 Vector;
}


[GenerateImplicitOperators]
public partial record struct Translation {
    public Vector3 Vector;
}


[Query]
internal ref partial struct Query {
    public ref readonly Velocity Velocity;
    public ref Translation Translation;
}


// [SimpleJob(launchCount: 2, warmupCount: 5, targetCount: 5, invocationCount: 5)]
[MemoryDiagnoser]
public class IterationBenchmark {

    [Params(500000)] public int N;


    [GlobalSetup]
    public void Setup() {
        var array = 
            new ComponentArray(Archetype<Velocity, Translation>.Instance, this.N);
        array.Add(this.N);
        this._array = array;
    }


    [Benchmark]
    public void QueryForeach() {
        var deltaTime = 1f / 60f;

        foreach (var item in Query.Select(this._array)) {
            item.Translation += deltaTime * item.Velocity.Vector;
        }
    }


    [Benchmark]
    public void QueryIndex() {
        var deltaTime = 1f / 60f;

        var array = Query.Select(this._array);
        var count = array.Length;
        for (var i = 0; i < count; ++i) {
            var item = array[i];
            item.Translation += deltaTime * item.Velocity.Vector;
        }
    }


    [Benchmark]
    public void ReadWrite() {
        var deltaTime = 1f / 60f;
        
        var (count, velocity, translation) =
            this._array.Read<Velocity>().Write<Translation>();

        for (var i = 0; i < count; ++i) {
            translation[i] += deltaTime * velocity[i].Vector;
        }
    }


    [Benchmark]
    public void ReadWriteNoCasting() {
        var deltaTime = 1f / 60f;
        
        var (count, velocity, translation) =
            this._array.Read<Velocity>().Write<Translation>();

        for (var i = 0; i < count; ++i) {
            translation[i].Vector += deltaTime * velocity[i].Vector;
        }
    }


    [Benchmark]
    public void ForEach() {
        this._array.ForEach((in Velocity velocity, ref Translation translation) => {
            var deltaTime = 1f / 60f;
            translation.Vector = deltaTime * velocity.Vector;
        });
    }


    [Benchmark]
    public void Manual() {
        if (!this._array.Archetype.Contains<Velocity, Translation>()) {
            return;
        }
        
        var deltaTime = 1f / 60f;

        var count = this._array.Count;
        var velocity = this._array.GetReadOnlySpan<Velocity>();
        var translation = this._array.GetSpan<Translation>();

        for (var i = 0; i < count; ++i) {
            translation[i].Vector = deltaTime * velocity[i].Vector;
        }
    }
    

    private IComponentArray _array;
}