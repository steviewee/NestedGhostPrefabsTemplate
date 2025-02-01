using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using NGPTemplate.Components;
using UnityEngine;
using NGPTemplate.Misc;
using System.Runtime.CompilerServices;
using Zhorman.NestedGhostPrefabs.Runtime.Components;

namespace NGPTemplate.Systems
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    //[UpdateAfter(typeof(PredictedFixedStepSimulationSystemGroup))]
    //[UpdateBefore(typeof(CameraControlSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [BurstCompile]
    public partial struct CameraSetSystem : ISystem
    {
        EntityQuery cameraTargetQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            cameraTargetQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<InputReference, CameraRotationReceiver, LocalTransform, Simulate>()
.WithNone<SpawneeIndex>()
.Build(ref state);
            state.RequireForUpdate<LookDirectionInput>();
            state.RequireForUpdate<LastProcessedLookDirection>();
            //state.RequireForUpdate<LastKnownTargetOrientation>();
            state.RequireForUpdate<MainCameraPivot>();

            state.RequireForUpdate(cameraTargetQuery);
        }
        public bool IsNaN(quaternion quaternion)
        {
            return float.IsNaN(quaternion.value.x) ||
                   float.IsNaN(quaternion.value.y) ||
                   float.IsNaN(quaternion.value.z) ||
                   float.IsNaN(quaternion.value.w);
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> cameraTargerEntities = cameraTargetQuery.ToEntityArray(Allocator.Temp);
            NativeArray<LocalTransform> cameraTargetTransforms = cameraTargetQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            NativeArray<CameraRotationReceiver> cameraTargetRotReceivers = cameraTargetQuery.ToComponentDataArray<CameraRotationReceiver>(Allocator.Temp);
            for (int i = 0; i < cameraTargerEntities.Length; i++)
            {
                LocalTransform cameraTargetTransform = cameraTargetTransforms[i];
                CameraRotationReceiver cameraTargetRotReceiver = cameraTargetRotReceivers[i];
                var inputReferences = state.EntityManager.GetBuffer<InputReference>(cameraTargerEntities[i]);

                for (int j = 0; j < inputReferences.Length; j++)
                {
                    if (inputReferences[j].Prefab == 1)
                    {
                        var LDInput = state.EntityManager.GetComponentData<LookDirectionInput>(inputReferences[j].Value);
                        var LPLD = state.EntityManager.GetComponentData<LastProcessedLookDirection>(inputReferences[j].Value);
                        var cameraPivot = state.EntityManager.GetComponentData<MainCameraPivot>(inputReferences[j].Value);
                        float cameraLookInputX = LDInput.Value.x * 2;
                        float cameraLookInputY = -LDInput.Value.y;

                        if (!LPLD.ready)
                        {
                            continue;
                        }

                        if (cameraPivot.pivot != Entity.Null)
                        {
                            LocalToWorld pivotLocalToWorld = state.EntityManager.GetComponentData<LocalToWorld>(cameraPivot.pivot);
                            if (cameraTargetRotReceiver.receiveRot)
                            {
                                cameraTargetRotReceiver.rotation = LPLD.Value;
                            }
                            if (cameraTargetRotReceiver.receiveOffset)
                            {
                                
                                quaternion localChildRot = math.mul(math.inverse(pivotLocalToWorld.Rotation), cameraTargetRotReceiver.rotation);
                                float3 childEuler = math.Euler(localChildRot);
                                localChildRot = quaternion.Euler(math.clamp(childEuler.x + cameraLookInputY, -1.4f, 1.4f), childEuler.y + cameraLookInputX, 0f);
                                cameraTargetRotReceiver.rotation = math.mul(pivotLocalToWorld.Rotation, localChildRot);

                            }
                            ecb.SetComponent(cameraTargerEntities[i], cameraTargetRotReceiver);
                            cameraTargetTransform.Rotation = cameraTargetRotReceiver.rotation;
                            ecb.SetComponent(cameraTargerEntities[i], cameraTargetTransform);
                        }
                        else
                        {
                            if (cameraTargetRotReceiver.receiveRot)
                            {
                                cameraTargetRotReceiver.rotation = LPLD.Value;
                                cameraTargetTransform.Rotation = cameraTargetRotReceiver.rotation;
                                ecb.SetComponent(cameraTargerEntities[i], cameraTargetTransform);
                            }
                            if (cameraTargetRotReceiver.receiveOffset)
                            {
                                float rotationX = math.Euler(cameraTargetTransform.Rotation).y + cameraLookInputX;
                                float rotationY = math.Euler(cameraTargetTransform.Rotation).x + cameraLookInputY;
                                cameraTargetTransform.Rotation = math.mul(cameraTargetTransform.Rotation, quaternion.Euler(new float3(rotationY, rotationX, 0)));
                                ecb.SetComponent(cameraTargerEntities[i], cameraTargetTransform);
                            }
                        }
                        break;
                    }
                }
            }
            ecb.Playback(state.EntityManager);
        }
    }
}
