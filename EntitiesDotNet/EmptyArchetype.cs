namespace EntitiesDotNet;


internal class EmptyArchetype : Archetype
{
    public EmptyArchetype() : base(Array.Empty<ComponentType>())
    {
    }


    public override bool Contains<T>() => false;
    public override Archetype Add<T>() => Archetype<T>.Instance;
    public override Archetype Remove<T>() => this;
}