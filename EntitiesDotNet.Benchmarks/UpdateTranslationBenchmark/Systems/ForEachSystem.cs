namespace EntitiesDotNet.Benchmarks.UpdateTranslationBenchmark.Systems;


public static partial class ForEachSystem
{
    [Inline.Public(nameof(Execute_Inlined))]
    public static void Execute_Lambda(EntityArrays entities, float deltaTime)
    {
        entities.ForEach([Inline](in Acceleration a, ref Velocity v) =>
        {
            UpdateVelocity(a, ref v, deltaTime);
        });
    }
}