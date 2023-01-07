using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace EntityComponentSystem.Generators;


public static class TypeDeclarationSyntaxUtil
{

    public static string? ToString(SyntaxNode node)
    {
        return node switch
        {
            StructDeclarationSyntax @struct => ToString(@struct),
            ClassDeclarationSyntax @class => ToString(@class),
            RecordDeclarationSyntax @record => ToString(@record),
            _ => null
        };
    }


    public static string ToString(StructDeclarationSyntax syntax) =>
        $"{string.Join(" ", syntax.Modifiers)} {syntax.Keyword} {syntax.Identifier}";


    public static string ToString(ClassDeclarationSyntax syntax) =>
        $"{string.Join(" ", syntax.Modifiers)} {syntax.Keyword} {syntax.Identifier}";


    public static string ToString(RecordDeclarationSyntax syntax) =>
        $"{string.Join(" ", syntax.Modifiers)} record {syntax.ClassOrStructKeyword} {syntax.Identifier}";

}


public static class NamespaceSyntaxUtil
{

    public static string? ToString(SyntaxNode node)
    {
        return node switch
        {
            StructDeclarationSyntax @struct => ToString(@struct),
            ClassDeclarationSyntax @class => ToString(@class),
            RecordDeclarationSyntax @record => ToString(@record),
            _ => null
        };
    }


    public static string ToString(NamespaceDeclarationSyntax syntax) => syntax.ToString();

    public static string ToString(FileScopedNamespaceDeclarationSyntax syntax) => syntax.ToString();


    public static string ToString(RecordDeclarationSyntax syntax) =>
        $"{string.Join(" ", syntax.Modifiers)} record {syntax.ClassOrStructKeyword} {syntax.Identifier}";

}