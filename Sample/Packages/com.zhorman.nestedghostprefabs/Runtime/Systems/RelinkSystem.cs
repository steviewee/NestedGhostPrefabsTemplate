using System;
using Unity.Entities;
using Unity.Collections;
//using Unity.Physics;
using Unity.Burst;
using UnityEngine;
using Unity.NetCode;
using Unity.Transforms;
using System.Linq;
using Unity.Mathematics;
using System.Collections.Generic;
using Unity.Entities.Hybrid.Baking;
using Zhorman.NestedGhostPrefabs.Runtime.Components;
namespace Zhorman.NestedGhostPrefabs.Runtime.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(GhostSimulationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(SpawneeSettingSystem))]
    [UpdateBefore(typeof(GhostGroupSettingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct RelinkSystem : ISystem
    {
        EntityQuery relinkQuery;
        public void OnCreate(ref SystemState state)
        {
            relinkQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<LinkedEntityRelink, Simulate>()
.WithNone<SpawneeIndex,FirstBornSpawnee>()
.Build(ref state);
            state.RequireForUpdate(relinkQuery);
            //   state.RequireAnyForUpdate(prespawnsWithLinksQuery, prespawnQuery);
        }
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> relinkEntities = relinkQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < relinkEntities.Length; i++)
            {
                DynamicBuffer<LinkedEntityRelink> relinks = state.EntityManager.GetBuffer<LinkedEntityRelink>(relinkEntities[i]);
                DynamicBuffer<LinkedEntityGroup> linkedEntityGroup = state.EntityManager.GetBuffer<LinkedEntityGroup>(relinkEntities[i]);
                for (int j = 0; j < relinks.Length; j++)
                {
                    linkedEntityGroup.Add(new LinkedEntityGroup { Value = relinks[j].Value });
                }
                ecb.RemoveComponent<LinkedEntityRelink>(relinkEntities[i]);
            }
            ecb.Playback(state.EntityManager);
        }
    }
}
