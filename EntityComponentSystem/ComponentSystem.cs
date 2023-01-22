namespace EntityComponentSystem;


public abstract record ComponentSystem
{
    public ComponentSystem(ReadOnlyArray<IComponentArray> components)
    {
        this.Components = components;
        if (this is IComponentSystem_Generated generated)
        {
            this._generated = generated;
            this._generated.OnInit();
        }
    }


    public ReadOnlyArray<IComponentArray> Components { get; }


    public void Execute()
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (this._generated != null)
        {
            this._generated.OnExecute();
            return;
        }

        this.OnExecute();
    }


    protected abstract void OnExecute();

    
    private readonly IComponentSystem_Generated? _generated;
}