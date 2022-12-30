﻿using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
internal ref partial struct UpdateTranslationQuery {
    public ref readonly Velocity Velocity;
    public ref Translation Translation;
}


[Query]
internal ref partial struct ResetTranslationQuery {
    public ref Translation Translation;
}


// [SimpleJob(launchCount: 2, warmupCount: 5, targetCount: 5, invocationCount: 5)]
[MemoryDiagnoser]
public partial class IterationBenchmark {

    [Params(1_000, 10_000, 100_000)]
    public int N;


    [GlobalSetup]
    public void Setup() {
        var array =
            new ComponentArray(Archetype<Velocity, Translation>.Instance, this.N);
        array.Add(this.N);
        this._array = array;

        var random = new Random(0);
        this._array.ForEach((ref Velocity v) => {
            v.Vector = new Vector3(random.NextSingle());
        });
    }


    // [Benchmark]
    public void QueryForeach() {
        foreach (var item in ResetTranslationQuery.Select(this._array)) {
            item.Translation = Vector3.Zero;
        }

        var deltaTime = 1f / 60f;

        foreach (var item in UpdateTranslationQuery.Select(this._array)) {
            item.Translation += deltaTime * item.Velocity.Vector;
        }
    }


    [Benchmark]
    public void QueryIndex() {
        var deltaTime = 1f / 60f;

        {
            var array = ResetTranslationQuery.Select(this._array);
            var count = array.Length;
            for (var i = 0; i < count; ++i) {
                var item = array[i];
                item.Translation = Vector3.Zero;
            }
        }

        {
            var array = UpdateTranslationQuery.Select(this._array);
            var count = array.Length;
            for (var i = 0; i < count; ++i) {
                var item = array[i];
                item.Translation += deltaTime * item.Velocity.Vector;
            }
        }
    }


    // [Benchmark]
    public void ReadWrite() {
        var deltaTime = 1f / 60f;

        var (count, velocity, translation) =
            this._array.Read<Velocity>().Write<Translation>();

        for (var i = 0; i < count; ++i) {
            translation[i] += deltaTime * velocity[i].Vector;
        }
    }


    // [Benchmark]
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
        this._array.ForEach((ref Translation translation) => {
            translation.Vector = Vector3.Zero;
        });

        this._array.ForEach((in Velocity velocity, ref Translation translation) => {
            var deltaTime = 1f / 60f;
            translation.Vector = deltaTime * velocity.Vector;
        });
    }


    [Benchmark]
    public void Manual() {
        {
            if (!this._array.Archetype.Contains<Translation>()) {
                return;
            }

            var count = this._array.Count;
            var translation = this._array.GetSpan<Translation>();
            for (var i = 0; i < count; ++i) {
                translation[i].Vector = Vector3.Zero;
            }
        }

        {
            var deltaTime = 1f / 60f;
            if (!this._array.Archetype.Contains<Velocity, Translation>()) {
                return;
            }

            var count = this._array.Count;
            var velocity = this._array.GetReadOnlySpan<Velocity>();
            var translation = this._array.GetSpan<Translation>();

            for (var i = 0; i < count; ++i) {
                translation[i].Vector = deltaTime * velocity[i].Vector;
            }
        }
    }


    [GenerateIteration]
    private partial struct GeneratedIteration : IComponentArrayIterationExpression {

        public void IterationExpression(IComponentArray array) {
            array.ForEach((ref Translation translation) =>
                translation.Vector = Vector3.Zero
            );

            var deltaTime = 1f / 60f;

            array.ForEach((in Velocity velocity, ref Translation translation) =>
                translation.Vector += velocity.Vector * deltaTime
            );
        }

    }


    [Benchmark]
    public void GeneratedFromForEach() {
        var generated = new GeneratedIteration();
        generated.IterationExpression_Generated(this._array);
    }


    private IComponentArray _array;
}