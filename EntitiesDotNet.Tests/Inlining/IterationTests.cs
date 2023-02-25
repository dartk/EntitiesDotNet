using EntitiesDotNet;
using Xunit.Abstractions;


namespace EntityComponentSystem.Tests.Inlining;


public partial class IterationTests
{
    public IterationTests(ITestOutputHelper output)
    {
        this.Output = output;
    }


    public ITestOutputHelper Output { get; }


    [Fact]
    public void EntityArraysForEach()
    {
        var em = CreateEntityManager();

        em.Entities.ForEach((in EntityId entityId, ref int i) => i = entityId.Id);
        em.Entities.ForEach((in int i, ref double d) => d = i);

        this.Output.WriteLine(em.ToReadableString());

        {
            var (count, ints) = Read<int>.From(em.GetArray(Archetype<int>.Instance));
            Assert.Equal(5, count);
            Assert.Equal(new[] { 0, 2, 4, 6, 8 }, ints.ToArray());
        }

        {
            var (count, ints, doubles) =
                Read<int, double>.From(em.GetArray(Archetype<int, double>.Instance));
            Assert.Equal(5, count);
            Assert.Equal(new[] { 1, 3, 5, 7, 9 }, ints.ToArray());
            Assert.Equal(new double [] { 1, 3, 5, 7, 9 }, doubles.ToArray());
        }
    }


    private static EntityManager CreateEntityManager()
    {
        var em = new EntityManager();
        for (var i = 0; i < 5; ++i)
        {
            em.CreateEntity(Archetype<int>.Instance);
            em.CreateEntity(Archetype<int, double>.Instance);
        }

        return em;
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