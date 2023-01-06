using System.Numerics;
using Xunit.Abstractions;


namespace EntityComponentSystem.Tests;


public class ComponentSystemsTests {
    public ComponentSystemsTests(ITestOutputHelper output) {
        this.Output = output;
    }


    public ITestOutputHelper Output { get; }


    [Fact]
    public void Foo() {
        var entityManager = new EntityManager();

        for (var i = 0; i < 5; ++i) {
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

        entityManager.CreateEntity(Archetype.Instance<int>().AddSharedValue(false));
        entityManager.CreateEntity(Archetype.Instance<int>().AddSharedValue(true));

        var system = new TestSystem(this.Output, entityManager);
        system.Execute();
    }
}


public partial class TestSystem : ComponentSystem {

    public TestSystem(ITestOutputHelper output, EntityManager em) {
        this.Output = output;
        this.Init(em);
    }


    public ITestOutputHelper Output { get; }


    [GenerateOnExecute]
    protected override void OnExecute() {
        var deltaTime = 1f / 60f;
        this.Entities.ForEach(
            (in Velocity velocity, ref Translation translation) => {
                translation = deltaTime * velocity;
            });

        this.Entities
            .Where(x => x.Archetype.Contains(SharedComponent.Instance(false)))
            .ForEach((ref int i) => i = -1);
            
        this.Entities
            .Where(x => x.Archetype.Contains(SharedComponent.Instance(true)))
            .ForEach((ref int i) => i = 1);

        this.Output.WriteLine(this.EntityManager.ToReadableString());
    }

}