#pragma once

#define DllExport extern "C" __declspec( dllexport )

struct float3
{
    float x;
    float y;
    float z;
};


DllExport void* entt_create_registry();
DllExport void entt_destroy_registry(void* registry);
DllExport void entt_create_entities(void* registry_ptr, int count);
DllExport void entt_system_update_velocity(void* registry_ptr, float delta_time);
DllExport void entt_system_update_velocity_and_translation(void* registry_ptr, float delta_time);
DllExport void update_velocity(int count, const float3* accelerations, float3* velocities,
                               float delta_time);
DllExport void update_translation(int count, const float3* velocities, float3* translations,
                                  float delta_time);
