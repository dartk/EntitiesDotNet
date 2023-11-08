#include "pch.h"
#include "native.h"
#include <cstdlib>

struct translation
{
    float3 value;
};

struct velocity
{
    float3 value;
};

struct acceleration
{
    float3 value;
};


float random()
{
    return static_cast<float>(rand()) / static_cast<float>(RAND_MAX);
}

float3 random3()
{
    return {random(), random(), random()};
}


arrays* arrays_new(int count)
{
    const auto arrays_ptr = new arrays();
    arrays_ptr->count = count;
    arrays_ptr->velocities = new float3[count];
    arrays_ptr->translations = new float3[count];

    for (auto i = 0; i < count; ++i)
    {
        arrays_ptr->velocities[i] = random3();
        arrays_ptr->translations[i] = random3();
    }

    return arrays_ptr;
}


void arrays_delete(arrays* ptr)
{
    delete [] ptr->velocities;
    delete [] ptr->translations;
    delete ptr;
}


void arrays_update(const arrays* ptr, float delta_time)
{
    update_translation(ptr->count, ptr->velocities, ptr->translations, delta_time);
}


void update_velocity(int count, const float3* accelerations, float3* velocities, float delta_time)
{
    for (auto i = 0; i < count; ++i)
    {
        auto& a = accelerations[i];
        auto& v = velocities[i];
        v.x += a.x * delta_time;
        v.y += a.y * delta_time;
        v.z += a.z * delta_time;
    }
}

void update_translation(int count, const float3* velocities, float3* translations, float delta_time)
{
    for (auto i = 0; i < count; ++i)
    {
        auto& v = velocities[i];
        auto& t = translations[i];
        t.x += v.x * delta_time;
        t.y += v.y * delta_time;
        t.z += v.z * delta_time;
    }
}