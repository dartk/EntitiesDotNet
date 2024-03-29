﻿using EntitiesDotNet;
using Xunit.Abstractions;


namespace EntityComponentSystem.Tests;


public partial class IterationTests
{
    public IterationTests(ITestOutputHelper output)
    {
        this.Output = output;
    }


    public ITestOutputHelper Output { get; }


    private static EntityManager CreateEntityManager()
    {
        var em = new EntityManager();
        for (var i = 0; i < 5; ++i)
        {
            em.CreateEntity(Archetype.Instance<int>());
            em.CreateEntity(Archetype.Instance<int, double>());
        }

        return em;
    }


    #region ForEach

    [Fact]
    public void EntityArraysForEach()
    {
        var em = CreateEntityManager();

        em.Entities.ForEach((in EntityId entityId, ref int i) => i = entityId.Id);
        em.Entities.ForEach((in int i, ref double d) => d = i);

        {
            var (count, ints) = Read<int>.From(em.GetArray(Archetype.Instance<int>()));
            Assert.Equal(5, count);
            Assert.Equal(new[] { 0, 2, 4, 6, 8 }, ints.ToArray());
        }

        {
            var (count, ints, doubles) =
                Read<int, double>.From(em.GetArray(Archetype.Instance<int, double>()));
            Assert.Equal(5, count);
            Assert.Equal(new[] { 1, 3, 5, 7, 9 }, ints.ToArray());
            Assert.Equal(new double[] { 1, 3, 5, 7, 9 }, doubles.ToArray());
        }
    }


    [Fact]
    public void EntityArraysForEachWithIndex()
    {
        var em = CreateEntityManager();

        em.Entities.ForEach((ref int i, int index) => i = index);

        {
            var (count, ints) = Read<int>.From(em.GetArray(Archetype.Instance<int>()));
            Assert.Equal(5, count);
            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, ints.ToArray());
        }

        {
            var (count, ints) = Read<int>.From(em.GetArray(Archetype.Instance<int, double>()));
            Assert.Equal(5, count);
            Assert.Equal(new[] { 5, 6, 7, 8, 9 }, ints.ToArray());
        }
    }

    #endregion


    #region ForEach EntityRef

    [EntityRef]
    public ref partial struct EInt
    {
        public ref readonly EntityId EntityId;
        public ref int Int;
    }


    [EntityRef]
    public ref partial struct EIntDouble
    {
        public ref int Int;
        public ref double Double;
    }


    [Inline.Private]
    private static void EntityRefForEachSystem(EntityArrays entities)
    {
        entities.ForEach((in EInt entity) => entity.Int = entity.EntityId.Id);
        entities.ForEach((in EIntDouble entity) => entity.Double = entity.Int);
    }


    [Fact]
    public void EntityRefForEach()
    {
        var em = CreateEntityManager();
        EntityRefForEachSystem(em.Entities);

        {
            var (count, ints) = Read<int>.From(em.GetArray(Archetype.Instance<int>()));
            Assert.Equal(5, count);
            Assert.Equal(new[] { 0, 2, 4, 6, 8 }, ints.ToArray());
        }

        {
            var (count, ints, doubles) =
                Read<int, double>.From(em.GetArray(Archetype.Instance<int, double>()));
            Assert.Equal(5, count);
            Assert.Equal(new[] { 1, 3, 5, 7, 9 }, ints.ToArray());
            Assert.Equal(new double[] { 1, 3, 5, 7, 9 }, doubles.ToArray());
        }
    }


    [Fact]
    public void EntityRefForEach_Inlined()
    {
        var em = CreateEntityManager();

        EntityRefForEachSystem_Inlined(em.Entities);

        {
            var (count, ints) = Read<int>.From(em.GetArray(Archetype.Instance<int>()));
            Assert.Equal(5, count);
            Assert.Equal(new[] { 0, 2, 4, 6, 8 }, ints.ToArray());
        }

        {
            var (count, ints, doubles) =
                Read<int, double>.From(em.GetArray(Archetype.Instance<int, double>()));
            Assert.Equal(5, count);
            Assert.Equal(new[] { 1, 3, 5, 7, 9 }, ints.ToArray());
            Assert.Equal(new double[] { 1, 3, 5, 7, 9 }, doubles.ToArray());
        }
    }


    [Inline.Private]
    private static void EntityRefForEachWithIndexSystem(EntityArrays entities)
    {
        entities.ForEach((in EInt entity, int index) => entity.Int = index);
        entities.ForEach((in EIntDouble entity, int index) => entity.Double = index);
    }


    [Fact]
    public void EntityRefForEachWithIndex()
    {
        var em = CreateEntityManager();

        EntityRefForEachWithIndexSystem(em.Entities);

        {
            var (count, ints) = Read<int>.From(em.GetArray(Archetype.Instance<int>()));
            Assert.Equal(5, count);
            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, ints.ToArray());
        }

        {
            var (count, ints, doubles) =
                Read<int, double>.From(em.GetArray(Archetype.Instance<int, double>()));
            Assert.Equal(5, count);
            Assert.Equal(new[] { 5, 6, 7, 8, 9 }, ints.ToArray());
            Assert.Equal(new double[] { 0, 1, 2, 3, 4 }, doubles.ToArray());
        }
    }


    [Fact]
    public void EntityRefForEachWithIndex_Inlined()
    {
        var em = CreateEntityManager();

        EntityRefForEachWithIndexSystem_Inlined(em.Entities);

        {
            var (count, ints) = Read<int>.From(em.GetArray(Archetype.Instance<int>()));
            Assert.Equal(5, count);
            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, ints.ToArray());
        }

        {
            var (count, ints, doubles) =
                Read<int, double>.From(em.GetArray(Archetype.Instance<int, double>()));
            Assert.Equal(5, count);
            Assert.Equal(new[] { 5, 6, 7, 8, 9 }, ints.ToArray());
            Assert.Equal(new double[] { 0, 1, 2, 3, 4 }, doubles.ToArray());
        }
    }

    #endregion
}