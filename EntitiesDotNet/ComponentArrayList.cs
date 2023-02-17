using System.Collections;


namespace EntitiesDotNet;


public class EntityArrays : IReadOnlyList<IComponentArray>, IHasVersion
{
    public EntityArrays(IHasVersion owner, ResizableArray<IComponentArray> arrays)
    {
        this._owner = owner;
        this._arrays = arrays;
    }


    public int Version => this._owner.Version;


    public int Count => this._arrays.Count;


    public ReadOnlySpan<IComponentArray>.Enumerator GetEnumerator() =>
        this._arrays.AsReadOnly().GetEnumerator();


    public IComponentArray this[int index] => this._arrays[index];


    IEnumerator<IComponentArray> IEnumerable<IComponentArray>.GetEnumerator() =>
        ((IEnumerable<IComponentArray>)this._arrays).GetEnumerator();


    IEnumerator IEnumerable.GetEnumerator() =>
        ((IEnumerable)this._arrays).GetEnumerator();


    public ReadOnlySpan<IComponentArray> AsSpan() => this._arrays.AsSpan();


    public static implicit operator ReadOnlySpan<IComponentArray>(EntityArrays @this) =>
        @this.AsSpan();


    private readonly IHasVersion _owner;
    private readonly ResizableArray<IComponentArray> _arrays;
}