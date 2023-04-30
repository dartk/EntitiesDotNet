using System.Collections.Immutable;
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
            WrapperStruct.AddAttributes(context);
            EntityRef.AddAttributes(context);
            Inlining.AddAttributes(context);
            GenerateSystem.AddAttributes(context);
        });

        var generateSystemProvider = GenerateSystem.CreateProvider(context);
        context.RegisterImplementationSourceOutputForResult(generateSystemProvider);

        var entityRefProvider = EntityRef.CreateProvider(context);
        context.RegisterImplementationSourceOutputForResult(entityRefProvider!);

        var wrapperStructsProvider = WrapperStruct.CreateProvider(context);
        context.RegisterImplementationSourceOutputForResult(wrapperStructsProvider!);

        var entityRefSyntaxTrees = entityRefProvider
            .Where(static x => x is Result.Ok)
            .Select(static (x, token) =>
            {
                var file = ((Result.Ok)x).File;
                return CSharpSyntaxTree.ParseText(file.Text, cancellationToken: token);
            })
            .Collect();

        var wrapperStructsSyntaxTrees = wrapperStructsProvider
            .Where(static x => x is Result.Ok)
            .Select(static (x, token) =>
            {
                var file = ((Result.Ok)x).File;
                return CSharpSyntaxTree.ParseText(file.Text, cancellationToken: token);
            })
            .Collect();

        var generatedSyntaxTreesProvider = entityRefSyntaxTrees.Combine(wrapperStructsSyntaxTrees)
            .Select(static (arg, _) =>
            {
                var (left, right) = arg;
                var builder = ImmutableArray.CreateBuilder<SyntaxTree>(left.Length + right.Length);
                builder.AddRange(left);
                builder.AddRange(right);
                return builder.MoveToImmutable();
            });

        var compilationWithGeneratedProvider = context.CompilationProvider
            .Combine(generatedSyntaxTreesProvider)
            .Select(static (arg, _) =>
            {
                var (compilation, syntaxTrees) = arg;
                var compilationWithGeneratedEntityRefs = compilation.AddSyntaxTrees(syntaxTrees);
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

        context.RegisterImplementationSourceOutputForResult(inliningProvider);
    }
}