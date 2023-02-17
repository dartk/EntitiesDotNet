namespace EntitiesDotNet;


public readonly record struct Entity : IDisposable
{

    public Entity(EntityManager entityManager, EntityId id)
    {
        this.Id = id;
        this.EntityManager = entityManager;
        entityManager.GetEntityLocation(id, out this._array, out this._index);
    }


    public readonly EntityId Id;
    public readonly EntityManager EntityManager;

    private readonly IComponentArray _array;
    private readonly int _index;


    public void Dispose()
    {
        this.EntityManager.DestroyEntity(this);
    }


    public ref T RefRW<T>()
    {
        return ref this._array.GetSpan<T>()[this._index];
    }


    public ref readonly T RefRO<T>()
    {
        return ref this._array.GetReadOnlySpan<T>()[this._index];
    }


    public static implicit operator EntityId(Entity entity) => entity.Id;
}


public readonly record struct EntityId(int Id, int Version)
{
    public readonly int Id = Id;
    public readonly int Version = Version;


    public override string ToString()
    {
        return $"{this.Id}.v{this.Version}";
    }
}