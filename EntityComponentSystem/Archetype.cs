namespace EntityComponentSystem;


public partial class Archetype {


    #region Public


    public static Archetype Instance() => Empty;


    public static Archetype Instance(params Type[] components) {
        return Instance(components.AsEnumerable());
    }


    public static Archetype Instance(IEnumerable<Type> components) {
        var newArchetype = new Archetype(components);
        lock (Archetypes) {
            var index = Archetypes.BinarySearch(newArchetype, ArchetypeComparer.Instance);
            if (index >= 0) {
                return Archetypes[index];
            }

            Archetypes.Add(newArchetype);
            Archetypes.Sort(ArchetypeComparer.Instance);

            return newArchetype;
        }
    }


    static Archetype() {
        Archetypes = new List<Archetype>();
        Empty = Instance(Array.Empty<Type>());
    }


    public static readonly Archetype Empty;


    private static readonly List<Archetype> Archetypes;


    private Archetype(IEnumerable<Type> components) :
        this(components.Distinct().ToArray()) {
    }


    private Archetype(Type[] components) {
        Array.Sort(components, ComponentComparer.Instance);
        this._components = components;
    }


    public ReadOnlySpan<Type> Components => this._components;


    public bool Contains(Type component) {
        return this.GetIndex(component) >= 0;
    }


    public Archetype Add(IEnumerable<Type> components) {
        return Instance(this._components.Concat(components));
    }


    public Archetype Add(params Type[] components) {
        return this.Add(components.AsEnumerable());
    }


    public Archetype Remove(params Type[] components) {
        var newFields = new List<Type>(this._components.Length);

        Array.Sort(components, ComponentComparer.Instance);

        foreach (var component in this._components) {
            if (Array.BinarySearch(components, component, ComponentComparer.Instance) <
                0) {
                newFields.Add(component);
            }
        }

        return Instance(newFields);
    }


    public int GetIndex(Type component) {
        return Array.BinarySearch(this._components, component,
            ComponentComparer.Instance);
    }


    #endregion


    #region Private


    private readonly Type[] _components;


    private class ComponentComparer : IComparer<Type> {

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static readonly ComponentComparer Instance = new ();


        private ComponentComparer() {
        }


        public int Compare(Type x, Type y) {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;

            return x.GUID.CompareTo(y.GUID);
        }

    }


    private class ArchetypeComparer : IComparer<Archetype> {

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static readonly ArchetypeComparer Instance = new ();


        private ArchetypeComparer() {
        }


        public int Compare(Archetype xArchetype, Archetype yArchetype) {
            var fieldComparer = ComponentComparer.Instance;
            var xFields = xArchetype.Components;
            var yFields = yArchetype.Components;

            if (xFields.Length < yFields.Length) {
                return -1;
            }
            else if (xFields.Length > yFields.Length) {
                return 1;
            }

            var length = xFields.Length;
            for (var i = 0; i < length; ++i) {
                var x = xFields[i];
                var y = yFields[i];
                var result = fieldComparer.Compare(x, y);
                if (result != 0) {
                    return result;
                }
            }

            return 0;
        }

    }


    #endregion


}