using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;


namespace EntityComponentSystem.Generators;


[Generator]
public class
    ImplicitCastGenerator : IncrementalGeneratorBase<ImplicitCastGenerator.Info>
{
    private const string TemplateFileName = "ImplicitCast.scriban";


    private const string GenerateImplicitOperators = nameof(GenerateImplicitOperators);
    private const string GenerateImplicitOperatorsAttribute = nameof(GenerateImplicitOperatorsAttribute);


    public record Info(
        string Namespace,
        string Name,
        string Declaration,
        string FieldName,
        string FieldType
    );


    protected override bool Choose(SyntaxNode node, CancellationToken token)
    {
        if (node is not AttributeSyntax attribute)
        {
            return false;
        }

        return attribute.Name.ExtractName()
            is GenerateImplicitOperatorsAttribute or GenerateImplicitOperators;
    }


    protected override Info? Select(
        GeneratorSyntaxContext context,
        CancellationToken token
    )
    {
        var semanticModel = context.SemanticModel;
        var attribute = (AttributeSyntax)context.Node;
        var structSyntax = attribute.Parent?.Parent;

        if (
            structSyntax == null
            || semanticModel.GetDeclaredSymbol(structSyntax)
                is not INamedTypeSymbol structSymbol
        )
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

        var fieldSymbol = fieldSymbols.First();

        var structDeclarationSyntax =
            (Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax)structSymbol.DeclaringSyntaxReferences.First()
                .GetSyntax();
        var typeDeclaration =
            structDeclarationSyntax.Modifiers.ToFullString()
            + (structDeclarationSyntax is RecordDeclarationSyntax ? "record struct " : "struct ")
            + structDeclarationSyntax.Identifier;

        return new Info(
            structSymbol.ContainingNamespace.ToDisplayString(),
            structSymbol.Name,
            typeDeclaration,
            fieldSymbol.Name,
            fieldSymbol.Type.ToDisplayString()
        );
    }


    protected override void Produce(
        SourceProductionContext context,
        ImmutableArray<Info> items
    )
    {
        var templateContent = GetTemplate(TemplateFileName);
        var template = Template.Parse(templateContent, TemplateFileName);

        var sourceCode = template.Render(new { Types = items });
        context.AddSource("Source.g.cs", sourceCode);
    }
}