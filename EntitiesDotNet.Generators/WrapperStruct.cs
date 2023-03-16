using CSharp.SourceGen.Extensions;
using Microsoft.CodeAnalysis;


// ReSharper disable NotAccessedPositionalProperty.Local


namespace EntitiesDotNet.Generators;


internal static class WrapperStruct
{
    public static void AddAttributes(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource("WrapperStructAttribute.g.cs", """
            namespace EntitiesDotNet
            {
                [AttributeUsage(AttributeTargets.Struct)]
                internal class WrapperStructAttribute : Attribute {}
            }
            """);
    }


    public static IncrementalValuesProvider<Result>
        CreateProvider(IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider
            .CreateSyntaxProvider(Predicate, Transform)
            .Where(static x => x != null)!;
    }


    public static bool Predicate(SyntaxNode node, CancellationToken _) =>
        node.IsAttribute(nameof(WrapperStruct));


    public static Result? Transform(GeneratorSyntaxContext context,
        CancellationToken token)
    {
        if (!context.TryGetAttributeAppliedType(out var structSyntax, out var structSymbol))
        {
            return default;
        }

        var fieldSymbols = structSymbol
            .GetMembers()
            .Where(x => x is IFieldSymbol && !x.IsImplicitlyDeclared)
            .Cast<IFieldSymbol>()
            .ToList();
        
        token.ThrowIfCancellationRequested();

        if (fieldSymbols.Count != 1)
        {
            return new Result.Error(new Exception(
                $"'{nameof(WrapperStruct)}' attribute can only be applied to a struct with a single field."));
        }

        var structType = structSymbol.Name;

        var fieldSymbol = fieldSymbols.First();
        var fieldName = fieldSymbol.Name;
        var fieldType = fieldSymbol.Type.ToDisplayString();

        var operators = $$"""
            public static implicit operator {{fieldType}}({{structType}} value) =>
                value.{{fieldName}};
            public static implicit operator {{structType}}({{fieldType}} value) {
                var result = default({{structType}});
                result.{{fieldName}} = value;
                return result;
            }
            """;

        token.ThrowIfCancellationRequested();

        var source = QualifiedDeclarationInfo.FromSyntax(structSyntax).ToString(operators);
        return new Result.Ok(new FileNameWithText(structSymbol.SuggestedFileName(), source));
    }
}