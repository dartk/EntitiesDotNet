using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace CSharp.SourceGen.Extensions;


internal static class SyntaxExtensions
{
    public static string? ToUnqualifiedString(this NameSyntax? name)
    {
        while (name != null)
        {
            switch (name)
            {
                case IdentifierNameSyntax ins:
                    return ins.Identifier.Text;

                case QualifiedNameSyntax qns:
                    name = qns.Right;
                    break;

                default:
                    return null;
            }
        }

        return null;
    }


    public static string UnqualifiedName(this AttributeSyntax attribute)
    {
        var name = attribute.Name;
        while (true)
        {
            switch (name)
            {
                case IdentifierNameSyntax ins:
                    return ins.Identifier.Text;

                case QualifiedNameSyntax qns:
                    name = qns.Right;
                    break;
            }
        }
    }


    public static bool IsAttribute(this SyntaxNode node, string attributeUnqualifiedName)
    {
        if (node is not AttributeSyntax attribute)
        {
            return false;
        }

        var actualName = attribute.UnqualifiedName();

        return actualName == attributeUnqualifiedName
            || actualName == (attributeUnqualifiedName + "Attribute");
    }


    public static bool TryGetAttributeAppliedType(this AttributeSyntax attributeSyntax,
        out TypeDeclarationSyntax typeDeclarationSyntax)
    {
        var syntax = attributeSyntax.Parent!.Parent as TypeDeclarationSyntax;
        typeDeclarationSyntax = syntax!;
        return syntax != null;
    }


    public static bool TryGetAttributeAppliedType(this GeneratorSyntaxContext context,
        AttributeSyntax attributeSyntax, out TypeDeclarationSyntax typeSyntax,
        out INamedTypeSymbol typeSymbol)
    {
        if (!attributeSyntax.TryGetAttributeAppliedType(out typeSyntax))
        {
            typeSymbol = null!;
            return false;
        }

        var symbol = (INamedTypeSymbol?)context.SemanticModel.GetDeclaredSymbol(typeSyntax);
        typeSymbol = symbol!;

        return symbol != null;
    }


    public static bool TryGetAttributeAppliedType(this GeneratorSyntaxContext context,
        out TypeDeclarationSyntax typeSyntax, out INamedTypeSymbol typeSymbol)
    {
        if (context.Node is AttributeSyntax attributeSyntax)
        {
            return context.TryGetAttributeAppliedType(attributeSyntax, out typeSyntax,
                out typeSymbol);
        }

        typeSyntax = null!;
        typeSymbol = null!;
        return false;
    }
}