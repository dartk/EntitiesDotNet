namespace EntityComponentSystem;


public class EntityQueryCache {
    
    public EntityQueryCache(
        EntityManager entityManager,
        params Func<IComponentArray, bool>[] predicates
    ) {
        this.EntityManager = entityManager;
        this.PredicateArray = predicates;
        this.Version = -1;
        this._entities = new ResizableArray<IComponentArray>();
        this.Entities = this._entities;
    }


    public EntityManager EntityManager { get; }
    public Func<IComponentArray, bool>[] PredicateArray { get; }
    public int Version { get; private set; }
    public ReadOnlyArray<IComponentArray> Entities { get; }


    public ReadOnlySpan<IComponentArray>.Enumerator GetEnumerator() =>
        this.Entities.GetEnumerator();


    public void Update() {
        if (this.Version == this.EntityManager.Version) {
            return;
        }

        this._entities.Clear();
        foreach (var array in this.EntityManager.Entities) {
            var predicateIsSuccessful = true;
            foreach (var predicate in this.PredicateArray.AsSpan()) {
                if (!predicate(array)) {
                    predicateIsSuccessful = false;
                    break;
                }
            }

            if (predicateIsSuccessful) {
                this._entities.Add(array);
            }
        }

        this.Version = this.EntityManager.Version;
    }


    private readonly ResizableArray<IComponentArray> _entities;
    
}