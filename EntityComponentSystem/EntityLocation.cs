namespace EntityComponentSystem;


public readonly record struct EntityLocation(ComponentArray Array, int Index) {
    public readonly ComponentArray Array = Array;
    public readonly int Index = Index;
}