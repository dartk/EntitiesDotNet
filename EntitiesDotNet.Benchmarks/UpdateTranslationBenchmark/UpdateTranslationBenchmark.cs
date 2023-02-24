﻿using BenchmarkDotNet.Attributes;
using EntitiesDotNet.Benchmarks.UpdateTranslationBenchmark.Systems;


namespace EntitiesDotNet.Benchmarks.UpdateTranslationBenchmark;


[MemoryDiagnoser]
public class UpdateTranslationBenchmark
{
    // [Params(10_000, 100_000, 1_000_000)]
    [Params(10_000)]
    public int N { get; set; }


    public const float DeltaTime = 1f / 30f;


    [GlobalSetup]
    public void GlobalSetup()
    {
        this._entityManager = new EntityManager();
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


    [Benchmark(Baseline = true)]
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
    public void ForEach_Generated()
    {
        this._foreachGeneratedSystem.DeltaTime = DeltaTime;
        this._foreachGeneratedSystem.Execute();
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
    public void EntityRefForEach_Lambda()
    {
        EntityRefForEachSystem.Execute_Lambda(this.Entities, DeltaTime);
    }


    [Benchmark]
    public void EntityRefForEach_Inlined()
    {
        EntityRefForEachSystem.Execute_Inlined(this.Entities, DeltaTime);
    }


    private EntityManager _entityManager;
    private ForEachGeneratedSystem _foreachGeneratedSystem;
    private EnttSystem _enttSystem;
    private EntityArrays Entities => this._entityManager.Entities;
}