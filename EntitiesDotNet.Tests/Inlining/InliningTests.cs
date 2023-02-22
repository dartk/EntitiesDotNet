using CSharp.SourceGen.Inlining.Attributes;
using EntitiesDotNet;


namespace EntityComponentSystem.Tests.Inlining;


public partial class InliningTests
{
    [GenerateInlined(nameof(ForEachArrays_Inlined))]
    public void ForEachArrays(EntityArrays arrays)
    {
        arrays.ForEach([Inline](int index, ref string s, ref double d) => { s = d.ToString(); });
        arrays.ForEach([Inline](ref string s, ref double d) => { s = d.ToString(); });
    }
    
    [GenerateInlined(nameof(ForEach_Inlined))]
    public void ForEach(IComponentArray array)
    {
        array.ForEach([Inline](int index, ref string s, ref double d) => { s = d.ToString(); });
        array.ForEach([Inline](ref string s, ref double d) => { s = d.ToString(); });
    }
}