using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using NGPTemplate.Components;
using Unity.Mathematics;
using Zhorman.NestedGhostPrefabs.Runtime.Components;
namespace NGPTemplate.Systems.Server
{

    /// <summary>
    /// Processes client join requests and spawns a character for each one of them.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [BurstCompile]
    public partial struct ServerGameSystem : ISystem
    {
        public struct JoinedClient : IComponentData
        {
            public Entity PlayerEntity;
        }


        private EntityQuery connectionQuery;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<NetworkStreamDriver>();
            connectionQuery = new EntityQueryBuilder(Allocator.Temp)
    .WithAll<NetworkId, LinkedEntityGroup, NetworkStreamConnection>()
    .Build(ref state);
            //state.RequireForUpdate<GameplayMaps>();
            /*
            gameResourcesQuery = new EntityQueryBuilder(Allocator.Temp)
    .WithAll<GameResources,SpawneeData,FirstBorn>()
    .Build(ref state);
    */

            // Creates random singleton
            var randomSeed = (uint)DateTime.Now.Millisecond;
            Entity randomEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(randomEntity, new FixedRandom
            {
                Random = Random.CreateFromIndex(randomSeed),
            });
            //var mapSingleton = state.EntityManager.CreateSingletonBuffer<GameplayMaps>();
            //state.EntityManager.GetBuffer<GameplayMaps>(mapSingleton).Add(default); // Default entry for index 0 (the server NetworkId index).
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton(out GameResources gameResources))
                return;
            NativeArray<Entity> connectionEntities = connectionQuery.ToEntityArray(Allocator.Temp);
            var connectionEventsForTick = SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick;
            var gameResourceEntity = SystemAPI.GetSingletonEntity<GameResources>();
            var generalGhostSpawneeBuffer = state.EntityManager.GetBuffer<Spawnee>(gameResourceEntity);
            var generalGhostFirstBornBuffer = state.EntityManager.GetBuffer<FirstBorn>(gameResourceEntity);

            Entity playerEntityPrefab = generalGhostFirstBornBuffer[0].Value;

            NameTag NameTag = state.EntityManager.GetComponentData<NameTag>(playerEntityPrefab);


            //Entity spectatorEntityPrefab = generalGhostFirstBornBuffer[1].Value;
            //Entity characterEntityPrefab = generalGhostFirstBornBuffer[2].Value;//variable between scenes, change later
            //var characterPrefabTransform = state.EntityManager.GetComponentData<LocalTransform>(characterEntityPrefab);


            var ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

