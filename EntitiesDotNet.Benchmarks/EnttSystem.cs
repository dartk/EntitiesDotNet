using static EntitiesDotNet.Benchmarks.Native;


namespace EntitiesDotNet.Benchmarks;


public class EnttSystem : IDisposable
{
    private readonly nint _registry;


    public EnttSystem(int count)
    {
        this._registry = entt_create_registry();
        entt_create_entities(this._registry, count);
    }


    ~EnttSystem()
    {
        this.ReleaseUnmanagedResources();
    }


    private void ReleaseUnmanagedResources()
    {
        entt_destroy_registry(this._registry);
    }


    public void Dispose()
    {
        this.ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }


    public void UpdateVelocity(float deltaTime)
    {
        entt_system_update_velocity(this._registry, deltaTime);
    }


    public void UpdateVelocityAndTranslation(float deltaTime)
    {
        entt_system_update_velocity_and_translation(this._registry, deltaTime);
    }
}