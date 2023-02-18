namespace EntitiesDotNet;


public abstract class ComponentSystem
{
    public ComponentSystem(EntityArrays entities)
    {
        this.Entities = entities;
        if (this is IComponentSystem_Generated generated)
        {
            this._generated = generated;
            this._generated.OnInit();
        }
    }


    public EntityArrays Entities { get; }


    public void Execute(bool executeGenerated = true)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (executeGenerated && this._generated != null)
        {
            this._generated.OnExecute();
            return;
        }

        this.OnExecute();
    }


    protected abstract void OnExecute();

    
    private readonly IComponentSystem_Generated? _generated;
}