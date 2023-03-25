global using static EntitiesDotNet.Generators.GeneratorUtil;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;


namespace EntitiesDotNet.Generators;


public readonly record struct SyntaxTreeAndSemanticModel(SyntaxTree Tree, SemanticModel Model);


public static class GeneratorUtil
{
    public static IncrementalValuesProvider<SyntaxTreeAndSemanticModel>
        GetSyntaxAndModelWithUndefinedPreprocessorName(
            IncrementalGeneratorInitializationContext context,
            IncrementalValueProvider<Compilation> compilationProvider,
            string preprocessorName)
    {
        var syntaxTreeProvider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (node, token) =>
                node is CompilationUnitSyntax
                && node.HasLeadingTrivia
                && node.GetLeadingTrivia()
                        .FirstOrDefault(static x => x.Kind() == SyntaxKind.IfDirectiveTrivia)
                    is var ifDirective && ifDirective.ToString() == "#if !" + preprocessorName,
            transform: static (context, token) => context.Node.SyntaxTree);

        var inlineEnabledProvider = context.ParseOptionsProvider.Select((options, _) =>
            options.PreprocessorSymbolNames.FirstOrDefault(x => x == preprocessorName) != null);

        var undefPreprocessorName = $"#undef {preprocessorName}{Environment.NewLine}#define SOURCEGEN{Environment.NewLine}";

        return syntaxTreeProvider
            .Combine(compilationProvider)
            .Combine(inlineEnabledProvider)
            .Where(x => x.Right)
            .Select((item, token) =>
            {
                var (tree, compilation) = item.Left;

                var newText = tree.GetText().Replace(new TextSpan(0, 0), undefPreprocessorName);
                var newTree = tree.WithChangedText(newText);
                var newCompilation = compilation.ReplaceSyntaxTree(tree, newTree);
                var newSemanticModel = newCompilation.GetSemanticModel(newTree);

                return new SyntaxTreeAndSemanticModel(newTree, newSemanticModel);
            });
    }
}