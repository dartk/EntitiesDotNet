using EntitiesDotNet;


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
    [Fact]
    public void InstanceMethodTest()
    {
        var xyz0 = Archetype.Instance(typeof(X), typeof(Y), typeof(Z));
        var xyz1 = Archetype.Instance(typeof(X), typeof(Y), typeof(Z));
        var xy = Archetype.Instance(typeof(X), typeof(Y));

        Assert.StrictEqual(xyz0, xyz1);
        Assert.NotStrictEqual(xyz0, xy);
    }


    [Fact]
    public void ComponentsPropertyTest()
    {
        var x = Archetype.Instance(typeof(X));
        var xy = Archetype.Instance(typeof(X), typeof(Y));
        var xyz = Archetype.Instance(typeof(X), typeof(Y), typeof(Z));

        Assert.Equal(
            new ComponentType[] { typeof(EntityId), typeof(X) },
            x.Components.ToArray()
        );

        Assert.Equal(
            new ComponentType[] { typeof(EntityId), typeof(X), typeof(Y) }
                .OrderBy(x => x.Id),
            xy.Components.ToArray()
        );

        Assert.Equal(
            new ComponentType[] { typeof(EntityId), typeof(X), typeof(Y), typeof(Z) }
                .OrderBy(x => x.Id),
            xyz.Components.ToArray()
        );
    }


    [Fact]
    public void ContainsMethodTest()
    {
        var xy = Archetype.Instance(typeof(X), typeof(Y));
        Assert.True(xy.Contains(typeof(X)));
        Assert.True(xy.Contains(typeof(Y)));
        Assert.False(xy.Contains(typeof(Z)));
    }


    [Fact]
    public void AddComponentsTest()
    {
        var x = Archetype.Instance(typeof(X));
        var xy = Archetype.Instance(typeof(X), typeof(Y));
        var xyz = Archetype.Instance(typeof(X), typeof(Y), typeof(Z));

        Assert.StrictEqual(xy, x.Add(typeof(Y)));
        Assert.StrictEqual(xy, x.Add(typeof(Y)));
        Assert.StrictEqual(xyz, xyz.Add(typeof(X), typeof(Y)));
    }


    [Fact]
    public void RemoveComponentsTest()
    {
        var x = Archetype.Instance(typeof(X));
        var xy = Archetype.Instance(typeof(X), typeof(Y));
        var yz = Archetype.Instance(typeof(Y), typeof(Z));
        var xyz = Archetype.Instance(typeof(X), typeof(Y), typeof(Z));

        Assert.StrictEqual(x, x.Remove(typeof(Y), typeof(Z)));
        Assert.StrictEqual(x, xy.Remove(typeof(Y)));
        Assert.StrictEqual(xy, xyz.Remove(typeof(Z)));
        Assert.StrictEqual(yz, xyz.Remove(typeof(X)));
        Assert.StrictEqual(x, xyz.Remove(typeof(Y), typeof(Z)));
    }
}