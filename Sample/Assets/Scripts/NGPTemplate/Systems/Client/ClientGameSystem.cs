using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine.Rendering;
using Random = Unity.Mathematics.Random;
using NGPTemplate.Components;
using NGPTemplate.Misc;
using UnityEngine;

namespace NGPTemplate.Systems.Client
{
    /// <summary>
    /// This system updates the player state used in the <see cref="RespawnScreen"/>.
    ///
    /// The player is considered alive once the Camera is attached to it.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    public partial struct PlayerStatusSystem : ISystem
    {
        EntityQuery m_PlayerAliveQuery;

        public void OnCreate(ref SystemState state)
        {
            m_PlayerAliveQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MainCamera>().Build(state.EntityManager);
        }

        public void OnUpdate(ref SystemState state)
        {
            // It's not possible to check on the CharacterInitialized component because in the case of Spectator mode it does not exist.
            GameSettings.Instance.PlayerState = m_PlayerAliveQuery.IsEmpty ? PlayerState.Dead : PlayerState.Playing;
        }
    }
    /*
    /// <summary>
    /// This system handles the player forced respawn using
    /// the <see cref="FPSInputActions.GameplayActions.RequestRespawn"/> action.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    public partial struct DebugPlayerRespawnSystem : ISystem//the kys  system
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(new EntityQueryBuilder(Allocator.Temp)
                .WithAll<FirstPersonCharacterComponent, GhostOwnerIsLocal>().WithDisabled<DelayedDespawn>()
                .Build(state.EntityManager));
        }

        public void OnUpdate(ref SystemState state)
        {
            if (GameInput.Actions.Gameplay.RequestRespawn.WasPerformedThisFrame())
                state.EntityManager.CreateEntity(ComponentType.ReadWrite<ClientRequestRespawnRpc>(),
                    ComponentType.ReadWrite<SendRpcCommandRequest>());
        }
    }
    */
    /// <summary>
    /// This system handles the client side of the player connection and character spawning.
    /// It creates the first player join request so the server knows it has to spawn a character.
    /// It handles the Spectator prefab spawn if the player is a spectator.
    /// It creates the NameTagProxy on any spawned character that is not the active player.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [BurstCompile]
    public partial struct ClientGameSystem : ISystem
    {

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameResources>();

            var randomSeed = (uint)DateTime.Now.Millisecond;
            var randomEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(randomEntity, new FixedRandom
            {
                Random = Random.CreateFromIndex(randomSeed),
            });
        }

        public void OnUpdate(ref SystemState state)
        {
            //if (SystemAPI.HasSingleton<DisableCharacterDynamicContacts>())
            //    state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<DisableCharacterDynamicContacts>());
            if (!SystemAPI.TryGetSingleton(out GameResources gameResources))
                return;

            ClientJoinRequestRpc clientJoinRequestRpc = new ClientJoinRequestRpc();
            clientJoinRequestRpc.joinType = GameSettings.Instance.SpectatorToggle ? JoinType.Spectator : JoinType.Player;

            HandleSendJoinRequest(ref state, clientJoinRequestRpc);

        }

