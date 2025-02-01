using Unity.Entities;
using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Transforms;
using Unity.Physics;
namespace NGPTemplate.Components
{
    public struct GameResources : IComponentData
    {
        //Add a dynamicbuffer that maps stuff
        public uint DespawnTicks;
        public uint PolledEventsTicks;
        public float RespawnTime;
        public float SpawnPointBlockRadius;
        public CollisionFilter SpawnPointCollisionFilter;
    }
    public struct GameManagedResources : IComponentData
    {
        public UnityObjectRef<GameObject> NameTagPrefab;
    }


    public struct VfxHitResources : IBufferElementData
    {
        public UnityObjectRef<GameObject> VfxPrefab;
    }

    public struct GameResourcesWeapon : IBufferElementData
    {
        public Entity WeaponPrefab;
    }
}
