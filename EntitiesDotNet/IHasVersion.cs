using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;


namespace EntitiesDotNet;


public interface IHasVersion
{
    int Version { get; }
}


public static class HasVersionExtensions
{
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool VersionsAreEqual(this IHasVersion first, IHasVersion second)
    {
        return first.Version == second.Version;
    }
}