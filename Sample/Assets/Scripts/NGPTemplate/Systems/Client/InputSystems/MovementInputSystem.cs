using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using NGPTemplate.Components;
using NGPTemplate.Misc;
using Unity.Burst;
using Unity.Collections;
namespace NGPTemplate.Systems.Client
{
    /// <summary>
    /// Reads player inputs and forward them to the <see cref="FirstPersoninput"/>
    /// that the server is using to process each character movement.
    /// </summary>
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct MovementInputSystem : ISystem
    {
    //    EntityQuery inputQuery;
        public void OnCreate(ref SystemState state)
        {
            //          inputQuery = new EntityQueryBuilder(Allocator.Temp)
            //   .WithAll<MovementInput, Simulate, GhostOwnerIsLocal>()
            //   .Build(ref state);
            //       state.RequireForUpdate(inputQuery);
            state.RequireForUpdate<MovementInput>();
            state.RequireForUpdate<GameResources>();
            state.RequireForUpdate<NetworkTime>();
        }
        public void OnUpdate(ref SystemState state)
        {
            //EntityCommandBuffer ecb = new(Allocator.Temp);
            var deltaTime = SystemAPI.Time.DeltaTime;
            var elapsedTime = (float)SystemAPI.Time.ElapsedTime;
            var defaultActionsMap = GameInput.Actions.Gameplay;
            foreach (var input in SystemAPI
                         .Query<RefRW<MovementInput>>()
                         .WithAll<GhostOwnerIsLocal, Simulate>())
            {
                if (GameSettings.Instance.IsPauseMenuOpen)
                {
                    input.ValueRW = default;
                }
                else
                {
                    // Move
                    input.ValueRW.moveInput =
                        Vector2.ClampMagnitude(defaultActionsMap.Move.ReadValue<Vector2>(), 1f);

                    // Jump
                    input.ValueRW.jumpPressed = default;
                    if (defaultActionsMap.Jump.WasPressedThisFrame())
                        input.ValueRW.jumpPressed.Set();


                    // Shoot pressed
                    input.ValueRW.crouchPressed = default;
                    if (defaultActionsMap.Crouch.WasPressedThisFrame())
                        input.ValueRW.crouchPressed.Set();

                    //Shoot released
                    input.ValueRW.crouchReleased = default;
                    if (defaultActionsMap.Crouch.WasReleasedThisFrame())//
                        input.ValueRW.crouchReleased.Set();


                    // Shoot pressed
                    input.ValueRW.shootPressed = default;
                    if (defaultActionsMap.Shoot.WasPressedThisFrame())
                        input.ValueRW.shootPressed.Set();

                    //Shoot released
                    input.ValueRW.shootReleased = default;
                    if (defaultActionsMap.Shoot.WasReleasedThisFrame())
                        input.ValueRW.shootReleased.Set();

                    // Aim
                    input.ValueRW.aimHeld = defaultActionsMap.Aim.IsPressed();
                }
            }
            /*
                //RefRW<MovementInput> input = inputQuery.GetSingletonRW< MovementInput>();
                Entity entity = inputQuery.GetSingletonEntity();
            MovementInput input = inputQuery.GetSingleton<MovementInput>();
            if (GameSettings.Instance.IsPauseMenuOpen)
            {
                //var currentRotation = input.ValueRO.worldLookRot;
                //var aimHeld = input.ValueRO.AimHeld;
                input = default;
                //input.worldLookRot = currentRotation;
                //input.ShootReleased.Set();
                //input.AimHeld = aimHeld;
            }
            else
            {
                // Move
                input.moveInput =
                    Vector2.ClampMagnitude(defaultActionsMap.Move.ReadValue<Vector2>(), 1f);

                // Jump
                input.jumpPressed = default;
                if (defaultActionsMap.Jump.WasPressedThisFrame())
                    input.jumpPressed.Set();


                // Shoot pressed
                input.crouchPressed = default;
                if (defaultActionsMap.Crouch.WasPressedThisFrame())
                    input.crouchPressed.Set();

                //Shoot released
                input.crouchReleased = default;
                if (defaultActionsMap.Crouch.WasReleasedThisFrame())//
                    input.crouchReleased.Set();


                // Shoot pressed
                input.shootPressed = default;
                if (defaultActionsMap.Shoot.WasPressedThisFrame())
                    input.shootPressed.Set();

                //Shoot released
                input.shootReleased = default;
                if (defaultActionsMap.Shoot.WasReleasedThisFrame())
                    input.shootReleased.Set();

                // Aim
                input.aimHeld = defaultActionsMap.Aim.IsPressed();
            }
            ecb.SetComponent(entity, input);
            ecb.Playback(state.EntityManager);
            */
        }
    }
}
