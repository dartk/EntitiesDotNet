using System.Reflection;


namespace EntitiesDotNet;


public partial class Archetype
{
    #region Public

    public static Archetype Instance() => Empty;


    public static Archetype Instance(params ComponentType[] components) =>
        Instance(components.AsEnumerable());


    public static Archetype Instance(IEnumerable<ComponentType> components)
    {
        var componentsArray = components.Append(ComponentType<EntityId>.Instance)
            .Distinct().ToArray();
        Array.Sort(componentsArray);

        lock (Archetypes)
        {
            if (Archetypes.TryGetValue(componentsArray, out var existingArchetype))
            {
                return existingArchetype;
            }

            Archetype newArchetype;
            switch (componentsArray.Length)
            {
                case 0:
                    newArchetype = Empty;
                    break;
                case < 8:
                {
                    var assembly = typeof(Archetype).Assembly;
                    var typeName = $"EntitiesDotNet.Archetype`{componentsArray.Length}";
                    var type = assembly.GetType(typeName);
                    var constructedArchetypeType =
                        type!.MakeGenericType(componentsArray.Select(x => x.Type).ToArray());

                    var constructor = constructedArchetypeType.GetConstructor(
                        BindingFlags.Instance | BindingFlags.NonPublic, null,
                        new[] { typeof(ComponentType[]) }, null)!;

                    newArchetype = (Archetype)constructor.Invoke(new object[] { componentsArray });
                    break;
                }
                default:
                    newArchetype = new Archetype(componentsArray);
                    break;
            }

            Archetypes.Add(componentsArray, newArchetype);

            return newArchetype;
        }
    }


    static Archetype()
    {
        Empty = new EmptyArchetype();
        Archetypes = new SortedList<ComponentType[], Archetype>(ComponentTypesComparer.Instance)
        {
            { Array.Empty<ComponentType>(), Empty }
        };
    }


    public static readonly Archetype Empty;


    private static readonly SortedList<ComponentType[], Archetype> Archetypes;


    protected Archetype(ComponentType[] components)
    {
        this._components = components;
    }


    public ReadOnlySpan<ComponentType> Components => this._components;


    public bool Contains(ComponentType component)
    {
        return this.GetIndex(component) >= 0;
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

        return true;
    }


    public virtual bool Contains<T>() => this.GetIndex(ComponentType<T>.Instance) >= 0;


    public virtual Archetype Add<T>() =>
        this.Contains<T>()
            ? this
            : Instance(this._components.Append(ComponentType<T>.Instance));


    public virtual Archetype Remove<T>() =>
        this.Contains<T>()
            ? Instance(this._components.Where(x => x != ComponentType<T>.Instance))
            : this;


    public Archetype Add(IEnumerable<ComponentType> components)
    {
        return Instance(this._components.Concat(components));
    }


    public Archetype Add(params ComponentType[] components)
    {
        return this.Add(components.AsEnumerable());
    }


    public Archetype Remove(params ComponentType[] components)
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

        return Instance(newComponents);
    }


    public int GetIndex(ComponentType component)
    {
        return Array.BinarySearch(this._components, component);
    }


    public int GetIndex<T>() => this.GetIndex(ComponentType<T>.Instance);


    public override string ToString()
    {
        var components = string.Join(", ", this._components.Select(x => x.Type.Name));
        return $"Archetype {{ {components} }}";
    }

    #endregion


    #region Private

    private readonly ComponentType[] _components;


    private class ComponentTypesComparer : IComparer<ComponentType[]>
    {
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static readonly ComponentTypesComparer Instance = new();


        private ComponentTypesComparer()
        {
        }


        public int Compare(ComponentType[] componentsX, ComponentType[] componentsY)
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
    }

    #endregion
}