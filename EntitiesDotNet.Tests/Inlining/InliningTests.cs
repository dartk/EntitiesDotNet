﻿using EntitiesDotNet;
using Xunit.Abstractions;


namespace EntityComponentSystem.Tests.Inlining;


public partial class InliningTests
{
    public InliningTests(ITestOutputHelper output)
    {
        this.Output = output;
    }


    public ITestOutputHelper Output { get; }


    [Fact]
    public void Test()
    {
        var em = new EntityManager();
        for (var i = 0; i < 10; ++i)
        {
            em.CreateEntity(Archetype<string, double>.Instance);
        }

        em.Entities.ForEach((ref double d, int index) => d = index);

        InlinedMethods.ForEachArrays_Inlined(em.Entities);

        this.Output.WriteLine(em.ToReadableString());
    }
}


public static partial class InlinedMethods
{
    [Inline.Public(nameof(ForEachArrays_Inlined))]
    public static void ForEachArrays(EntityArrays arrays)
    {
        arrays.ForEach([Inline](ref string s, ref double d, int index) => { s = d.ToString(); });
    }


    [Inline.Public(nameof(ForEachArraysWithIndex_Inlined))]
    public static void ForEachArraysWithIndex(EntityArrays arrays)
    {
        arrays.ForEach([Inline](ref string s, ref double d) => { s = d.ToString(); });
    }


    [Inline.Public(nameof(ForEach_Inlined))]
    public static void ForEach(IComponentArray array)
    {
        array.ForEach([Inline](ref string s, ref double d, int index) => { s = d.ToString(); });
        array.ForEach([Inline](ref string s, ref double d) => { s = d.ToString(); });
    }


    [EntityRef]
    private ref partial struct MyEntity
    {
        public ref readonly Velocity velocity;
        public ref Translation translation;
    }
}