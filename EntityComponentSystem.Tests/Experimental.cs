using System;
using Xunit.Abstractions;


namespace EntityComponentSystem.Tests;


public partial class IterationTests {
    public IterationTests(ITestOutputHelper output) {
        this.Output = output;
    }


    public ITestOutputHelper Output { get; }


    // [GenerateOptimized]
    // private static float Iterate(IComponentArray array) {
    //     var result = 0f;
    //     
    //     array.ForEach((in int i, ref float f) => {
    //         result += i;
    //         f = i * 0.5f;
    //     });
    //     
    //     return result;
    // }


    // [GenerateOptimized]
    // private static float IterateWithIndex(IComponentArray array) {
    //     array.ForEach((int index, ref int i) => i = index);
    //     
    //     var result = 0f;
    //     array.ForEach((in int i, ref float f) => {
    //         result += i;
    //         f = i * 0.5f;
    //     });
    //     
    //     return result;
    // }


    // [Fact]
    // public void IterateMethodTest() {
    //     var array = new ComponentArray(Archetype.Instance<int, float>());
    //     array.Add(3);
    //     
    //     array.ForEach((int index, ref int i) => i = index);
    //
    //     var result = Iterate_Optimized(array);
    //
    //     this.Output.WriteLine(array.ToReadableString());
    //
    //     Assert.Equal(3, result);
    //     Assert.Equal(new[] { 0f, 0.5f, 1f }, array.GetReadOnlySpan<float>().ToArray());
    // }
    //
    //
    // [Fact]
    // public void IterateWithIndexMethodTest() {
    //     var array = new ComponentArray(Archetype.Instance<int, float>());
    //     array.Add(3);
    //
    //     var result = IterateWithIndex_Optimized(array);
    //
    //     this.Output.WriteLine(array.ToReadableString());
    //
    //     Assert.Equal(3, result);
    //     Assert.Equal(new[] { 0f, 0.5f, 1f }, array.GetReadOnlySpan<float>().ToArray());
    // }
}