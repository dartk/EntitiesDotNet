using System.Reflection;


namespace CSharp.SourceGen.Extensions;


internal static class ManifestResource
{
    public static string ReadAllText(params string[] path)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = string.Join(".", path);
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            throw new ArgumentException(
                $"Resource '{resourceName}' not found.");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }


    public static string[] GetAllResourceNames()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceNames();
    }
}