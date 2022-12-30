using System;
using Xunit.Abstractions;


namespace EntityComponentSystem.Tests;


public partial class SomeClass {

    [GenerateIteration]
    public partial struct MyIteration : IComponentArrayIterationExpression {
        public void IterationExpression(IComponentArray array) {
            var state = 10f;
            array.ForEach((in int i, ref float f) => {
                f = i + state;
                state += i;
            });
        }
    }

}


[GenerateIteration]
public partial struct MyIteration2 : IComponentArrayIterationExpression {
    public void IterationExpression(IComponentArray array) {
        var state = 10f;
        array.ForEach((in int i, ref float f) => f = i + state);
    }
}


public partial class IterationTests {
    public IterationTests(ITestOutputHelper output) {
        this.Output = output;
    }


    public ITestOutputHelper Output { get; }


    [GenerateIteration]
    public partial struct IterateStruct : IComponentArrayIterationExpression {

        public int Counter;


        public void IterationExpression(IComponentArray array) {
            var counter = 0;
            array.ForEach((in int i, ref float f) => {
                counter += i;
                f = i * 0.5f;
            });

            this.Counter = counter;
        }

    }


    [GenerateOptimized]
    private static float Iterate(IComponentArray array) {
        var result = 0f;
        
        array.ForEach((in int i, ref float f) => {
            result += i;
            f = i * 0.5f;
        });
        
        return result;
    }


    [GenerateOptimized]
    private static float IterateWithIndex(IComponentArray array) {
        array.ForEach((int index, ref int i) => i = index);
        
        var result = 0f;
        array.ForEach((in int i, ref float f) => {
            result += i;
            f = i * 0.5f;
        });
        
        return result;
    }


    [Fact]
    public void IterateMethodTest() {
        var array = new ComponentArray(Archetype.Instance<int, float>());
        array.Add(3);
        
        array.ForEach((int index, ref int i) => i = index);

        var result = Iterate_Optimized(array);

        this.Output.WriteLine(array.ToReadableString());

        Assert.Equal(3, result);
        Assert.Equal(new[] { 0f, 0.5f, 1f }, array.GetReadOnlySpan<float>().ToArray());
    }


    [Fact]
    public void IterateWithIndexMethodTest() {
        var array = new ComponentArray(Archetype.Instance<int, float>());
        array.Add(3);

        var result = IterateWithIndex_Optimized(array);

        this.Output.WriteLine(array.ToReadableString());

        Assert.Equal(3, result);
        Assert.Equal(new[] { 0f, 0.5f, 1f }, array.GetReadOnlySpan<float>().ToArray());
    }
    

    [Fact]
    public void IterateStructTest() {
        var array = new ComponentArray(Archetype.Instance<int, float>());
        array.Add(3);

        array.ForEach((int index, ref int i) => i = index);
        
        var iterate = new IterateStruct();
        iterate.IterationExpression_Generated(array);

        this.Output.WriteLine(array.ToReadableString());

        Assert.Equal(3, iterate.Counter);
        Assert.Equal(new[] { 0f, 0.5f, 1f }, array.GetReadOnlySpan<float>().ToArray());
    }
}