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
using Unity.Mathematics;

namespace NGPTemplate.Systems
{

    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct SimpleMovementSystem : ISystem
    {
        EntityQuery bodyQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            bodyQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<LocalTransform, LocalToWorld, BodyTag,InputReference, Simulate>()
.WithNone<SpawneeIndex>()
.Build(ref state);
            state.RequireForUpdate(bodyQuery);
            state.RequireForUpdate<MovementInput>();
        }
        [GenerateTestsForBurstCompatibility]
        public static float3 ProjectOnPlane(float3 vector, float3 planeNormal)
        {
            float num = math.dot(planeNormal, planeNormal);
            if (num < math.EPSILON)
            {
                return vector;
            }

            float num2 = math.dot(vector, planeNormal);
            return new float3(vector.x - planeNormal.x * num2 / num, vector.y - planeNormal.y * num2 / num, vector.z - planeNormal.z * num2 / num);
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> entities = bodyQuery.ToEntityArray(Allocator.Temp);
            NativeArray<LocalToWorld> bodyLtWs = bodyQuery.ToComponentDataArray<LocalToWorld>(Allocator.Temp);
            NativeArray<LocalTransform> bodyTransforms = bodyQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            NativeArray<BodyTag> bodyTags = bodyQuery.ToComponentDataArray<BodyTag>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                if (bodyTags[i].head != null)
                {
                    var inputRefs = state.EntityManager.GetBuffer<InputReference>(entities[i]);
                    for (int j = 0; j < inputRefs.Length; j++)
                    {
                        if (inputRefs[j].Prefab == 0)
                        {
                            BodyTag bodyTag = bodyTags[i];
                            var headLtW = state.EntityManager.GetComponentData<LocalToWorld>(bodyTag.head);
                            var input = state.EntityManager.GetComponentData<MovementInput>(inputRefs[j].Value);
                            LocalTransform bodyTransform = bodyTransforms[i];
                            LocalToWorld bodyLtW = bodyLtWs[j];
                            float4x4 bodyWtL = math.inverse(bodyLtWs[i].Value);

  

                            float3 headPlanarDirectionForward = math.normalize(ProjectOnPlane(math.normalize(math.mul(headLtW.Value.Rotation(), new float3(0f, 0f, 1f))), math.normalize(math.mul(bodyLtW.Value.Rotation(), new float3(0f, 1f, 0f)))));//can be optimized
                            quaternion headPlanarRotation = quaternion.LookRotation(headPlanarDirectionForward, bodyLtW.Up);
                            float currentSpeed = bodyTag.currentMoveSpeed;
                            float2 inputVector = math.normalizesafe(input.moveInput, float2.zero);
                            //Debug.Log($"inputVector = {inputVector}");
                            if (inputVector.x == math.NAN || inputVector.y == math.NAN || math.all(inputVector == float2.zero))
                            {
                                inputVector = float2.zero;
                                currentSpeed = 0f;
                            }
                            else
                            {
                                currentSpeed += 0.05f;
                                currentSpeed = math.clamp(currentSpeed, 0f, 0.4f);
                            }
                            //float3 planarForward = math.mul(headPlanarRotation, new float3(0f, 0f, 1f));
                            float3 moveDir = math.mul(headPlanarRotation, new float3(inputVector.x, 0f, inputVector.y));
                            moveDir *= currentSpeed;

                            bodyTransform.Position += moveDir;//bodyWtL.TransformDirection(moveDir);
                            ecb.SetComponent<LocalTransform>(entities[i], bodyTransform);
                            break;
                        }
                    }
                }
            }


            ecb.Playback(state.EntityManager);

        }

    }
}
