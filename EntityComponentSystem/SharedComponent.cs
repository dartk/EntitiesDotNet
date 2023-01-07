using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace EntityComponentSystem;


public interface ISharedComponent
{
    int Id { get; }
    Type Type { get; }
}


[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
public class SharedComponent<T> : ISharedComponent
{

    public int Id { get; }
    public T Value { get; }
    public Type Type => typeof(T);


    public override string ToString()
    {
        return $"{this.Type.Name} = {this.Value}";
    }


    private SharedComponent(T value)
    {
        this.Id = SharedComponent.GetNextId();
        this.Value = value;
    }


    public static SharedComponent<T> Instance(T value)
    {
        lock (Locker)
        {
            if (ComponentByValue.TryGetValue(value, out var component))
            {
                return component;
            }

            component = new SharedComponent<T>(value);
            ComponentByValue[value] = component;

            return component;
        }
    }


    private static readonly object Locker = new();
    private static readonly Dictionary<T, SharedComponent<T>> ComponentByValue = new();
}


public static class SharedComponent
{

    public static SharedComponent<T> Instance<T>(T value) =>
        SharedComponent<T>.Instance(value);


    internal static int GetNextId()
    {
        return Interlocked.Increment(ref NextId);
    }


    private static int NextId;
}