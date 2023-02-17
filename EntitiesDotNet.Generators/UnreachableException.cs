namespace EntitiesDotNet.Generators;


internal class UnreachableException : Exception
{
    public UnreachableException()
    {
    }


    public UnreachableException(string message) : base(message)
    {
    }
}