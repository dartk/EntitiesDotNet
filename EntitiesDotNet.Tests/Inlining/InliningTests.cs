using CSharp.SourceGen.Inlining;
using EntitiesDotNet;
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

        em.Entities.ForEach((int index, ref double d) => d = index);

        InlinedMethods.ForEachArrays(em.Entities);

        this.Output.WriteLine(em.ToReadableString());
    }
}


public static partial class InlinedMethods
{
    [Inline.Public(nameof(ForEachArrays_Inlined))]
    public static void ForEachArrays(EntityArrays arrays)
    {
        arrays.ForEach([Inline](int index, ref string s, ref double d) => { s = d.ToString(); });
    }


    [Inline.Public(nameof(ForEachArraysWithIndex_Inlined))]
    public static void ForEachArraysWithIndex(EntityArrays arrays)
    {
        arrays.ForEach([Inline](ref string s, ref double d) => { s = d.ToString(); });
    }


    [Inline.Public(nameof(ForEach_Inlined))]
    public static void ForEach(IComponentArray array)
    {
        array.ForEach([Inline](int index, ref string s, ref double d) => { s = d.ToString(); });
        array.ForEach([Inline](ref string s, ref double d) => { s = d.ToString(); });
    }


    [EntityRefStruct]
    private ref partial struct MyEntity
    {
        public ref readonly Velocity velocity;
        public ref Translation translation;
    }


    [Inline.Public(nameof(ForEachMyEntity_Inlined))]
    public static void ForEachMyEntity(EntityArrays arrays_, float deltaTime)
    {
        MyEntity.ForEach_inlining(arrays_, [Inline] (entity) =>
        {
            entity.translation += entity.velocity * deltaTime;
        });
    }
}