using System.Collections.Immutable;
using CSharp.SourceGen.Extensions;
using Microsoft.CodeAnalysis;
using Scriban;


// ReSharper disable NotAccessedPositionalProperty.Local
// ReSharper disable UnusedParameter.Local


namespace EntitiesDotNet.Generators;


[Generator]
public class EntityRefStructGenerator : IIncrementalGenerator
{
    private const string EntityRefStruct = nameof(EntityRefStruct);


    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, token) => node.IsAttribute(EntityRefStruct),
            transform: static (context, token) =>
            {
                if (!context.TryGetAttributeAppliedType(
                    out var typeDeclarationSyntax, out var typeSymbol))
                {
                    return null;
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
                    return null;
                }

                var declaration = QualifiedDeclarationInfo.FromSyntax(typeDeclarationSyntax);

                return new Info(
                    typeSymbol.SuggestedFileName(),
                    typeSymbol.Name,
                    typeSymbol.DeclaredAccessibility == Accessibility.Public,
                    declaration.TypeOpen(),
                    declaration.TypeClose(),
                    readMembers.ToImmutableArray(),
                    writeMembers.ToImmutableArray());
            }).Collect();

        context.RegisterSourceOutput(provider, static (context, args) =>
        {
            var template = Template.Parse(ScribanTemplate);

            foreach (var item in args)
            {
                if (item == null) continue;
                context.AddSource(item.FileName, template.Render(item));
            }
        });
    }


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


    private const string ScribanTemplate = """
{{- # Functions

# Creates string "RR...RWW...W", readCount 'R' and (parameterCount - readCount) 'W'
func Suffix (parameterCount, readCount)
    $suffix = ""
    if readCount > 0
        for $i in 1..readCount
            $suffix += "R"
        end
    end
    
    $writeCount = parameterCount - readCount
    if $writeCount > 0
        for $i in 1..$writeCount
            $suffix += "W"
        end
    end
    
    ret $suffix
end

-}}

using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

{{ type_open }}
{

    {{- all_components = array.add_range read_components write_components }}
    {{-
        all_components_with_spans = read_components |
            array.each @(do
                ret {
                    span: ("ReadOnlySpan<" + $0.type + ">"),
                    name: $0.name
                }
            end) |
            array.concat (write_components |
                array.each @(do
                    ret {
                        span: ("Span<" + $0.type + ">"),
                        name: $0.name
                    }
                end))
    }}
    
    {{- # Creates string for IComponentArray.From method "Component0.Read, Component1.Write, ..." }}
    {{-
        if (array.size read_components) > 0
            read_components_str = ".Read<" + (read_components | array.each @(do; ret $0.type; end) | array.join ", ") + ">"
        else
            read_components_str = ""
        end
        
        if (array.size write_components) > 0
            write_components_str = ".Write<" + (write_components | array.each @(do; ret $0.type; end) | array.join ", ") + ">"
        else
            write_components_str = ""
        end
    }}
    
    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static {{ name }}.Array From(IComponentArray array) => new (array);


    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static {{ name }}.ArrayList From(IReadOnlyList<IComponentArray> arrayList) => new (arrayList);


    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static {{ name }}.ArrayEnumerable From(IEnumerable<IComponentArray> arrayEnumerable) => new (arrayEnumerable);

    
    public readonly ref struct Array {

        public Array(IComponentArray array) {
            {{- select_str = "EntitiesDotNet" + read_components_str + write_components_str }}
            (this.Length, {{ all_components | array.each @(do; ret ("this." + $0.name + "Span"); end) | array.join ", " }}) = {{ select_str }}.From(array);
        }
        
        
        public readonly int Length;
        
        {{- for component in all_components_with_spans }}
        public readonly {{ component.span }} {{ component.name }}Span;
        {{- end }}
        
        
        public {{ name }} this[int index] {
            get {
                {{ name }} result = default;
                {{- for component in all_components }}
                result.{{ component.name }} = ref this.{{ component.name }}Span[index];
                {{- end }}
                return result;
            }
        }
        

        public Enumerator GetEnumerator() {
            return new Enumerator(this);
        }


        public ref struct Enumerator {
            public Enumerator(Array array) {
                this._index = -1;
                this._array = array;
            }


            private Array _array;
            private int _index;


            public bool MoveNext() {
                return ++this._index < this._array.Length;
            }


            public {{ name }} Current => this._array[this._index];
        }
        
    }
    
    
    public readonly struct ArrayList {
    
        public ArrayList(IReadOnlyList<IComponentArray> arrayList) {
            this._arrayList = arrayList;
        }
        
        private readonly IReadOnlyList<IComponentArray> _arrayList;
        
        public Enumerator GetEnumerator() => new (this);
        
        
        public ref struct Enumerator {
        
            public Enumerator(ArrayList arrayList) {
                this._arrayList = arrayList._arrayList;
                this._arrayIndex = -1;
                this._itemIndex = -1;
            }
        
            private readonly IReadOnlyList<IComponentArray> _arrayList;
            private {{ name }}.Array _currentArray;
            private int _arrayIndex;
            private int _itemIndex;
            
            public bool MoveNext() {
                if (++this._itemIndex < this._currentArray.Length) {
                    return true;
                }
                
                while (this._itemIndex >= this._currentArray.Length) {
                    ++this._arrayIndex;
                    
                    if (this._arrayIndex >= this._arrayList.Count) {
                        return false;
                    }
                    
                    this._currentArray = From(_arrayList[this._arrayIndex]);
                    this._itemIndex = 0;
                }
                
                return true;
            }
            
            public {{ name }} Current => this._currentArray[this._itemIndex];
            
        }
        
    }
    
    
    public readonly struct ArrayEnumerable {
        public ArrayEnumerable(IEnumerable<IComponentArray> arraySeq) {
            this._arraySeq = arraySeq;
        }
        
        private readonly IEnumerable<IComponentArray> _arraySeq;
        
        public Enumerator GetEnumerator() => new (this);
        
        
        public ref struct Enumerator {
        
            public Enumerator(ArrayEnumerable arrayEnumerable) {
                this._enumerator = arrayEnumerable._arraySeq.GetEnumerator();
                this._index = -1;
            }
        
            private readonly IEnumerator<IComponentArray> _enumerator;
            private {{ name }}.Array _currentArray;
            private int _index;
            
            public bool MoveNext() {
                if (++this._index < this._currentArray.Length) {
                    return true;
                }
                
                while (this._index >= this._currentArray.Length) {
                    if (!this._enumerator.MoveNext()) {
                        return false;
                    }
                    
                    this._currentArray = From(this._enumerator.Current);
                    this._index = 0;
                }
                
                return true;
            }
            
            public {{ name }} Current => this._currentArray[this._index];
            
        }
    }

}
{{ type_close }}
""";
}