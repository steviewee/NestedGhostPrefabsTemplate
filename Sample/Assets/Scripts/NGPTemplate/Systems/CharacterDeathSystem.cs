using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using NGPTemplate.Components;
namespace NGPTemplate.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    //[UpdateAfter(typeof(ProjectilePredictionUpdateGroup))]
    [BurstCompile]
    public partial struct CharacterDeathServerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameResources>();
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<Health>().WithDisabled<DelayedDespawn>().Build());
            state.RequireForUpdate<ConnectionReference>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            CharacterDeathServerJob serverJob = new CharacterDeathServerJob
            {
                Ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW
                    .CreateCommandBuffer(state.WorldUnmanaged),
                RespawnTime = SystemAPI.GetSingleton<GameResources>().RespawnTime,
                DelayedDespawnLookup = SystemAPI.GetComponentLookup<DelayedDespawn>(),
            };
            state.Dependency = serverJob.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        [WithDisabled(typeof(DelayedDespawn))]
        public partial struct CharacterDeathServerJob : IJobEntity
        {
            public EntityCommandBuffer Ecb;
            public float RespawnTime;

            public ComponentLookup<DelayedDespawn> DelayedDespawnLookup;

            void Execute(Entity entity,in ConnectionReference connectionReference, in Health health,
                in GhostOwner ghostOwner)
            {
                if (health.IsDead())
                {
                    if (connectionReference.Value != Entity.Null)
                    {
                        // Set up the server to perform local respawn for this client
                        Entity spawnCharacterRequestEntity = Ecb.CreateEntity();
                        Ecb.AddComponent(spawnCharacterRequestEntity,
                            new SpawnCharacter { ClientEntity = connectionReference.Value, Delay = RespawnTime });
                    }

                    // Activate delayed despawn
                    DelayedDespawnLookup.SetComponentEnabled(entity, true);
                }
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct CharacterDeathClientSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<DeathVFXSpawnPoint,DelayedDespawn>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            CharacterDeathClientJob job = new CharacterDeathClientJob
            {
                Ecb = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW
                    .CreateCommandBuffer(state.WorldUnmanaged),
                LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
            };
            state.Dependency = job.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(DeathVFXSpawnPoint), typeof(DelayedDespawn))]
        public partial struct CharacterDeathClientJob : IJobEntity
        {
            public EntityCommandBuffer Ecb;
            [ReadOnly]
            public ComponentLookup<LocalToWorld> LocalToWorldLookup;

            void Execute(ref DeathVFXSpawnPoint character, ref VfxAttributeSettings vfxAttributeSettings)
            {
                if (character.HasProcessedDeath == 0)
                {
                    if (LocalToWorldLookup.TryGetComponent(character.Value, out LocalToWorld deathVfxLtW))
                    {
                        Entity spawnVfxDeathRequestEntity = Ecb.CreateEntity();
                        Ecb.AddComponent(spawnVfxDeathRequestEntity,
                            new VfxHitRequest()
                        {
                            VfxHitType = VfxType.Death,
                            LowCount = vfxAttributeSettings.LowVfxSpawnCount,
                            MidCount = vfxAttributeSettings.MidVfxSpawnCount,
                            HighCount = vfxAttributeSettings.HighVfxSpawnCount,
                            Position = deathVfxLtW.Position,
                            HitNormal = new float3(0, 1, 0),
                        });
                    }

                    character.HasProcessedDeath = 1;
                }
            }
        }
    }
}
