namespace EntityComponentSystem;


public static class ComponentType<T> {
    public static readonly ComponentType Instance = ComponentType.Instance(typeof(T));
}


public class ComponentType : IComparable<ComponentType> {
    public readonly Type Type;
    public readonly int Id;

    public override string ToString() {
        return this.Type.ToString();
    }


    public int CompareTo(ComponentType? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;

        return this.Id.CompareTo(other.Id);
    }


    public static ComponentType Instance<T>() => ComponentType<T>.Instance;


    public static ComponentType Instance(Type type) {
        lock (ComponentTypes) {
            var count = ComponentTypes.Count;
            for (var i = 0; i < count; ++i) {
                var componentType = ComponentTypes[i];
                if (componentType.Type == type) {
                    return componentType;
                }
            }

            {
                var componentType = new ComponentType(count, type);
                ComponentTypes.Add(componentType);
                return componentType;
            }
        }
    }


    public static implicit operator ComponentType(Type type) => Instance(type);
    public static implicit operator Type(ComponentType type) => type.Type;


    private ComponentType(int id, Type type) {
        this.Id = id;
        this.Type = type;
    }


    private static readonly ResizableArray<ComponentType> ComponentTypes = new () {
        new ComponentType(0, typeof(EntityId))
    };

}