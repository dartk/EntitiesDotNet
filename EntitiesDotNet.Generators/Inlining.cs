using System.Collections.Immutable;
using System.Text;
using CSharp.SourceGen.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;


namespace EntitiesDotNet.Generators;


internal static class Inlining
{
    private const string INLINE = "INLINE";


    private const string Inline = nameof(Inline);
    private const string SupportsInliningAttribute = nameof(SupportsInliningAttribute);


    public static IncrementalValuesProvider<Result> GetGeneratedFilesProvider(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<Compilation> compilationProvider)
    {
        var treeAndModelProvider = GetSyntaxAndModelWithUndefinedPreprocessorName(
            context, compilationProvider, INLINE);

        return treeAndModelProvider.Select<SyntaxTreeAndSemanticModel, Result>(
            static (arg, token) =>
            {
                try
                {
                    var (tree, model) = arg;

                    var originalSource = tree.GetText();
                    var generatedSource = originalSource;

                    var inlinedMethods = GetMethodsWithInlinedAttribute(tree.GetRoot());
                    var writer = new StringWriter();

                    foreach (var method in inlinedMethods)
                    {
                        if (method.Body == null) continue;

                        writer.GetStringBuilder().Clear();
                        WriteInlinedMethodBlock(writer, method, model, token);
                        var newBody = writer.ToString();

                        generatedSource = generatedSource.Replace(method.Body.Span, newBody);
                    }

                    var originalFileName = tree.FilePath;
                    var generatedFileName =
                        Path.GetFileNameWithoutExtension(originalFileName) + ".inlined.cs";

                    var generatedSourceText = generatedSource.ToString();
                    // var generatedSourceText = tree.WithChangedText(generatedSource)
                    //     .GetRoot().NormalizeWhitespace().ToFullString();

                    return new Result.Ok(new FileNameWithText(generatedFileName,
                        generatedSourceText));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    return new Result.Error(ex);
                }
            });
    }


    private static IEnumerable<MethodDeclarationSyntax>
        GetMethodsWithInlinedAttribute(SyntaxNode root)
    {
        return root.DescendantNodes(static node => node
                is CompilationUnitSyntax
                or BaseNamespaceDeclarationSyntax
                or TypeDeclarationSyntax
                or MethodDeclarationSyntax
                or AttributeListSyntax)
            .OfType<AttributeSyntax>()
            .Where(static attribute => IsInlineAttribute(attribute))
            .Select(static attribute => attribute.Parent!.Parent as MethodDeclarationSyntax)
            .Where(static x => x != null)!;
    }


    public static bool IsInlineAttribute(AttributeSyntax attribute)
    {
        var name = attribute.Name;
        while (true)
        {
            if (name is IdentifierNameSyntax ins)
            {
                return ins.Identifier.Text is Inline or Inline + "Attribute";
            }

            if (name is not QualifiedNameSyntax qns)
            {
                return false;
            }

            if (qns is
            {
                Right: IdentifierNameSyntax,
                Left: IdentifierNameSyntax leftName
            })
            {
                return leftName.Identifier.Text == Inline;
            }

            name = qns.Right;
        }
    }


    public static void AddAttributes(
        IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource("InliningAttributes.cs", """
#nullable enable
namespace EntitiesDotNet;


[AttributeUsage(AttributeTargets.Method)]
internal class GeneratedFromAttribute : Attribute
{
    public GeneratedFromAttribute(string originalName)
    {
    }
}


[AttributeUsage(AttributeTargets.Method)]
internal class InlineAttribute : Attribute
{
    public InlineAttribute(string? name = null)
    {
    }
}


internal static class Inline
{

    [AttributeUsage(AttributeTargets.Method)]
    internal class PrivateAttribute : Attribute
    {
        public PrivateAttribute(string? name = null)
        {
        }
    }


    [AttributeUsage(AttributeTargets.Method)]
    internal class PublicAttribute : Attribute
    {
        public PublicAttribute(string? name = null)
        {
        }
    }


    [AttributeUsage(AttributeTargets.Method)]
    internal class ProtectedAttribute : Attribute
    {
        public ProtectedAttribute(string? name = null)
        {
        }
    }


    [AttributeUsage(AttributeTargets.Method)]
    internal class InternalAttribute : Attribute
    {
        public InternalAttribute(string? name = null)
        {
        }
    }

}
""");
    }


