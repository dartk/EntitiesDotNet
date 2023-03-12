using System.Collections.Immutable;


namespace EntitiesDotNet;


public sealed partial class Archetype
{
    #region Static

    static Archetype()
    {
        ArchetypePool = new ArchetypePoolClass();
        Empty = ArchetypePool.Get(new ComponentTypeFlags());
    }


    public static readonly Archetype Empty;
    public static Archetype Instance() => Empty;


    public static Archetype Instance(ComponentTypeFlags flags)
    {
        lock (Locker)
        {
            return ArchetypePool.Get(flags);
        }
    }


    private static readonly object Locker = new();
    private static readonly ArchetypePoolClass ArchetypePool;

    #endregion


    #region Public

    private Archetype(ComponentTypeFlags flags)
    {
        this.Flags = flags;
        this.Components = flags.GetComponentTypeArray();
    }


    public ComponentTypeFlags Flags { get; }


    public ImmutableArray<ComponentType> Components { get; }


    public bool Contains(ComponentType component)
    {
        return this.GetIndex(component) >= 0;
    }


    public bool Contains(ComponentTypeFlags components)
    {
        return (this.Flags & components) == components;
    }


    public Archetype Add(ComponentTypeFlags components)
    {
        var newFlags = this.Flags | components;
        return newFlags == this.Flags
            ? this
            : Instance(newFlags);
    }


    public Archetype Remove(ComponentTypeFlags components)
    {
        var newFlags = this.Flags & ~components;
        return newFlags == this.Flags
            ? this
            : Instance(newFlags);
    }


    public int GetIndex(ComponentType component)
    {
        return this.Components.BinarySearch(component);
    }


    public int GetIndex<T>() => this.GetIndex(ComponentType<T>.Instance);


    public override string ToString()
    {
        var components = string.Join(", ", this.Components.Select(x => x.Type.Name));
        return $"Archetype {{ {components} }}";
    }

    #endregion


    #region Private

    #endregion


    private class ArchetypePoolClass
    {
        public Archetype Get(ComponentTypeFlags flags)
        {
            if (!this._dictionary.TryGetValue(flags, out var archetype))
            {
                this._dictionary[flags] = archetype = new Archetype(flags);
            }

            return archetype;
        }


        private readonly Dictionary<ComponentTypeFlags, Archetype> _dictionary = new()
        {
            { default, new Archetype(default) }
        };
    }
}