using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using NGPTemplate.Components;
using NGPTemplate.Misc;
using Zhorman.NestedGhostPrefabs.Runtime.Systems;
using Zhorman.NestedGhostPrefabs.Runtime.Components;

namespace NGPTemplate.Systems
{

    [BurstCompile]
    [UpdateInGroup(typeof(GhostSimulationSystemGroup), OrderFirst = true)]//I am not sure why I have put it into GhostSimulationSystemGroup, and not some other group, but it works, so... don't touch it?
    [UpdateAfter(typeof(GhostGroupSettingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct SetOwningPlayerSystem : ISystem
    {
        EntityQuery requestQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            requestQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<OwningPlayer,Simulate>()
.WithNone<SpawneeIndex>()
.Build(ref state);
            state.RequireForUpdate(requestQuery);
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> requestEntities = requestQuery.ToEntityArray(Allocator.Temp);
            NativeArray<OwningPlayer> requestOwningPlayers = requestQuery.ToComponentDataArray<OwningPlayer>(Allocator.Temp);

            for(int i = 0; i < requestEntities.Length; i++)
            {
                if (requestOwningPlayers[i].PreviousValue!= requestOwningPlayers[i].Value)
                {
                    OwningPlayer requestOwningPlayer = requestOwningPlayers[i];
                    if (requestOwningPlayer.Value == Entity.Null)
                    {
                        continue;
                    }
                    if(requestOwningPlayer.PreviousValue != Entity.Null)
                    {
                        var linkedEntityGroup = state.EntityManager.GetBuffer<LinkedEntityGroup>(requestOwningPlayers[i].PreviousValue);
                        for(int j = 0; j < linkedEntityGroup.Length; j++)
                        {
                            if (linkedEntityGroup[j].Value == requestEntities[i])
                            {
                                linkedEntityGroup.RemoveAt(j);
                                break;
                            }
                        }
                    }
                    requestOwningPlayer.PreviousValue = requestOwningPlayer.Value;
                    ecb.AppendToBuffer<LinkedEntityGroup>(requestOwningPlayer.Value, new LinkedEntityGroup { Value = requestEntities[i] });
                    ecb.SetComponent(requestEntities[i], requestOwningPlayer);
                }
            }


            ecb.Playback(state.EntityManager);

        }

    }
}
