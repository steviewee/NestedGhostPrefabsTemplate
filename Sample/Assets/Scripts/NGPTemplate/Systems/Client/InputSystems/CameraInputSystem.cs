using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using NGPTemplate.Components;
using NGPTemplate.Misc;
using Unity.Burst;
using Unity.Collections;
using Unity.Transforms;
namespace NGPTemplate.Systems.Client
{
    /// <summary>
    /// Reads player inputs and forward them to the <see cref="FirstPersoninput"/>
    /// that the server is using to process each character movement.
    /// </summary>
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct CameraInputSystem : ISystem
    {
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LookDirectionInput>();
            state.RequireForUpdate<GameResources>();
            state.RequireForUpdate<NetworkTime>();
        }
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var elapsedTime = (float)SystemAPI.Time.ElapsedTime;
            var defaultActionsMap = GameInput.Actions.Gameplay;
            foreach (var input in SystemAPI
                         .Query<RefRW<LookDirectionInput>>()
                         .WithAll<GhostOwnerIsLocal, Simulate>())
            {
                if (GameSettings.Instance.IsPauseMenuOpen)
                {
                    input.ValueRW = default;
                }
                else
                {
                    float2 lookInputValue = (float2)defaultActionsMap.LookDelta.ReadValue<Vector2>();
                    float2 lookInput = default;
                    if (deltaTime != 0f)
                    {
                        lookInput = new float2
                        {
                            x = lookInputValue.x * 1,
                            y = lookInputValue.y * (GameSettings.Instance.InvertYAxis?-1f:1f),

                        };
                    }
                    float sensitivity = 0.1f;
                    //float2 ln = lookInput;
                    lookInput *= GameSettings.Instance.LookSensitivity * sensitivity;
                    /*
                    if (math.length(lookInput) > maximum)
                    {
                        maximum = math.length(lookInput);
                        Debug.Log($"new maximum reached: {maximum}; "); //without delta time:{lookInput* GameSettings.Instance.LookSensitivity * sensitivity}, {GameSettings.Instance.LookSensitivity}");
                    }
                    avearge[counter] = math.length(lookInput);
                    counter++;
                    if(counter== avearge.Length)
                    {
                        counter = 0;
                    }
                    average = 0;
                    for (int i = 0; i < avearge.Length; i++)
                    {
                        average += avearge[i];
                    }
                    average/=avearge.Length;
                    if (average > averageMaximum)
                    {
                        averageMaximum = average;
                        Debug.Log($"new average maximum reached: {averageMaximum}; "); //without delta time:{lookInput* GameSettings.Instance.LookSensitivity * sensitivity}, {GameSettings.Instance.LookSensitivity}");
                    }
                    */
                    input.ValueRW.Value = lookInput;
                    FirstPersonInputDeltaUtilities.AddInputDelta(ref input.ValueRW.Value,
                        lookInput);
                }
            }
        }

    }
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    [UpdateAfter(typeof(CameraInputSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [BurstCompile]
    public partial struct CameraControlSystem : ISystem
    {
        EntityQuery mainCameraQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            mainCameraQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<MainCameraPivot, LastProcessedLookDirection, LookDirectionInput,LocalTransform, GhostOwnerIsLocal, Simulate>()
.Build(ref state);
            state.RequireForUpdate(mainCameraQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            CameraControlJob job = new CameraControlJob
            {
                LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(),
            };
            state.Dependency = job.Schedule(state.Dependency);

        }
        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct CameraControlJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<LocalToWorld> LocalToWorldLookup;
            void Execute(Entity entity,ref LocalTransform localTransform,
                ref LastProcessedLookDirection lpld, in LookDirectionInput lookDirectionInput, in MainCameraPivot mainCameraPivot)
            {
                quaternion rot = lpld.Value;
                float cameraLookInputX = lookDirectionInput.Value.x * 2;
                float cameraLookInputY = -lookDirectionInput.Value.y;
                if (mainCameraPivot.pivot != Entity.Null)
                {
                    if (LocalToWorldLookup.TryGetComponent(mainCameraPivot.pivot, out var pivotLocalToWorld))
                    {
                        quaternion localChildRot = math.mul(math.inverse(pivotLocalToWorld.Rotation), rot);
                        float3 childEuler = math.Euler(localChildRot);
                        localChildRot = quaternion.Euler(math.clamp(childEuler.x + cameraLookInputY, -1.4f, 1.4f), childEuler.y + cameraLookInputX, 0f);
                        rot = math.mul(pivotLocalToWorld.Rotation, localChildRot);
                    }
                    else
                    {
                        float rotationX = math.Euler(rot).y + cameraLookInputX;
                        float rotationY = math.Euler(rot).x + cameraLookInputY;
                        rot = quaternion.Euler(new float3(rotationY, rotationX, 0));//idk if this works
                    }
                }
                else
                {
                    float rotationX = math.Euler(rot).y + cameraLookInputX;
                    float rotationY = math.Euler(rot).x + cameraLookInputY;
                    rot  =quaternion.Euler(new float3(rotationY, rotationX, 0));//idk if this works
                }
                lpld = new LastProcessedLookDirection { Value = rot, ready = true };
                localTransform.Rotation = rot;
            }
            
        }
    }
   
}
