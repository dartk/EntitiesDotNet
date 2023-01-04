namespace EntityComponentSystem;


public readonly record struct Entity(EntityManager EntityManager, int Id) : IDisposable {
    public readonly int Id = Id;
    public readonly EntityManager EntityManager = EntityManager;


    public void Dispose() {
        this.EntityManager.DestroyEntity(this);
    }


    public EntityLocation GetLocation() => this.EntityManager.GetEntityLocation(this.Id);


    public ref T Ref<T>() {
        var (array, index) = this.GetLocation();
        return ref array.GetSpan<T>()[index];
    }


    public ref readonly T RefReadonly<T>() {
        var (array, index) = this.GetLocation();
        return ref array.GetReadOnlySpan<T>()[index];
    }


    public static implicit operator EntityId(Entity entity) => new (entity.Id);
}


public readonly record struct EntityId(int Id) {
    public readonly int Id = Id;

    public override string ToString() {
        return this.Id.ToString();
    }
}