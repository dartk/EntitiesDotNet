namespace EntitiesDotNet;


[AttributeUsage(AttributeTargets.Method)]
public class SupportsInliningAttribute : Attribute
{
    public SupportsInliningAttribute(string template)
    {
    }
}