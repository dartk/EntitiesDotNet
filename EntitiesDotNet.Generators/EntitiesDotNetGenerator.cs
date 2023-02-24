﻿using Microsoft.CodeAnalysis;
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
        context.RegisterSourceOutput(entityRefProvider, FileNameWithText.AddSource);

        var compilationWithGeneratedProvider = context.CompilationProvider
            .Combine(entityRefProvider.Collect())
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
                var (node, compilationWithGenerated) = arg;
                var semanticModel = compilationWithGenerated.GetSemanticModel(node.SyntaxTree);
                return Inlining.Transform(node, semanticModel, token);
            })
            .Where(x => !x.IsEmpty)
            .Select(static (x, token) => x.FormatText(token));

        context.RegisterSourceOutput(inliningProvider, FileNameWithText.AddSource);
    }
}