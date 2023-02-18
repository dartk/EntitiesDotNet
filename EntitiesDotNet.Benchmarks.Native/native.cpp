#include "pch.h"
#include "native.h"
#include "entt.hpp"

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


void* entt_create_registry()
{
    return new entt::registry();
}


void entt_destroy_registry(void* registry)
{
    const auto ptr = static_cast<entt::registry*>(registry);
    delete ptr;
}


void entt_create_entities(void* registry_ptr, int count)
{
    const auto registry = static_cast<entt::registry*>(registry_ptr);

    for (auto i = 0; i < count; ++i)
    {
        const auto entity = registry->create();
        registry->emplace<translation>(entity, random3());
        registry->emplace<velocity>(entity, random3());
    }

    for (auto i = 0; i < count; ++i)
    {
        const auto entity = registry->create();
        registry->emplace<velocity>(entity, random3());
        registry->emplace<acceleration>(entity, random3());
    }

    for (auto i = 0; i < count; ++i)
    {
        const auto entity = registry->create();
        registry->emplace<translation>(entity, random3());
        registry->emplace<velocity>(entity, random3());
        registry->emplace<acceleration>(entity, random3());
    }
}


void entt_system_update_velocity(void* registry_ptr, float delta_time)
{
    const auto registry = static_cast<entt::registry*>(registry_ptr);

    registry->group<acceleration, velocity>()
            .each([=](const auto& a, auto& v)
            {
                v.value.x += a.value.x * delta_time;
                v.value.y += a.value.y * delta_time;
                v.value.z += a.value.z * delta_time;
            });
}

void entt_system_update_velocity_and_translation(void* registry_ptr, float delta_time)
{
    const auto registry = static_cast<entt::registry*>(registry_ptr);

    registry->group<acceleration, velocity>()
            .each([=](const auto& a, auto& v)
            {
                v.value.x += a.value.x * delta_time;
                v.value.y += a.value.y * delta_time;
                v.value.z += a.value.z * delta_time;
            });

    registry->group<translation, velocity>()
            .each([=](auto& t, auto& v)
            {
                t.value.x += v.value.x * delta_time;
                t.value.y += v.value.y * delta_time;
                t.value.z += v.value.z * delta_time;
            });
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
