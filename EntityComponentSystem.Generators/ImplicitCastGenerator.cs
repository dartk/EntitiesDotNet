using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;


namespace EntityComponentSystem.Generators;


[Generator]
public class
    ImplicitCastGenerator : IncrementalGeneratorBase<ImplicitCastGenerator.Info> {
    private const string TemplateFileName = "ImplicitCast.scriban";


    private const string ImplicitCast = nameof(ImplicitCast);
    private const string ImplicitCastAttribute = nameof(ImplicitCastAttribute);


    public record Info(
        string Namespace,
        string Name,
        string Declaration,
        string FieldName,
        string FieldType
    );


    protected override bool Choose(SyntaxNode node, CancellationToken token) {
        if (node is not AttributeSyntax attribute) {
            return false;
        }

        return attribute.Name.ExtractName()
            is ImplicitCastAttribute or ImplicitCast;
    }


    protected override Info? Select(
        GeneratorSyntaxContext context,
        CancellationToken token
    ) {
        using var log = File.CreateText(@"C:\Users\user\Desktop\tmp\out.txt");

        var semanticModel = context.SemanticModel;
        var attribute = (AttributeSyntax)context.Node;

        if (
            semanticModel.GetEnclosingSymbol(attribute.SpanStart)
            is not INamedTypeSymbol structSymbol
        ) {
            return null;
        }

        var fieldDeclaration = attribute.FirstAncestorOrSelf<FieldDeclarationSyntax>();
        if (fieldDeclaration == null) {
            return null;
        }

        var variableDeclaratorSyntax =(VariableDeclaratorSyntax)
            fieldDeclaration.DescendantNodes().First(x => x is VariableDeclaratorSyntax);

        var fieldSymbol = (IFieldSymbol)structSymbol.GetMembers(variableDeclaratorSyntax.ToFullString()).First();

        var structDeclarationSyntax =
            (StructDeclarationSyntax)structSymbol.DeclaringSyntaxReferences.First()
                .GetSyntax();
        var typeDeclaration =
            structDeclarationSyntax.Modifiers.ToFullString()
            + structDeclarationSyntax.Keyword + " " + structDeclarationSyntax.Identifier;

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
    ) {
        var templateContent = GetTemplate(TemplateFileName);
        var template = Template.Parse(templateContent, TemplateFileName);

        var sourceCode = template.Render(new { Types = items });
        context.AddSource("Source.g.cs", sourceCode);
    }
}