            HandleJoinRequests(ref state, playerEntityPrefab, NameTag, ecb);
            HandleCharacters(ref state, gameResources, generalGhostFirstBornBuffer, generalGhostSpawneeBuffer, ecb);//
            //HandleClientRequestRespawn(ref state, ecb);
        }
        [GenerateTestsForBurstCompatibility]
        void HandleJoinRequests(ref SystemState state, Entity playerEntityPrefab, NameTag NameTag, EntityCommandBuffer ecb)
        {
            // Process join requests
            foreach (var (request, rpcReceive, entity) in
                     SystemAPI.Query<ClientJoinRequestRpc, ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                if (SystemAPI.HasComponent<NetworkId>(rpcReceive.SourceConnection) &&
                    !SystemAPI.HasComponent<NetworkStreamInGame>(rpcReceive.SourceConnection))
                {
                    var ownerNetworkId = SystemAPI.GetComponent<NetworkId>(rpcReceive.SourceConnection);
                    // Spawn player
                    var playerEntity = ecb.Instantiate(playerEntityPrefab);
                    ecb.SetComponent(playerEntity, new GhostOwner { NetworkId = ownerNetworkId.Value });
                    ecb.AppendToBuffer(rpcReceive.SourceConnection, new LinkedEntityGroup { Value = playerEntity });

                    NameTag.Name = request.PlayerName;
                    ecb.SetComponent(playerEntity, NameTag);

                    Entity spawnCharacterRequestEntity = ecb.CreateEntity();
                    ecb.AddComponent(spawnCharacterRequestEntity,
                        new SpawnCharacter { ClientEntity = rpcReceive.SourceConnection, Delay = -1f, Prefab = (int)request.joinType });

                    // Remember player for connection
                    ecb.AddComponent(rpcReceive.SourceConnection, new JoinedClient { PlayerEntity = playerEntity });
                    // Stream in game
                    ecb.AddComponent(rpcReceive.SourceConnection, new NetworkStreamInGame());

                    state.EntityManager.GetName(playerEntityPrefab, out var playerNameFs);
                    if (playerNameFs.IsEmpty) playerNameFs = nameof(playerEntityPrefab);
                    Debug.Log($"[{state.WorldUnmanaged.Name}] Spawning '{playerNameFs}' (the netcode input wrapper) for {ownerNetworkId.ToFixedString()} called '{request.PlayerName}'!");
                }
                ecb.DestroyEntity(entity);
            }
        }
        [GenerateTestsForBurstCompatibility]
        void HandleCharacters(ref SystemState state, GameResources gameResources, DynamicBuffer<FirstBorn> generalGhostFirstBornBuffer, DynamicBuffer<Spawnee> generalGhostSpawneeBuffer, EntityCommandBuffer ecb)
        {
            /*
            // Initialize characters common
            foreach (var (physicsCollider, characterInitialized, entity) in SystemAPI
                         .Query<RefRW<PhysicsCollider>, EnabledRefRW<CharacterInitialized>>()//will need to check what EnabledRefRW does
                         .WithAll<FirstPersonCharacterComponent>()
                         .WithDisabled<CharacterInitialized>()
                         .WithEntityAccess())
            {
                physicsCollider.ValueRW.MakeUnique(entity, ecb);//??? why

                // Mark initialized
                characterInitialized.ValueRW = true;
            }
            */

            // Spawn character requests
            if (SystemAPI.QueryBuilder().WithAll<SpawnCharacter>().Build().CalculateEntityCount() > 0)
            {
                var spawnPointsQuery = SystemAPI.QueryBuilder().WithAll<SpawnPoint, LocalToWorld>().Build();
                var spawnPointLtWs = spawnPointsQuery.ToComponentDataArray<LocalToWorld>(Allocator.Temp);
                var consumedSpawnPoints = new NativeBitArray(spawnPointLtWs.Length, Allocator.Temp);

                ref FixedRandom random = ref SystemAPI.GetSingletonRW<FixedRandom>().ValueRW;
                foreach (var (spawnRequest, entity) in SystemAPI.Query<RefRW<SpawnCharacter>>().WithEntityAccess())
                {
                    if (spawnRequest.ValueRW.Delay > 0f)
                    {
                        spawnRequest.ValueRW.Delay -= SystemAPI.Time.DeltaTime;
                    }
                    else
                    {
                        if (SystemAPI.HasComponent<NetworkId>(spawnRequest.ValueRO.ClientEntity) &&
                            SystemAPI.HasComponent<JoinedClient>(spawnRequest.ValueRO.ClientEntity))
                        {
                            // Try to find a free (i.e. unblocked by other players) spawn point:
                            if (!TryFindSpawnPoint(gameResources, spawnPointLtWs, random, consumedSpawnPoints,
                                    out var spawnPoint))
                                break;// TODO: rework

                            var ownerNetworkId = SystemAPI.GetComponent<NetworkId>(spawnRequest.ValueRO.ClientEntity);
                            Entity playerEntity = SystemAPI.GetComponent<JoinedClient>(spawnRequest.ValueRO.ClientEntity).PlayerEntity;
                            Entity prefab = generalGhostFirstBornBuffer[spawnRequest.ValueRO.Prefab].Value;
                            var newRootEntity = ecb.Instantiate(prefab);//do check later if they are even allowed to use that entity
                            ecb.SetComponent(newRootEntity, new GhostOwner { NetworkId = ownerNetworkId.Value });
                            ecb.AppendToBuffer(spawnRequest.ValueRO.ClientEntity, new LinkedEntityGroup { Value = newRootEntity });
                            ecb.AddComponent<GhostConnectionPosition>(newRootEntity);
                            ecb.SetComponent(newRootEntity, new OwningPlayer { Value = playerEntity });
                            ecb.AddComponent(newRootEntity, new ConnectionReference {Value = spawnRequest.ValueRO.ClientEntity });
                            LocalTransform newLocalTransform = LocalTransform.FromPositionRotation(spawnPoint.Position, spawnPoint.Rotation);
                            ecb.SetComponent(newRootEntity, newLocalTransform);
                            var deltaMatrix = math.mul(math.inverse(generalGhostSpawneeBuffer[generalGhostFirstBornBuffer[spawnRequest.ValueRO.Prefab].firstIndex].transform.ToMatrix()), newLocalTransform.ToMatrix());
                            for (int j = generalGhostFirstBornBuffer[spawnRequest.ValueRO.Prefab].firstIndex + 1; j < generalGhostFirstBornBuffer[spawnRequest.ValueRO.Prefab].lastIndex; j++)
                            {
                                var child = ecb.Instantiate(generalGhostSpawneeBuffer[j].Value);
                                
                                ecb.AddComponent(child, new ConnectionReference { Value = spawnRequest.ValueRO.ClientEntity });
                                ecb.SetComponent(child, new OwningPlayer { Value = playerEntity });
                                ecb.SetComponent(child, new GhostRootLink { Value = newRootEntity });

                                ecb.AppendToBuffer(spawnRequest.ValueRO.ClientEntity, new LinkedEntityGroup { Value = child });
                                if (state.EntityManager.HasComponent<LocalTransform>(generalGhostSpawneeBuffer[j].Value))
                                {
                                    var transformedMatrix = math.mul(deltaMatrix, generalGhostSpawneeBuffer[j].transform.ToMatrix());
                                    ecb.SetComponent(child, LocalTransform.FromMatrix(transformedMatrix));
                                }
                                if (state.EntityManager.HasComponent<GhostOwner>(generalGhostSpawneeBuffer[j].Value))
                                {
                                    ecb.SetComponent(child, new GhostOwner { NetworkId = ownerNetworkId.Value });
                                }
                                
                            }

                            state.EntityManager.GetName(prefab, out var characterNameFs);
                            if (characterNameFs.IsEmpty) characterNameFs = nameof(prefab);
                            Debug.Log($"[{state.WorldUnmanaged.Name}] Spawning (or respawning) character '{characterNameFs}' for {ownerNetworkId.ToFixedString()}!");
                        }

                        ecb.DestroyEntity(entity);
                    }
                }

                consumedSpawnPoints.Dispose();
                spawnPointLtWs.Dispose();
            }
        }
        [GenerateTestsForBurstCompatibility]
        bool TryFindSpawnPoint(GameResources gameResources, NativeArray<LocalToWorld> spawnPointLtWs,
            FixedRandom random, NativeBitArray consumedSpawnPoints, out LocalToWorld spawnPoint)
        {
            spawnPoint = default;
            if (spawnPointLtWs.Length > 0)
            {
                var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
                var randSpawnPointIndex = random.Random.NextInt(0, spawnPointLtWs.Length - 1);
                for (var attempt = 0; attempt < spawnPointLtWs.Length; attempt++)
                {
                    var spawnPointIndex = (randSpawnPointIndex + attempt) % spawnPointLtWs.Length;
                    if (!consumedSpawnPoints.IsSet(spawnPointIndex))
                    {
                        Debug.Assert(gameResources.SpawnPointCollisionFilter.CollidesWith != default);
                        var spawnPointBlocked = collisionWorld.CheckSphere(
                            spawnPointLtWs[spawnPointIndex].Position,
                            gameResources.SpawnPointBlockRadius,
                            gameResources.SpawnPointCollisionFilter,
                            QueryInteraction.IgnoreTriggers);

                        if (!spawnPointBlocked)
                        {
                            spawnPoint = spawnPointLtWs[spawnPointIndex];
                            consumedSpawnPoints.Set(spawnPointIndex, true);
                            return true;
                        }
                    }
                }
                return false;
            }
            return true;
        }
        /*
        void HandleClientRequestRespawn(ref SystemState state, EntityCommandBuffer ecb)
        {
            foreach (var (receiveRpc, rpcEntity) in SystemAPI.Query<ReceiveRpcCommandRequest>().WithAll<ClientRequestRespawnRpc>().WithEntityAccess())
            {
                var ownerNetworkId = SystemAPI.GetComponent<NetworkId>(receiveRpc.SourceConnection);
                var characterControllerEntity = maps.ElementAt(ownerNetworkId.Value).CharacterControllerEntity;
                if (state.EntityManager.HasComponent<Health>(characterControllerEntity))
                {
                    var health = state.EntityManager.GetComponentData<Health>(characterControllerEntity);
                    health.CurrentHealth = 0;
                    ecb.SetComponent(characterControllerEntity, health);
                    Debug.Log($"[{state.WorldUnmanaged.Name}] Client {ownerNetworkId.ToFixedString()} requested respawn, killing their player {characterControllerEntity.ToFixedString()}!");
                }
                else Debug.LogWarning($"[{state.WorldUnmanaged.Name}] Client {ownerNetworkId.ToFixedString()} requested respawn, but CC {characterControllerEntity.ToFixedString()} has no Health. Probably deleted.");
                ecb.DestroyEntity(rpcEntity);
            }
        }
        */
    }
}
