using System.Globalization;
using System.Numerics;
using EntitiesDotNet;
using Xunit.Abstractions;


namespace EntityComponentSystem.Tests;


[GenerateImplicitOperators]
public partial record struct Width
{
    public float Float;


    public override string ToString() =>
        this.Float.ToString(CultureInfo.InvariantCulture);
}


[GenerateImplicitOperators]
public partial record struct Height
{
    public float Float;


    public override string ToString() =>
        this.Float.ToString(CultureInfo.InvariantCulture);
}


[EntityRef]
public ref partial struct WidthAndHeight
{
    public ref Height Height;
    public ref Width Width;
}


public class ComponentArrayTests
{
    public ComponentArrayTests(ITestOutputHelper output)
    {
        this.Output = output;
    }


    public ITestOutputHelper Output { get; }


    [Fact]
    public void ArrayAddTest()
    {
        var array = EntitiesDotNet.ComponentArray.Create<Width, Height>();
        for (var i = 0; i < 3; ++i)
        {
            array.Add(new Width { Float = i * 2 }, new Height { Float = i * 3 });
        }

        Assert.Equal(3, array.Count);
        Assert.Equal(new Width[] { 0, 2, 4 },
            array.GetReadOnlySpan<Width>().ToArray());
        Assert.Equal(new Height[] { 0, 3, 6 },
            array.GetReadOnlySpan<Height>().ToArray());
    }


    [Fact]
    public void FillArrayUsingReadWrite()
    {
        var array = ComponentArray.Create<Width, Height>();
        array.Add(5);

        var (count, width, height) = Write<Width, Height>.From(array);

        Assert.Equal(5, count);

        for (var i = 0; i < count; ++i)
        {
            width[i] = i * 2;
            height[i] = i * 3;
        }

        this.Output.WriteLine(array.ToReadableString());

        Assert.Equal(new Width[] { 0, 2, 4, 6, 8 },
            array.GetReadOnlySpan<Width>().ToArray());

        Assert.Equal(new Height[] { 0, 3, 6, 9, 12 },
            array.GetReadOnlySpan<Height>().ToArray());
    }


    [Fact]
    public void FillArrayUsingQueryForEach()
    {
        var array = new ComponentArray(Archetype<Width, Height>.Instance);
        array.Add(5);
        Assert.Equal(5, array.Count);

        var i = 0;
        foreach (var item in WidthAndHeight.From(array))
        {
            item.Width = i * 2;
            item.Height = i * 3;
            ++i;
        }

        this.Output.WriteLine(array.ToReadableString());

        Assert.Equal(new Width[] { 0, 2, 4, 6, 8 },
            array.GetReadOnlySpan<Width>().ToArray());

        Assert.Equal(new Height[] { 0, 3, 6, 9, 12 },
            array.GetReadOnlySpan<Height>().ToArray());
    }


    [Fact]
    public void FillArrayUsingQueryIndex()
    {
        var array = new EntitiesDotNet.ComponentArray(Archetype<Width, Height>.Instance);
        array.Add(5);
        Assert.Equal(5, array.Count);

        var items = WidthAndHeight.From(array);
        for (var i = 0; i < items.Length; ++i)
        {
            var item = items[i];
            item.Width = i * 2;
            item.Height = i * 3;
        }

        this.Output.WriteLine(array.ToReadableString());

        Assert.Equal(new Width[] { 0, 2, 4, 6, 8 },
            array.GetReadOnlySpan<Width>().ToArray());

        Assert.Equal(new Height[] { 0, 3, 6, 9, 12 },
            array.GetReadOnlySpan<Height>().ToArray());
    }


    [Fact]
    public void FillArrayUsingForEach()
    {
        var array = new EntitiesDotNet.ComponentArray(Archetype<Width, Height>.Instance);
        array.Add(5);
        Assert.Equal(5, array.Count);

        array.ForEach((ref Width width, ref Height height, int i) =>
        {
            width = i * 2;
            height = i * 3;
        });

        this.Output.WriteLine(array.ToReadableString());

        Assert.Equal(new Width[] { 0, 2, 4, 6, 8 },
            array.GetReadOnlySpan<Width>().ToArray());

        Assert.Equal(new Height[] { 0, 3, 6, 9, 12 },
            array.GetReadOnlySpan<Height>().ToArray());
    }
}