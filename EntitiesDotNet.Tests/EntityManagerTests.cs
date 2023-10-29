using EntitiesDotNet;
using Xunit.Abstractions;


namespace EntityComponentSystem.Tests;


public class EntityManagerTests
{

    public EntityManagerTests(ITestOutputHelper output)
    {
        this.Output = output;
    }


    public ITestOutputHelper Output { get; }


    [Fact]
    public void ChangeEntityArchetype()
    {
        var manager = new EntityManager();
        var entity = manager.CreateEntity(1, 12L, 12.3);

        Assert.Equal(entity.Archetype, Archetype<EntityId, int, long, double>.Instance);

        entity.RemoveComponents<long, double>();
        entity.AddComponents("My entity");

        Assert.Equal(entity.Archetype, Archetype<EntityId, int, string>.Instance);

        Assert.Equal(1, entity.RefRO<int>());
        Assert.Equal("My entity", entity.RefRO<string>());
    }
}