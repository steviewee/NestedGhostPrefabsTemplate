//using Unity.Physics;
using Unity.Burst;
using UnityEngine;
using Unity.NetCode;
using Unity.Transforms;
using System.Linq;
using Unity.Mathematics;
using Zhorman.NestedGhostPrefabs.Runtime.Components;
using Unity.Entities;
using Unity.Collections;
namespace Zhorman.NestedGhostPrefabs.Runtime.Systems
{
    /*
    [BurstCompile]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    //[UpdateAfter(typeof(JointToGhostJointConversionBakingSystem))]
    [UpdateBefore(typeof(GhostAuthoringBakingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]

    public partial struct PrespawnNestedGhostsSystem : ISystem
    {
        //  EntityQuery prespawnQuery;
        EntityQuery nestedQuery;
        public void OnCreate(ref SystemState state)
        {
            nestedQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<NestedGhostAdditionalData, GhostRoot, LinkedEntityGroup, Simulate>()
.WithNone<SpawneeIndex, Spawnee>()
.Build(ref state);
            state.RequireForUpdate(nestedQuery);
            //   state.RequireAnyForUpdate(prespawnsWithLinksQuery, prespawnQuery);
        }
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> nestedPrefabEntities = nestedQuery.ToEntityArray(Allocator.Temp);
            Debug.Log($"PrefabNestedGhostsSystem, {nestedPrefabEntities.Length}");
            //NativeArray<PrefabToGhostAdditionalData> nestedPrefabAdditionalInfo = nestedQuery.ToComponentDataArray<PrefabToGhostAdditionalData>(Allocator.Temp);

            //NativeHashSet<GhostAuthoringComponentBakingData> 
            for (int i = 0; i < nestedPrefabEntities.Length; i++)
            {
                DynamicBuffer<LinkedEntityGroup> linkedEntityGroup = state.EntityManager.GetBuffer<LinkedEntityGroup>(nestedPrefabEntities[i]);
                for (int j = 1; j < linkedEntityGroup.Length; j++)
                {
                    Entity childGhost = linkedEntityGroup[j].Value;

                    if (state.EntityManager.HasComponent<Simulate>(childGhost))
                    {
                        if (state.EntityManager.HasComponent<GhostAuthoringComponentBakingData>(childGhost))
                        {
                            if (state.EntityManager.HasComponent<Parent>(childGhost))
                            {
                                if (state.EntityManager.HasComponent<NestedGhostAdditionalData>(childGhost))
                                {
                                    NestedGhostAdditionalData NGAD = state.EntityManager.GetComponentData<NestedGhostAdditionalData>(childGhost);
                                    if (NGAD.NetworkedParenting || NGAD.ShouldBeUnParented)
                                    {
                                        ecb.RemoveComponent<Parent>(childGhost);
                                    }
                                }
                                else
                                {

                                    ecb.RemoveComponent<Parent>(childGhost);
                                }
                            }
                            linkedEntityGroup.RemoveAt(j);
                            j--;
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
    */
}
