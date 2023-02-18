using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using static EntitiesDotNet.Benchmarks.FloatTranslation.Functions;


namespace EntitiesDotNet.Benchmarks.FloatTranslation;


public class CalculateFloatTranslationBenchmark
{
    [Params(1_000_000)]
    public int N { get; set; }


    public const float DeltaTime = 1f / 30f;


    [GlobalSetup]
    public void GlobalSetup()
    {
        this._entityManager = new EntityManager();
        this._entityRefSystem = new EntityRefSystem(this.Entities);
        this._readWriteSystem = new ReadWriteSystem(this.Entities);
        this._foreachSystem = new ForEachSystem(this.Entities);
        this._foreachGeneratedSystem = new ForEachGeneratedSystem(this.Entities);


        var random = new Random(0);
        float RandomFloat() => random.NextSingle();

        SetRandomTranslation(SetRandomVelocity(
            CreateEntities(Archetype<Translation, Velocity>.Instance)));

        SetRandomVelocity(SetRandomAcceleration(
            CreateEntities(Archetype<Velocity, Acceleration>.Instance)));

        SetRandomTranslation(SetRandomVelocity(SetRandomAcceleration(
            CreateEntities(Archetype<Translation, Velocity, Acceleration>.Instance))));


        return;


        IEnumerable<IComponentArray> SetRandomTranslation(IEnumerable<IComponentArray> arrays)
        {
            arrays.ForEach((ref Translation value) => value = RandomFloat());
            return arrays;
        }


        IEnumerable<IComponentArray> SetRandomVelocity(IEnumerable<IComponentArray> arrays)
        {
            arrays.ForEach((ref Translation value) => value = RandomFloat());
            return arrays;
        }


        IEnumerable<IComponentArray> SetRandomAcceleration(IEnumerable<IComponentArray> arrays)
        {
            arrays.ForEach((ref Translation value) => value = RandomFloat());
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


    [Benchmark]
    public void EntityRef()
    {
        this._entityRefSystem.DeltaTime = DeltaTime;
        this._entityRefSystem.Execute();
    }


    [Benchmark]
    public void ReadWrite()
    {
        this._readWriteSystem.DeltaTime = DeltaTime;
        this._readWriteSystem.Execute();
    }


    [Benchmark]
    public void ForEach()
    {
        this._foreachSystem.DeltaTime = DeltaTime;
        this._foreachSystem.Execute();
    }


    [Benchmark]
    public void ForEachGenerated()
    {
        this._foreachGeneratedSystem.DeltaTime = DeltaTime;
        this._foreachGeneratedSystem.Execute();
    }


    private EntityManager _entityManager;
    private EntityRefSystem _entityRefSystem;
    private ReadWriteSystem _readWriteSystem;
    private ForEachSystem _foreachSystem;
    private ForEachGeneratedSystem _foreachGeneratedSystem;
    private EntityArrays Entities => this._entityManager.Entities;
}


public class EntityRefSystem : ComponentSystem
{
    public EntityRefSystem(EntityArrays entities) : base(entities)
    {
    }


    protected override void OnExecute()
    {
        foreach (var entity in AccelerationAndVelocity.From(this.Entities))
        {
            UpdateVelocity(entity.Acceleration, ref entity.Velocity, this.DeltaTime);
        }

        foreach (var entity in VelocityAndTranslation.From(this.Entities))
        {
            UpdateTranslation(entity.Velocity, ref entity.Translation, this.DeltaTime);
        }
    }


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

        foreach (var (count, velocity, translation) in
            Read<Velocity>.Write<Translation>.From(this.Entities))
        {
            for (var i = 0; i < count; ++i)
            {
                UpdateTranslation(velocity[i], ref translation[i], this.DeltaTime);
            }
        }
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
        this.Entities.ForEach((in Acceleration a, ref Velocity v) =>
            UpdateVelocity(a, ref v, this.DeltaTime));
        this.Entities.ForEach((in Velocity v, ref Translation t) =>
            UpdateTranslation(v, ref t, this.DeltaTime));
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
        
        this.Entities.ForEach((in Velocity v, ref Translation t) =>
            UpdateTranslation(v, ref t, this.DeltaTime));
    }
}


public static class Functions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateVelocity(in Acceleration a, ref Velocity v, float deltaTime)
    {
        v += a * deltaTime;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateTranslation(in Velocity v, ref Translation t, float deltaTime)
    {
        t += v * deltaTime;
    }
}


[EntityRefStruct]
public ref partial struct VelocityAndTranslation
{
    public ref readonly Velocity Velocity;
    public ref Translation Translation;
}


[EntityRefStruct]
public ref partial struct AccelerationAndVelocity
{
    public ref readonly Acceleration Acceleration;
    public ref Velocity Velocity;
}


[GenerateImplicitOperators]
public partial record struct Velocity
{
    public float Float;
}


[GenerateImplicitOperators]
public partial record struct Translation
{
    public float Float;
}


[GenerateImplicitOperators]
public partial record struct Acceleration
{
    public float Float;
}