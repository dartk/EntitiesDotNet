using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;


namespace EntitiesDotNet.Generators;


[Generator]
public class EntityRefStructGenerator : IncrementalGeneratorBase<EntityRefStructGenerator.Info>
{

    private const string TemplateFileName = "EntityRefStruct.scriban";


    private const string EntityRefStruct = nameof(EntityRefStruct);
    private const string EntityRefStructAttribute = nameof(EntityRefStructAttribute);


    public record Info(
        StructDeclarationSyntax StructDeclarationSyntax,
        ITypeSymbol TypeSymbol,
        ImmutableArray<ComponentInfo> ReadComponents,
        ImmutableArray<ComponentInfo> WriteComponents
    );


    public record ComponentInfo(
        string Name,
        string Type
    );


    protected override bool Where(SyntaxNode node, CancellationToken token)
    {
        if (node is not AttributeSyntax attribute)
        {
            return false;
        }

        return attribute.Name.ExtractName()
            is EntityRefStructAttribute or EntityRefStruct;
    }


    protected override Info? Select(
        GeneratorSyntaxContext context,
        CancellationToken token
    )
    {
        var semanticModel = context.SemanticModel;
        var attribute = (AttributeSyntax)context.Node;

        var classDeclaration = attribute.Parent?.Parent;
        if (
            classDeclaration is not StructDeclarationSyntax structDeclarationSyntax
            || semanticModel.GetDeclaredSymbol(structDeclarationSyntax)
                is not ITypeSymbol type
        )
        {
            return null;
        }


        var readMembers = new List<ComponentInfo>();
        var writeMembers = new List<ComponentInfo>();

        foreach (var member in type.GetMembers())
        {
            if (member is not IFieldSymbol fieldSymbol)
            {
                continue;
            }

            if (fieldSymbol.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            foreach (var @ref in fieldSymbol.DeclaringSyntaxReferences)
            {
                var node = @ref.GetSyntax();
                var parent = node.Parent;
                if (parent == null)
                {
                    continue;
                }

                var parentStr = parent.ToString();
                if (parentStr.Contains("ref readonly"))
                {
                    readMembers.Add(
                        new ComponentInfo(
                            member.Name,
                            fieldSymbol.Type.ToDisplayString()
                        ));
                }
                else if (parentStr.Contains("ref "))
                {
                    writeMembers.Add(
                        new ComponentInfo(
                            member.Name,
                            fieldSymbol.Type.ToDisplayString()
                        ));
                }
            }
        }

        if (readMembers.Any() || writeMembers.Any())
        {
            return new Info(
                structDeclarationSyntax,
                type,
                readMembers.ToImmutableArray(),
                writeMembers.ToImmutableArray());
        }
        else
        {
            return null;
        }
    }


    protected override void Produce(
        SourceProductionContext context,
        ImmutableArray<Info> items
    )
    {
        var templateContent = GetTemplate(TemplateFileName);
        var template = Template.Parse(templateContent, TemplateFileName);

        var sourceCode = template.Render(new
        {
            Types = items.Select(item => new
            {
                Namespace = item.TypeSymbol.ContainingNamespace.ToDisplayString(),
                Name = item.TypeSymbol.Name,
                Modifiers = item.StructDeclarationSyntax.Modifiers.ToFullString(),
                ReadComponents = item.ReadComponents,
                WriteComponents = item.WriteComponents,
                IsPublic = item.TypeSymbol.DeclaredAccessibility == Accessibility.Public
            })
        });
        context.AddSource("Source.g.cs", sourceCode);
    }
}