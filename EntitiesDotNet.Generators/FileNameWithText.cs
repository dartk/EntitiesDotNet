using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace EntitiesDotNet.Generators;


public readonly record struct FileNameWithText(string FileName, string Text)
{
    public bool IsEmpty => string.IsNullOrEmpty(this.Text);


    public FileNameWithText FormatText(CancellationToken token)
    {
        return this with
        {
            Text = CSharpSyntaxTree.ParseText(this.Text, cancellationToken: token)
                .GetRoot().NormalizeWhitespace().ToFullString()
        };
    }


    public static void AddSource(SourceProductionContext context, FileNameWithText file)
    {
        context.AddSource(file.FileName, file.Text);
    }
}