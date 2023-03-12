using System.Runtime.CompilerServices;


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


public interface IArchetype
{
    int ComponentCount { get; }
    int GetComponentIndex<T>();
    IArchetype Add<T>();
    IArchetype CopyComponentTo(IArchetype target, int componentIndex);
    IArchetype RemoveAt(int index);
}


public static class ArchetypeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this IArchetype archetype) => archetype.ComponentCount == 0;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains<T>(this IArchetype archetype) =>
        archetype.GetComponentIndex<T>() >= 0;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IArchetype Remove<T>(this IArchetype archetype)
    {
        var index = archetype.GetComponentIndex<T>();
        return index >= 0 ? archetype.RemoveAt(index) : archetype;
    }
}


internal class ComplexArchetype : IArchetype
{
    protected ComplexArchetype(IArchetype left, IArchetype right)
    {
        this._left = left;
        this._right = right;
    }


    public int ComponentCount => this._left.ComponentCount + this._right.ComponentCount;


    public int GetComponentIndex<T>()
    {
        var leftComponentCount = this._left.ComponentCount;

        var index = leftComponentCount;
        if (index >= 0)
        {
            return index;
        }

        if (~index < leftComponentCount)
        {
            return index;
        }

        index = this._right.GetComponentIndex<T>();
        if (index >= 0)
        {
            return index + leftComponentCount;
        }

        return ~(~index + leftComponentCount);
    }


    public IArchetype CopyComponentTo(IArchetype target, int componentIndex)
    {
        return componentIndex < this._left.ComponentCount
            ? this._left.CopyComponentTo(target, componentIndex)
            : this._right.CopyComponentTo(target, componentIndex - this._left.ComponentCount);
    }


    public IArchetype Add<T>()
    {
        var leftComponentCount = this._left.ComponentCount;

        var index = this.GetComponentIndex<T>();
        if (index >= 0) return this;

        index = ~index;

        IArchetype instance;
        if (index < leftComponentCount)
        {
            var leftLastIndex = this._left.ComponentCount - 1;
            var newRight = this._left.CopyComponentTo(this._right, leftLastIndex);
            var newLeft = this._left.RemoveAt(leftLastIndex).Add<T>();
            instance = Instance(newLeft, newRight);
        }
        else
        {
            instance = Instance(this._left, this._right.Add<T>());
        }

        return instance;
    }


    public IArchetype RemoveAt(int index)
    {
        IArchetype newLeft;
        IArchetype newRight;
        if (index < this._left.ComponentCount)
        {
            newLeft = this._right.CopyComponentTo(this._left.RemoveAt(index), 0);
            newRight = this._right.RemoveAt(0);
        }
        else
        {
            newLeft = this._left;
            newRight = this._right.RemoveAt(index - this._left.ComponentCount);
        }

        return newRight.ComponentCount > 0 ? Instance(newLeft, newRight) : newLeft;
    }


    private readonly IArchetype _left;
    private readonly IArchetype _right;


    public static IArchetype Instance(IArchetype left, IArchetype right)
    {
        lock (Instances)
        {
            var key = (left, right);
            if (!Instances.TryGetValue(key, out var instance))
            {
                Instances[key] = instance = new ComplexArchetype(left, right);
            }

            return instance;
        }
    }


    private static readonly Dictionary<(IArchetype Left, IArchetype Right), IArchetype> Instances =
        new();
}