namespace EntityComponentSystem;


public class EntityQueryCache : IHasVersion
{
    public EntityQueryCache(
        EntityArrays entities,
        params Func<IComponentArray, bool>[] predicates
    )
    {
        this._targetEntities = entities;
        this.Predicates = predicates;
        this.Version = -1;
        this._array = new ResizableArray<IComponentArray>();
        this.Entities = new EntityArrays(this, this._array);
    }


    public Func<IComponentArray, bool>[] Predicates { get; }
    public int Version { get; private set; }
    public EntityArrays Entities { get; }


    public ReadOnlySpan<IComponentArray>.Enumerator GetEnumerator() =>
        this.Entities.GetEnumerator();


    public void Update()
    {
        if (this.VersionsAreEqual(this._targetEntities)) return;

        this._array.Clear();
        foreach (var array in this._targetEntities)
        {
            var allPredicatesSucceded = true;
            foreach (var predicate in this.Predicates.AsSpan())
            {
                if (!predicate(array))
                {
                    allPredicatesSucceded = false;
                    break;
                }
            }

            if (allPredicatesSucceded)
            {
                this._array.Add(array);
            }
        }

        this.Version = this._targetEntities.Version;
    }


    private readonly ResizableArray<IComponentArray> _array;
    private readonly EntityArrays _targetEntities;
}