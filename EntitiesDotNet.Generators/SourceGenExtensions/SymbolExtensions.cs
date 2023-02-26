using Microsoft.CodeAnalysis;


namespace CSharp.SourceGen.Extensions;


internal static class SymbolExtensions
{
    public static string SuggestedFileName(this ITypeSymbol symbol, string suffix = "")
    {
        return symbol.ToDisplayString()
            .Replace('<', '[')
            .Replace('>', ']') + suffix + ".g.cs";
    }
    
    
    public static string SuggestedFileName(this IMethodSymbol symbol, string suffix = "")
    {
        return symbol.ToDisplayString()
            .Replace('<', '[')
            .Replace('>', ']') + suffix + ".g.cs";
    }
}