namespace EntitiesDotNet.Benchmarks.UpdateTranslationBenchmark;


[WrapperStruct]
public partial record struct Velocity
{
    public float3 Float3;
    public static float3 operator *(in Velocity velocity, float value) => new float3
    {
        X = velocity.Float3.X * value,
        Y = velocity.Float3.Y * value,
        Z = velocity.Float3.Z * value
    };
}


[WrapperStruct]
public partial record struct Translation
{
    public float3 Float3;
}


[WrapperStruct]
public partial record struct Acceleration
{
    public float3 Float3;
}