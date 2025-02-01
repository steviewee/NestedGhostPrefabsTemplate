using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Unity.Burst;
using Unity.Physics;
using Zhorman.JointModificators.Runtime.Components;

namespace Zhorman.JointModificators.Runtime.Systems
{
    /// <summary>
    /// System to change the parent with a delay
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PostBakingSystemGroup), OrderFirst = true)]
    //[UpdateAfter(typeof(EndJointBakingSystem))]
    //[UpdateAfter(typeof(JointToGhostJointConversionBakingSystem))]
    //[UpdateBefore(typeof(PrefabToGhostConversionSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    //[UpdateAfter(typeof(Skinwal))]
    public partial struct JointModificatorSystem : ISystem
    {
        //EntityQuery modificatorParentCleanupQuery;
        EntityQuery modificatorQuery;
        EntityQuery jointQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            modificatorQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<JointModificator, RootEntityLink>().WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludeMetaChunks | EntityQueryOptions.IgnoreComponentEnabledState)
.Build(ref state);
            jointQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<PhysicsJoint, PhysicsConstrainedBodyPair>().WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludeMetaChunks | EntityQueryOptions.IgnoreComponentEnabledState)
.Build(ref state);

            state.RequireForUpdate(modificatorQuery);
            state.RequireForUpdate(jointQuery);
        }
        public bool AreJointsEqual(in PhysicsJoint joint1, in PhysicsJoint joint2)
        {
            return joint1.GetHashCode() == joint2.GetHashCode();
        }
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);

            NativeParallelMultiHashMap<Entity, Entity> modificatorToCompanionMap = new NativeParallelMultiHashMap<Entity, Entity>(0, Allocator.Temp);
            NativeParallelHashMap<Entity, Entity> jointToModificatorMap = new NativeParallelHashMap<Entity, Entity>(0, Allocator.Temp);

            NativeArray<Entity> modificatorEntities = modificatorQuery.ToEntityArray(Allocator.Temp);
            NativeArray<JointModificator> modificatorData = modificatorQuery.ToComponentDataArray<JointModificator>(Allocator.Temp);
            NativeArray<RootEntityLink> rootLink = modificatorQuery.ToComponentDataArray<RootEntityLink>(Allocator.Temp);

            NativeArray<Entity> jointEntities = jointQuery.ToEntityArray(Allocator.Temp);
            NativeArray<PhysicsJoint> joints = jointQuery.ToComponentDataArray<PhysicsJoint>(Allocator.Temp);
            NativeArray<PhysicsConstrainedBodyPair> jointBodyPairs = jointQuery.ToComponentDataArray<PhysicsConstrainedBodyPair>(Allocator.Temp);

            for (int j = 0; j < jointEntities.Length; j++)
            {
                PhysicsJoint joint = joints[j];
                PhysicsConstrainedBodyPair jointPair = jointBodyPairs[j];
                PhysicsWorldIndex index = state.EntityManager.GetSharedComponent<PhysicsWorldIndex>(jointEntities[j]);

                for (int i = 0; i < modificatorEntities.Length; i++)
                {

                    if (modificatorData[i].physicsConstrainedBodyPair.EntityA == jointBodyPairs[j].EntityA && modificatorData[i].physicsConstrainedBodyPair.EntityB == jointBodyPairs[j].EntityB && modificatorData[i].physicsConstrainedBodyPair.EnableCollision == jointBodyPairs[j].EnableCollision)
                    {
                        //if(modificatorData[i].worldIndex ==)
                        if (modificatorData[i].jointHash == joints[j].GetHashCode())//(AreJointsEqual(modificatorData[i].targetJoint, jointData[j]))
                        {
                            Debug.Log($"Joint modificator found its joint and stole its components!");
                            //Debug.Log($"found! {jointEntities[j]}; {modificatorEntities[i]}");
                            ecb.AddComponent<PhysicsJoint>(modificatorEntities[i], joint);
                            ecb.AddComponent<PhysicsConstrainedBodyPair>(modificatorEntities[i], jointPair);
                            ecb.AddSharedComponent<PhysicsWorldIndex>(modificatorEntities[i], index);

                            jointToModificatorMap.Add(jointEntities[j], modificatorEntities[i]);
                            if (modificatorData[i].companionship)
                            {
                                var companionship = state.EntityManager.GetBuffer<PhysicsJointCompanion>(jointEntities[j]);

                                for (int k = 0; k < companionship.Length; k++)
                                {
                                    modificatorToCompanionMap.Add(modificatorEntities[i], companionship[k].JointEntity);
                                }

                            }

                            ecb.RemoveComponent<JointModificator>(modificatorEntities[i]);



                            var linkedEntityGroup = state.EntityManager.GetBuffer<LinkedEntityGroup>(rootLink[i].Value);

                            for (int k = 0; k < linkedEntityGroup.Length; k++)
                            {
                                if (linkedEntityGroup[k].Value == jointEntities[j])
                                {
                                    linkedEntityGroup.RemoveAt(k);
                                    k--;
                                }
                                else if (state.EntityManager.HasBuffer<LinkedEntityGroup>(linkedEntityGroup[k].Value))
                                {
                                    var linkedEntityGroup2 = state.EntityManager.GetBuffer<LinkedEntityGroup>(linkedEntityGroup[k].Value);

                                    for (int u = 0; u < linkedEntityGroup2.Length; u++)
                                    {
                                        if (linkedEntityGroup2[u].Value == jointEntities[j])
                                        {
                                            linkedEntityGroup2.RemoveAt(u);
                                            u--;
                                        }
                                    }
                                }
                            }
                            ecb.DestroyEntity(jointEntities[j]);
                            break;
                        }
                        else
                        {
                           // Debug.Log($"compared, but failed second, {modificatorData[i].jointHash}!={joints[j].GetHashCode()}");
                        }
                    }
                    //Debug.Log($"did not find! {jointEntities[j]}; {modificatorEntities[i]}, skibidi? {modificatorData[i].physicsConstrainedBodyPair.GetHashCode() == jointBodyPairs[j].GetHashCode()}");//
                   // Debug.Log($"compared, but failed first {modificatorData[i].physicsConstrainedBodyPair.EntityA}=?{jointBodyPairs[j].EntityA}, {modificatorData[i].physicsConstrainedBodyPair.EntityB}=?{jointBodyPairs[j].EntityB}, {modificatorData[i].physicsConstrainedBodyPair.EnableCollision}=?{jointBodyPairs[j].EnableCollision}");

                }
            }
            (NativeArray<Entity> keys, int length) = modificatorToCompanionMap.GetUniqueKeyArray(Allocator.Temp);

            for (int i = 0; i < length; i++)
            {
                var values = modificatorToCompanionMap.GetValuesForKey(keys[i]);
                while (values.MoveNext())
                {
                    var entity = values.Current;
                    if (jointToModificatorMap.TryGetValue(entity, out Entity modificator))
                    {
                        ecb.AppendToBuffer(keys[i], new PhysicsJointCompanion { JointEntity = modificator });
                    }
                    else
                    {
                        ecb.AppendToBuffer(keys[i], new PhysicsJointCompanion { JointEntity = entity });
                    }
                }

            }

            ecb.Playback(state.EntityManager);

        }
    }
    [BurstCompile]
    [UpdateInGroup(typeof(PostBakingSystemGroup), OrderFirst = true)]
    //[UpdateAfter(typeof(EndJointBakingSystem))]
    //[UpdateAfter(typeof(JointToGhostJointConversionBakingSystem))]
    //[UpdateBefore(typeof(PrefabToGhostConversionSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    //[UpdateAfter(typeof(Skinwal))]
    public partial struct JointModificatorPrefabSystem : ISystem
    {
        EntityQuery modificatorPrefabQuery;
        EntityQuery jointPrefabQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            modificatorPrefabQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<JointModificator, RootEntityLink, Prefab>().WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludeMetaChunks | EntityQueryOptions.IgnoreComponentEnabledState)
