namespace EntityComponentSystem;


public abstract class ComponentSystem {

    public ComponentSystem() {
        this._generated = this as IComponentSystem_Generated;
    }


    public EntityManager EntityManager => this._entityManager!;


    public void Initialize(EntityManager entityManager) {
        this._entityManager = entityManager;
    }


    public void OnStart() {
        if (this._entityManager == null) {
            throw new NullReferenceException(
                $"{nameof(ComponentSystem)} is not initialized.");
        }
    }


    public void OnExecute() {
        if (this._generated != null) {
            this._generated.Execute();
            return;
        }

        this.Execute();
    }


    protected abstract void Execute();


    protected ReadOnlyArray<IComponentArray> Entities => this.EntityManager.Entities;


    private readonly IComponentSystem_Generated? _generated;
    private EntityManager? _entityManager;

}


public partial class ExampleSystem : ComponentSystem {

    // [GenerateOptimized]
    protected override void Execute() {
        this.Entities
            .Where(x => !x.Archetype.Contains<float>())
            .ForEach((in int intValue) => { });
    }

}