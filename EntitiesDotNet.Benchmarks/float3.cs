namespace EntitiesDotNet.Benchmarks;


public struct float3
{
    public float3(float x, float y, float z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }


    public float X;
    public float Y;
    public float Z;


    public static float3 operator *(float f, float3 f3) => new(f * f3.X, f * f3.Y, f * f3.Z);
    public static float3 operator *(float3 f3, float f) => new(f * f3.X, f * f3.Y, f * f3.Z);
    public static float3 operator +(float3 a, float3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
}