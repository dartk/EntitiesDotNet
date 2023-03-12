using System.Collections.Immutable;


namespace EntitiesDotNet;


public partial class Archetype
{
    #region Static

    static Archetype()
    {
        ArchetypePool = new ArchetypePoolClass();
        Empty = ArchetypePool.Get(new ComponentTypeSet());
    }


    public static readonly Archetype Empty;
    public static Archetype Instance() => Empty;


    public static Archetype Instance(ComponentTypeSet id)
    {
        lock (Locker)
        {
            return ArchetypePool.Get(id);
        }
    }


    private static readonly object Locker = new();
    private static readonly ArchetypePoolClass ArchetypePool;

    #endregion


    #region Public

    protected Archetype(ComponentTypeSet components)
    {
        this.ComponentTypeSet = components;
        this._components = components.ToImmutableArray();
    }


    public ComponentTypeSet ComponentTypeSet { get; }


    public ReadOnlySpan<ComponentType> Components => this._components.AsSpan();


    public bool Contains(ComponentType component)
    {
        return this.GetIndex(component) >= 0;
    }


    public bool Contains(ComponentTypeSet components)
    {
        return this.ComponentTypeSet.Contains(components);
    }


    public Archetype Add(ComponentTypeSet components)
    {
        var newComponentTypeSet = this.ComponentTypeSet.Add(components);
        return newComponentTypeSet == this.ComponentTypeSet
            ? this
            : Instance(newComponentTypeSet);
    }


    public Archetype Remove(ComponentTypeSet components)
    {
        var newComponentTypeSet = this.ComponentTypeSet.Remove(components);
        return newComponentTypeSet == this.ComponentTypeSet
            ? this
            : Instance(newComponentTypeSet);
    }


    public int GetIndex(ComponentType component)
    {
        return this._components.BinarySearch(component);
    }


    public int GetIndex<T>() => this.GetIndex(ComponentType<T>.Instance);


    public override string ToString()
    {
        var components = string.Join(", ", this._components.Select(x => x.Type.Name));
        return $"Archetype {{ {components} }}";
    }

    #endregion


    #region Private

    private readonly ImmutableArray<ComponentType> _components;

    #endregion


    private class ArchetypePoolClass
    {
        public Archetype Get(ComponentTypeSet id)
        {
            if (!this._archetypeById.TryGetValue(id, out var archetype))
            {
                this._archetypeById[id] = archetype = new Archetype(id);
            }

            return archetype;
        }


        private readonly Dictionary<ComponentTypeSet, Archetype> _archetypeById = new()
        {
            { new ComponentTypeSet(), new Archetype(new ComponentTypeSet()) }
        };
    }
}