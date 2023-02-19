// ReSharper disable once CheckNamespace


namespace EntitiesDotNet;


[AttributeUsage(AttributeTargets.Method)]
internal class GenerateOnExecute : Attribute
{
}


[AttributeUsage(AttributeTargets.Struct)]
internal class EntityRefStructAttribute : Attribute
{
}


[AttributeUsage(AttributeTargets.Struct)]
internal class GenerateImplicitOperators : Attribute
{
}
