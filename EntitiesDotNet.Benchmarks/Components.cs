using System.Numerics;


namespace EntitiesDotNet.Benchmarks;


[GenerateImplicitOperators]
public partial record struct Velocity
{
    public Vector3 Vector;
}


[GenerateImplicitOperators]
public partial record struct Translation
{
    public Vector3 Vector;
}


[GenerateImplicitOperators]
public partial record struct Scale
{
    public Vector3 Vector;
}


[GenerateImplicitOperators]
public partial record struct Rotation
{
    public Quaternion Quaternion;
}


[GenerateImplicitOperators]
public partial record struct LocalToWorld
{
    public Matrix4x4 Matrix;
}


[EntityRefStruct]
internal ref partial struct VelocityAndTranslation
{
    public ref readonly Velocity Velocity;
    public ref Translation Translation;
}