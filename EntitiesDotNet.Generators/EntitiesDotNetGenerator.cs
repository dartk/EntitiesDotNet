using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace EntitiesDotNet.Generators;


[Generator]
public class EntitiesDotNetGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static context =>
        {
            EntityRef.AddAttributes(context);
            Inlining.AddAttributes(context);
        });

        var entityRefProvider = EntityRef.CreateProvider(context);
        context.RegisterImplementationSourceOutput(entityRefProvider, static (context, arg) =>
        {
            switch (arg)
            {
                case Result.Ok { File: var file }:
                    FileNameWithText.AddSource(context, file);
                    break;
                case Result.Error { Exception: var ex }:
                    context.ReportDiagnostic(Diagnostic.Create(ErrorDescriptor, Location.None,
                        ex.Message));
                    break;
            }
        });

        var compilationWithGeneratedProvider = context.CompilationProvider
            .Combine(
                entityRefProvider
                    .Where(static x => x is Result.Ok)
                    .Select(static (x, _) => ((Result.Ok)x).File)
                    .Collect())
            .Select(static (arg, token) =>
            {
                var (compilation, files) = arg;

                var compilationWithGeneratedEntityRefs = compilation.AddSyntaxTrees(
                    files.Select(file =>
                        CSharpSyntaxTree.ParseText(file.Text, cancellationToken: token)));

                return compilationWithGeneratedEntityRefs;
            });


        var inliningProvider = context.SyntaxProvider.CreateSyntaxProvider(
                Inlining.Predicate,
                static (context, _) => context.Node)
            .Combine(compilationWithGeneratedProvider)
            .Select(static (arg, token) =>
            {
                try
                {
                    var (node, compilationWithGenerated) = arg;
                    var semanticModel = compilationWithGenerated.GetSemanticModel(node.SyntaxTree);
                    var file = Inlining.Transform(node, semanticModel, token);
                    if (file.IsEmpty)
                    {
                        return null;
                    }

                    file = file.FormatText(token);

                    return new Result.Ok(file).AsResult;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    return new Result.Error(ex);
                }
            })
            .Where(x => x != null);

        context.RegisterImplementationSourceOutput(inliningProvider, static (context, result) =>
        {
            switch (result)
            {
                case Result.Ok { File: var file }:
                    FileNameWithText.AddSource(context, file);
                    break;
                case Result.Error { Exception: var ex }:
                    context.ReportDiagnostic(Diagnostic.Create(ErrorDescriptor, Location.None,
                        ex.Message));
                    break;
            }
        });
    }


    private static readonly DiagnosticDescriptor ErrorDescriptor = new(
        id: $"{DiagnosticIdPrefix}001",
        title: "EntitiesDotNet source generator error",
        messageFormat: "{0}",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    private const string DiagnosticIdPrefix = "EntitiesDotNet";
    private const string DiagnosticCategory = "EntitiesDotNet";
}