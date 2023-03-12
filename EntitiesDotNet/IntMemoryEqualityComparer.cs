namespace EntitiesDotNet;


internal class IntMemoryEqualityComparer : IEqualityComparer<ReadOnlyMemory<int>>
{
    public bool Equals(ReadOnlyMemory<int> x, ReadOnlyMemory<int> y)
    {
        if (x.Equals(y)) return true;
        if (x.Length != y.Length) return false;

        var xSpan = x.Span;
        var ySpan = y.Span;
        for (var i = 0; i < x.Length; ++i)
        {
            if (xSpan[i] != ySpan[i]) return false;
        }

        return true;
    }


    public int GetHashCode(ReadOnlyMemory<int> obj)
    {
        unchecked
        {
            var hashCode = obj.Length;
            foreach (var i in obj.Span)
            {
                hashCode = hashCode * 397 ^ i;
            }

            return hashCode;
        }
    }
}