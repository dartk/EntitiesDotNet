using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace CSharp.SourceGen.Extensions;


internal static class TypeDeclarationSyntaxUtil
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
        $"{string.Join(" ", syntax.Modifiers)} {syntax.Keyword} {syntax.Identifier}{syntax.TypeParameterList}";


    public static string ToString(ClassDeclarationSyntax syntax) =>
        $"{string.Join(" ", syntax.Modifiers)} {syntax.Keyword} {syntax.Identifier}{syntax.TypeParameterList}";


    public static string ToString(RecordDeclarationSyntax syntax) =>
        $"{string.Join(" ", syntax.Modifiers)} record {syntax.ClassOrStructKeyword} {syntax.Identifier}{syntax.TypeParameterList}";
}