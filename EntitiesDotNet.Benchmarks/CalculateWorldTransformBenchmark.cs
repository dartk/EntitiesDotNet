using System.Numerics;
using BenchmarkDotNet.Attributes;


namespace EntitiesDotNet.Benchmarks;


public partial class CalculateWorldTransformSystem_ForEachGenerated : ComponentSystem
{
    public CalculateWorldTransformSystem_ForEachGenerated(EntityArrays entities) : base(entities)
    {
    }


    [GenerateOnExecute]
    protected override void OnExecute()
    {
        this.Entities.ForEach((ref LocalToWorld localToWorld) =>
            localToWorld.Matrix = Matrix4x4.Identity);

        this.Entities.ForEach((in Scale scale, ref LocalToWorld localToWorld) =>
            localToWorld.Matrix *= Matrix4x4.CreateScale(scale));

        this.Entities.ForEach((in Rotation rotation, ref LocalToWorld localToWorld) =>
            localToWorld.Matrix *= Matrix4x4.CreateFromQuaternion(rotation));

        this.Entities.ForEach((in Translation translation, ref LocalToWorld localToWorld) =>
            localToWorld.Matrix *= Matrix4x4.CreateTranslation(translation));
    }
}


public class CalculateWorldTransformSystem_ForEach : ComponentSystem
{
    public CalculateWorldTransformSystem_ForEach(EntityArrays entities) : base(entities)
    {
    }


    protected override void OnExecute()
    {
        this.Entities.ForEach((ref LocalToWorld localToWorld) =>
            localToWorld.Matrix = Matrix4x4.Identity);

        this.Entities.ForEach((in Scale scale, ref LocalToWorld localToWorld) =>
            localToWorld.Matrix *= Matrix4x4.CreateScale(scale));

        this.Entities.ForEach((in Rotation rotation, ref LocalToWorld localToWorld) =>
            localToWorld.Matrix *= Matrix4x4.CreateFromQuaternion(rotation));

        this.Entities.ForEach((in Translation translation, ref LocalToWorld localToWorld) =>
            localToWorld.Matrix *= Matrix4x4.CreateTranslation(translation));
    }
}


public class CalculateWorldTransformSystem_ReadWrite : ComponentSystem
{
    public CalculateWorldTransformSystem_ReadWrite(EntityArrays entities) : base(entities)
    {
    }


    protected override void OnExecute()
    {
        foreach (var (count, localToWorld) in Write<LocalToWorld>.From(this.Entities))
        {
            for (var i = 0; i < count; ++i)
            {
                localToWorld[i] = Matrix4x4.Identity;
            }
        }

        foreach (var (count, scale, localToWorld) in
            Read<Scale>.Write<LocalToWorld>.From(this.Entities))
        {
            for (var i = 0; i < count; ++i)
            {
                localToWorld[i] *= Matrix4x4.CreateScale(scale[i]);
            }
        }

        foreach (var (count, rotation, localToWorld) in
            Read<Rotation>.Write<LocalToWorld>.From(this.Entities))
        {
            for (var i = 0; i < count; ++i)
            {
                localToWorld[i] *= Matrix4x4.CreateFromQuaternion(rotation[i]);
            }
        }

        foreach (var (count, translation, localToWorld) in
            Read<Translation>.Write<LocalToWorld>.From(this.Entities))
        {
            for (var i = 0; i < count; ++i)
            {
                localToWorld[i] *= Matrix4x4.CreateTranslation(translation[i]);
            }
        }
    }
}


[MemoryDiagnoser]
public class CalculateWorldTransformBenchmark
{
    [Params(1_000, 100_000)]
    public int N { get; set; } = 100_000;


    [GlobalSetup]
    public void GlobalSetup()
    {
        this.EntityManager = new EntityManager();
        this._ForEach_Generated =
            new CalculateWorldTransformSystem_ForEachGenerated(this.Entities);
        this._ForEach = new CalculateWorldTransformSystem_ForEach(this.Entities);
        this._ReadWrite = new CalculateWorldTransformSystem_ReadWrite(this.Entities);

        var random = new Random(0);
        Vector3 RandomVector() => new(random.Next(), random.Next(), random.Next());

        SetRandomTranslation(
            CreateEntities(Archetype<LocalToWorld, Translation>.Instance));

        SetRandomRotation(
            CreateEntities(Archetype<LocalToWorld, Rotation>.Instance));

        SetRandomScale(
            CreateEntities(Archetype<LocalToWorld, Scale>.Instance));

        SetRandomTranslation(SetRandomRotation(
            CreateEntities(Archetype<LocalToWorld, Translation, Rotation>.Instance)));

        SetRandomTranslation(SetRandomScale(
            CreateEntities(Archetype<LocalToWorld, Translation, Scale>.Instance)));

        SetRandomRotation(SetRandomScale(
            CreateEntities(Archetype<LocalToWorld, Rotation, Scale>.Instance)));

        SetRandomTranslation(SetRandomRotation(SetRandomScale(
            CreateEntities(Archetype<LocalToWorld, Translation, Rotation, Scale>.Instance))));


        return;


        IEnumerable<IComponentArray> SetRandomTranslation(IEnumerable<IComponentArray> arrays)
        {
            arrays.ForEach((ref Translation translation) => translation.Vector = RandomVector());
            return arrays;
        }

        IEnumerable<IComponentArray> SetRandomRotation(IEnumerable<IComponentArray> arrays)
        {
            arrays.ForEach((ref Rotation rotation) =>
            {
                var angles = MathF.PI * RandomVector();
                rotation.Quaternion =
                    Quaternion.CreateFromYawPitchRoll(angles.X, angles.Y, angles.Z);
            });
            return arrays;
        }

        IEnumerable<IComponentArray> SetRandomScale(IEnumerable<IComponentArray> arrays)
        {
            arrays.ForEach((ref Scale scale) => scale.Vector = RandomVector());
            return arrays;
        }

        IEnumerable<IComponentArray> CreateEntities(Archetype archetype)
        {
            for (var i = 0; i < this.N; ++i)
            {
                this.EntityManager.CreateEntity(archetype);
            }

            return this.Entities.Where(x => x.Archetype == archetype);
        }
    }


    [Benchmark]
    public void ForEach_Generated()
    {
        this._ForEach_Generated.Execute();
    }


    [Benchmark]
    public void ForEach()
    {
        this._ForEach.Execute();
    }


    [Benchmark]
    public void ReadWrite()
    {
        this._ReadWrite.Execute();
    }


    public EntityManager EntityManager { get; private set; }
    public EntityArrays Entities => this.EntityManager.Entities;
    public CalculateWorldTransformSystem_ForEachGenerated _ForEach_Generated;
    public CalculateWorldTransformSystem_ForEach _ForEach;
    public CalculateWorldTransformSystem_ReadWrite _ReadWrite;
}