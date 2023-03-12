using EntitiesDotNet;
using Xunit.Abstractions;


namespace EntityComponentSystem.Tests;


public struct X
{
}


public struct Y
{
}


public struct Z
{
}


public record struct SharedX(int Int);
public record struct SharedY(int Int);
public record struct SharedZ(int Int);


public class ArchetypeTests
{
    public ArchetypeTests(ITestOutputHelper output)
    {
        this.Output = output;
    }


    private ITestOutputHelper Output { get; }


    [Fact]
    public void InstanceMethodTest()
    {
        var xyz0 = Archetype.Instance<X, Y, Z>();
        var xyz1 = Archetype.Instance<Y, X, Z>();
        var xy = Archetype.Instance<X, Y>();

        Assert.StrictEqual(xyz0, xyz1);
        Assert.NotStrictEqual(xyz0, xy);
    }


    [Fact]
    public void ComponentsPropertyTest()
    {
        var x = Archetype.Instance<X>();
        var xy = Archetype.Instance<X, Y>();
        var xyz = Archetype.Instance<X, Y, Z>();

        Assert.Equal(
            new ComponentType[] { typeof(X) },
            x.Components.ToArray()
        );

        Assert.Equal(
            new ComponentType[] { typeof(X), typeof(Y) }
                .OrderBy(x => x.Id),
            xy.Components.ToArray()
        );

        Assert.Equal(
            new ComponentType[] { typeof(X), typeof(Y), typeof(Z) }
                .OrderBy(x => x.Id),
            xyz.Components.ToArray()
        );
    }


    [Fact]
    public void ContainsMethodTest()
    {
        var xy = Archetype.Instance<X, Y>();
        Assert.True(xy.Contains<X>());
        Assert.True(xy.Contains<Y>());
        Assert.False(xy.Contains<Z>());
    }


    [Fact]
    public void AddComponentsTest()
    {
        var x = Archetype.Instance<X>();
        var xy = Archetype.Instance<X, Y>();
        var xyz = Archetype.Instance<X, Y, Z>();

        Assert.StrictEqual(xy, x.Add<Y>());
        Assert.StrictEqual(xy, xy.Add<X>());
        Assert.StrictEqual(xyz, xyz.Add<X, Y>());
    }


    [Fact]
    public void RemoveComponentsTest()
    {
        var x = Archetype.Instance<X>();
        var xy = Archetype.Instance<X, Y>();
        var yz = Archetype.Instance<Y, Z>();
        var xyz = Archetype.Instance<X, Y, Z>();

        Assert.StrictEqual(x, x.Remove<Y, Z>());
        Assert.StrictEqual(x, xy.Remove<Y>());
        Assert.StrictEqual(xy, xyz.Remove<Z>());
        Assert.StrictEqual(yz, xyz.Remove<X>());
        Assert.StrictEqual(x, xyz.Remove<Y, Z>());
    }
}