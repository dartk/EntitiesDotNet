namespace EntitiesDotNet.Benchmarks.UpdateTranslationBenchmark.Systems;


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
    }
}