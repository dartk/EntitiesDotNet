using System.Collections.Immutable;
using CSharp.SourceGen.Extensions;
using Microsoft.CodeAnalysis;
using Scriban;


// ReSharper disable NotAccessedPositionalProperty.Local
// ReSharper disable UnusedParameter.Local


namespace EntitiesDotNet.Generators;


[Generator]
public class EntityRefStructGenerator : IIncrementalGenerator
{
    private const string EntityRefStruct = nameof(EntityRefStruct);


    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, token) => node.IsAttribute(EntityRefStruct),
            transform: static (context, token) =>
            {
                if (!context.TryGetAttributeAppliedType(
                    out var typeDeclarationSyntax, out var typeSymbol))
                {
                    return null;
                }


                var readMembers = new List<ComponentInfo>();
                var writeMembers = new List<ComponentInfo>();

                foreach (var member in typeSymbol.GetMembers())
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

                if (!readMembers.Any() && !writeMembers.Any())
                {
                    return null;
                }

                var declaration = QualifiedDeclarationInfo.FromSyntax(typeDeclarationSyntax);

                return new Info(
                    typeSymbol.SuggestedFileName(),
                    typeSymbol.Name,
                    typeSymbol.DeclaredAccessibility == Accessibility.Public,
                    declaration.TypeOpen(),
                    declaration.TypeClose(),
                    readMembers.ToImmutableArray(),
                    writeMembers.ToImmutableArray());
            }).Collect();

        context.RegisterSourceOutput(provider, static (context, args) =>
        {
            var template = Template.Parse(ManifestResource.ReadAllText(
                "EntitiesDotNet.Generators",
                "Scriban",
                "EntityRefStruct.scriban"));

            foreach (var item in args)
            {
                if (item == null) continue;
                context.AddSource(item.FileName, template.Render(item));
            }
        });
    }


    private record Info(
        string FileName,
        string Name,
        bool IsPublic,
        string TypeOpen,
        string TypeClose,
        ImmutableArray<ComponentInfo> ReadComponents,
        ImmutableArray<ComponentInfo> WriteComponents
    );


    private record ComponentInfo(
        string Name,
        string Type
    );
}