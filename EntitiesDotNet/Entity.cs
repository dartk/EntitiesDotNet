namespace EntitiesDotNet;


public partial record struct Entity : IDisposable
{
    public Entity(EntityManager entityManager, EntityId id)
    {
        this.Id = id;
        this.EntityManager = entityManager;
        entityManager.GetEntityLocation(id, out this._array, out this._index);
    }


    public readonly EntityId Id;
    public readonly EntityManager EntityManager;
    public Archetype Archetype => this.EntityManager.GetEntityArchetype(this.Id);

    private IComponentArray _array;
    private int _index;


    public void Dispose()
    {
        this.EntityManager.DestroyEntity(this);
    }


    public readonly ref T RefRW<T>()
    {
        return ref this._array.GetSpan<T>()[this._index];
    }


    public readonly ref readonly T RefRO<T>()
    {
        return ref this._array.GetReadOnlySpan<T>()[this._index];
    }


    public void SetArchetype(Archetype newArchetype)
    {
        var newLocation = this.EntityManager.SetEntityArchetype(this, newArchetype);
        this._array = newLocation.Array;
        this._index = newLocation.Index;
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