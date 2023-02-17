using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;


namespace EntitiesDotNet.Generators;


[Generator]
public class OptimizedIterationGenerator :
    IncrementalGeneratorBase<OptimizedIterationGenerator.Info>
{

    private const string GenerateOptimized = nameof(GenerateOptimized);
    private const string GenerateOptimizedAttribute = nameof(GenerateOptimizedAttribute);


    public record Info(string FileName, string Source);


    protected override bool Where(SyntaxNode node, CancellationToken token)
    {
        if (node is not AttributeSyntax attributeSyntax)
        {
            return false;
        }

        return attributeSyntax.Name.ExtractName()
            is GenerateOptimized or GenerateOptimizedAttribute;
    }


    protected override Info? Select(
        GeneratorSyntaxContext context,
        CancellationToken token
    )
    {
        var attribute = (AttributeSyntax)context.Node;
        var methodSyntax = (MethodDeclarationSyntax?)attribute.Parent?.Parent;
        if (methodSyntax == null)
        {
            throw new UnreachableException(
                $"{nameof(MethodDeclarationSyntax)} is not found.");
        }

        var source =
            GenerateSourceFile(methodSyntax, () => GenerateMethod(methodSyntax));

        return new Info($"{methodSyntax.Identifier}.g.cs", source);
    }


    protected override void Produce(
        SourceProductionContext context,
        ImmutableArray<Info> items
    )
    {
        foreach (var item in items)
        {
            context.AddSource(item.FileName, item.Source);
        }
    }


    private static string GenerateSourceFile(SyntaxNode node, Func<string> getContent)
    {
        var source = new StringBuilder();

        source.AppendLine(getContent());

        var ancestors = node.AncestorsAndSelf();
        foreach (var ancestor in ancestors)
        {
            if (TypeDeclarationSyntaxUtil.ToString(ancestor) is { } str)
            {
                source.Insert(0,
                    Environment.NewLine
                    + str + " {"
                    + Environment.NewLine);

                source.AppendLine("}" + Environment.NewLine);
            }

            var usingList = ancestor.ChildNodes()
                .Where(x => x is UsingDirectiveSyntax or UsingStatementSyntax);

            foreach (var item in usingList)
            {
                source.Insert(0, item + Environment.NewLine);
            }

            {
                if (ancestor is NamespaceDeclarationSyntax @namespace)
                {
                    source.Insert(0,
                        Environment.NewLine
                        + $"namespace {@namespace.Name} {{"
                        + Environment.NewLine);

                    source.AppendLine("}" + Environment.NewLine);
                }
            }

            {
                if (ancestor is FileScopedNamespaceDeclarationSyntax @namespace)
                {
                    source.Insert(0,
                        Environment.NewLine
                        + $"namespace {@namespace.Name};"
                        + Environment.NewLine);
                }
            }
        }

        return source.ToString();
    }


    private static (IdentifierNameSyntax Target, IdentifierNameSyntax Method)
        GetInvocationNames(InvocationExpressionSyntax node)
    {
        var memberAccess = node.ChildNodes().OfType<MemberAccessExpressionSyntax>()
            .First();

        var childNodes = memberAccess.ChildNodes()
            .OfType<IdentifierNameSyntax>()
            .ToList();

        if (childNodes.Count != 2)
        {
            throw new UnreachableException(
                $"Unexpected number of child nodes ({childNodes.Count}) for {nameof(InvocationExpressionSyntax)}");
        }

        return (childNodes[0], childNodes[1]);
    }


    private static string GenerateMethod(MethodDeclarationSyntax methodSyntax)
    {
        var source = new StringBuilder();

        var parameterList = (ParameterListSyntax)methodSyntax.ChildNodes()
            .First(x => x.Kind() == SyntaxKind.ParameterList);

        var parameter = parameterList.Parameters.First();
        var parameterIdentifier = parameter.Identifier;
        var componentArrayName = parameterIdentifier.ToString();

        var block = methodSyntax.ChildNodes().OfType<BlockSyntax>().First();

        var invocations = ChooseForEachInvocations(block, componentArrayName).ToList();

        var blockText = block.GetText();
        var lastPosition = 0;
        foreach (var invocation in invocations)
        {
            var span = TextSpan.FromBounds(
                lastPosition, invocation.SpanStart - block.SpanStart);
            var text = blockText.ToString(span);
            source.AppendLine(text);

            source.AppendLine("#region ForEach");
            source.AppendLine();

            source.AppendLine(GenerateComponentArrayForEach(invocation));

            source.AppendLine();
            source.AppendLine("#endregion");

            lastPosition = invocation.Span.End + 1 - block.SpanStart;
        }

        source.AppendLine(blockText.ToString(TextSpan.FromBounds(
            lastPosition, block.Span.Length)));

        source.Insert(0, $"""
[EntitiesDotNet.GeneratedFrom(nameof({methodSyntax.Identifier}))]
{string.Join(" ", methodSyntax.Modifiers)} {methodSyntax.ReturnType} {methodSyntax.Identifier}_Optimized{parameterList}
""");

        return source.ToString();
    }


    private static IEnumerable<InvocationExpressionSyntax> ChooseForEachInvocations(
        SyntaxNode node,
        string componentArrayName
    )
    {
        var lambdas = node.DescendantNodesAndSelf()
            .OfType<ParenthesizedLambdaExpressionSyntax>();

        foreach (var lambda in lambdas)
        {
            if (lambda?.Parent?.Parent?.Parent is not InvocationExpressionSyntax
                invocation
            )
            {
                continue;
            }

            var (target, method) = GetInvocationNames(invocation);
            if (target.ToString() == componentArrayName &&
                method.ToString() == "ForEach")
            {
                yield return invocation;
            }
        }
    }


    private static string GenerateComponentArrayForEach(
        InvocationExpressionSyntax foreachInvocation
    )
    {
        var lambda = foreachInvocation
            .DescendantNodes()
            .OfType<ParenthesizedLambdaExpressionSyntax>()
            .First();

        var (arrayNameSyntax, _) = GetInvocationNames(foreachInvocation);
        var arrayName = arrayNameSyntax.ToString();

        var readComponents = new List<ParameterSyntax>();
        var writeComponents = new List<ParameterSyntax>();

        var lambdaParameters = lambda.ParameterList.Parameters;
        foreach (var componentParameter in lambdaParameters)
        {
            foreach (var modifier in componentParameter.Modifiers)
            {
                switch (modifier.Kind())
                {
                    case SyntaxKind.InKeyword:
                        readComponents.Add(componentParameter);
                        break;
                    case SyntaxKind.RefKeyword:
                        writeComponents.Add(componentParameter);
                        break;
                }
            }
        }

        var indexName = (string?)null;
        var indexParameter = lambdaParameters.First();
        if (indexParameter.Type!.ToString() == "int" && !indexParameter.Modifiers.Any())
        {
            indexName = indexParameter.Identifier.ToString();
        }

        // "T0, T1, ..."
        var componentsStr = string.Join(", ",
            readComponents.Concat(writeComponents).Select(x => x.Type!.ToString()));

        return $$"""
    if ({{arrayName}}.Archetype.Contains<{{componentsStr}}>()) {
        var _count = {{arrayName}}.Count;
        {{string.Join(" ", readComponents.Select(
                    x => $"ref var {x.Identifier} = ref System.Runtime.InteropServices.MemoryMarshal.GetReference({arrayName}.GetReadOnlySpan<{x.Type}>());")
                .Concat(writeComponents.Select(
                    x => $"ref var {x.Identifier} = ref System.Runtime.InteropServices.MemoryMarshal.GetReference({arrayName}.GetSpan<{x.Type}>());")))}}

        for (var _i = 0; _i < _count; ++_i) {
            {{(indexName != null ? $"var {indexName} = _i;" : "")}}

            #region Body

            {{(
                lambda.ExpressionBody != null
                    ? lambda.ExpressionBody + ";"
                    : lambda.Block
            )}}

            #endregion

            {{string.Join(" ", readComponents.Select(
                        x => $"{x.Identifier} = ref System.Runtime.CompilerServices.Unsafe.Add(ref {x.Identifier}, 1);")
                    .Concat(writeComponents.Select(
                        x => $"{x.Identifier} = ref System.Runtime.CompilerServices.Unsafe.Add(ref {x.Identifier}, 1);")))}}
        }
    }
""";
    }
}