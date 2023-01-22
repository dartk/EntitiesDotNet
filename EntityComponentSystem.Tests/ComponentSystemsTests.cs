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
        var entityManager = new EntityManager();

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

        entityManager.CreateEntity(Archetype.Instance<int>().WithShared(false));
        entityManager.CreateEntity(Archetype.Instance<int>().WithShared(true));

        var system = new TestSystem(entityManager, this.Output);
        system.Execute();
    }
}


public partial record AnotherSystem(ReadOnlyArray<IComponentArray> Components)
    : ComponentSystem(Components)
{
    protected override void OnExecute()
    {
        throw new NotImplementedException();
    }
}


public partial record TestSystem(EntityManager EntityManager, ITestOutputHelper Output)
    : ComponentSystem(EntityManager.Entities)
{
    [GenerateOnExecute]
    protected override void OnExecute()
    {
        var deltaTime = 1f / 60f;
        this.Components.ForEach(
            (in Velocity velocity, ref Translation translation) =>
            {
                translation = deltaTime * velocity;
            });

        this.Components
            .Where(x => x.Archetype.Contains(SharedComponent.Instance(false)))
            .ForEach((ref int i) => i = -1);

        this.Components
            .Where(x => x.Archetype.Contains(SharedComponent.Instance(true)))
            .ForEach((ref int i) => i = 1);

        this.Output.WriteLine(this.EntityManager.ToReadableString());
    }
}