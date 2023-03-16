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
        var fieldType = fieldSymbol.Type;
        var fieldTypeName = fieldType.ToDisplayString();

        var toStringInvocation =
            fieldType.IsValueType
            && fieldType.NullableAnnotation != NullableAnnotation.Annotated
                ? fieldName + ".ToString()"
                : fieldName + "?.ToString()";

        var operators = $$"""
            public static implicit operator {{fieldTypeName}}({{structType}} value) =>
                value.{{fieldName}};
            public static implicit operator {{structType}}({{fieldTypeName}} value) {
                var result = default({{structType}});
                result.{{fieldName}} = value;
                return result;
            }
            public override string ToString() => this.{{toStringInvocation}};
            """;

        token.ThrowIfCancellationRequested();

        var source = QualifiedDeclarationInfo.FromSyntax(structSyntax).ToString(operators);
        return new Result.Ok(new FileNameWithText(structSymbol.SuggestedFileName(), source));
    }
}