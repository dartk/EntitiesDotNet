# EntitiesDotNet

An Entity Component System library for .NET inspired by [Unity Entities](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/index.html)


## Overview

Entities are represented by EntityId.

Entity's data is stored in components.

Combination of components form an Archetype.

Components are stored in parallel arrays. Each Archetype has it's own set of arrays to store components.


## Creating and destroying entities

```c#
```