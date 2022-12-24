using System.Numerics;
using System.Runtime.CompilerServices;


namespace EntityComponentSystem.Tests;



public partial struct LocalTransform : IComponent {
    [ImplicitCast] public Matrix4x4 Matrix;
}


public partial struct Translation : IComponent {
    [ImplicitCast] public Vector3 Vector;
}


public partial struct Velocity : IComponent {
    [ImplicitCast] public Vector3 Vector;
}


public partial struct Rotation : IComponent {
    [ImplicitCast] public Quaternion Quaternion;
}


public partial struct Scale : IComponent {
    [ImplicitCast] public Vector3 Vector;
}


public partial struct WorldTransform : IComponent {
    [ImplicitCast] public Matrix4x4 Matrix;
}


public struct Length : IComponent {
    public float Float;
}


public struct EntityId : IComponent {
    public int Id;
}


public ref struct TRS {
    public ref Translation Translation;
    public ref Rotation Rotation;
    public ref Scale Scale;
    public ref LocalTransform Transform;

    public static readonly SelectorClass Selector = new ();


    public class SelectorClass {
    }


    public ref struct Array {
        public TRS this[int index] => throw new NotImplementedException();

        public Enumerator GetEnumerator() => new ();


        public ref struct Enumerator {
            public bool MoveNext() => throw new NotImplementedException();
            public TRS Current => throw new NotImplementedException();
        }
    }
}


public class ExcludeAttribute<T> : Attribute {
}


public class IncludeAttribute<T> : Attribute {
}


public class RefAttribute<T> : Attribute {
}


public interface IComponentSystem {
    void Execute();
}


public class World {
}


public interface ISomeInterface {
    
}


public abstract class ComponentBase {

    public abstract void Execute();

    protected IComponentArray Entities { get; }
    
}
    
[Query]
[Exclude<WorldTransform>]
public ref partial struct Query {
    public ref readonly Translation Translation;
    public ref readonly Rotation Rotation;
    public ref readonly Scale Scale;
    public ref LocalTransform Transform;
}


public static partial class TestSystem {


    public static void Execute(IReadOnlyList<IComponentArray> entities) {
        foreach (var array in entities) {
            // array condition
            var (count, translation, rotation, scale, transform) = array
                .Read<Translation, Rotation, Scale>()
                .Write<LocalTransform>();

            for (var i = 0; i < count; ++i) {
                transform[i] =
                    Matrix4x4.CreateScale(scale[i])
                    * Matrix4x4.CreateFromQuaternion(rotation[i])
                    * Matrix4x4.CreateTranslation(translation[i]);
            }
        }

        foreach (var entity in Query.Select(entities)) {
            entity.Transform =
                Matrix4x4.CreateScale(entity.Scale)
                * Matrix4x4.CreateFromQuaternion(entity.Rotation)
                * Matrix4x4.CreateTranslation(entity.Translation);
        }
    }

}