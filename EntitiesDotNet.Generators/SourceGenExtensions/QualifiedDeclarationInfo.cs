using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace CSharp.SourceGen.Extensions;


public record NamespaceDeclarationInfo(string? Declaration, string Usings);
public record TypeDeclarationInfo(string Declaration);


/// <summary>
/// Gets type declaration info that is used for code generation.
/// To get partial type use <see cref="ToString(string)"/> method.
/// </summary>
/// <example>
/// <code>
/// QualifiedDeclarationInfo.FromSyntax(typeDeclarationSyntax).ToString("""
///     public static string HelloWorld = "Hello, World!";
///     """)
/// </code>
/// </example>
public record QualifiedDeclarationInfo(
    ImmutableArray<NamespaceDeclarationInfo> Namespaces,
    ImmutableArray<TypeDeclarationInfo> Types)
{
    public string NamespaceOpen()
    {
        var builder = new StringBuilder();

        builder.AppendLine(
            "#pragma warning disable CS0105    // disables warning about using the same namespaces several times");
        builder.AppendLine();

        foreach (var nm in this.Namespaces)
        {
            if (nm.Declaration != null)
            {
                builder.Append("namespace ");
                builder.AppendLine(nm.Declaration);
                builder.AppendLine("{");
            }

            builder.AppendLine(nm.Usings);
        }

        return builder.ToString();
    }


    public string NamespaceClose()
    {
        var builder = new StringBuilder();

        foreach (var nm in this.Namespaces)
        {
            if (nm.Declaration != null)
            {
                builder.AppendLine("}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("#pragma warning restore CS0104");

        return builder.ToString();
    }


    public string TypeOpenNoNamespace()
    {
        var builder = new StringBuilder();

        for (var i = 0; i < this.Types.Length; ++i)
        {
            var type = this.Types[i];
            builder.Append(type.Declaration);
            
            // is not last
            if (i < this.Types.Length - 1)
            {
                builder.AppendLine();
                builder.AppendLine("{");
            }
        }

        return builder.ToString();
    }


    public string TypeCloseNoNamespace()
    {
        var builder = new StringBuilder();

        // skip one
        for (var i = 1; i < this.Types.Length; ++i)
        {
            builder.AppendLine("}");
        }

        return builder.ToString();
    }


    public string TypeOpen() => this.NamespaceOpen() + this.TypeOpenNoNamespace();
    public string TypeClose() => this.TypeCloseNoNamespace() + this.NamespaceClose();


    public override string ToString()
    {
        return this.ToString("");
    }


    public string ToString(
        string withMembers,
        string? withUsing = null,
        string? withConstraints = null)
    {
        var builder = new StringBuilder();
        if (withUsing != null)
        {
            builder.AppendLine(withUsing);
        }

        builder.Append(this.TypeOpen());

        if (withConstraints != null)
        {
            builder.Append(" ");
            builder.Append(withConstraints);
        }

        builder.AppendLine();
        builder.AppendLine("{");

        builder.AppendLine(withMembers);
        builder.AppendLine("}");
        builder.AppendLine(this.TypeClose());

        return builder.ToString();
    }


    public static QualifiedDeclarationInfo FromSyntax(SyntaxNode targetNode)
    {
        var namespaceDeclarations = ImmutableArray.CreateBuilder<NamespaceDeclarationInfo>();
        var typeDeclarations = ImmutableArray.CreateBuilder<TypeDeclarationInfo>();

        foreach (var node in targetNode.AncestorsAndSelf())
        {
            switch (node)
            {
                case NamespaceDeclarationSyntax namespaceSyntax:
                {
                    var name = namespaceSyntax.Name.ToString();
                    namespaceDeclarations.Add(
                        new NamespaceDeclarationInfo(name, GetChildUsingStatements(node)));
                    break;
                }
                case FileScopedNamespaceDeclarationSyntax namespaceSyntax:
                {
                    var name = namespaceSyntax.Name.ToString();
                    namespaceDeclarations.Add(
                        new NamespaceDeclarationInfo(name, GetChildUsingStatements(node)));
                    break;
                }
                case CompilationUnitSyntax:
                    namespaceDeclarations.Add(
                        new NamespaceDeclarationInfo(null, GetChildUsingStatements(node)));
                    break;
                default:
                {
                    if (TypeDeclarationSyntaxUtil.ToString(node) is { } declaration)
                    {
                        typeDeclarations.Add(new TypeDeclarationInfo(declaration));
                    }

                    break;
                }
            }
        }

        typeDeclarations.Reverse();
        namespaceDeclarations.Reverse();

        return new QualifiedDeclarationInfo(
            namespaceDeclarations.ToImmutableArray(),
            typeDeclarations.ToImmutableArray());
    }


    private static string GetChildUsingStatements(SyntaxNode node)
    {
        var builder = new StringBuilder();

        var usingSeq = node.ChildNodes()
            .Where(x => x is UsingDirectiveSyntax or UsingStatementSyntax);

        foreach (var item in usingSeq)
        {
            builder.AppendLine(item.ToString());
        }

        return builder.ToString();
    }
}