using System.Text;


namespace EntitiesDotNet;


public partial class EntityManager : IHasVersion
{

    public EntityManager()
    {
        this._arrays = new ResizableArray<IComponentArray>();
        this.Entities = new EntityArrays(this, this._arrays);
    }


    public readonly EntityArrays Entities;
    public int Version { get; private set; }


    public Entity CreateEntity(Archetype archetype)
    {
        var array = (ComponentArray)this.GetArray(archetype);

        int entityId;
        if (this._deadEntityIndices.Count > 0)
        {
            entityId = this._deadEntityIndices.Dequeue();
        }
        else
        {
            entityId = this._entityInfoArray.Count;
            this._entityInfoArray.Add(default);
        }

        var index = array.Count;

        ref var entityInfo = ref this._entityInfoArray[entityId];
        entityInfo.Array = array;
        entityInfo.Index = index;
        entityInfo.Version++;

        if (array.Count == 0)
        {
            this.IncreaseVersion();
        }

        array.Add(new EntityId(entityId, entityInfo.Version));
        return new Entity(this, new EntityId(entityId, entityInfo.Version));
    }


    public void DestroyEntity(EntityId entity)
    {
        if (entity.Id >= this._entityInfoArray.Count)
        {
            throw new InvalidOperationException($"Entity [{entity}] does not exist.");
        }

        ref var entityInfo = ref this._entityInfoArray[entity.Id];
        if (entityInfo.Array == null)
        {
            throw new InvalidOperationException($"Entity [{entity}] was destroyed.");
        }

        var array = entityInfo.Array;
        var index = entityInfo.Index;

        if (index == array.Count - 1)
        {
            array.Remove();
        }
        else
        {
            ComponentArray.CopyTo(array, array.Count - 1, array, index, 1);
            var movedEntityId = array.GetReadOnlySpan<EntityId>()[index].Id;
            this._entityInfoArray[movedEntityId].Index = index;
        }

        if (array.Count == 0)
        {
            this.IncreaseVersion();
        }

        entityInfo.Array = null;
        this._deadEntityIndices.Enqueue(entityInfo.Index);
    }


    public EntityLocation GetEntityLocation(EntityId entityId)
    {
        this.GetEntityLocation(entityId, out var array, out var index);
        return new EntityLocation(array, index);
    }


    public void GetEntityLocation(
        EntityId entityId, out IComponentArray array, out int index
    )
    {
        ref var info = ref this._entityInfoArray[entityId.Id];
        if (info.Version != entityId.Version)
        {
            throw new ArgumentException(
                $"Wrong entity [{entityId.Id}] version. Expected: {info.Version}. Actual: {entityId.Version}");
        }

        if (info.Array == null)
        {
            throw new ArgumentException($"Entity [{entityId}] was destroyed.");
        }

        array = info.Array;
        index = info.Index;
    }


    public IComponentArray GetArray(Archetype archetype)
    {
        archetype = archetype.Add<EntityId>();
        if (this._arrayByArchetype.TryGetValue(archetype, out var array))
        {
            return array;
        }

        array = new ComponentArray(archetype);
        this._arrayByArchetype[archetype] = array;
        this._arrays.Add(array);
        this.IncreaseVersion();

        return array;
    }


    public string ToReadableString()
    {
        var builder = new StringBuilder();
        foreach (var array in this._arrays)
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.AppendLine(array.Archetype.ToString());
            builder.AppendLine(array.ToReadableString());
        }

        return builder.ToString();
    }


    #region Private


    private readonly ResizableArray<IComponentArray> _arrays;


    private readonly Dictionary<Archetype, IComponentArray>
        _arrayByArchetype = new();


    private readonly ResizableArray<EntityInfo> _entityInfoArray = new();
    private readonly Queue<int> _deadEntityIndices = new();


    private void IncreaseVersion()
    {
        ++this.Version;
    }


    private struct EntityInfo
    {
        public int Version;
        public ComponentArray? Array;
        public int Index;
    }


    #endregion


}