namespace EntitiesDotNet;


public interface IComponentArray
{
    Archetype Archetype { get; }
    int Count { get; }
    bool TryGetReadOnlySpan<T>(out ReadOnlySpan<T> span);
    bool TryGetSpan<T>(out Span<T> span);
    ReadOnlySpan<T> GetReadOnlySpan<T>();
    Span<T> GetSpan<T>();
    object? GetValue(ComponentType component, int index);
    void SetValue(ComponentType component, int index, object? value);
}