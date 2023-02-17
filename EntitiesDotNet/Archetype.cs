namespace EntitiesDotNet;


public partial class Archetype
{


    #region Public


    public static Archetype Instance() => Empty;


    public static Archetype Instance(params ComponentType[] components)
    {
        return Instance(components.AsEnumerable(), null);
    }


    public static Archetype Instance(
        IEnumerable<ComponentType> components,
        IEnumerable<ISharedComponent>? sharedComponents
    )
    {
        var newArchetype = new Archetype(components, sharedComponents);
        lock (Archetypes)
        {
            var index = Archetypes.BinarySearch(newArchetype, ArchetypeComparer.Instance);
            if (index >= 0)
            {
                return Archetypes[index];
            }

            Archetypes.Add(newArchetype);
            Archetypes.Sort(ArchetypeComparer.Instance);

            return newArchetype;
        }
    }


    static Archetype()
    {
        Archetypes = new List<Archetype>();
        Empty = Instance(Array.Empty<ComponentType>(), null);
    }


    public static readonly Archetype Empty;


    private static readonly List<Archetype> Archetypes;


    private Archetype(
        IEnumerable<ComponentType> components,
        IEnumerable<ISharedComponent>? sharedComponents
    )
    {
        var componentsArray = components.Append(ComponentType<EntityId>.Instance)
            .Distinct().ToArray();

        var sharedComponentsArray =
            sharedComponents?.Distinct().ToArray()
            ?? Array.Empty<ISharedComponent>();

        Array.Sort(componentsArray);
        Array.Sort(sharedComponentsArray, SharedComponentComparer.Instance);

        this._components = componentsArray;
        this._sharedComponents = sharedComponentsArray;
    }


    public ReadOnlySpan<ComponentType> Components => this._components;
    public ReadOnlySpan<ISharedComponent> SharedComponents => this._sharedComponents;


    public bool Contains(ComponentType component)
    {
        return this.GetIndex(component) >= 0;
    }


    public bool Contains(ISharedComponent sharedComponent)
    {
        return Array.BinarySearch(this._sharedComponents, sharedComponent,
            SharedComponentComparer.Instance) >= 0;
    }


    public bool Contains(Archetype archetype)
    {
        foreach (var component in archetype.Components)
        {
            if (!this.Contains(component))
            {
                return false;
            }
        }

        foreach (var sharedComponent in archetype.SharedComponents)
        {
            if (!this.Contains(sharedComponent))
            {
                return false;
            }
        }

        return true;
    }


    public Archetype With(IEnumerable<ComponentType> components)
    {
        return Instance(this._components.Concat(components), this._sharedComponents);
    }


    public Archetype With(params ComponentType[] components)
    {
        return this.With(components.AsEnumerable());
    }


    public Archetype With(IEnumerable<ISharedComponent> sharedComponents)
    {
        return Instance(this._components,
            this._sharedComponents.Concat(sharedComponents));
    }


    public Archetype With(params ISharedComponent[] sharedComponents)
    {
        return this.With(sharedComponents.AsEnumerable());
    }


    public Archetype Without(params ComponentType[] components)
    {
        var newComponents = new List<ComponentType>(this._components.Length);

        Array.Sort(components);

        foreach (var component in this._components)
        {
            if (
                Array.BinarySearch(components, component) < 0
            )
            {
                newComponents.Add(component);
            }
        }

        return Instance(newComponents, this._sharedComponents);
    }


    public Archetype Without(params ISharedComponent[] sharedComponents)
    {
        var newSharedComponents =
            new List<ISharedComponent>(this._sharedComponents.Length);

        Array.Sort(sharedComponents, SharedComponentComparer.Instance);

        foreach (var sharedComponent in this._sharedComponents)
        {
            if (Array.BinarySearch(sharedComponents, sharedComponent,
                    SharedComponentComparer.Instance) < 0
            )
            {
                newSharedComponents.Add(sharedComponent);
            }
        }

        return Instance(this._components, newSharedComponents);
    }


    public int GetIndex(ComponentType component)
    {
        return Array.BinarySearch(this._components, component);
    }


    public int GetIndex<T>() => this.GetIndex(ComponentType<T>.Instance);


    public override string ToString()
    {
        var components = string.Join(", ", this._components.Select(x => x.Type.Name));
        var sharedComponents =
            string.Join<ISharedComponent>(", ", this._sharedComponents);

        var allComponents = string.Join(", ",
            new[] { components, sharedComponents }.Where(x => !string.IsNullOrEmpty(x)));

        return $"Archetype {{ {allComponents} }}";
    }


    #endregion


    #region Private


    private readonly ComponentType[] _components;
    private readonly ISharedComponent[] _sharedComponents;


    private class SharedComponentComparer : IComparer<ISharedComponent>
    {

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static readonly SharedComponentComparer Instance = new();


        private SharedComponentComparer()
        {
        }


        public int Compare(ISharedComponent x, ISharedComponent y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;

            return x.Id.CompareTo(y.Id);
        }

    }


    private class ArchetypeComparer : IComparer<Archetype>
    {

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static readonly ArchetypeComparer Instance = new();


        private ArchetypeComparer()
        {
        }


        public int Compare(Archetype archetypeX, Archetype archetypeY)
        {
            return CompareComponents(archetypeX.Components, archetypeY.Components)
                switch
            {
                0 => CompareSharedComponents(
                    archetypeX.SharedComponents,
                    archetypeY.SharedComponents),
                var result => result
            };
        }


        private static int CompareComponents(
            ReadOnlySpan<ComponentType> componentsX,
            ReadOnlySpan<ComponentType> componentsY
        )
        {
            if (componentsX.Length < componentsY.Length)
            {
                return -1;
            }
            else if (componentsX.Length > componentsY.Length)
            {
                return 1;
            }

            var length = componentsX.Length;
            for (var i = 0; i < length; ++i)
            {
                var x = componentsX[i];
                var y = componentsY[i];
                var result = x.CompareTo(y);
                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        }


        private static int CompareSharedComponents(
            ReadOnlySpan<ISharedComponent> componentsX,
            ReadOnlySpan<ISharedComponent> componentsY
        )
        {
            if (componentsX.Length < componentsY.Length)
            {
                return -1;
            }
            else if (componentsX.Length > componentsY.Length)
            {
                return 1;
            }

            var length = componentsX.Length;
            for (var i = 0; i < length; ++i)
            {
                var x = componentsX[i];
                var y = componentsY[i];
                var result = SharedComponentComparer.Instance.Compare(x, y);
                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        }
    }


    #endregion


}