.Build(ref state);
            jointPrefabQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<PhysicsJoint, PhysicsConstrainedBodyPair, Prefab>().WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludeMetaChunks | EntityQueryOptions.IgnoreComponentEnabledState)
.Build(ref state);

            state.RequireForUpdate(modificatorPrefabQuery);
            state.RequireForUpdate(jointPrefabQuery);
        }
        public bool AreJointsEqual(in PhysicsJoint joint1, in PhysicsJoint joint2)
        {
            return joint1.GetHashCode() == joint2.GetHashCode();
        }
        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);

            NativeParallelMultiHashMap<Entity, Entity> modificatorToCompanionMap = new NativeParallelMultiHashMap<Entity, Entity>(0, Allocator.Temp);
            NativeParallelHashMap<Entity, Entity> jointToModificatorMap = new NativeParallelHashMap<Entity, Entity>(0, Allocator.Temp);

            NativeArray<Entity> modificatorPrefabEntities = modificatorPrefabQuery.ToEntityArray(Allocator.Temp);
            NativeArray<JointModificator> modificatorPrefabData = modificatorPrefabQuery.ToComponentDataArray<JointModificator>(Allocator.Temp);
            NativeArray<RootEntityLink> rootLinkPrefab = modificatorPrefabQuery.ToComponentDataArray<RootEntityLink>(Allocator.Temp);

            NativeArray<Entity> jointPrefabEntities = jointPrefabQuery.ToEntityArray(Allocator.Temp);
            NativeArray<PhysicsJoint> jointsPrefab = jointPrefabQuery.ToComponentDataArray<PhysicsJoint>(Allocator.Temp);
            NativeArray<PhysicsConstrainedBodyPair> jointBodyPairsPrefab = jointPrefabQuery.ToComponentDataArray<PhysicsConstrainedBodyPair>(Allocator.Temp);

            for (int j = 0; j < jointPrefabEntities.Length; j++)
            {
                PhysicsJoint joint = jointsPrefab[j];
                PhysicsConstrainedBodyPair jointPair = jointBodyPairsPrefab[j];
                PhysicsWorldIndex index = state.EntityManager.GetSharedComponent<PhysicsWorldIndex>(jointPrefabEntities[j]);

                for (int i = 0; i < modificatorPrefabEntities.Length; i++)
                {
                    if (modificatorPrefabData[i].physicsConstrainedBodyPair.EntityA == jointBodyPairsPrefab[j].EntityA && modificatorPrefabData[i].physicsConstrainedBodyPair.EntityB == jointBodyPairsPrefab[j].EntityB && modificatorPrefabData[i].physicsConstrainedBodyPair.EnableCollision == jointBodyPairsPrefab[j].EnableCollision)
                    {
                        //if(modificatorPrefabData[i].worldIndex ==)
                        if (modificatorPrefabData[i].jointHash == jointsPrefab[j].GetHashCode())//(AreJointsEqual(modificatorPrefabData[i].targetJoint, jointData[j]))
                        {
                            Debug.Log($"Joint modificator (prefab) found its joint and stole its components!");
                            //Debug.Log($"prefab found! {jointPrefabEntities[j]}; {modificatorPrefabEntities[i]}");
                            ecb.AddComponent<PhysicsJoint>(modificatorPrefabEntities[i], joint);
                            ecb.AddComponent<PhysicsConstrainedBodyPair>(modificatorPrefabEntities[i], jointPair);
                            ecb.AddSharedComponent<PhysicsWorldIndex>(modificatorPrefabEntities[i], index);
                            jointToModificatorMap.Add(jointPrefabEntities[j], modificatorPrefabEntities[i]);
                            if (modificatorPrefabData[i].companionship)
                            {
                                var companionship = state.EntityManager.GetBuffer<PhysicsJointCompanion>(jointPrefabEntities[j]);

                                for(int k  = 0; k < companionship.Length; k++)
                                {
                                    modificatorToCompanionMap.Add(modificatorPrefabEntities[i], companionship[k].JointEntity);
                                }

                            }

                            ecb.RemoveComponent<JointModificator>(modificatorPrefabEntities[i]);



                            var linkedEntityGroup = state.EntityManager.GetBuffer<LinkedEntityGroup>(rootLinkPrefab[i].Value);

                            for (int k = 0; k < linkedEntityGroup.Length; k++)
                            {
                                if (linkedEntityGroup[k].Value == jointPrefabEntities[j])
                                {
                                    linkedEntityGroup.RemoveAt(k);
                                    k--;
                                }
                                else if (state.EntityManager.HasBuffer<LinkedEntityGroup>(linkedEntityGroup[k].Value))
                                {
                                    var linkedEntityGroup2 = state.EntityManager.GetBuffer<LinkedEntityGroup>(linkedEntityGroup[k].Value);

                                    for (int u = 0; u < linkedEntityGroup2.Length; u++)
                                    {
                                        if (linkedEntityGroup2[u].Value == jointPrefabEntities[j])
                                        {
                                            linkedEntityGroup2.RemoveAt(u);
                                            u--;
                                        }
                                    }
                                }
                            }
                            ecb.DestroyEntity(jointPrefabEntities[j]);
                            break;
                        }
                        else
                        {
                             //   Debug.Log($"prefab compared, but failed second, {modificatorPrefabData[i].jointHash == jointsPrefab[j].GetHashCode()} {modificatorPrefabData[i].jointHash}!={jointsPrefab[j].GetHashCode()}!");
                        }
                    }
                    // Debug.Log($"prefab did not find! {jointPrefabEntities[j]}; {modificatorPrefabEntities[i]}");//
                    //Debug.Log($"prefab compared, but failed first WHHHHHHHHHHHHHHHHAAAAAAAAAAATTT??????? \n {modificatorPrefabData[i].physicsConstrainedBodyPair.EntityA}=?{jointBodyPairsPrefab[j].EntityA}, {modificatorPrefabData[i].physicsConstrainedBodyPair.EntityB}=?{jointBodyPairsPrefab[j].EntityB}, {modificatorPrefabData[i].physicsConstrainedBodyPair.EnableCollision}=?{jointBodyPairsPrefab[j].EnableCollision}\n {modificatorPrefabData[i].physicsConstrainedBodyPair.EntityA == jointBodyPairsPrefab[j].EntityA && modificatorPrefabData[i].physicsConstrainedBodyPair.EntityB == jointBodyPairsPrefab[j].EntityB && modificatorPrefabData[i].physicsConstrainedBodyPair.EnableCollision == jointBodyPairsPrefab[j].EnableCollision} becayse {modificatorPrefabData[i].physicsConstrainedBodyPair.EntityA == jointBodyPairsPrefab[j].EntityA} && {modificatorPrefabData[i].physicsConstrainedBodyPair.EntityB == jointBodyPairsPrefab[j].EntityB} && {modificatorPrefabData[i].physicsConstrainedBodyPair.EnableCollision == jointBodyPairsPrefab[j].EnableCollision}");

                }
            }

            (NativeArray<Entity> keys, int length) = modificatorToCompanionMap.GetUniqueKeyArray(Allocator.Temp);

            for (int i = 0; i < length; i++)
            {
                var values = modificatorToCompanionMap.GetValuesForKey(keys[i]);
                while (values.MoveNext())
                {
                    var entity = values.Current;
                    if (jointToModificatorMap.TryGetValue(entity, out Entity modificator))
                    {
                        ecb.AppendToBuffer(keys[i], new PhysicsJointCompanion {JointEntity= modificator });
                    }
                    else
                    {
                        ecb.AppendToBuffer(keys[i], new PhysicsJointCompanion { JointEntity = entity });
                    }
                }

            }

            ecb.Playback(state.EntityManager);

        }
    }
    /*
    [BurstCompile]
    [UpdateInGroup(typeof(PostBakingSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(JointModificatorPrefabSystem))]
    [UpdateAfter(typeof(JointModificatorSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct FailedJointModificatorSystem : ISystem
    {
        EntityQuery modificatorQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            modificatorQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<RootEntityLink>()
.WithAny<JointModificator,FailedJointModificator>()
.WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludeMetaChunks | EntityQueryOptions.IgnoreComponentEnabledState)
.Build(ref state);

            state.RequireForUpdate(modificatorQuery);
        }
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> modificatorEntities = modificatorQuery.ToEntityArray(Allocator.Temp);
            NativeArray<RootEntityLink> rootLinkPrefab = modificatorQuery.ToComponentDataArray<RootEntityLink>(Allocator.Temp);

            for (int i = 0; i < modificatorEntities.Length; i++)
            {
                Debug.LogWarning("Removing failed joint modificator");

                var linkedEntityGroup = state.EntityManager.GetBuffer<LinkedEntityGroup>(rootLinkPrefab[i].Value);

                for (int k = 0; k < linkedEntityGroup.Length; k++)
                {
                    if (linkedEntityGroup[k].Value == modificatorEntities[i])
                    {
                        linkedEntityGroup.RemoveAt(k);
                        k--;
                    }
                    else if (state.EntityManager.HasBuffer<LinkedEntityGroup>(linkedEntityGroup[k].Value))
                    {
                        var linkedEntityGroup2 = state.EntityManager.GetBuffer<LinkedEntityGroup>(linkedEntityGroup[k].Value);

                        for (int u = 0; u < linkedEntityGroup2.Length; u++)
                        {
                            if (linkedEntityGroup2[u].Value == modificatorEntities[i])
                            {
                                linkedEntityGroup2.RemoveAt(u);
                                u--;
                            }
                        }
                    }
                }


                ecb.DestroyEntity(modificatorEntities[i]);
            }

            ecb.Playback(state.EntityManager);
        }
    }
    */
}
        