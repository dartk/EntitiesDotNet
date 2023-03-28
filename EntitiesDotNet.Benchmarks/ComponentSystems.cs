using System.Runtime.CompilerServices;


namespace EntitiesDotNet.Benchmarks;


public static partial class ComponentSystems
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UpdateTranslation(in Velocity v, ref Translation t, float deltaTime)
    {
        t.Float3.X += v.Float3.X * deltaTime;
        t.Float3.Y += v.Float3.Y * deltaTime;
        t.Float3.Z += v.Float3.Z * deltaTime;
    }


    /// <summary>
    /// Calls C++ function to process spans of components
    /// </summary>
    public static unsafe void loop_native(EntityArrays entities, float deltaTime)
    {
        foreach (var (count, velocities, translations) in
            Read<Velocity>.Write<Translation>.From(entities))
        {
            fixed (Velocity* velocitiesPtr = velocities)
            fixed (Translation* translationsPtr = translations)
            {
                // C++ function call
                Native.update_translation(count, velocitiesPtr, translationsPtr, deltaTime);
            }
        }
    }


    public static void loop(EntityArrays entities, float deltaTime)
    {
        foreach (var (count, velocities, translations) in
            Read<Velocity>.Write<Translation>.From(entities))
        {
            for (var i = 0; i < count; ++i)
            {
                UpdateTranslation(velocities[i], ref translations[i], deltaTime);
            }
        }
    }


    [Inline.Public(nameof(ext_inl))]
    public static void ext(EntityArrays entities, float deltaTime)
    {
        entities.ForEach((in Velocity v, ref Translation t) =>
        {
            UpdateTranslation(v, ref t, deltaTime);
        });
    }


    [EntityRef]
    public ref partial struct UpdateTranslationEntity
    {
        public ref readonly Velocity Velocity;
        public ref Translation Translation;
    }


    [Inline.Public(nameof(ER_ext_inl))]
    public static void ER_ext(EntityArrays entities, float deltaTime)
    {
        entities.ForEach((in UpdateTranslationEntity entity) =>
            UpdateTranslation(entity.Velocity, ref entity.Translation, deltaTime));
    }


    public static void ER_loop(EntityArrays entities, float deltaTime)
    {
        foreach (var entity in UpdateTranslationEntity.From(entities))
        {
            UpdateTranslation(entity.Velocity, ref entity.Translation, deltaTime);
        }
    }
}