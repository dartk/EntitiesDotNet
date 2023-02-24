namespace EntitiesDotNet.Generators;


internal abstract record Result
{
    public Result AsResult => this;

    public record Ok(FileNameWithText File) : Result;
    public record Error(Exception Exception) : Result;
}