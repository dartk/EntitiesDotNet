using System.Runtime.InteropServices;


namespace EntitiesDotNet.Benchmarks;


internal partial class Native
{
    private const string LibPath = "x64/EntitiesDotNet.Benchmarks.Native.dll";
    
    [LibraryImport(LibPath)]
    public static partial nint entt_create_registry();


    [LibraryImport(LibPath)]
    public static partial void entt_destroy_registry(nint registry);


    [LibraryImport(LibPath)]
    public static partial void entt_create_entities(nint registry, int count);


    [LibraryImport(LibPath)]
    public static partial void entt_system_update_velocity(nint registry, float deltaTime);


    [LibraryImport(LibPath)]
    public static partial void entt_system_update_velocity_and_translation(nint registry,
        float deltaTime);


    [LibraryImport(LibPath)]
    public static unsafe partial void update_velocity(int count, Acceleration* accelerations,
        Velocity* velocities, float deltaTime);


    [LibraryImport(LibPath)]
    public static unsafe partial void update_translation(int count, Velocity* velocities,
        Translation* translations, float deltaTime);
}