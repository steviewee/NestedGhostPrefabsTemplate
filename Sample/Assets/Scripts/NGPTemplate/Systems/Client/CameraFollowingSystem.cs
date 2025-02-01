using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using NGPTemplate.Systems;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using NGPTemplate.Components;
using Zhorman.NestedGhostPrefabs.Runtime.Components;
#if UNITY_SERVER && !UNITY_EDITOR
using Unity.Networking.Transport;
#endif
//
namespace NGPTemplate.Systems
{

    /*
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    public partial struct CameraPivotingSystem : ISystem
    {
        EntityQuery cameraQuery;
        EntityQuery cameraTargetQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            cameraQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<MainCameraPivot, OwningPlayer, LocalTransform>()
.Build(ref state);
            cameraTargetQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<CameraRotationReceiver, OwningPlayer, LocalTransform>()
.Build(ref state);
            state.RequireForUpdate(cameraQuery);
            state.RequireForUpdate(cameraTargetQuery);
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> cameraEntities = cameraQuery.ToEntityArray(Allocator.Temp);
            NativeArray<MainCameraPivot> cameraPivot = cameraQuery.ToComponentDataArray<MainCameraPivot>(Allocator.Temp);
            NativeArray<LocalTransform> cameraTransform = cameraQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            NativeArray<OwningPlayer> cameraOwners = cameraQuery.ToComponentDataArray<OwningPlayer>(Allocator.Temp);
            for (int i = 0; i < cameraEntities.Length; i++)
            {
                var playerOwnedEntities = state.EntityManager.GetBuffer<LinkedEntityGroup>(cameraOwners[i].Value).Reinterpret<Entity>().ToNativeArray(Allocator.Temp);
                LocalTransform newCameraTransform = cameraTransform[i];


                for (int j = 0; j < playerOwnedEntities.Length; j++)
                {

                    if (cameraTargetQuery.Matches(playerOwnedEntities[j]))
                    {
                        LocalTransform receiverTransform = state.EntityManager.GetComponentData<LocalTransform>(playerOwnedEntities[j]);
                        newCameraTransform.Position = receiverTransform.Position;

                        MainCameraPivot newCameraPivot = cameraPivot[i];
                        if (cameraPivot[i].forceNoPivot)
                        {
                            newCameraPivot.pivot = Entity.Null;
                        }
                        else
                        {
                            //Entity cameraRotationReceiverEntity = cameraTargetQuery.ToEntityArray(Allocator.Temp)[0];
                            CameraRotationReceiver cameraRotationReceiver = cameraTargetQuery.ToComponentDataArray<CameraRotationReceiver>(Allocator.Temp)[0];
                            newCameraPivot.pivot = cameraRotationReceiver.pivot;//cameraRotationReceiverEntity;
                        }

                        ecb.SetComponent(cameraEntities[i], newCameraTransform);
                        ecb.SetComponent(cameraEntities[i], newCameraPivot);
                    } 
                }
            };


            ecb.Playback(state.EntityManager);

        }

    }
    */
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    public partial struct CameraPivotingSystem : ISystem
    {
        EntityQuery cameraTargetQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            cameraTargetQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<CameraRotationReceiver, LocalTransform,InputReference,Simulate>()
.WithNone<SpawneeIndex>()
.Build(ref state);
            state.RequireForUpdate<MainCameraPivot>();
            state.RequireForUpdate(cameraTargetQuery);
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);

            NativeArray<Entity> cameraTargetEntities = cameraTargetQuery.ToEntityArray(Allocator.Temp);
            NativeArray<CameraRotationReceiver> cameraTargetReceiver = cameraTargetQuery.ToComponentDataArray<CameraRotationReceiver>(Allocator.Temp);
            NativeArray<LocalTransform> cameraTargetTransform = cameraTargetQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            for (int i = 0; i < cameraTargetEntities.Length; i++)
            {
                var inputReferences = state.EntityManager.GetBuffer<InputReference>(cameraTargetEntities[i]);

                for (int j = 0; j< inputReferences.Length; j++)
                {
                    if (inputReferences[j].Prefab == 1)
                    {
                        MainCameraPivot pivot = state.EntityManager.GetComponentData<MainCameraPivot>(inputReferences[j].Value);
                        LocalTransform newCameraTransform = state.EntityManager.GetComponentData<LocalTransform>(inputReferences[j].Value);
                        newCameraTransform.Position = cameraTargetTransform[i].Position;

                        MainCameraPivot newCameraPivot = pivot;
                        if (newCameraPivot.forceNoPivot)
                        {
                            newCameraPivot.pivot = Entity.Null;
                        }
                        else
                        {
                            newCameraPivot.pivot = cameraTargetReceiver[i].pivot;//cameraRotationReceiverEntity;
                        }

                        ecb.SetComponent(inputReferences[j].Value, newCameraTransform);
                        ecb.SetComponent(inputReferences[j].Value, newCameraPivot);
                        break;
                    }
                }
            }




            ecb.Playback(state.EntityManager);

        }

    }
}
/*
EntityCommandBuffer ecb = new(Allocator.Temp);
NativeArray<Entity> cameraEntities = cameraQuery.ToEntityArray(Allocator.Temp);
NativeArray<MainCameraPivot> cameraPivot = cameraQuery.ToComponentDataArray<MainCameraPivot>(Allocator.Temp);
NativeArray<LocalTransform> cameraTransform = cameraQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
for (int i = 0; i < cameraEntities.Length; i++)
{
    if(cameraPivot.)
};


ecb.Playback(state.EntityManager);
            */