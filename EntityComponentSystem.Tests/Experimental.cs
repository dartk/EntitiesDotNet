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
    public partial struct Iterate : IComponentArrayIterationExpression {

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


    [Fact]
    public void IterateTest() {
        var array = new ComponentArray(Archetype.Instance<int, float>(), 10);
        array.Add(3);

        array.ForEach((int index, ref int i) => i = index);
        
        var iterate = new Iterate();
        iterate.IterationExpression_Generated(array);

        this.Output.WriteLine(array.ToReadableString());

        Assert.Equal(3, iterate.Counter);
        Assert.Equal(new[] { 0f, 0.5f, 1f }, array.GetReadOnlySpan<float>().ToArray());
    }
}