    public static bool Predicate(SyntaxNode syntaxNode, CancellationToken _)
    {
        return syntaxNode is AttributeSyntax attribute && IsInlineAttribute(attribute)
            && syntaxNode.Parent?.Parent is MethodDeclarationSyntax;
    }


    public static FileNameWithText Transform(SyntaxNode inlineAttributeNode,
        SemanticModel semanticModel, CancellationToken token)
    {
        var methodSyntax = inlineAttributeNode.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (methodSyntax == null)
        {
            return default;
        }

        var typeSyntax = methodSyntax.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (typeSyntax == null)
        {
            return default;
        }

        var methodSymbol =
            (IMethodSymbol?)ModelExtensions.GetDeclaredSymbol(semanticModel,
                methodSyntax, token)
            ?? throw new Exception("Type symbol was not found");

        var declarationInfo = QualifiedDeclarationInfo.FromSyntax(typeSyntax);

        var (inlinedMethodName, accessibility) =
            GetInlinedMethodNameAndAccessibility(methodSymbol);

        var writer = new StringWriter();
        WriteInlinedMethod(writer, inlinedMethodName, methodSyntax, semanticModel, accessibility,
            token);

        return new FileNameWithText
        {
            FileName = methodSymbol.SuggestedFileName(methodSyntax.Identifier.Text),
            Text = declarationInfo.ToString(
                withUsing: "#define SOURCEGEN",
                withMembers: writer.ToString())
        };
    }


    private readonly record struct ParameterInfo(string Name, string? Type);


    private readonly record struct ArgumentInfo(string Name, string? Value, bool IsLambda);


    private readonly record struct InlinableMethodInfo(
        IMethodSymbol Symbol, InvocationExpressionSyntax InvocationSyntax)
    {
        /// <summary>
        /// Extracts extension method receiver text. For example 'Foo.Bar' for 'Foo.Bar.ExtensionMethod()'
        /// </summary>
        public string GetExtensionMethodReceiverText()
        {
            var memberAccess = this.InvocationSyntax.ChildNodes()
                .OfType<MemberAccessExpressionSyntax>().First();
            return memberAccess.ChildNodes().First().ToString();
        }


        public ImmutableArray<ArgumentInfo> GetArguments(ParenthesizedLambdaExpressionSyntax lambda)
        {
            var args = this.InvocationSyntax.ArgumentList.Arguments;
            if (args.Count == 0)
            {
                return ImmutableArray<ArgumentInfo>.Empty;
            }

            var lambdaArg = (ArgumentSyntax?)lambda.Parent;

            var names = this.Symbol.Parameters.Select(x => x.Name);
            var values = args.Select(x => x != lambdaArg ? x.ToString() : null);

            var argsCount = args.Count;
            var isExtensionMethod = this.Symbol.IsExtensionMethod;
            if (isExtensionMethod) ++argsCount;

            var builder = ImmutableArray.CreateBuilder<ArgumentInfo>(argsCount);
            builder.AddRange(names.Zip(values, (name, value) =>
            {
                var isLambda = value == null;
                if (name == "this")
                {
                    name = "@this";
                }

                if (!isLambda)
                {
                    name = "__" + name;
                }

                return new ArgumentInfo(name, value, isLambda);
            }));

            if (isExtensionMethod)
            {
                builder.Add(new ArgumentInfo("@this", this.GetExtensionMethodReceiverText(),
                    false));
            }

            return builder.MoveToImmutable();
        }
    }


    private static ImmutableArray<ParameterInfo> GetLambdaParameters(LambdaExpressionSyntax lambda)
    {
        var parameters = lambda.ChildNodes().OfType<ParameterListSyntax>()
            .First().Parameters;

        var builder = ImmutableArray.CreateBuilder<ParameterInfo>(parameters.Count);
        foreach (var parameter in parameters)
        {
            builder.Add(new ParameterInfo(
                parameter.Identifier.Text,
                parameter.Type?.ToString()));
        }

        return builder.MoveToImmutable();
    }


