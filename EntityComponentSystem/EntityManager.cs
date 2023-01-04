using System.Text;


namespace EntityComponentSystem;


public partial class EntityManager {

    public ReadOnlySpan<IComponentArray> Arrays => this._arrays;


    public Entity CreateEntity(Archetype archetype) {
        var array = (ComponentArray)this.GetArray(archetype);
        var entityId = this._nextEntityId++;
        var index = array.Count;
        array.Add(new EntityId(entityId));

        var location = new EntityLocation(array, index);

        this._entityLocationById[entityId] = location;
        return new Entity(this, entityId);
    }


    public void DestroyEntity(EntityId entity) {
        if (!this._entityLocationById.TryGetValue(entity.Id, out var location)) {
            throw new InvalidOperationException($"Entity #{entity.Id} does not exist.");
        }

        var (array, index) = location;
        ComponentArray.CopyTo(array, array.Count - 1, array, index, 1);
    }


    public EntityLocation GetEntityLocation(EntityId entityId) =>
        this._entityLocationById[entityId.Id];


    internal EntityLocation GetEntityLocation(int entityId) =>
        this._entityLocationById[entityId];


    public IComponentArray GetArray(Archetype archetype) {
        if (this._arrayByArchetype.TryGetValue(archetype, out var array)) {
            return array;
        }

        array = new ComponentArray(archetype);
        this._arrayByArchetype[archetype] = array;
        this._arrays.Add(array);

        return array;
    }


    public string ToReadableString() {
        var builder = new StringBuilder();
        foreach (var array in this._arrays) {
            if (builder.Length > 0) {
                builder.AppendLine();
            }

            builder.AppendLine(array.Archetype.ToString());
            builder.AppendLine(array.ToReadableString());
        }

        return builder.ToString();
    }


    #region Private


    private readonly ResizableArray<IComponentArray> _arrays = new ();


    private readonly Dictionary<Archetype, IComponentArray>
        _arrayByArchetype = new ();


    private readonly Dictionary<int, EntityLocation> _entityLocationById = new ();
    private int _nextEntityId;


    #endregion


}