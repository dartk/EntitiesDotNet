using System.Runtime.CompilerServices;


namespace EntitiesDotNet.Benchmarks.UpdateTranslationBenchmark;


public static class Functions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateVelocity(in Acceleration a, ref Velocity v, float deltaTime)
    {
        v.Float3.X += a.Float3.X * deltaTime;
        v.Float3.Y += a.Float3.Y * deltaTime;
        v.Float3.Z += a.Float3.Z * deltaTime;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateVelocity(in float3 a, ref float3 v, float deltaTime)
    {
        v.X += a.X * deltaTime;
        v.Y += a.Y * deltaTime;
        v.Z += a.Z * deltaTime;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateTranslation(in Velocity v, ref Translation t, float deltaTime)
    {
        t.Float3.X += v.Float3.X * deltaTime;
        t.Float3.Y += v.Float3.Y * deltaTime;
        t.Float3.Z += v.Float3.Z * deltaTime;
    }
}