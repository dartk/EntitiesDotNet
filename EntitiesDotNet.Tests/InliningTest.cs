#if !INLINE
using EntitiesDotNet;


namespace EntityComponentSystem.Tests;


public static class SystemWithInlinedMethod
{
    [Inline] public static int Sum(EntityArrays entities)
    {
        var sum = 0;
        entities.ForEach((in int i) =>  sum += i);
        return sum;
    }
}
#endif