        void HandleSendJoinRequest(ref SystemState state, ClientJoinRequestRpc clientJoinRequestRpc)
        {
            if (!SystemAPI.TryGetSingletonEntity<NetworkId>(out var clientEntity)
    || SystemAPI.HasComponent<NetworkStreamInGame>(clientEntity))
                return;
            var joinRequestEntity = state.EntityManager.CreateEntity(ComponentType.ReadOnly<ClientJoinRequestRpc>(),
                ComponentType.ReadWrite<SendRpcCommandRequest>());
            var playerName = GameSettings.Instance.PlayerName;
            if (state.WorldUnmanaged.IsThinClient()) // Random names for thin clients.
            {
                ref var random = ref SystemAPI.GetSingletonRW<FixedRandom>().ValueRW;
                playerName = $"[Bot {random.Random.NextInt(1, 99):00}] {playerName}";
            }
            clientJoinRequestRpc.PlayerName.CopyFromTruncated(playerName); // Prevents exceptions on long strings.
            state.EntityManager.SetComponentData(joinRequestEntity, clientJoinRequestRpc);
            state.EntityManager.AddComponentData(clientEntity, new NetworkStreamInGame());

        }
    }
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateAfter(typeof(ClientGameSystem))]
    [BurstCompile]
    public partial struct ClientCameraSetupSystem : ISystem
    {
        EntityQuery cameraRequestQuery;
        EntityQuery cameraRequestNonOwnerQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameResources>();
            cameraRequestQuery = new EntityQueryBuilder(Allocator.Temp)
    .WithAll<CameraRequest, Simulate, GhostOwnerIsLocal>()
    .Build(ref state);
            cameraRequestNonOwnerQuery = new EntityQueryBuilder(Allocator.Temp)
    .WithAll<CameraRequest>()
    .WithNone<GhostOwnerIsLocal>()
    .Build(ref state);
            state.RequireForUpdate<CameraRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {

            if (!SystemAPI.TryGetSingleton(out GameResources gameResources))
                return;
            EntityCommandBuffer ecb = new(Allocator.Temp);

            if (cameraRequestQuery.CalculateEntityCount() > 0)
            {
                Entity cameraRequestEntity = cameraRequestQuery.ToEntityArray(Allocator.Temp)[0];
                //CameraRequest cameraRequest = cameraRequestQuery.ToComponentDataArray<CameraRequest>(Allocator.Temp)[0];
                ecb.AddComponent(cameraRequestEntity, new MainCamera(90f));
                ecb.RemoveComponent<CameraRequest>(cameraRequestEntity);
            }
            NativeArray<Entity> cameraRequestNonOwnerEntities = cameraRequestNonOwnerQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < cameraRequestNonOwnerEntities.Length; i++)
            {
                ecb.RemoveComponent<CameraRequest>(cameraRequestNonOwnerEntities[i]);
            }
            ecb.Playback(state.EntityManager);
        }
    }
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateAfter(typeof(ClientGameSystem))]
    [BurstCompile]
    public partial struct ClientNameTagSetupSystem : ISystem
    {
        EntityQuery nameTagRequestQuery;
        EntityQuery nameTagRequestNonOwnerQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameResources>();
            nameTagRequestQuery = new EntityQueryBuilder(Allocator.Temp)
    .WithAll<NameTagRequest, Simulate, GhostOwnerIsLocal>()
    .Build(ref state);
            nameTagRequestNonOwnerQuery = new EntityQueryBuilder(Allocator.Temp)
    .WithAll<NameTagRequest, OwningPlayer>()
    .WithNone<GhostOwnerIsLocal>()
    .Build(ref state);
            state.RequireForUpdate<NameTagRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {

            if (!SystemAPI.TryGetSingleton(out GameResources gameResources))
                return;
            EntityCommandBuffer ecb = new(Allocator.Temp);

            NativeArray<Entity> nameTagRequestEntities = nameTagRequestQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < nameTagRequestEntities.Length; i++)
            {
                ecb.RemoveComponent<NameTagRequest>(nameTagRequestEntities[i]);
            }
            NativeArray<Entity> nameTagRequestNonOwnerEntities = nameTagRequestNonOwnerQuery.ToEntityArray(Allocator.Temp);
            NativeArray<NameTagRequest> nameTagRequestNonOwner = nameTagRequestNonOwnerQuery.ToComponentDataArray< NameTagRequest>(Allocator.Temp);
            NativeArray<OwningPlayer> nameTagOwningPlayer = nameTagRequestNonOwnerQuery.ToComponentDataArray<OwningPlayer>(Allocator.Temp);
            for (int i = 0; i < nameTagRequestNonOwnerEntities.Length; i++)
            {
                ecb.AddComponent(nameTagRequestNonOwner[i].Value, new NameTagProxy { PlayerEntity = nameTagOwningPlayer[i].Value });
                ecb.RemoveComponent<NameTagRequest>(nameTagRequestNonOwnerEntities[i]);
            }
            ecb.Playback(state.EntityManager); 
        }
    }
}



















/*
// Spectator mode
if (GameSettings.Instance.SpectatorToggle)
{
    LocalToWorld spawnPoint = default;
    using var spectatorSpawnPoints =
        m_SpectatorSpawnPointsQuery.ToComponentDataArray<LocalToWorld>(Allocator.Temp);
    if (spectatorSpawnPoints.Length > 0)
    {
        ref var random = ref SystemAPI.GetSingletonRW<FixedRandom>().ValueRW;
        spawnPoint = spectatorSpawnPoints[random.Random.NextInt(0, spectatorSpawnPoints.Length - 1)];
    }

    var spectatorEntity = state.EntityManager.Instantiate(spectatorEntityPrefab);
    state.EntityManager.SetComponentData(spectatorEntity,
        LocalTransform.FromPositionRotation(spawnPoint.Position, spawnPoint.Rotation));
}
*/
/*
// Initialize local-owned characters
foreach (var (character, entity) in SystemAPI
             .Query<FirstPersonCharacterComponent>()
             .WithAll<GhostOwnerIsLocal, OwningPlayer, GhostOwner>()
             .WithDisabled<CharacterInitialized>()
             .WithEntityAccess())
{
    // Make camera follow character's view
    ecb.AddComponent(character.ViewEntity, new MainCamera
    {
        BaseFov = character.BaseFov,
    });
    // Make local character meshes rendering be shadow-only
    var childBufferLookup = SystemAPI.GetBufferLookup<Child>();
    MiscUtilities.SetShadowModeInHierarchy(state.EntityManager, ecb, entity, ref childBufferLookup,
        ShadowCastingMode.ShadowsOnly);
}

// Initialize remote characters
foreach (var (character, owningPlayer) in SystemAPI
             .Query<FirstPersonCharacterComponent, OwningPlayer>()
             .WithNone<GhostOwnerIsLocal>()
             .WithDisabled<CharacterInitialized>())
    // Spawn nameTag
    ecb.AddComponent(character.NameTagSocketEntity, new NameTagProxy
    {
        PlayerEntity = owningPlayer.Entity,
    });

// Initialize characters common
foreach (var (physicsCollider, characterInitialized, entity) in SystemAPI
             .Query<RefRW<PhysicsCollider>, EnabledRefRW<CharacterInitialized>>()
             .WithAll<FirstPersonCharacterComponent>()
             .WithDisabled<CharacterInitialized>()
             .WithEntityAccess())
{
    physicsCollider.ValueRW.MakeUnique(entity, ecb);

    // Mark initialized
    characterInitialized.ValueRW = true;
}
*/