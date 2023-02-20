using CSharp.SourceGen.Extensions;
using Microsoft.CodeAnalysis;


// ReSharper disable NotAccessedPositionalProperty.Local


namespace EntitiesDotNet.Generators;


[Generator]
public class ImplicitCastGenerator : IIncrementalGenerator
{
    private const string GenerateImplicitOperators = nameof(GenerateImplicitOperators);


    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => node.IsAttribute(GenerateImplicitOperators),
            transform: static (context, _) =>
            {
                if (!context.TryGetAttributeAppliedType(out var structSyntax, out var structSymbol))
                {
                    return null;
                }

                var fieldSymbols = structSymbol
                    .GetMembers()
                    .Where(x => x is IFieldSymbol && !x.IsImplicitlyDeclared)
                    .Cast<IFieldSymbol>()
                    .ToList();

                if (fieldSymbols.Count != 1)
                {
                    return null;
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

                var source = QualifiedDeclarationInfo.FromSyntax(structSyntax).ToString(operators);
                return new Info(structSymbol.SuggestedFileName(), source);
            }).Collect();

        context.RegisterSourceOutput(provider, static (context, items) =>
        {
            foreach (var item in items)
            {
                if (item == null) continue;
                context.AddSource(item.FileName, item.Source);
            }
        });
    }


    private record Info(
        string FileName,
        string Source
    );


    private const string ScribanTemplate = """

{{ type.declaration }} {
    public static implicit operator {{ type.field_type }}({{ type.name }} value) => value.{{ type.field_name }};
    public static implicit operator {{ type.name }}({{ type.field_type }} value) {
        var result = default({{ type.name }});
        result.{{ type.field_name }} = value;
        return result;
    }
}
""";
}