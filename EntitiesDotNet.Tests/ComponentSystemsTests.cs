using EntitiesDotNet;
using Xunit.Abstractions;


namespace EntityComponentSystem.Tests;


public class ComponentSystemsTests
{
    public ComponentSystemsTests(ITestOutputHelper output)
    {
        this.Output = output;
    }


    public ITestOutputHelper Output { get; }


    [Fact]
    public void Foo()
    {
        var entityManager = new EntitiesDotNet.EntityManager();

        for (var i = 0; i < 5; ++i)
        {
            entityManager.CreateEntity(
                new Velocity { Float = i },
                new Translation { }
            );

            entityManager.CreateEntity(
                new Velocity { Float = i },
                default(int)
            );

            entityManager.CreateEntity(
                new Translation { }
            );
        }

        entityManager.CreateEntity(EntitiesDotNet.Archetype.Instance<int>().WithShared(false));
        entityManager.CreateEntity(EntitiesDotNet.Archetype.Instance<int>().WithShared(true));

        var system = new TestSystem(entityManager.Entities);
        system.Execute();
    }
}


public partial class AnotherSystem : ComponentSystem
{
    protected override void OnExecute()
    {
        throw new NotImplementedException();
    }


    public AnotherSystem(EntityArrays entities) : base(entities)
    {
    }
}


public partial class TestSystem : ComponentSystem
{
    [GenerateOnExecute]
    protected override void OnExecute()
    {
        var deltaTime = 1f / 60f;
        this.Entities.ForEach(
            (in Velocity velocity, ref Translation translation) =>
            {
                translation = deltaTime * velocity;
            });

        this.Entities
            .Where(x => x.Archetype.Contains(SharedComponent.Instance(false)))
            .ForEach((ref int i) => i = -1);

        this.Entities
            .Where(x => x.Archetype.Contains(SharedComponent.Instance(true)))
            .ForEach((ref int i) => i = 1);
    }


    public TestSystem(EntityArrays entities) : base(entities)
    {
    }
}