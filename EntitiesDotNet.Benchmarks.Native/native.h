#pragma once

#define DllExport extern "C" __declspec( dllexport )

struct float3
{
    float x;
    float y;
    float z;
};

struct arrays
{
    int count;
    float3* velocities;
    float3* translations;
};


DllExport arrays* arrays_new(int count);
DllExport void arrays_delete(arrays* ptr);
DllExport void arrays_update(const arrays* ptr, float delta_time);

DllExport void update_velocity(int count, const float3* accelerations, float3* velocities,
                               float delta_time);
DllExport void update_translation(int count, const float3* velocities, float3* translations,
                                  float delta_time);