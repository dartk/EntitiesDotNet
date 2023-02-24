namespace EntitiesDotNet.Benchmarks.UpdateTranslationBenchmark;


[GenerateImplicitOperators]
public partial record struct Velocity
{
    public float3 Float3;
}


[GenerateImplicitOperators]
public partial record struct Translation
{
    public float3 Float3;
}


[GenerateImplicitOperators]
public partial record struct Acceleration
{
    public float3 Float3;
}