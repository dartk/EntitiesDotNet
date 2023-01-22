using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;


namespace EntityComponentSystem.Generators;


public abstract class IncrementalGeneratorBase<T> : IIncrementalGenerator
{

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context
            .SyntaxProvider
            .CreateSyntaxProvider(this.Where, this.Select)
            .Where(x => x is not null)
            .Collect();

        context.RegisterSourceOutput(provider, this.Produce!);
    }


    protected abstract bool Where(SyntaxNode node, CancellationToken token);
    protected abstract T? Select(GeneratorSyntaxContext context, CancellationToken token);


    protected abstract void Produce(
        SourceProductionContext context, ImmutableArray<T> items);


    protected static string GetTemplate(string fileName)
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"EntityComponentSystem.Generators.Templates.{fileName}");
        if (stream == null)
        {
            throw new ArgumentException($"Template '{fileName}' is not found.");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}