namespace EntityComponentSystem;


public abstract class ComponentSystem {

    public ComponentSystem() {
        // ReSharper disable once SuspiciousTypeConversion.Global
        this._generated = this as IComponentSystem_Generated;
    }


    public EntityManager EntityManager => this._entityManager!;


    public void Init(EntityManager entityManager) {
        if (this._entityManager != null) {
            throw new InvalidOperationException("Already initialized.");
        }

        this._entityManager = entityManager;
        this._generated?.OnInit();
    }


    public void Execute() {
        if (this._generated != null) {
            this._generated.OnExecute();
            return;
        }

        this.OnExecute();
    }


    protected abstract void OnExecute();


    protected ReadOnlyArray<IComponentArray> Entities => this.EntityManager.Entities;


    private readonly IComponentSystem_Generated? _generated;
    private EntityManager? _entityManager;

}