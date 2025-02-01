using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using Unity.Burst;
using Zhorman.NestedGhostPrefabs.Runtime.Components;

namespace Zhorman.NestedGhostPrefabs.Runtime.Systems
{

    /// <summary>
    /// System to change the parent with a delay
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    //[UpdateAfter(typeof(Skinwal))]
    public partial struct DesiredParentSystem : ISystem
    {
        private EntityQuery desiredParentQuery;
        private EntityQuery desiredParentWithParentQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            desiredParentQuery = new EntityQueryBuilder(Allocator.Temp)
    .WithAll<DesiredParent>().WithNone<Parent>()
    .Build(ref state);
            desiredParentWithParentQuery = new EntityQueryBuilder(Allocator.Temp)
    .WithAll<DesiredParent, Parent>()
    .Build(ref state);
            NativeArray<EntityQuery> entityQueries = new NativeArray<EntityQuery>(2, Allocator.Temp);
            entityQueries[0] = desiredParentQuery;
            entityQueries[1] = desiredParentWithParentQuery;
            state.RequireAnyForUpdate(entityQueries);
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> desiredParentEntities = desiredParentQuery.ToEntityArray(Allocator.Temp);
            NativeArray<DesiredParent> desiredParentData = desiredParentQuery.ToComponentDataArray<DesiredParent>(Allocator.Temp);
            NativeArray<Entity> desiredParentWithParentEntities = desiredParentWithParentQuery.ToEntityArray(Allocator.Temp);
            NativeArray<DesiredParent> desiredParentWithParentData = desiredParentWithParentQuery.ToComponentDataArray<DesiredParent>(Allocator.Temp);
            NativeArray<Parent> parentData = desiredParentWithParentQuery.ToComponentDataArray<Parent>(Allocator.Temp);

            for (int i = 0; i < desiredParentEntities.Length; i++)
            {
                if (desiredParentData[i].NextParent == desiredParentEntities[i])
                {
                    continue;
                }
                if(desiredParentData[i].NextParent == Entity.Null)
                {
                    continue;
                }
                ecb.AddComponent(desiredParentEntities[i], new Parent { Value = desiredParentData[i].NextParent });
            }
            for (int i = 0; i < desiredParentWithParentEntities.Length; i++)
            {
                if (desiredParentWithParentData[i].NextParent == desiredParentWithParentEntities[i])
                {
                    //ecb.RemoveComponent<Parent>(desiredParentWithParentEntities[i]);
                    continue;
                }
                if (desiredParentWithParentData[i].NextParent == Entity.Null)
                {
                    ecb.RemoveComponent<Parent>(desiredParentWithParentEntities[i]);
                    continue;
                }
                if(desiredParentWithParentData[i].NextParent!= parentData[i].Value)
                {
                    ecb.SetComponent(desiredParentWithParentEntities[i], new Parent { Value = desiredParentWithParentData[i].NextParent });
                }
            }
            ecb.Playback(state.EntityManager);
        }
    }
}
/*
[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
//[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
//[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
public partial struct DesiredParentSystem : ISystem
{
    private EntityQuery desiredParentQuery;
    private EntityQuery desiredParentWithParentQuery;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        desiredParentQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<DesiredParent>().WithNone<Parent>()
.Build(ref state);
        desiredParentWithParentQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<DesiredParent, Parent>()
.Build(ref state);
        NativeArray<EntityQuery> entityQueries = new NativeArray<EntityQuery>(2, Allocator.Temp);
        entityQueries[0] = desiredParentQuery;
        entityQueries[1] = desiredParentWithParentQuery;
        state.RequireAnyForUpdate(entityQueries);
    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Allocator.Temp);
        NativeArray<Entity> desiredParentEntities = desiredParentQuery.ToEntityArray(Allocator.Temp);
        NativeArray<DesiredParent> desiredParentData = desiredParentQuery.ToComponentDataArray<DesiredParent>(Allocator.Temp);
        NativeArray<Entity> desiredParentWithParentEntities = desiredParentWithParentQuery.ToEntityArray(Allocator.Temp);
        NativeArray<DesiredParent> desiredParentWithParentData = desiredParentWithParentQuery.ToComponentDataArray<DesiredParent>(Allocator.Temp);
        NativeArray<Parent> parentData = desiredParentWithParentQuery.ToComponentDataArray<Parent>(Allocator.Temp);

        for (int i = 0; i < desiredParentEntities.Length; i++)
        {
            if (desiredParentData[i].NextParent == Entity.Null)
            {

            }
            else
            {

            }
        }
        for (int i = 0; i < desiredParentWithParentEntities.Length; i++)
        {
            if (desiredParentWithParentData[i].NextParent == Entity.Null)
            {
                if (desiredParentWithParentData[i].shouldBeUnParented)
                {
                    if (parentData[i].Value != Entity.Null)
                    {
                        ecb.RemoveComponent<Parent>(desiredParentWithParentEntities[i]);
                        //ecb.SetComponent(desiredParentWithParentEntities[i], new Parent { Value = Entity.Null });
                    }
                }
                else if(parentData[i].Value !=)
            }
            else
            {

            }
        }
        ecb.Playback(state.EntityManager);
    }
}
*/
/*
[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
//[UpdateAfter(typeof(Skinwal))]
public partial struct DesiredParentSystem : ISystem
{
    private EntityQuery desiredParentQuery;
    private EntityQuery desiredParentWithParentQuery;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        desiredParentQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<DesiredParent>().WithNone<Parent>()
.Build(ref state);
        desiredParentWithParentQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<DesiredParent,Parent>()
.Build(ref state);
        NativeArray<EntityQuery> entityQueries = new NativeArray<EntityQuery>(2, Allocator.Temp);
        entityQueries[0] = desiredParentQuery;
        entityQueries[1] = desiredParentWithParentQuery;
        state.RequireAnyForUpdate(entityQueries);
    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Allocator.Temp);
        NativeArray<Entity> desiredParentEntities = desiredParentQuery.ToEntityArray(Allocator.Temp);
        NativeArray<DesiredParent> desiredParentData = desiredParentQuery.ToComponentDataArray<DesiredParent>(Allocator.Temp);
        NativeArray<Entity> desiredParentWithParentEntities = desiredParentWithParentQuery.ToEntityArray(Allocator.Temp);
        NativeArray<DesiredParent> desiredParentWithParentData = desiredParentWithParentQuery.ToComponentDataArray<DesiredParent>(Allocator.Temp);


        for (int i = 0; i < desiredParentEntities.Length; i++)
        {
            ecb.AddComponent(desiredParentEntities[i], new Parent { Value = desiredParentData[i].Value });
        }
        for (int i = 0; i < desiredParentWithParentEntities.Length; i++)
        {
            ecb.SetComponent(desiredParentWithParentEntities[i], new Parent { Value = desiredParentWithParentData[i].Value });
        }
        ecb.Playback(state.EntityManager);
    }
}
*/