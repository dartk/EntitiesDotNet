namespace EntitiesDotNet.Benchmarks.UpdateTranslationBenchmark.Systems;


public static class ReadWriteSystem
{
    public static unsafe void Execute_Native(EntityArrays entities, float deltaTime)
    {
        foreach (var (count, acceleration, velocity) in
            Read<Acceleration>.Write<Velocity>.From(entities))
        {
            fixed (Acceleration* accelerationPtr = acceleration)
            fixed (Velocity* velocityPtr = velocity)
            {
                Native.update_velocity(count, accelerationPtr, velocityPtr, deltaTime);
            }
        }
    }


    public static void Execute_Managed(EntityArrays entities, float deltaTime)
    {
        foreach (var (count, acceleration, velocity) in
            Read<Acceleration>.Write<Velocity>.From(entities))
        {
            for (var i = 0; i < count; ++i)
            {
                UpdateVelocity(acceleration[i], ref velocity[i], deltaTime);
            }
        }
    }
}