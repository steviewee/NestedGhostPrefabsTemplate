
using System;
using Unity.Entities;
using Unity.Collections;
using Zhorman.NestedGhostPrefabs.Runtime.Systems;
//using Unity.Physics;
using Unity.Burst;
using UnityEngine;
using Unity.NetCode;
using Unity.Transforms;
using System.Linq;
using Unity.Mathematics;
using Zhorman.NestedGhostPrefabs.Runtime.Components;
namespace Zhorman.NestedGhostPrefabs.Runtime.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    [UpdateBefore(typeof(GhostAuthoringBakingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    
    public partial struct PrefabNestedGhostsSystem : ISystem
    {
        //  EntityQuery prespawnQuery;
        EntityQuery nestedPrefabQuery;
        public void OnCreate(ref SystemState state)
        {
            nestedPrefabQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<NestedGhostAdditionalData, GhostRoot, LinkedEntityGroup, Simulate, Prefab>().WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
.WithNone<FirstBorn, SpawneeIndex, Spawnee>()
.Build(ref state);
            state.RequireForUpdate(nestedPrefabQuery);
            //   state.RequireAnyForUpdate(prespawnsWithLinksQuery, prespawnQuery);
        }
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> nestedPrefabEntities = nestedPrefabQuery.ToEntityArray(Allocator.Temp);
            Debug.Log($"PrefabNestedGhostsSystem, {nestedPrefabEntities.Length}");


            //NativeArray<PrefabToGhostAdditionalData> nestedPrefabAdditionalInfo = nestedPrefabQuery.ToComponentDataArray<PrefabToGhostAdditionalData>(Allocator.Temp);

            //NativeHashSet<GhostAuthoringComponentBakingData> 
            for (int i = 0; i < nestedPrefabEntities.Length; i++)
            {
                DynamicBuffer<LinkedEntityGroup> linkedEntityGroup = state.EntityManager.GetBuffer<LinkedEntityGroup>(nestedPrefabEntities[i]);

                for (int j = 1; j < linkedEntityGroup.Length; j++)
                {
                    Entity entity = linkedEntityGroup[j].Value;

                    if (state.EntityManager.HasComponent<Simulate>(entity))
                    {
                        if (state.EntityManager.HasComponent<GhostAuthoringComponentBakingData>(entity))
                        {
                            if (state.EntityManager.HasComponent<NestedGhostAdditionalData>(entity))
                            {
                                NestedGhostAdditionalData NGAD = state.EntityManager.GetComponentData<NestedGhostAdditionalData>(entity);
                                if (state.EntityManager.HasComponent<Parent>(entity))
                                {
                                    if (NGAD.NetworkedParenting || NGAD.ShouldBeUnParented)
                                    {
                                        ecb.RemoveComponent<Parent>(entity);
                                    }
                                }

                                DynamicBuffer<LinkedEntityGroup> linkedEntityGroup2 = state.EntityManager.GetBuffer<LinkedEntityGroup>(entity);

                                for (int u = 1; u < linkedEntityGroup2.Length; u++)
                                {
                                    if (state.EntityManager.HasComponent<Simulate>(entity))
                                    {
                                        //Debug.Log($"Has simulante");
                                        if (state.EntityManager.HasComponent<GhostAuthoringComponentBakingData>(linkedEntityGroup2[u].Value))
                                        {
                                            linkedEntityGroup2.RemoveAt(u);
                                            u--;
                                        }
                                    }
                                    else
                                    {
                                        linkedEntityGroup2.RemoveAt(u);
                                        u--;
                                    }
                                }
                                linkedEntityGroup.RemoveAt(j);
                                j--;
                            }
                            else
                            {
                                linkedEntityGroup.RemoveAt(j);
                                j--;
                            }
                        }
                    }
                    else
                    {
                        linkedEntityGroup.RemoveAt(j);
                        j--;
                    }
                }
            }
            ecb.Playback(state.EntityManager);
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    //[UpdateAfter(typeof(JointToGhostJointConversionBakingSystem))]
    [UpdateBefore(typeof(PrefabNestedGhostsSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    
    public partial struct SpawnerSystem : ISystem
    {
        EntityQuery spawnerQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            spawnerQuery = new EntityQueryBuilder(Allocator.Temp)
        .WithAll<FirstBorn>()
        .Build(ref state);
            state.RequireForUpdate(spawnerQuery);
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            EntityCommandBuffer ecb = new(Allocator.Temp);

            NativeArray<Entity> spawnerEntities = spawnerQuery.ToEntityArray(Allocator.Temp);
            Debug.Log($"SpawnerPreparingSystem, spawnerCount = {spawnerEntities.Length}");
            for (int i = 0; i < spawnerEntities.Length; i++)
            {
                DynamicBuffer<FirstBorn> firstBornGroup = state.EntityManager.GetBuffer<FirstBorn>(spawnerEntities[i]);
                DynamicBuffer<Spawnee> spawnees = state.EntityManager.GetBuffer<Spawnee>(spawnerEntities[i]);
                for (int j = 0; j < firstBornGroup.Length; j++)
                {
                    int firstIndex = spawnees.Length;
                    if (state.EntityManager.HasComponent<Simulate>(firstBornGroup[j].Value))
                    {
                        if (state.EntityManager.HasComponent<GhostAuthoringComponentBakingData>(firstBornGroup[j].Value))
                        {
                            Debug.Log($"SpawnerPreparingSystem, firstborn {j} is  a ghost!");
                            LocalTransform localTransform = new LocalTransform { Position = new float3(float.PositiveInfinity), Rotation = new quaternion(new float4(float.PositiveInfinity)) };
                            if (state.EntityManager.HasComponent<LocalTransform>(firstBornGroup[j].Value))
                            {
                                localTransform = state.EntityManager.GetComponentData<LocalTransform>(firstBornGroup[j].Value);
                            }
                            spawnees.Add(new Spawnee { Value = firstBornGroup[j].Value, FirstBornParent = j, transform = localTransform });

                            if (state.EntityManager.HasBuffer<LinkedEntityGroup>(firstBornGroup[j].Value))
                            {
                                DynamicBuffer<LinkedEntityGroup> linkedEntityGroup = state.EntityManager.GetBuffer<LinkedEntityGroup>(firstBornGroup[j].Value);
                                if (linkedEntityGroup.Length > 1)
                                {
                                    ecb.AddComponent(firstBornGroup[j].Value, new SpawneeIndex { firstBornIndex = j, spawner = spawnerEntities[i], index = spawnees.Length });
                                }

                                for (int k = 1; k < linkedEntityGroup.Length; k++)
                                {
                                    Entity entity = linkedEntityGroup[k].Value;
                                    if (state.EntityManager.HasComponent<Simulate>(entity))
                                    {
                                        //Debug.Log($"Has simulante");
                                        if (state.EntityManager.HasComponent<GhostAuthoringComponentBakingData>(entity))
                                        {
                                            //Debug.Log($"Has GhostAuthoringComponentBakingData");
                                            if (state.EntityManager.HasComponent<NestedGhostAdditionalData>(entity))
                                            {
                                                NestedGhostAdditionalData NGAD = state.EntityManager.GetComponentData<NestedGhostAdditionalData>(entity);
                                                if (state.EntityManager.HasComponent<Parent>(entity))
                                                {
                                                    if (NGAD.NetworkedParenting || NGAD.ShouldBeUnParented)
                                                    {
                                                        ecb.RemoveComponent<Parent>(entity);
                                                    }
                                                }
                                                localTransform = new LocalTransform { Position = new float3(float.PositiveInfinity), Rotation = new quaternion(new float4(float.PositiveInfinity)) };//???
                                                if (state.EntityManager.HasComponent<LocalTransform>(entity))
                                                {
                                                    localTransform = state.EntityManager.GetComponentData<LocalTransform>(entity);
                                                }
                                                ecb.AddComponent(entity, new SpawneeIndex { firstBornIndex=j, spawner = spawnerEntities[i], index = spawnees.Length });
                                                spawnees.Add(new Spawnee { Value = entity, FirstBornParent = j, transform = localTransform });

                                                DynamicBuffer<LinkedEntityGroup> linkedEntityGroup2 = state.EntityManager.GetBuffer<LinkedEntityGroup>(entity);

                                                for (int u = 1; u < linkedEntityGroup2.Length; u++)
                                                {
                                                    if (state.EntityManager.HasComponent<Simulate>(entity))
                                                    {
                                                        //Debug.Log($"Has simulante");
                                                        if (state.EntityManager.HasComponent<GhostAuthoringComponentBakingData>(linkedEntityGroup2[u].Value))
                                                        {
                                                            linkedEntityGroup2.RemoveAt(u);
                                                            u--;
                                                        }
                                                        else if (!NGAD.Unnested)
                                                        {
                                                            //ecb.AddComponent<DestroyAfterGhostBaking>(linkedEntityGroup2[u].Value);
                                                            //   ecb.SetComponentEnabled(linkedEntityGroup2[u].Value, typeof(Simulate),false);
                                                           //if (state.EntityManager.HasComponent<Parent>(linkedEntityGroup2[u].Value))
                                                            //{
                                                            //    ecb.RemoveComponent<Parent>(linkedEntityGroup2[u].Value);
                                                            //}
                                                            //ecb.DestroyEntity(linkedEntityGroup2[u].Value);

                                                            //linkedEntityGroup2.RemoveAt(u);
                                                            //u--;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        linkedEntityGroup2.RemoveAt(u);
                                                        u--;
                                                    }
                                                }
                                                linkedEntityGroup.RemoveAt(k);
                                                k--;
                                            }
                                            else
                                            {
                                                if (state.EntityManager.HasComponent<Parent>(entity))
                                                {
                                                    ecb.RemoveComponent<Parent>(entity);
                                                }
                                                localTransform = new LocalTransform { Position = new float3(float.PositiveInfinity), Rotation = new quaternion(new float4(float.PositiveInfinity)) };//???
                                                if (state.EntityManager.HasComponent<LocalTransform>(entity))
                                                {
                                                    localTransform = state.EntityManager.GetComponentData<LocalTransform>(entity);
                                                }
                                                ecb.AddComponent(entity, new SpawneeIndex { firstBornIndex=j, spawner = spawnerEntities[i], index = spawnees.Length });
                                                spawnees.Add(new Spawnee { Value = entity, FirstBornParent = j, transform = localTransform });

                                                DynamicBuffer<LinkedEntityGroup> linkedEntityGroup2 = state.EntityManager.GetBuffer<LinkedEntityGroup>(entity);

                                                for (int u = 1; u < linkedEntityGroup2.Length; u++)
                                                {
                                                    if (state.EntityManager.HasComponent<Simulate>(entity))
                                                    {
                                                        //Debug.Log($"Has simulante");
                                                        if (state.EntityManager.HasComponent<GhostAuthoringComponentBakingData>(linkedEntityGroup2[u].Value))
                                                        {
                                                            linkedEntityGroup2.RemoveAt(u);
                                                            u--;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        linkedEntityGroup2.RemoveAt(u);
                                                        u--;
                                                    }
                                                }
                                                linkedEntityGroup.RemoveAt(k);
                                                k--;
                                            }
                                        }
                                        else
                                        {
                                            if (state.EntityManager.HasComponent<Parent>(entity))
                                            {
                                                if(state.EntityManager.GetComponentData<Parent>(entity).Value!= firstBornGroup[j].Value)
                                                {
                                                    linkedEntityGroup.RemoveAt(k);
                                                    k--;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        linkedEntityGroup.RemoveAt(k);
                                        k--;
                                    }
                                }
                            }
                            else
                            {
                                Debug.Log($"SpawnerPreparingSystem, firstborn {j} has no LinkedEntityGroup...");
                            }
                        }
                        else
                        {
                            Debug.Log($"SpawnerPreparingSystem, firstborn {j} is not a ghost!, Kys!");
                            /*
                            LocalTransform localTransform = new LocalTransform { Position = new float3(float.PositiveInfinity), Rotation = new quaternion(new float4(float.PositiveInfinity)) };
                            if (state.EntityManager.HasComponent<LocalTransform>(firstBornGroup[j].Value))
                            {
                                localTransform = state.EntityManager.GetComponentData<LocalTransform>(firstBornGroup[j].Value);
                            }
                            ecb.AddSharedComponent(firstBornGroup[j].Value, new SpawneeData { firstBornParent = j });
                            ecb.AddComponent(firstBornGroup[j].Value, new SpawneeIndex { spawner = spawnerEntities[i], index = spawnees.Length });
                            spawnees.Add(new Spawnee { Value = firstBornGroup[j].Value, FirstBornParent = j, transform = localTransform });

                            if (state.EntityManager.HasBuffer<LinkedEntityGroup>(firstBornGroup[j].Value))
                            {
                                Debug.Log($"SpawnerPreparingSystem, firstborn {j} has LinkedEntityGroup!");
                                DynamicBuffer<LinkedEntityGroup> linkedEntityGroup = state.EntityManager.GetBuffer<LinkedEntityGroup>(firstBornGroup[j].Value);
                                for (int k = 1; k < linkedEntityGroup.Length; k++)
                                {
                                    Entity entity = linkedEntityGroup[k].Value;
                                    if (state.EntityManager.HasComponent<Simulate>(entity))
                                    {
                                        if (state.EntityManager.HasComponent<GhostAuthoringComponentBakingData>(entity))
                                        {
                                            if (state.EntityManager.HasComponent<Parent>(entity))
                                            {
                                                ecb.RemoveComponent<Parent>(entity);
                                            }
                                            localTransform = new LocalTransform { Position = new float3(float.PositiveInfinity), Rotation = new quaternion(new float4(float.PositiveInfinity)) };//???
                                            if (state.EntityManager.HasComponent<LocalTransform>(entity))
                                            {
                                                localTransform = state.EntityManager.GetComponentData<LocalTransform>(entity);
                                            }
                                            ecb.AddSharedComponent(entity, new SpawneeData { firstBornParent = j });
                                            ecb.AddComponent(entity, new SpawneeIndex { spawner = spawnerEntities[i], index = spawnees.Length });
                                            spawnees.Add(new Spawnee { Value = entity, FirstBornParent = j, transform = localTransform });
                                            linkedEntityGroup.RemoveAt(k);
                                            k--;
                                        }
                                    }
                                    else
                                    {
                                        linkedEntityGroup.RemoveAt(k);
                                        k--;
                                    }
                                }
                            }
                            else
                            {
                                Debug.Log($"SpawnerPreparingSystem, firstborn {j} has no LinkedEntityGroup...");
                            }
                            */
                        }
                    }
                    else
                    {
                        Debug.Log($"not Simulated, {j}");
                    }
                    int lastIndex = spawnees.Length;
                    firstBornGroup[j] = new FirstBorn { Value = firstBornGroup[j].Value, firstIndex = firstIndex, lastIndex = lastIndex };
                    ecb.AddComponent(firstBornGroup[j].Value, new FirstBornSpawnee { firstIndex = firstIndex, lastIndex = lastIndex });
                }
            }
            ecb.Playback(state.EntityManager);
        }
    }
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct DestroyNestedGhostChildren : ISystem
    {
        EntityQuery targetQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            targetQuery = new EntityQueryBuilder(Allocator.Temp)
        .WithAll<DestroyAfterGhostBaking,Prefab>().WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
        .Build(ref state);
            //state.RequireForUpdate(targetQuery);
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            EntityCommandBuffer ecb = new(Allocator.Temp);

            NativeArray<Entity> targetEntities = targetQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < targetEntities.Length; i++)
            {
                ecb.DestroyEntity(targetEntities[i]);
            }
            ecb.Playback(state.EntityManager);
        }
    }
    /*
    [BurstCompile]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    [UpdateAfter(typeof(GhostAuthoringBakingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct DestroyNestedGhostChildren : ISystem
    {
        EntityQuery targetQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            targetQuery = new EntityQueryBuilder(Allocator.Temp)
        .WithAll<DestroyAfterGhostBaking>().WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
        .Build(ref state);
            //state.RequireForUpdate(targetQuery);
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            EntityCommandBuffer ecb = new(Allocator.Temp);

            NativeArray<Entity> targetEntities = targetQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < targetEntities.Length; i++)
            {
                ecb.DestroyEntity(targetEntities[i]);
            }
            ecb.Playback(state.EntityManager);
        }
    }
    */
    /*
    [BurstCompile]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    [UpdateAfter(typeof(GhostAuthoringBakingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct DestroyNestedGhostChildren : ISystem
    {
        EntityQuery targetQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            targetQuery = new EntityQueryBuilder(Allocator.Temp)
        .WithAll<DestroyAfterGhostBaking>()
        .Build(ref state);
            //state.RequireForUpdate(targetQuery);
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            EntityCommandBuffer ecb = new(Allocator.Temp);

            NativeArray<Entity> targetEntities = targetQuery.ToEntityArray(Allocator.Temp);
            Debug.Log($"???, {targetEntities.Length}");
            for (int i = 0; i < targetEntities.Length; i++)
            {
                ecb.DestroyEntity(targetEntities[i]);
            }
            ecb.Playback(state.EntityManager);
        }
    }
    */
}



