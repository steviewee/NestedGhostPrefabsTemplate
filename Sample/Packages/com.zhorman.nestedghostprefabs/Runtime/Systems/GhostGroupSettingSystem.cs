using System;
using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;
using Zhorman.NestedGhostPrefabs.Runtime.Components;
using Unity.NetCode;
namespace Zhorman.NestedGhostPrefabs.Runtime.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(GhostSimulationSystemGroup), OrderFirst = true)]//GhostSimulationSystemGroup))]//PrespawnGhostSystemGroup))]//GhostSimulationSystemGroup
    [UpdateAfter(typeof(RelinkSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct GhostGroupSettingSystem : ISystem
    {
        EntityQuery ghostGroupRequestQuery;
        public void OnCreate(ref SystemState state)
        {
            ghostGroupRequestQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<GhostInstance, GhostGroupRequest>()
.WithNone<SpawneeIndex, GhostGroup>()
.Build(ref state);
            state.RequireForUpdate(ghostGroupRequestQuery);
        }
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> ghostGroupRequestEntities = ghostGroupRequestQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < ghostGroupRequestEntities.Length; i++)
            {
                DynamicBuffer<GhostGroupRequest> ghostGroupRequest = state.EntityManager.GetBuffer<GhostGroupRequest>(ghostGroupRequestEntities[i]);
                DynamicBuffer<GhostGroup> ghostGroup = ecb.AddBuffer<GhostGroup>(ghostGroupRequestEntities[i]);

                for (int j = 0; j < ghostGroupRequest.Length; j++)
                {
                    ghostGroup.Add(new GhostGroup { Value = ghostGroupRequest[j].Value });
                }
                ecb.RemoveComponent<GhostGroupRequest>(ghostGroupRequestEntities[i]);
            }
            ecb.Playback(state.EntityManager);
        }
    }
    [BurstCompile]
    [UpdateInGroup(typeof(GhostSimulationSystemGroup), OrderFirst = true)]//GhostSimulationSystemGroup))]//PrespawnGhostSystemGroup))]//GhostSimulationSystemGroup
    [UpdateAfter(typeof(GhostGroupSettingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct GhostGroupChildSystem : ISystem
    {
        EntityQuery ghostGroupDeleteQuery;
        public void OnCreate(ref SystemState state)
        {
            ghostGroupDeleteQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<GhostInstance, GhostGroupAddChild>()
.WithNone<SpawneeIndex, GhostChildEntity>()
.Build(ref state);
            state.RequireForUpdate(ghostGroupDeleteQuery);
        }
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> ghostGroupDeleteEntities = ghostGroupDeleteQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < ghostGroupDeleteEntities.Length; i++)
            {
                ecb.AddComponent<GhostChildEntity>(ghostGroupDeleteEntities[i]);
                ecb.RemoveComponent<GhostGroupAddChild>(ghostGroupDeleteEntities[i]);
            }
            ecb.Playback(state.EntityManager);
        }
    }
    /*
    [BurstCompile]
    [UpdateInGroup(typeof(PrespawnGhostSystemGroup), OrderLast = true)]//GhostSimulationSystemGroup))]//PrespawnGhostSystemGroup))]//GhostSimulationSystemGroup
    //[UpdateAfter(typeof(GhostReceiveSystem))]
    //[UpdateBefore(typeof(GhostAuthoringBakingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct GhostGroupSettingSystem : ISystem
    {
        EntityQuery ghostGroupRequestQuery;
        public void OnCreate(ref SystemState state)
        {
            ghostGroupRequestQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<GhostInstance, GhostGroupRequest>()
.Build(ref state);
            state.RequireForUpdate(ghostGroupRequestQuery);
        }
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> ghostGroupRequestEntities = ghostGroupRequestQuery.ToEntityArray(Allocator.Temp);
            Debug.Log($"GhostGroupSettingSystem, {ghostGroupRequestEntities.Length}");
            for (int i = 0; i < ghostGroupRequestEntities.Length; i++)
            {
                DynamicBuffer<GhostGroupRequest> ghostGroupRequest = state.EntityManager.GetBuffer<GhostGroupRequest>(ghostGroupRequestEntities[i]);
                DynamicBuffer<GhostGroup> ghostGroup;
                if (state.EntityManager.HasBuffer<GhostGroup>(ghostGroupRequestEntities[i]))
                {
                    ghostGroup = state.EntityManager.GetBuffer<GhostGroup>(ghostGroupRequestEntities[i]);
                    ghostGroup.Clear();
                }
                else
                {
                    ghostGroup = ecb.AddBuffer<GhostGroup>(ghostGroupRequestEntities[i]);
                }

                for (int j = 0; j < ghostGroupRequest.Length; j++)
                {
                    ghostGroup.Add(new GhostGroup { Value = ghostGroupRequest[j].Value });
                }
                ecb.RemoveComponent<GhostGroupRequest>(ghostGroupRequestEntities[i]);
            }
            ecb.Playback(state.EntityManager);
        }
    }
    [BurstCompile]
    [UpdateInGroup(typeof(GhostSimulationSystemGroup),OrderLast =true)]//GhostSimulationSystemGroup))]//PrespawnGhostSystemGroup))]//GhostSimulationSystemGroup
    [UpdateAfter(typeof(GhostGroupSettingSystem))]
    //[UpdateBefore(typeof(GhostAuthoringBakingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct GhostGroupDeletingSystem : ISystem
    {
        EntityQuery ghostGroupDeleteQuery;
        public void OnCreate(ref SystemState state)
        {
            ghostGroupDeleteQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<GhostInstance, GhostGroupDelete>()
.Build(ref state);
            state.RequireForUpdate(ghostGroupDeleteQuery);
        }
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> ghostGroupDeleteEntities = ghostGroupDeleteQuery.ToEntityArray(Allocator.Temp);
            NativeArray<GhostGroupDelete> ghostGroupDeleteInfo = ghostGroupDeleteQuery.ToComponentDataArray<GhostGroupDelete>(Allocator.Temp);
            Debug.Log($"GhostGroupDeletingSystem, {ghostGroupDeleteEntities.Length}");
            for (int i = 0; i < ghostGroupDeleteEntities.Length; i++)
            {
                if (state.EntityManager.HasBuffer<GhostGroup>(ghostGroupDeleteEntities[i]))
                {
                    ecb.RemoveComponent<GhostGroup>(ghostGroupDeleteEntities[i]);
                }
                if (ghostGroupDeleteInfo[i].addGhostChildComponent && !state.EntityManager.HasComponent<GhostChildEntity>(ghostGroupDeleteEntities[i]))
                {
                    ecb.AddComponent<GhostChildEntity>(ghostGroupDeleteEntities[i]);
                }
                
                ecb.RemoveComponent<GhostGroupDelete>(ghostGroupDeleteEntities[i]);
            }
            ecb.Playback(state.EntityManager);
        }
    }
    */
}
