using System.Dynamic;
using Newtonsoft.Json;


namespace EntitiesDotNet.Generators;


internal class ExpandoObject : DynamicObject
{

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        return this._values.TryGetValue(binder.Name, out result);
    }


    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
        this._values[binder.Name] = value;
        return true;
    }


    [JsonProperty("Values")]
    private readonly Dictionary<string, object> _values = new();
}