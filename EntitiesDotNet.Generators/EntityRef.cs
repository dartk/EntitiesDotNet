using System.Collections.Immutable;
using CSharp.SourceGen.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;


// ReSharper disable NotAccessedPositionalProperty.Local
// ReSharper disable UnusedParameter.Local


namespace EntitiesDotNet.Generators;


public static class EntityRef
{
    private const string AttributeName = "EntityRef";


    public static void AddAttributes(
        IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource($"{AttributeName}.g.cs", $$"""
            namespace EntitiesDotNet;


            [AttributeUsage(AttributeTargets.Struct)]
            internal class {{AttributeName}}Attribute : Attribute { }
            """);
    }


    public static IncrementalValuesProvider<FileNameWithText>
        CreateProvider(IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider
            .CreateSyntaxProvider(Predicate, Transform)
            .Where(static x => !x.IsEmpty);
    }


    public static bool Predicate(SyntaxNode node, CancellationToken _) =>
        node.IsAttribute(AttributeName);


    public static FileNameWithText Transform(GeneratorSyntaxContext context,
        CancellationToken token)
    {
        if (!context.TryGetAttributeAppliedType(
            out var typeDeclarationSyntax, out var typeSymbol))
        {
            return default;
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
            return default;
        }

        var declaration = QualifiedDeclarationInfo.FromSyntax(typeDeclarationSyntax);
        token.ThrowIfCancellationRequested();


        var info = new Info(
            typeSymbol.SuggestedFileName(),
            typeSymbol.Name,
            typeSymbol.ToString(),
            GetExtensionsClassAccessibility(typeSymbol),
            declaration.NamespaceOpen(),
            declaration.NamespaceClose(),
            declaration.TypeOpenNoNamespace(),
            declaration.TypeCloseNoNamespace(),
            readMembers,
            writeMembers);

        var text = Template.Render(info);

        return new FileNameWithText
        {
            FileName = info.FileName,
            Text = text
        };
    }


    private static string? GetExtensionsClassAccessibility(ITypeSymbol typeSymbol)
    {
        var accessibility = typeSymbol.DeclaredAccessibility;
        if (accessibility is Accessibility.Internal or Accessibility.Public)
        {
            for (var containingType = typeSymbol.ContainingType;
                containingType != null;
                containingType = containingType.ContainingType)
            {
                accessibility = containingType.DeclaredAccessibility switch
                {
                    Accessibility.Public => accessibility,
                    Accessibility.Internal => Accessibility.Internal,
                    _ => Accessibility.Private
                };

                if (accessibility == Accessibility.Private)
                {
                    break;
                }
            }
        }

        return accessibility switch
        {
            Accessibility.Internal => "internal",
            Accessibility.Public => "public",
            _ => null
        };
    }


    private record Info(
        string FileName,
        string Name,
        string FullName,
        string? ExtensionsAccessibility,
        string NamespaceOpen,
        string NamespaceClose,
        string TypeOpen,
        string TypeClose,
        IReadOnlyList<ComponentInfo> ReadComponents,
        IReadOnlyList<ComponentInfo> WriteComponents
    );


    private record ComponentInfo(
        string Name,
        string Type
    );


    private static Template ParseTemplate()
    {
        return Template.Parse(ManifestResource.ReadAllText(
            "EntitiesDotNet.Generators", "Scriban", "EntityRefStruct.scriban"));
    }


    private static readonly Template Template = ParseTemplate();
}