    private static string GetLambdaBody(LambdaExpressionSyntax lambda)
    {
        var body = lambda.ChildNodes().Last(x => x is BlockSyntax or ExpressionSyntax);
        switch (body)
        {
            case BlockSyntax block:
                var returnStatements = block
                    .DescendantNodes(static node => node is not InvocationExpressionSyntax)
                    .OfType<ReturnStatementSyntax>();

                var continueStatement = SyntaxFactory.ContinueStatement();
                var newBlock = block.ReplaceNodes(returnStatements,
                    (_, _) => continueStatement);

                return newBlock.ToString();
            case ExpressionSyntax expression:
                return expression + ";";
            default:
                throw new UnreachableException();
        }
    }


    private static bool GetInlinableMethodInfo(
        LambdaExpressionSyntax lambda, SemanticModel semanticModel, CancellationToken token,
        out InlinableMethodInfo info)
    {
        info = default;

        var invocationExpression = lambda.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocationExpression == null)
        {
            throw new Exception("Invocation was not found");
        }

        var child = invocationExpression.ChildNodes().First();
        var methodIdentifier = child.DescendantNodesAndSelf()
            .OfType<IdentifierNameSyntax>().Last();

        if (!IsInlinableMethodName(methodIdentifier.Identifier.Text)) return false;

        var getMethodSymbolResult =
            ModelExtensions.GetSymbolInfo(semanticModel, methodIdentifier, token);

        var methodSymbol = (IMethodSymbol?)getMethodSymbolResult.Symbol;
        if (methodSymbol == null)
        {
            return false;
        }

