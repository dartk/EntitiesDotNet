using Microsoft.CodeAnalysis;


namespace EntitiesDotNet.Generators;


internal static class IncrementalGeneratorInitializationContextExtensions
{
    public static void RegisterImplementationSourceOutputForResult(
        this IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<Result?> provider)
    {
        context.RegisterImplementationSourceOutput(provider, static (context, arg) =>
        {
            if (arg == null) return;

            switch (arg)
            {
                case Result.Ok { File: var file }:
                    FileNameWithText.AddSource(context, file);
                    break;
                case Result.Error { Exception: var ex }:
                    context.ReportDiagnostic(Diagnostic.Create(ErrorDescriptor, Location.None,
                        ex.Message));
                    break;
            }
        });
    }


    private static readonly DiagnosticDescriptor ErrorDescriptor = new(
        id: $"{DiagnosticIdPrefix}001",
        title: "EntitiesDotNet source generator error",
        messageFormat: "{0}",
        category: DiagnosticCategory,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    private const string DiagnosticIdPrefix = "EntitiesDotNet";
    private const string DiagnosticCategory = "EntitiesDotNet";
}