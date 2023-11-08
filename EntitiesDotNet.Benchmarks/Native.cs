using System.Runtime.InteropServices;


namespace EntitiesDotNet.Benchmarks;


internal partial class Native
{
    private const string LibPath = "x64/EntitiesDotNet.Benchmarks.Native.dll";


    [LibraryImport(LibPath)]
    public static unsafe partial void update_velocity(int count,
        Acceleration* accelerations, Velocity* velocities, float deltaTime);


    [LibraryImport(LibPath)]
    public static unsafe partial void update_translation(int count,
        Velocity* velocities, Translation* translations, float deltaTime);


    [LibraryImport(LibPath)]
    public static partial nint arrays_new(int count);


    [LibraryImport(LibPath)]
    public static partial void arrays_update(IntPtr ptr, float deltaTime);


    [LibraryImport(LibPath)]
    public static partial void arrays_delete(IntPtr ptr);
}