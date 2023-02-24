using System.Collections.Immutable;
using CSharp.SourceGen.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;


// ReSharper disable NotAccessedPositionalProperty.Local
// ReSharper disable UnusedParameter.Local


namespace EntitiesDotNet.Generators;


public static class EntityRefStructGenerator 
{
    private const string EntityRefStruct = nameof(EntityRefStruct);


    public static void AddAttributes(
        IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource("EntityRefStructAttribute.g.cs", """
            namespace EntitiesDotNet;


            [AttributeUsage(AttributeTargets.Struct)]
            internal class EntityRefStructAttribute : Attribute { }
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
        node.IsAttribute(EntityRefStruct);


    public static FileNameWithText Transform( GeneratorSyntaxContext context,
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
            typeSymbol.DeclaredAccessibility == Accessibility.Public,
            declaration.TypeOpen(),
            declaration.TypeClose(),
            readMembers.ToImmutableArray(),
            writeMembers.ToImmutableArray());

        return new FileNameWithText
        {
            FileName = info.FileName,
            Text = Template.Render(info)
        };
    }


//     public void Initialize(IncrementalGeneratorInitializationContext context)
//     {
//         var generatedTextFilesProvider = context.SyntaxProvider.CreateSyntaxProvider(
//                 predicate: static (node, token) => node.IsAttribute(EntityRefStruct),
//                 transform: static (context, token) =>
//                 {
//                     if (!context.TryGetAttributeAppliedType(
//                         out var typeDeclarationSyntax, out var typeSymbol))
//                     {
//                         return null;
//                     }
//
//
//                     var readMembers = new List<ComponentInfo>();
//                     var writeMembers = new List<ComponentInfo>();
//
//                     foreach (var member in typeSymbol.GetMembers())
//                     {
//                         if (member is not IFieldSymbol fieldSymbol)
//                         {
//                             continue;
//                         }
//
//                         if (fieldSymbol.DeclaredAccessibility != Accessibility.Public)
//                         {
//                             continue;
//                         }
//
//                         foreach (var @ref in fieldSymbol.DeclaringSyntaxReferences)
//                         {
//                             var node = @ref.GetSyntax();
//                             var parent = node.Parent;
//                             if (parent == null)
//                             {
//                                 continue;
//                             }
//
//                             var parentStr = parent.ToString();
//                             if (parentStr.Contains("ref readonly"))
//                             {
//                                 readMembers.Add(
//                                     new ComponentInfo(
//                                         member.Name,
//                                         fieldSymbol.Type.ToDisplayString()
//                                     ));
//                             }
//                             else if (parentStr.Contains("ref "))
//                             {
//                                 writeMembers.Add(
//                                     new ComponentInfo(
//                                         member.Name,
//                                         fieldSymbol.Type.ToDisplayString()
//                                     ));
//                             }
//                         }
//                     }
//
//                     if (!readMembers.Any() && !writeMembers.Any())
//                     {
//                         return null;
//                     }
//
//                     var declaration = QualifiedDeclarationInfo.FromSyntax(typeDeclarationSyntax);
//
//                     return new Info(
//                         typeSymbol.SuggestedFileName(),
//                         typeSymbol.Name,
//                         typeSymbol.DeclaredAccessibility == Accessibility.Public,
//                         declaration.TypeOpen(),
//                         declaration.TypeClose(),
//                         readMembers.ToImmutableArray(),
//                         writeMembers.ToImmutableArray());
//                 })
//             .Where(x => x != null)
//             .Collect()
//             .SelectMany(static (items, token) =>
//             {
//                 var template = Template.Parse(ManifestResource.ReadAllText(
//                     "EntitiesDotNet.Generators",
//                     "Scriban",
//                     "EntityRefStruct.scriban"));
//
//                 var textArray =
//                     ImmutableArray.CreateBuilder<(string FileName, string Text)>(items.Length);
//                 foreach (var item in items)
//                 {
//                     token.ThrowIfCancellationRequested();
//                     textArray.Add((item!.FileName, template.Render(item)));
//                 }
//
//                 return textArray.MoveToImmutable();
//             });
//
//         var generatedSyntaxTreeProvider = generatedTextFilesProvider
//             .Collect()
//             .Combine(context.CompilationProvider)
//             .Select(static (arg, token) =>
//             {
//                 var treeText = """
//                     using EntitiesDotNet.Benchmarks;
//
//                     public static class Test
//                     {
//                         public static void InvokeForEach(EntityArrays arrays)
//                         {
//                             UpdateVelocityEntity.ForEach(arrays, (UpdateVelocityEntity entity) =>
//                                 { entity.Velocity += entity.Acceleration * 1f / 30f; });
//                         }
//                     }
//                     """;
//                 var tree = CSharpSyntaxTree.ParseText(treeText);
//
//                 var (generatedTexts, compilation) = arg;
//                 var generatedCompilation = compilation.AddSyntaxTrees(
//                     generatedTexts.Select(static item => CSharpSyntaxTree.ParseText(item.Text))
//                         .Append(tree));
//
//                 var typeName = "EntitiesDotNet.Benchmarks.UpdateVelocityEntity";
//                 var typeSymbol = generatedCompilation.Assembly.GetTypeByMetadataName(typeName);
//                 Logger.Log("TypeSymbol: " + typeSymbol);
//                 Logger.Log("Members");
//                 // foreach (var typeMember in typeSymbol.GetMembers())
//                 // {
//                 //     Logger.Log(typeMember);
//                 // }
//
//                 var model = generatedCompilation.GetSemanticModel(tree);
//
//                 var memberAccess =
//                     tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>()
//                         .FirstOrDefault();
//                 if (memberAccess == null)
//                 {
//                     Logger.Log("member access not found");
//                     return null;
//                 }
//                 else
//                 {
//                     Logger.LogYaml(memberAccess);
//                 }
//
//                 var methodName = memberAccess.ChildNodes().OfType<NameSyntax>().Last();
//                 Logger.Log("MethodName: " + methodName);
//
//                 var methodSymbolInfo = ModelExtensions.GetSymbolInfo(model, methodName);
//                 Logger.Log("Symbol: " + methodSymbolInfo.Symbol);
//                 Logger.Log("Candidates: " + methodSymbolInfo.CandidateSymbols.Length);
//
//                 return "";
//             });
//
//         context.RegisterSourceOutput(generatedSyntaxTreeProvider, static (_, _) => { });
//
//         context.RegisterSourceOutput(generatedTextFilesProvider,
//             static (context, arg) => { context.AddSource(arg.FileName, arg.Text); });
//     }


    private record Info(
        string FileName,
        string Name,
        bool IsPublic,
        string TypeOpen,
        string TypeClose,
        ImmutableArray<ComponentInfo> ReadComponents,
        ImmutableArray<ComponentInfo> WriteComponents
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