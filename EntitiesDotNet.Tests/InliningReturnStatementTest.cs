using EntitiesDotNet;


namespace EntityComponentSystem.Tests;


public partial class InliningReturnStatementTest
{
    private static EntityManager CreateEntityManager()
    {
        var em = new EntityManager();
        for (var i = 0; i < 5; ++i)
        {
            em.CreateEntity(Archetype.Instance<int>());
        }

        return em;
    }


    [Fact]
    public void InliningReturnStatement_Components()
    {
        var em = CreateEntityManager();
        ForEachComponents_Inlined(em.Entities);
        Assert.Equal(
            new[] { 0, 1, 0, 3, 0 },
            em.Entities[0].GetReadOnlySpan<int>().ToArray());
    }


    [Fact]
    public void InliningReturnStatement_EntityRef()
    {
        var em = CreateEntityManager();
        ForEachEntityRef_Inlined(em.Entities);
        Assert.Equal(
            new[] { 0, 1, 0, 3, 0 },
            em.Entities[0].GetReadOnlySpan<int>().ToArray());
    }


    [Inline.Private]
    private static void ForEachComponents(EntityArrays entities)
    {
        entities.ForEach([Inline](ref int i, int index) =>
        {
            if (index % 2 == 0)
            {
                return;
            }

            i = index;
        });
    }


    [Inline.Private]
    private static void ForEachEntityRef(EntityArrays entities)
    {
        entities.ForEach([Inline](in IterationTests.EInt entity, int index) =>
        {
            if (index % 2 == 0)
            {
                return;
            }

            entity.Int = index;
        });
    }
}