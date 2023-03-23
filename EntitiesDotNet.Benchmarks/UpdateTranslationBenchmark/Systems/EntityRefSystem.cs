namespace EntitiesDotNet.Benchmarks.UpdateTranslationBenchmark.Systems;


public static partial class EntityRefSystem
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
        entities.ForEach((in UpdateVelocityEntity entity) =>
            UpdateVelocity(entity.Acceleration, ref entity.Velocity, deltaTime));
    }


    public static void Execute_From(EntityArrays entities, float deltaTime)
    {
        foreach (var entity in UpdateVelocityEntity.From(entities))
        {
            UpdateVelocity(entity.Acceleration, ref entity.Velocity, deltaTime);
        }
    }
}