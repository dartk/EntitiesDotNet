using System.Collections.Immutable;
using System.Text;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;


namespace EntityComponentSystem.Generators;


[Generator]
public class ComponentSystemGenerator :
    IncrementalGeneratorBase<ComponentSystemGenerator.Info>
{

    private const string GenerateOnExecute = nameof(GenerateOnExecute);


    private const string GenerateOnExecuteAttribute =
        nameof(EntityComponentSystem.GenerateOnExecute);


    public record Info(string FileName, string Source);


    protected override bool Where(SyntaxNode node, CancellationToken token)
    {
        if (node is not AttributeSyntax attributeSyntax)
        {
            return false;
        }

        return attributeSyntax.Name.ExtractName()
            is GenerateOnExecute or GenerateOnExecuteAttribute;
    }


    protected override Info? Select(
        GeneratorSyntaxContext context,
        CancellationToken token
    )
    {
        var attribute = (AttributeSyntax)context.Node;

        var methodDeclarationSyntax = (MethodDeclarationSyntax?)attribute.Parent?.Parent;
        if (methodDeclarationSyntax == null)
        {
            throw new UnreachableException(
                $"{nameof(MethodDeclarationSyntax)} is not found.");
        }

        var classDeclarationSyntax = methodDeclarationSyntax.Ancestors()
            .OfType<RecordDeclarationSyntax>().First();

        var source = GenerateSourceFile(classDeclarationSyntax, () =>
            GenerateComponentSystemClass(
                classDeclarationSyntax,
                methodDeclarationSyntax));

        return new Info(
            $"{classDeclarationSyntax.Identifier}.{methodDeclarationSyntax.Identifier}_{this._sourceFileCounter++}.g.cs",
            source);
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

        var ancestors = node.Ancestors();
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


    private static IdentifierNameSyntax GetInvokedMethodName(
        InvocationExpressionSyntax node)
    {
        var memberAccess = node.ChildNodes().OfType<MemberAccessExpressionSyntax>()
            .First();

        return memberAccess.ChildNodes()
            .OfType<IdentifierNameSyntax>()
            .Last();
    }


    private static string GenerateComponentSystemClass(
        RecordDeclarationSyntax classDeclarationSyntax,
        MethodDeclarationSyntax methodSyntax
    )
    {
        var source = new StringBuilder();

        source.AppendLine(
            $"{TypeDeclarationSyntaxUtil.ToString(classDeclarationSyntax)} : {nameof(IComponentSystem_Generated)}");
        source.AppendLine("{");


        var methodBlock = methodSyntax.ChildNodes().OfType<BlockSyntax>().First();

        var forEachInvocations = ChooseForEachInvocations(methodBlock).ToList();
        source.AppendLine();
        for (var i = 0; i < forEachInvocations.Count; ++i)
        {
            source.AppendLine($"private {nameof(EntityQueryCache)} __cache{i};");
        }

        var filters = new List<WhereLambdaInfo>[forEachInvocations.Count];
        var foreachInfos = new ForEachInfo[forEachInvocations.Count];

        source.AppendLine();
        source.AppendLine("void IComponentSystem_Generated.OnInit() {");
        for (var i = 0; i < forEachInvocations.Count; ++i)
        {
            var invocation = forEachInvocations[i];
            var filterList = WhereLambdaInfo.FromExpression(invocation.Expression);
            var foreachInfo = ForEachInfo.FromSyntax(invocation);

            filters[i] = filterList;
            foreachInfos[i] = foreachInfo;

            source.AppendLine(
                GenerateCacheInitialization(foreachInfo, filterList, CacheName(i)));
        }

        source.AppendLine("}");

        source.AppendLine();
        source.AppendLine(@"void IComponentSystem_Generated.OnExecute()");

        var sourceText = methodBlock.SyntaxTree.GetText();
        
        var lastPosition = methodBlock.SpanStart;
        for (var i = 0; i < forEachInvocations.Count; ++i)
        {
            var invocation = forEachInvocations[i];

            var statement = invocation.Ancestors()
                .OfType<ExpressionStatementSyntax>().First();

            var sourceTextBeforeForEach =
                sourceText.ToString(TextSpan.FromBounds(lastPosition, statement.SpanStart));
            
            source.AppendLine(sourceTextBeforeForEach);
            source.AppendLine(GenerateOptimizedForEach(foreachInfos[i], filters[i], CacheName(i)));

            lastPosition = statement.Span.End;
        }

        source.AppendLine(
            sourceText.ToString(TextSpan.FromBounds(lastPosition, methodBlock.Span.End)));
        
        source.AppendLine("}");

        return source.ToString();
    }


    private static string CacheName(int i) => "__cache" + i;


    private static string GenerateCacheInitialization(
        ForEachInfo info, List<WhereLambdaInfo> filters, string cacheName
    )
    {
        // "T0, T1, ..."
        var componentTypesStr =
            string.Join(", ", info.Components.Select(x => x.Type));

        var predicateList = new List<string> {
            $"array => array.Archetype.Contains<{componentTypesStr}>()"
        };

        foreach (var filter in filters)
        {
            predicateList.Add(filter.Lambda);
        }

        return $$"""
this.{{cacheName}} = new {{nameof(EntityQueryCache)}}(
    this.Entities,
    {{string.Join("," + Environment.NewLine, predicateList)}}
);
""";
    }


    private static IEnumerable<InvocationExpressionSyntax> ChooseForEachInvocations(
        SyntaxNode node
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

            var method = GetInvokedMethodName(invocation);
            if (method.ToString() == "ForEach")
            {
                yield return invocation;
            }
        }
    }


    private static string GenerateOptimizedForEach(
        ForEachInfo foreachInfo, List<WhereLambdaInfo> whereLambdas, string cacheName
    )
    {
        // "T0, T1, ..."
        var componentTypesStr =
            string.Join(", ", foreachInfo.Components.Select(x => x.Type));

        var predicateList = new List<string> {
            $"array => array.Archetype.Contains<{componentTypesStr}>()"
        };

        foreach (var whereLambda in whereLambdas)
        {
            predicateList.Add(whereLambda.Lambda);
        }

        return $$"""
        this.{{cacheName}}.Update();

        foreach (var __array in this.{{cacheName}}) {
            var __count = __array.Count;
            {{string.Join(Environment.NewLine, foreachInfo.Components.Select(x =>
                $"ref var {x.Identifier} = ref System.Runtime.InteropServices.MemoryMarshal.GetReference(__array.Get{(x.IsReadOnly ? "ReadOnly" : "")}Span<{x.Type}>());"))}}

            for (var __i = 0; __i < __count; ++__i) {
                {{(foreachInfo.IndexArgName != null ? $"var {foreachInfo.IndexArgName} = __i;" : "")}}

                {{foreachInfo.Body}}

                {{string.Join(Environment.NewLine, foreachInfo.Components.Select(
                    x => $"{x.Identifier} = ref System.Runtime.CompilerServices.Unsafe.Add(ref {x.Identifier}, 1);"))}}
            }
        }
        """;
    }


    private record ForEachInfo(
        string? IndexArgName,
        IReadOnlyList<ForEachArgInfo> Components,
        string Body,
        int LineStart
    )
    {

        public IEnumerable<ForEachArgInfo> ReadComponents =>
            this.Components.Where(x => x.IsReadOnly);


        public IEnumerable<ForEachArgInfo> WriteComponents =>
            this.Components.Where(x => !x.IsReadOnly);


        public static ForEachInfo FromSyntax(
            InvocationExpressionSyntax foreachInvocation
        )
        {
            var lambda = foreachInvocation
                .DescendantNodes()
                .OfType<ParenthesizedLambdaExpressionSyntax>()
                .First();

            string lambdaBodyStr;
            int lineStart;
            var tree = lambda.SyntaxTree;
            if (lambda.ExpressionBody != null)
            {
                lambdaBodyStr = lambda.ExpressionBody + ";";
                var lineSpan = tree.GetMappedLineSpan(lambda.ExpressionBody.Span);
                lineStart = lineSpan.StartLinePosition.Line;
            }
            else
            {
                if (
                    lambda.Block!
                    .DescendantNodes()
                    .OfType<ReturnStatementSyntax>()
                    .Any()
                )
                {
                    throw new ArgumentException(
                        $"Return statement is not supported inside ForEach.");
                }

                lambdaBodyStr = lambda.Block!.ToString();
                var lineSpan = tree.GetMappedLineSpan(lambda.Block.Span);
                lineStart = lineSpan.StartLinePosition.Line;
            }

            var lambdaParameters = lambda.ParameterList.Parameters;

            var indexName = (string?)null;
            var indexParameter = lambdaParameters.First();
            if (indexParameter.Type!.ToString() == "int" &&
                !indexParameter.Modifiers.Any())
            {
                indexName = indexParameter.Identifier.ToString();
            }

            var components = new List<ForEachArgInfo>(lambdaParameters.Count);

            foreach (var parameter in lambdaParameters)
            {
                foreach (var modifier in parameter.Modifiers)
                {
                    switch (modifier.Kind())
                    {
                        case SyntaxKind.InKeyword:
                            components.Add(new ForEachArgInfo(
                                parameter.Type!.ToString(),
                                parameter.Identifier.ToString(),
                                true));
                            break;
                        case SyntaxKind.RefKeyword:
                            components.Add(new ForEachArgInfo(
                                parameter.Type!.ToString(),
                                parameter.Identifier.ToString(),
                                false));
                            break;
                    }
                }
            }

            return new ForEachInfo(indexName, components, lambdaBodyStr, lineStart);
        }
    }


    private record ForEachArgInfo(string Type, string Identifier, bool IsReadOnly);


    private record WhereLambdaInfo(string ParameterName, string Body, string Lambda)
    {

        /// <summary>
        /// Extracts info from IEnumerable.Where(x => ...) invocation.
        /// </summary>
        /// <returns>
        /// null if cannot extract the info.
        /// </returns>
        public static WhereLambdaInfo FromInvocationSyntax(
            InvocationExpressionSyntax invocation
        )
        {
            var childNodes = new List<SyntaxNode>(2);
            childNodes.AddRange(invocation.ChildNodes());
            if (childNodes.Count != 2)
            {
                throw new ArgumentException(
                    $"Unexpected child node count: {childNodes.Count}.");
            }

            var memberName =
                ((MemberAccessExpressionSyntax)childNodes[0]).Name.ToString();
            if (memberName != "Where")
            {
                throw new ArgumentException(
                    $"Unexpected member name: '{memberName}'.");
            }

            var arg = ((ArgumentListSyntax)childNodes[1])
                .ChildNodes()
                .First()
                .ChildNodes()
                .First();

            if (arg is not LambdaExpressionSyntax lambda)
            {
                throw new ArgumentException(
                    $"Unexpected argument type: {arg.GetType()}");
            }

            var bodyStr =
                lambda.Block?.ToString()
                ?? lambda.ExpressionBody!.ToString().Trim('{', '}');

            var parameter = lambda.ChildNodes().OfType<ParameterSyntax>().First();
            var parameterStr = parameter.ToString();

            return new WhereLambdaInfo(parameterStr, bodyStr, lambda.ToString());
        }


        /// <summary>
        /// Extracts info from IEnumerable.Where(x => ...).Where(x => ...) invocation.
        /// </summary>
        public static List<WhereLambdaInfo> FromExpression(ExpressionSyntax expression)
        {
            var infoList = new List<WhereLambdaInfo>();

            var invocation = expression.ChildNodes().First()
                as InvocationExpressionSyntax;

            while (invocation != null)
            {
                if (FromInvocationSyntax(invocation) is { } info)
                {
                    infoList.Add(info);
                }

                invocation = invocation.Expression.ChildNodes().First()
                    as InvocationExpressionSyntax;
            }

            infoList.Reverse();
            return infoList;
        }
    }


    public record NodeInfo(string Kind, string Text, List<NodeInfo> Children)
    {
        public NodeInfo() : this("", "", new List<NodeInfo>())
        {
        }


        public static NodeInfo FromSyntaxNode(SyntaxNode node)
        {
            var info = new NodeInfo(
                node.Kind().ToString(),
                node.ToString(),
                new List<NodeInfo>()
            );

            foreach (var child in node.ChildNodes())
            {
                info.Children.Add(FromSyntaxNode(child));
            }

            return info;
        }


        public string ToXml()
        {
            using var stringWriter = new StringWriter();
            var serializer = new XmlSerializer(typeof(NodeInfo));
            serializer.Serialize(stringWriter, this);
            return stringWriter.ToString();
        }


        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }


        public string ToYaml()
        {
            var serializer = new YamlDotNet.Serialization.SerializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions
                    .CamelCaseNamingConvention.Instance)
                .Build();
            return serializer.Serialize(this);
        }

    }


    private int _sourceFileCounter;
}