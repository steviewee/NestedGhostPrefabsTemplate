using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.NetCode;

namespace Zhorman.NestedGhostPrefabs.Runtime.Components
{
    public struct FirstBorn : IBufferElementData
    {
        public Entity Value;
        /// <summary>
        /// inclusive
        /// </summary>
        public int firstIndex;
        /// <summary>
        /// not inclusive
        /// </summary>
        public int lastIndex;
    }
    public struct FirstBornSpawnee : IComponentData
    {
        /// <summary>
        /// inclusive
        /// </summary>
        public int firstIndex;
        /// <summary>
        /// not inclusive
        /// </summary>
        public int lastIndex;
    }
    public struct Spawnee : IBufferElementData
    {
        public int FirstBornParent;
        public Entity Value;
        public LocalTransform transform;
    }
    /*
    public struct SpawneeSharedRootReference : ISharedComponentData
    {
        public Entity Value;
    }
    public struct PrefabRemap : IBufferElementData
    {
        public Entity Prefab;
        public Entity Instance;
    }
    */

    public struct FakeEntity
    {
        public int Version;
        public int Index;

        public static implicit operator Entity(FakeEntity a)
        {
            return new Entity { Version = a.Version, Index = a.Index }; // Convert int to string
        }

        // Explicit conversion from StructB to StructA
        public static implicit operator FakeEntity(Entity b)
        {
            return new FakeEntity { Version = b.Version, Index = b.Index };
        }
    }
    public struct SpawneeSharedRootReference : ISharedComponentData
    {
        public FakeEntity Prefab;
        public FakeEntity Instance;
    }
    public struct PrefabRemap : IBufferElementData
    {
        public FakeEntity Prefab;
        public FakeEntity Instance;
    }


    public struct SpawneeIndex : IComponentData
    {
        public int firstBornIndex;
        public Entity spawner;
        public int index;
    }
    public struct DestroyAfterGhostBaking : IComponentData
    {

    }
    /*
        public struct SpawneeIndex : IComponentData
    {
        public Entity spawner;
        public int index;
    }
     */
}