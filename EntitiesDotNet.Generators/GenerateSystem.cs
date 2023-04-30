using CSharp.SourceGen.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;


namespace EntitiesDotNet.Generators;


internal static class GenerateSystem
{
    private const string GenerateSystemAttributeName = "GenerateSystem";


    public static void AddAttributes(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource(
            $"{GenerateSystemAttributeName}.g.cs",
            $$"""
namespace EntitiesDotNet;


[AttributeUsage(AttributeTargets.Method)]
internal class {{GenerateSystemAttributeName}}Attribute : Attribute {}
""");
    }


    public static IncrementalValuesProvider<Result?>
        CreateProvider(IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider
            .CreateSyntaxProvider(Predicate, Transform)
            .Where(static x => x != null)!;
    }


    public static bool Predicate(SyntaxNode node, CancellationToken token) =>
        node.IsAttribute(GenerateSystemAttributeName);


    public static Result? Transform(GeneratorSyntaxContext context,
        CancellationToken token)
    {
        var attributeSyntax = (AttributeSyntax)context.Node;
        var methodSyntax = (MethodDeclarationSyntax)attributeSyntax.Parent!.Parent!;
        var parameterList = methodSyntax.ChildNodes()
            .OfType<ParameterListSyntax>().First();
        var parameters = parameterList.Parameters;

        var typeSyntax = methodSyntax.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (typeSyntax == null) return null;

        var methodSymbol =
            (IMethodSymbol?)context.SemanticModel.GetDeclaredSymbol(methodSyntax, token)
            ?? throw new Exception("Method symbol was not found");

        var declarationInfo = QualifiedDeclarationInfo.FromSyntax(typeSyntax);

        var parameterInfoList = new List<ParameterInfo>(parameters.Count);
        foreach (var parameter in parameterList.Parameters)
        {
            if (parameter.AttributeLists.Count != 0) continue;

            var type = parameter.Type;
            var typeIdentifier = type switch
            {
                PredefinedTypeSyntax predefinedTypeSyntax => predefinedTypeSyntax.Keyword.Text,
                IdentifierNameSyntax identifierNameSyntax => identifierNameSyntax.Identifier.Text,
                _ => throw new ArgumentOutOfRangeException()
            };

            var modifier = parameter.Modifiers.FirstOrDefault().Text;
            var isReadOnly = modifier is "" or "in";

            var parameterInfo = new ParameterInfo(
                Identifier: parameter.Identifier.Text,
                Type: typeIdentifier,
                Modifier: modifier,
                IsReadOnly: isReadOnly);

            parameterInfoList.Add(parameterInfo);
        }

        var methodIdentifier = methodSyntax.Identifier.Text;
        var templateArg = new TemplateArg(methodIdentifier, parameterInfoList);

        var fileName = methodSymbol.SuggestedFileName(methodIdentifier);
        var generatedText = declarationInfo.ToString(
            withUsing: """
                #define SOURCEGEN
                using System.Runtime.InteropServices;
                using System.Runtime.CompilerServices;
                """,
            withMembers: RenderTemplate(templateArg));

        return new Result.Ok(new FileNameWithText(fileName, generatedText));
    }


    private static string RenderTemplate(TemplateArg arg)
    {
        return _template.Render(arg, member => member.Name);
    }


    private record TemplateArg(string MethodName, List<ParameterInfo> ParameterInfoList);
    private record ParameterInfo(string Identifier, string Type, string Modifier, bool IsReadOnly);


    private static Template _template =
        Template.Parse("""
    public static void {{ MethodName }}(EntityArrays arrays)
    {
        foreach (var array in arrays)
        {
            {{ MethodName }}(array);
        }
    }


    public static void {{ MethodName }}(IComponentArray array)
    {
        if (!array.Archetype.Contains<{{ ParameterInfoList | array.map "Type" | array.join ", " }}>()) return;
        {{~}}

        {{- for parameter in ParameterInfoList }}
        var {{ parameter.Identifier }}Span = array.Get{{ parameter.IsReadOnly ? "ReadOnly" : "" }}Span<{{ parameter.Type }}>();
        {{- end }}
        {{~}}

        {{- for parameter in ParameterInfoList }}
        ref var {{ parameter.Identifier }} = ref MemoryMarshal.GetReference({{ parameter.Identifier }}Span);
        {{- end }}

        var length = array.Count;
        for (var i = 0; i < length;
            {{- for parameter in ParameterInfoList }}
            {{ parameter.Identifier }} = ref Unsafe.Add(ref {{ parameter.Identifier }}, 1),
            {{- end }}
            ++i)
        {
            {{ MethodName }}({{
                ParameterInfoList |
                array.each @(do
                    ret $0.Modifier + " " + $0.Identifier
                end) |
                array.join ", " }});
        }
    }
""");
}