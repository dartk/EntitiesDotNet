namespace EntityComponentSystem.Tests;


public struct X {
}


public struct Y {
}


public struct Z {
}


public class ArchetypeTests {

    [Fact]
    public void InstanceMethodTest() {
        var xyz0 = Archetype.Instance(typeof(X), typeof(Y), typeof(Z));
        var xyz1 = Archetype.Instance(typeof(X), typeof(Y), typeof(Z));
        var xy = Archetype.Instance(typeof(X), typeof(Y));

        Assert.StrictEqual(xyz0, xyz1);
        Assert.NotStrictEqual(xyz0, xy);
    }


    [Fact]
    public void ComponentsPropertyTest() {
        var x = Archetype.Instance(typeof(X));
        var xy = Archetype.Instance(typeof(X), typeof(Y));
        var xyz = Archetype.Instance(typeof(X), typeof(Y), typeof(Z));

        Assert.Equal(
            new[] { typeof(X) },
            x.Components.ToArray()
        );

        Assert.Equal(
            new[] { typeof(X), typeof(Y) }.OrderBy(component => component.GUID),
            xy.Components.ToArray()
        );

        Assert.Equal(
            new[] { typeof(X), typeof(Y), typeof(Z) }.OrderBy(component => component.GUID),
            xyz.Components.ToArray()
        );
    }


    [Fact]
    public void ContainsMethodTest() {
        var xy = Archetype.Instance(typeof(X), typeof(Y));
        Assert.True(xy.Contains(typeof(X)));
        Assert.True(xy.Contains(typeof(Y)));
        Assert.False(xy.Contains(typeof(Z)));
    }


    [Fact]
    public void AddComponentsTest() {
        var x = Archetype.Instance(typeof(X));
        var xy = Archetype.Instance(typeof(X), typeof(Y));
        var xyz = Archetype.Instance(typeof(X), typeof(Y), typeof(Z));

        Assert.StrictEqual(xy, x.Add(typeof(Y)));
        Assert.StrictEqual(xy, x.Add(typeof(Y)));
        Assert.StrictEqual(xyz, xyz.Add(typeof(X), typeof(Y)));
    }


    [Fact]
    public void RemoveComponentsTest() {
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