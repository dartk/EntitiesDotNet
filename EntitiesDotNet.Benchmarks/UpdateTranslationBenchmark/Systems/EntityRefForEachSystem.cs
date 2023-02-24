namespace EntitiesDotNet.Benchmarks.UpdateTranslationBenchmark.Systems;


public static partial class EntityRefForEachSystem
{
    [EntityRef]
    public ref partial struct UpdateVelocityEntity
    {
        public ref readonly Acceleration Acceleration;
        public ref Velocity Velocity;
    }


    [Inline.Public(nameof(Execute_Inlined))]
    public static void Execute_Lambda(EntityArrays entities, float deltaTime)
    {
        entities.ForEach([Inline](in UpdateVelocityEntity entity) =>
        {
            UpdateVelocity(entity.Acceleration, ref entity.Velocity, deltaTime);
        });
    }
}