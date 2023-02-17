using System.Numerics;
using EntitiesDotNet;


namespace EntityComponentSystem.Tests;


[GenerateImplicitOperators]
public partial record struct Velocity
{
    public float Float;
}