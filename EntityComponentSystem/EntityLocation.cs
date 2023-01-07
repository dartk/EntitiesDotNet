namespace EntityComponentSystem;


public readonly record struct EntityLocation(IComponentArray Array, int Index)
{
    public readonly IComponentArray Array = Array;
    public readonly int Index = Index;
}