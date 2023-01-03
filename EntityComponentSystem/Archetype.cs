namespace EntityComponentSystem;


public partial class Archetype {


    #region Public


    public static Archetype Instance() => Empty;


    public static Archetype Instance(params Type[] components) {
        return Instance(components.AsEnumerable(), null);
    }


    public static Archetype Instance(
        IEnumerable<Type> components,
        IEnumerable<ISharedComponent>? sharedComponents
    ) {
        var newArchetype = new Archetype(components, sharedComponents);
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
        Empty = Instance(Array.Empty<Type>(), null);
    }


    public static readonly Archetype Empty;


    private static readonly List<Archetype> Archetypes;


    private Archetype(
        IEnumerable<Type> components,
        IEnumerable<ISharedComponent>? sharedComponents
    ) {
        var componentsArray = components.Distinct().ToArray();
        var sharedComponentsArray =
            sharedComponents?.Distinct().ToArray()
            ?? Array.Empty<ISharedComponent>();

        Array.Sort(componentsArray, ComponentComparer.Instance);
        Array.Sort(sharedComponentsArray, SharedComponentComparer.Instance);

        this._components = componentsArray;
        this._sharedComponents = sharedComponentsArray;
    }


    public ReadOnlySpan<Type> Components => this._components;
    public ReadOnlySpan<ISharedComponent> SharedComponents => this._sharedComponents;


    public bool Contains(Type component) {
        return this.GetIndex(component) >= 0;
    }


    public bool Contains(ISharedComponent sharedComponent) {
        return Array.BinarySearch(this._sharedComponents, sharedComponent,
            SharedComponentComparer.Instance) >= 0;
    }


    public bool Contains(Archetype archetype) {
        foreach (var component in archetype.Components) {
            if (!this.Contains(component)) {
                return false;
            }
        }

        foreach (var sharedComponent in archetype.SharedComponents) {
            if (!this.Contains(sharedComponent)) {
                return false;
            }
        }

        return true;
    }


    public Archetype Add(IEnumerable<Type> components) {
        return Instance(this._components.Concat(components), this._sharedComponents);
    }


    public Archetype Add(params Type[] components) {
        return this.Add(components.AsEnumerable());
    }


    public Archetype AddShared(IEnumerable<ISharedComponent> sharedComponents) {
        return Instance(this._components,
            this._sharedComponents.Concat(sharedComponents));
    }


    public Archetype AddShared(params ISharedComponent[] sharedComponents) {
        return this.AddShared(sharedComponents.AsEnumerable());
    }


    public Archetype Remove(params Type[] components) {
        var newComponents = new List<Type>(this._components.Length);

        Array.Sort(components, ComponentComparer.Instance);

        foreach (var component in this._components) {
            if (
                Array.BinarySearch(components, component, ComponentComparer.Instance) < 0
            ) {
                newComponents.Add(component);
            }
        }

        return Instance(newComponents, this._sharedComponents);
    }


    public Archetype Remove(params ISharedComponent[] sharedComponents) {
        var newSharedComponents =
            new List<ISharedComponent>(this._sharedComponents.Length);

        Array.Sort(sharedComponents, SharedComponentComparer.Instance);

        foreach (var sharedComponent in this._sharedComponents) {
            if (Array.BinarySearch(sharedComponents, sharedComponent,
                    SharedComponentComparer.Instance) < 0
            ) {
                newSharedComponents.Add(sharedComponent);
            }
        }

        return Instance(this._components, newSharedComponents);
    }


    public int GetIndex(Type component) {
        return Array.BinarySearch(this._components, component,
            ComponentComparer.Instance);
    }


    public override string ToString() {
        var components = string.Join<Type>(", ", this._components);
        var sharedComponents =
            string.Join<ISharedComponent>(", ", this._sharedComponents);

        var allComponents = string.Join(", ",
            new[] { components, sharedComponents }.Where(x => !string.IsNullOrEmpty(x)));

        return $"Archetype {{ {allComponents} }}";
    }


    #endregion


    #region Private


    private readonly Type[] _components;
    private readonly ISharedComponent[] _sharedComponents;


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


    private class SharedComponentComparer : IComparer<ISharedComponent> {

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static readonly SharedComponentComparer Instance = new ();


        private SharedComponentComparer() {
        }


        public int Compare(ISharedComponent x, ISharedComponent y) {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;

            return x.Id.CompareTo(y.Id);
        }

    }


    private class ArchetypeComparer : IComparer<Archetype> {

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static readonly ArchetypeComparer Instance = new ();


        private ArchetypeComparer() {
        }


        public int Compare(Archetype archetypeX, Archetype archetypeY) {
            return CompareComponents(archetypeX.Components, archetypeY.Components)
                switch {
                    0 => CompareSharedComponents(
                        archetypeX.SharedComponents,
                        archetypeY.SharedComponents),
                    var result => result
                };
        }


        private static int CompareComponents(
            ReadOnlySpan<Type> componentsX,
            ReadOnlySpan<Type> componentsY
        ) {
            if (componentsX.Length < componentsY.Length) {
                return -1;
            }
            else if (componentsX.Length > componentsY.Length) {
                return 1;
            }

            var length = componentsX.Length;
            for (var i = 0; i < length; ++i) {
                var x = componentsX[i];
                var y = componentsY[i];
                var result = ComponentComparer.Instance.Compare(x, y);
                if (result != 0) {
                    return result;
                }
            }

            return 0;
        }


        private static int CompareSharedComponents(
            ReadOnlySpan<ISharedComponent> componentsX,
            ReadOnlySpan<ISharedComponent> componentsY
        ) {
            if (componentsX.Length < componentsY.Length) {
                return -1;
            }
            else if (componentsX.Length > componentsY.Length) {
                return 1;
            }

            var length = componentsX.Length;
            for (var i = 0; i < length; ++i) {
                var x = componentsX[i];
                var y = componentsY[i];
                var result = SharedComponentComparer.Instance.Compare(x, y);
                if (result != 0) {
                    return result;
                }
            }

            return 0;
        }
    }


    #endregion


}