        info = new InlinableMethodInfo(methodSymbol, invocationExpression);
        return true;
    }


    private static bool GetInliningTemplate(IMethodSymbol method, out string template)
    {
        var methodAttribute = method.GetAttributes()
            .FirstOrDefault(x => x.AttributeClass?.Name == SupportsInliningAttribute);

        if (methodAttribute == null)
        {
            template = null!;
            return false;
        }

        var templateText = GetAttributeArgumentValue(methodAttribute);

        template = templateText!;
        return templateText != null;
    }


    private static (string?, Accessibility) GetInlinedMethodNameAndAccessibility(
        IMethodSymbol methodSymbol)
    {
        var attribute = methodSymbol.GetAttributes()
            .First(x => x.AttributeClass is
                { Name: "InlineAttribute" } or
                { ContainingType.Name: Inline });

        var accessibility = attribute.AttributeClass!.Name switch
        {
            "InlineAttribute" or "PublicAttribute" => Accessibility.Public,
            "InternalAttribute" => Accessibility.Internal,
            "ProtectedAttribute" => Accessibility.Protected,
            _ => Accessibility.Private
        };

        return (GetAttributeArgumentValue(attribute), accessibility);
    }


    private static string? GetAttributeArgumentValue(AttributeData attribute)
    {
        var arg = attribute.ConstructorArguments.First();
        return (string?)arg.Value;
    }


    private static string RenderTemplate(string lambdaArgName, string template,
        ImmutableArray<ParameterInfo> parameters, string body)
    {
        var source = template;
        for (var i = 0; i < parameters.Length; ++i)
        {
            var parameter = parameters[i];
            source = source.Replace($"{{{lambdaArgName}.arg{i}}}", parameter.Name);
            source = source.Replace($"{{{lambdaArgName}.arg{i}.type}}", parameter.Type);
        }

        return source.Replace($"{{{lambdaArgName}.body}}", body);
    }


    private static string? GetInlinedText(ParenthesizedLambdaExpressionSyntax lambda,
        SemanticModel semanticModel, CancellationToken token)
    {
        if (!(GetInlinableMethodInfo(lambda, semanticModel, token, out var methodInfo)
            && GetInliningTemplate(methodInfo.Symbol, out var template)))
        {
            return null;
        }

        var parameters = GetLambdaParameters(lambda);
        var body = GetLambdaBody(lambda);

        var writer = new StringWriter();
        writer.WriteLine("{");

        var args = methodInfo.GetArguments(lambda);
        foreach (var arg in args)
        {
            if (arg.IsLambda) continue;
            writer.Write("var ");
            writer.Write(arg.Name);
            writer.Write(" = ");
            writer.Write(arg.Value);
            writer.WriteLine(";");
        }

        var lambdaArgName = args.First(x => x.IsLambda).Name;
        writer.WriteLine(RenderTemplate(lambdaArgName, template, parameters, body));
        writer.WriteLine("}");

        return writer.ToString();
    }


    private static void WriteInlinedMethod(TextWriter writer, string? inlinedName,
        MethodDeclarationSyntax methodSyntax, SemanticModel semanticModel,
        Accessibility accessibility, CancellationToken token)
    {
        WriteInlinedMethodDeclaration(writer, inlinedName, methodSyntax, semanticModel,
            accessibility);
        writer.WriteLine();
        WriteInlinedMethodBlock(writer, methodSyntax, semanticModel, token);
    }


    private static void WriteInlinedMethodDeclaration(TextWriter writer,
        string? inlinedName, MethodDeclarationSyntax methodSyntax, SemanticModel semanticModel,
        Accessibility accessibility)
    {
        var symbol = (IMethodSymbol?)ModelExtensions.GetDeclaredSymbol(semanticModel, methodSyntax);
        if (symbol == null)
        {
            throw new Exception("Method symbol not found");
        }

        writer.Write($"[EntitiesDotNet.GeneratedFrom(nameof({symbol.Name}))]");

        var accessibilityStr = accessibility switch
        {
            Accessibility.Private => "private ",
            Accessibility.Protected => "protected ",
            Accessibility.Internal => "internal ",
            Accessibility.Public => "public ",
            _ => ""
        };

        writer.Write(accessibilityStr);
        if (symbol.IsStatic)
        {
            writer.Write("static ");
        }

        writer.Write(symbol.ReturnType);
        writer.Write(" ");

        if (inlinedName == null)
        {
            if (symbol.Name[0] == '_')
            {
                inlinedName = symbol.Name.TrimStart('_');
            }
            else
            {
                inlinedName = symbol.Name + "_Inlined";
            }
        }

        writer.Write(inlinedName);

        var typeParameterList = methodSyntax.ChildNodes().OfType<TypeParameterListSyntax>()
            .FirstOrDefault();

        if (typeParameterList != null)
        {
            writer.Write(typeParameterList);
        }

        var parameterList = methodSyntax.ChildNodes().OfType<ParameterListSyntax>()
            .First();
        writer.Write(parameterList.ToString());
    }


    private static void WriteInlinedMethodBlock(TextWriter writer,
        MethodDeclarationSyntax methodSyntax, SemanticModel semanticModel, CancellationToken token)
    {
        var lambdaNodes = methodSyntax.DescendantNodes()
            .OfType<ParenthesizedLambdaExpressionSyntax>();

        var inlineBlocks = lambdaNodes.Select(lambdaNode =>
        {
            var expression = lambdaNode.FirstAncestorOrSelf<ExpressionStatementSyntax>()
                ?? throw new Exception("Expression statement was not found.");

            var inlinedText = GetInlinedText(lambdaNode, semanticModel, token);
            if (inlinedText == null)
            {
                return null;
            }

            return new
            {
                InlinedText = inlinedText,
                expression.SpanStart,
                SpanEnd = expression.Span.End
            };
        }).Where(x => x != null);

        var methodBlock = methodSyntax.Body!;
        var methodBlockText = methodSyntax.SyntaxTree.GetText();

        var lastPosition = methodBlock.SpanStart;
        foreach (var inlineBlock in inlineBlocks)
        {
            var sourceTextBeforeForEach =
                methodBlockText.ToString(TextSpan.FromBounds(lastPosition, inlineBlock!.SpanStart));

            writer.WriteLine(sourceTextBeforeForEach);
            writer.WriteLine(inlineBlock.InlinedText);

            lastPosition = inlineBlock.SpanEnd;
        }

        writer.WriteLine(
            methodBlockText.ToString(TextSpan.FromBounds(lastPosition, methodBlock.Span.End)));
    }


    private static bool IsInlinableMethodName(string name)
    {
        return name == "ForEach";
    }
}