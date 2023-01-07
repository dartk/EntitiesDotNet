using System.Diagnostics.Contracts;


namespace EntityComponentSystem;


public static partial class ComponentArrayExtensions
{
    [Pure]
    public static string ToReadableString(this IComponentArray @this)
    {
        var components = @this.Archetype.Components;
        var writer = new StringTable().CreateWriter();

        writer.SetDefaultAlignment(HorizontalAlignment.Center);
        foreach (var component in components)
        {
            writer.Cell(component.Type.Name);
        }

        writer.NewRow();

        for (var i = 0; i < @this.Count; ++i)
        {
            foreach (var component in components)
            {
                writer.Cell(@this.GetValue(component, i)?.ToString() ?? "");
            }

            writer.NewRow();
        }

        return writer.ToString();
    }
}