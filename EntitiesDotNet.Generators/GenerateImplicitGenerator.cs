using CSharp.SourceGen.Extensions;
using Microsoft.CodeAnalysis;


// ReSharper disable NotAccessedPositionalProperty.Local


namespace EntitiesDotNet.Generators;


[Generator]
public class GenerateImplicitGenerator : IIncrementalGenerator
{
    private const string GenerateImplicitOperators = nameof(GenerateImplicitOperators);


    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static context =>
            context.AddSource("GenerateImplicitOperatorsAttribute.g.cs", """
                namespace EntitiesDotNet {

                    [AttributeUsage(AttributeTargets.Struct)]
                    internal class GenerateImplicitOperatorsAttribute : Attribute
                    {
                    }
                }
                """));

        var provider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node.IsAttribute(GenerateImplicitOperators),
                transform: static (context, _) =>
                {
                    if (!context.TryGetAttributeAppliedType(out var structSyntax,
                        out var structSymbol))
                    {
                        return default;
                    }

                    var fieldSymbols = structSymbol
                        .GetMembers()
                        .Where(x => x is IFieldSymbol && !x.IsImplicitlyDeclared)
                        .Cast<IFieldSymbol>()
                        .ToList();

                    if (fieldSymbols.Count != 1)
                    {
                        return default;
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

                    var source = QualifiedDeclarationInfo.FromSyntax(structSyntax)
                        .ToString(operators);
                    return new FileNameWithText(structSymbol.SuggestedFileName(), source);
                })
            .Where(x => !x.IsEmpty);

        context.RegisterSourceOutput(provider,
            static (context, file) => { context.AddSource(file.FileName, file.Text); });
    }
}