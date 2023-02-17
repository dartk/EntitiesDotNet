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
        var xyz0 = EntitiesDotNet.Archetype.Instance(typeof(X), typeof(Y), typeof(Z));
        var xyz1 = EntitiesDotNet.Archetype.Instance(typeof(X), typeof(Y), typeof(Z));
        var xy = EntitiesDotNet.Archetype.Instance(typeof(X), typeof(Y));

        Assert.StrictEqual(xyz0, xyz1);
        Assert.NotStrictEqual(xyz0, xy);
    }


    [Fact]
    public void ComponentsPropertyTest()
    {
        var x = EntitiesDotNet.Archetype.Instance(typeof(X));
        var xy = EntitiesDotNet.Archetype.Instance(typeof(X), typeof(Y));
        var xyz = EntitiesDotNet.Archetype.Instance(typeof(X), typeof(Y), typeof(Z));

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
        var xy = EntitiesDotNet.Archetype.Instance(typeof(X), typeof(Y));
        Assert.True(xy.Contains(typeof(X)));
        Assert.True(xy.Contains(typeof(Y)));
        Assert.False(xy.Contains(typeof(Z)));
    }


    [Fact]
    public void AddComponentsTest()
    {
        var x = EntitiesDotNet.Archetype.Instance(typeof(X));
        var xy = EntitiesDotNet.Archetype.Instance(typeof(X), typeof(Y));
        var xyz = EntitiesDotNet.Archetype.Instance(typeof(X), typeof(Y), typeof(Z));

        Assert.StrictEqual(xy, x.With(typeof(Y)));
        Assert.StrictEqual(xy, x.With(typeof(Y)));
        Assert.StrictEqual(xyz, xyz.With(typeof(X), typeof(Y)));
    }


    [Fact]
    public void RemoveComponentsTest()
    {
        var x = EntitiesDotNet.Archetype.Instance(typeof(X));
        var xy = EntitiesDotNet.Archetype.Instance(typeof(X), typeof(Y));
        var yz = EntitiesDotNet.Archetype.Instance(typeof(Y), typeof(Z));
        var xyz = EntitiesDotNet.Archetype.Instance(typeof(X), typeof(Y), typeof(Z));

        Assert.StrictEqual(x, x.Without(typeof(Y), typeof(Z)));
        Assert.StrictEqual(x, xy.Without(typeof(Y)));
        Assert.StrictEqual(xy, xyz.Without(typeof(Z)));
        Assert.StrictEqual(yz, xyz.Without(typeof(X)));
        Assert.StrictEqual(x, xyz.Without(typeof(Y), typeof(Z)));
    }


    [Fact]
    public void SharedComponentTests()
    {
        var xy = EntitiesDotNet.Archetype.Instance<X, Y>();
        var xyWithSharedX10 = EntitiesDotNet.Archetype.Instance<X, Y>()
            .With(SharedComponent.Instance(new SharedX(10)));
        var xyWithSharedX20 = EntitiesDotNet.Archetype.Instance<X, Y>()
            .With(SharedComponent.Instance(new SharedX(20)));
        var xyWithSharedX10_2 = EntitiesDotNet.Archetype.Instance<X, Y>()
            .WithShared(new SharedX(10), new SharedX(10));
        var xyWithSharedX10_X11 = EntitiesDotNet.Archetype.Instance<X, Y>()
            .WithShared(new SharedX(10), new SharedX(11));

        Assert.NotStrictEqual(xy, xyWithSharedX10);
        Assert.NotStrictEqual(xy, xyWithSharedX20);
        Assert.NotStrictEqual(xyWithSharedX10, xyWithSharedX20);
        Assert.StrictEqual(xyWithSharedX10, xyWithSharedX10_2);
        Assert.NotStrictEqual(xyWithSharedX10, xyWithSharedX10_X11);
    }
}