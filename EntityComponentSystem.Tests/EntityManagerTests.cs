using Xunit.Abstractions;


namespace EntityComponentSystem.Tests; 


public class EntityManagerTests {
    
    public EntityManagerTests(ITestOutputHelper output) {
        this.Output = output;
    }


    public ITestOutputHelper Output { get; }


    [Fact]
    public void Foo() {
        var manager = new EntityManager();
        manager.CreateEntity(10);
        manager.CreateEntity(12, 120.0f);

        manager.Entities.ForEach((int i, in int value) =>
            this.Output.WriteLine($"#{i}: {value}"));

        this.Output.WriteLine(manager.ToReadableString());
    }
}
