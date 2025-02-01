using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using Zhorman.NestedGhostPrefabs.Runtime.Components;
using Unity.Burst.Intrinsics;
using Unity.Assertions;
using Unity.Jobs;
using Zhorman.NestedGhostPrefabs.Runtime.Misc;

namespace Zhorman.NestedGhostPrefabs.Runtime.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(GhostSimulationSystemGroup), OrderFirst = true)]//I am not sure why I have put it into GhostSimulationSystemGroup, and not some other group, but it works, so... don't touch it?
    [UpdateBefore(typeof(SpawneeSettingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct SpawneeCheckingSystemServer : ISystem
    {
        EntityQuery spawneeQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            spawneeQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<GhostRootLink, SpawneeIndex>()//, PrefabReference>()
.WithNone<FirstBornSpawnee>()
.Build(ref state);
            state.RequireForUpdate(spawneeQuery);
        }

        //no burst because of EntityComponentAccess.FindAndReplaceAllFieldsInEntityComponentsDictionaryWithBlackList
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //Debug.LogWarning($"SpawneeCheckingSystem, clientworld?:{(state.WorldUnmanaged.Flags & WorldFlags.GameClient) != 0} NO, SERVER");
            NativeParallelMultiHashMap<Entity, int> rootAndChildrenMap = new NativeParallelMultiHashMap<Entity, int>(0, Allocator.Temp);
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> spawneeEntities = spawneeQuery.ToEntityArray(Allocator.Temp);
            NativeArray<GhostRootLink> ghostRootLinks = spawneeQuery.ToComponentDataArray<GhostRootLink>(Allocator.Temp);
            NativeArray<SpawneeIndex> spawneeIndex = spawneeQuery.ToComponentDataArray<SpawneeIndex>(Allocator.Temp);
            for (int i = 0; i < spawneeEntities.Length; i++)
            {
                if (ghostRootLinks[i].Value != Entity.Null)
                {
                    rootAndChildrenMap.Add(ghostRootLinks[i].Value, i);
                }
            }
            
            (NativeArray < Entity > roots, int length) = rootAndChildrenMap.GetUniqueKeyArray(Allocator.Temp);
            /*
            Debug.LogWarning($"There are {length} roots");
            for (int i = 0; i < length; i++)
            {
                Debug.LogWarning($"root: e{roots[i].Index}:{roots[i].Version}");

            }
            */

            for (int i = 0; i < length; i++)
            {

                Entity root = roots[i];
                //Debug.LogWarning($"Iterating over referenced root: e{root.Index}:{root.Version}");
                if (state.EntityManager.HasComponent<Prefab>(root))
                {
                    //Debug.LogWarning($"Referenced root: e{root.Index}:{root.Version} is a prefab, continuing");
                    continue;
                }






                FirstBornSpawnee firstBornSpawnee = state.EntityManager.GetComponentData<FirstBornSpawnee>(root);
                int size = firstBornSpawnee.lastIndex - firstBornSpawnee.firstIndex - 1;
                if (size == 0)
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.LogWarning($"Something broke, some entities referencese e{root.Index}:{root.Version} but it should have no children for that!\n Entities in question:");
                    var values = rootAndChildrenMap.GetValuesForKey(root);
                    while (values.MoveNext())
                    {
                        var index = values.Current;
                        Debug.LogWarning($"{spawneeEntities[index]}");
                    }
#endif
                }
                else if (rootAndChildrenMap.CountValuesForKey(root) == size)
                {
                    //Debug.LogWarning($"Referenced root: e{root.Index}:{root.Version} rootAndChildrenMap.CountValuesForKey(root) == size");
                    /*
                    if (!firstBornSpawnee.waited && (state.WorldUnmanaged.Flags & WorldFlags.GameClient) != 0)
                    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        Debug.Log($"[SpawneeSettingSystem] Waiting another frame to ensure that all references are valid");
#endif
                        ecb.SetComponent(root, new FirstBornSpawnee { firstIndex = firstBornSpawnee.firstIndex, lastIndex = firstBornSpawnee.lastIndex, waited = true });
                        continue;
                    }
                    */


                    NestedGhostAdditionalData NGAD = state.EntityManager.GetComponentData<NestedGhostAdditionalData>(root);
                    /*
                    if (!NGAD.waited)
                    {
                        NGAD.waited = true;
                        ecb.SetComponent(root, NGAD);
                    }
                    */



                    if (!NGAD.Rereferencing)
                    {
                        if (NGAD.NonGhostRereferencing)
                        {
                            ecb.RemoveComponent<RelinkNonGhostChildrenReference>(root);
                        }
                        ecb.RemoveComponent<SpawneeIndex>(root);
                        ecb.RemoveComponent<FirstBornSpawnee>(root);
                        var val = rootAndChildrenMap.GetValuesForKey(root);
                        while (val.MoveNext())
                        {
                            Entity instance = spawneeEntities[val.Current];
                            if (state.EntityManager.HasBuffer<RelinkNonGhostChildrenReference>(instance))
                            {
                                ecb.RemoveComponent<RelinkNonGhostChildrenReference>(instance);
                            }
                            ecb.RemoveComponent<SpawneeIndex>(instance);
                        }
                        continue;
                    }
                    SpawneeIndex firstBornIndex = state.EntityManager.GetComponentData<SpawneeIndex>(root);
                    var spawneeBuffer = state.EntityManager.GetBuffer<Spawnee>(firstBornIndex.spawner);
                    Entity rootPrefab = spawneeBuffer[firstBornSpawnee.firstIndex].Value;
                    var remapBuffer = ecb.AddBuffer<PrefabRemap>(root);
                    remapBuffer.Add(new PrefabRemap { Prefab = rootPrefab, Instance = root });
                    if (NGAD.NonGhostRereferencing)
                    {
                        var instanceReferencesRoot = state.EntityManager.GetBuffer<RelinkNonGhostChildrenReference>(root);
                        var prefabReferencesRoot = state.EntityManager.GetBuffer<RelinkNonGhostChildrenReference>(rootPrefab);
                        for (int j = 0; j < prefabReferencesRoot.Length; j++)
                        {
                            remapBuffer.Add(new PrefabRemap { Prefab = prefabReferencesRoot[j].Reference, Instance = instanceReferencesRoot[j].Reference });
                            ecb.AddSharedComponent(instanceReferencesRoot[j].Reference, new SpawneeSharedRootReference { Instance = root, Prefab = rootPrefab });
                        }
                        ecb.RemoveComponent<RelinkNonGhostChildrenReference>(root);
                    }
                    ecb.AddSharedComponent(root, new SpawneeSharedRootReference { Instance = root, Prefab = rootPrefab });
                    ecb.RemoveComponent<SpawneeIndex>(root);
                    ecb.RemoveComponent<FirstBornSpawnee>(root);
                    /*
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.Log($"SERVER[SpawneeSettingSystem] Added root relink:  e{rootPrefab.Index}:{rootPrefab.Version} -> e{root.Index}:{root.Version}");
#endif
                    */
                    var values = rootAndChildrenMap.GetValuesForKey(root);
                    while (values.MoveNext())
                    {
                        var index = values.Current;

                        Entity instance = spawneeEntities[index];

                        Entity prefab = spawneeBuffer[spawneeIndex[index].index].Value;
                        /*
                        if (state.EntityManager.HasComponent<Test>(spawneeEntities[index]))
                        {
                            var test = state.EntityManager.GetComponentData<Test>(spawneeEntities[index]);
                            Debug.LogWarning($"SERVER-------------------------------------------Test: gh{test.ghostedField.Index}:{test.ghostedField.Version}; ngh{test.non_GhostedField.Index}:{test.non_GhostedField.Version}");
                        }
                        */

                        remapBuffer.Add(new PrefabRemap { Prefab = prefab, Instance = instance });

                        if (state.EntityManager.HasComponent<RelinkNonGhostChildrenReference>(instance) && state.EntityManager.HasComponent<RelinkNonGhostChildrenReference>(prefab))
                        {
                            var instanceReferences = state.EntityManager.GetBuffer<RelinkNonGhostChildrenReference>(instance);
                            var prefabReferences = state.EntityManager.GetBuffer<RelinkNonGhostChildrenReference>(prefab);
                            for (int j = 0; j < prefabReferences.Length; j++)
                            {
                                remapBuffer.Add(new PrefabRemap { Prefab = prefabReferences[j].Reference, Instance = instanceReferences[j].Reference });
                                ecb.AddSharedComponent(instanceReferences[j].Reference, new SpawneeSharedRootReference { Instance = root, Prefab = rootPrefab });
                            }
                            ecb.RemoveComponent<RelinkNonGhostChildrenReference>(instance);
                        }
                        ecb.AddSharedComponent(instance, new SpawneeSharedRootReference { Instance = root, Prefab = rootPrefab });
                        ecb.RemoveComponent<SpawneeIndex>(instance);
                        /*
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        Debug.Log($"SERVER[SpawneeSettingSystem] Added spawnee relink: e{prefab.Index}:{prefab.Version} -> e{instance.Index}:{instance.Version}");
#endif
                        */
                    }

                }
                else if (rootAndChildrenMap.CountValuesForKey(root) > size)
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.LogWarning($"SERVER[SpawneeSettingSystem] Found too many spawnees for the firstborn:{root.Index}:{root.Version}. This shouldn't happen. {rootAndChildrenMap.CountValuesForKey(root)}>{size}");
#endif
                }
                else
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.Log($"SERVER[SpawneeSettingSystem] Couldn't find all spawnees for the firstborn:{root.Index}:{root.Version}, will try next frame. {rootAndChildrenMap.CountValuesForKey(root)}!={size}");
#endif
                }
            }








            ecb.Playback(state.EntityManager);

        }

    }
    [BurstCompile]
    [UpdateInGroup(typeof(GhostSimulationSystemGroup), OrderFirst = true)]//I am not sure why I have put it into GhostSimulationSystemGroup, and not some other group, but it works, so... don't touch it?
    [UpdateBefore(typeof(SpawneeSettingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct SpawneeCheckingSystem : ISystem
    {
        EntityQuery spawneeQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            spawneeQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<GhostRootLink, SpawneeIndex>()//, PrefabReference>()
.WithNone<FirstBornSpawnee>()
.Build(ref state);
            state.RequireForUpdate(spawneeQuery);
        }

        //no burst because of EntityComponentAccess.FindAndReplaceAllFieldsInEntityComponentsDictionaryWithBlackList
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //Debug.LogWarning($"SpawneeCheckingSystem, clientworld?:{(state.WorldUnmanaged.Flags & WorldFlags.GameClient) != 0}");
            NativeParallelMultiHashMap<Entity, int> rootAndChildrenMap = new NativeParallelMultiHashMap<Entity, int>(0, Allocator.Temp);
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> spawneeEntities = spawneeQuery.ToEntityArray(Allocator.Temp);
            NativeArray<GhostRootLink> ghostRootLinks = spawneeQuery.ToComponentDataArray<GhostRootLink>(Allocator.Temp);
            NativeArray<SpawneeIndex> spawneeIndex = spawneeQuery.ToComponentDataArray<SpawneeIndex>(Allocator.Temp);
            for (int i = 0; i < spawneeEntities.Length; i++)
            {
                if (ghostRootLinks[i].Value != Entity.Null)
                {
                    rootAndChildrenMap.Add(ghostRootLinks[i].Value, i);
                }
            }
            (NativeArray<Entity> roots, int length) = rootAndChildrenMap.GetUniqueKeyArray(Allocator.Temp);
            for (int i = 0; i < length; i++)
            {

                Entity root = roots[i];
                if (state.EntityManager.HasComponent<Prefab>(root))
                {
                    continue;
                }






                FirstBornSpawnee firstBornSpawnee = state.EntityManager.GetComponentData<FirstBornSpawnee>(root);
                int size = firstBornSpawnee.lastIndex - firstBornSpawnee.firstIndex - 1;
                if (size == 0)
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.LogWarning($"Something broke, some entities referencese {root.Index}:{root.Version} but it should have no children for that!\n Entities in question:");
                    var values = rootAndChildrenMap.GetValuesForKey(root);
                    while (values.MoveNext())
                    {
                        var index = values.Current;
                        Debug.LogWarning($"{spawneeEntities[index]}");
                    }
#endif
                }
                else if (rootAndChildrenMap.CountValuesForKey(root) == size)
                {
                        /*
                        if (!firstBornSpawnee.waited && (state.WorldUnmanaged.Flags & WorldFlags.GameClient) != 0)
                        {
    #if DEVELOPMENT_BUILD || UNITY_EDITOR
                            Debug.Log($"[SpawneeSettingSystem] Waiting another frame to ensure that all references are valid");
    #endif
                            ecb.SetComponent(root, new FirstBornSpawnee { firstIndex = firstBornSpawnee.firstIndex, lastIndex = firstBornSpawnee.lastIndex, waited = true });
                            continue;
                        }
                        */


                        NestedGhostAdditionalData NGAD = state.EntityManager.GetComponentData<NestedGhostAdditionalData>(root);
                    /*
                    if (!NGAD.waited)
                    {
                        continue;
                    }
                    */



                    if (!NGAD.Rereferencing)
                    {
                        if (NGAD.NonGhostRereferencing)
                        {
                            ecb.RemoveComponent<RelinkNonGhostChildrenReference>(root);
                        }
                        ecb.RemoveComponent<SpawneeIndex>(root);
                        ecb.RemoveComponent<FirstBornSpawnee>(root);
                        var val = rootAndChildrenMap.GetValuesForKey(root);
                        while (val.MoveNext())
                        {
                            Entity instance = spawneeEntities[val.Current];
                            if (state.EntityManager.HasBuffer<RelinkNonGhostChildrenReference>(instance))
                            {
                                ecb.RemoveComponent<RelinkNonGhostChildrenReference>(instance);
                            }
                            ecb.RemoveComponent<SpawneeIndex>(instance);
                        }
                        continue;
                    }
                    SpawneeIndex firstBornIndex = state.EntityManager.GetComponentData<SpawneeIndex>(root);
                    var spawneeBuffer = state.EntityManager.GetBuffer<Spawnee>(firstBornIndex.spawner);
                    Entity rootPrefab = spawneeBuffer[firstBornSpawnee.firstIndex].Value;
                    var remapBuffer = ecb.AddBuffer<PrefabRemap>(root);
                    remapBuffer.Add(new PrefabRemap { Prefab = rootPrefab, Instance = root });
                    if (NGAD.NonGhostRereferencing)
                    {
                        var instanceReferencesRoot = state.EntityManager.GetBuffer<RelinkNonGhostChildrenReference>(root);
                        var prefabReferencesRoot = state.EntityManager.GetBuffer<RelinkNonGhostChildrenReference>(rootPrefab);
                        for (int j = 0; j < prefabReferencesRoot.Length; j++)
                        {
                            remapBuffer.Add(new PrefabRemap { Prefab = prefabReferencesRoot[j].Reference, Instance = instanceReferencesRoot[j].Reference });
                            ecb.AddSharedComponent(instanceReferencesRoot[j].Reference, new SpawneeSharedRootReference { Instance = root, Prefab = rootPrefab });
                        }
                        ecb.RemoveComponent<RelinkNonGhostChildrenReference>(root);
                    }
                    ecb.AddSharedComponent(root, new SpawneeSharedRootReference { Instance = root, Prefab = rootPrefab });
                    ecb.RemoveComponent<SpawneeIndex>(root);
                    ecb.RemoveComponent<FirstBornSpawnee>(root);
                    /*
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                                        Debug.Log($"[SpawneeSettingSystem] Added root relink:  e{rootPrefab.Index}:{rootPrefab.Version} -> e{root.Index}:{root.Version}");
#endif
                    */
                    var values = rootAndChildrenMap.GetValuesForKey(root);
                    while (values.MoveNext())
                    {
                        var index = values.Current;

                        Entity instance = spawneeEntities[index];

                        Entity prefab = spawneeBuffer[spawneeIndex[index].index].Value;
                        /*
                        if (state.EntityManager.HasComponent<Test>(spawneeEntities[index]))
                        {
                            var test = state.EntityManager.GetComponentData<Test>(spawneeEntities[index]);
                            Debug.LogWarning($"-------------------------------------------Test: gh{test.ghostedField.Index}:{test.ghostedField.Version}; ngh{test.non_GhostedField.Index}:{test.non_GhostedField.Version}");
                        }
                        */

                        remapBuffer.Add(new PrefabRemap { Prefab = prefab, Instance = instance });

                        if (state.EntityManager.HasComponent<RelinkNonGhostChildrenReference>(instance) && state.EntityManager.HasComponent<RelinkNonGhostChildrenReference>(prefab))
                        {
                            var instanceReferences = state.EntityManager.GetBuffer<RelinkNonGhostChildrenReference>(instance);
                            var prefabReferences = state.EntityManager.GetBuffer<RelinkNonGhostChildrenReference>(prefab);
                            for (int j = 0; j < prefabReferences.Length; j++)
                            {
                                remapBuffer.Add(new PrefabRemap { Prefab = prefabReferences[j].Reference, Instance = instanceReferences[j].Reference });
                                ecb.AddSharedComponent(instanceReferences[j].Reference, new SpawneeSharedRootReference { Instance = root, Prefab = rootPrefab });
                            }
                            ecb.RemoveComponent<RelinkNonGhostChildrenReference>(instance);
                        }
                        ecb.AddSharedComponent(instance, new SpawneeSharedRootReference { Instance = root, Prefab = rootPrefab });
                        ecb.RemoveComponent<SpawneeIndex>(instance);
                        /*
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                                                Debug.Log($"[SpawneeSettingSystem] Added spawnee relink: e{prefab.Index}:{prefab.Version} -> e{instance.Index}:{instance.Version}");
#endif
                        */
                    }

                }
                else if (rootAndChildrenMap.CountValuesForKey(root) > size)
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.LogWarning($"[SpawneeSettingSystem] Found too many spawnees for the firstborn:{root.Index}:{root.Version}. This shouldn't happen. {rootAndChildrenMap.CountValuesForKey(root)}>{size}");
#endif
                }
                else
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.Log($"[SpawneeSettingSystem] Couldn't find all spawnees for the firstborn:{root.Index}:{root.Version}, will try next frame. {rootAndChildrenMap.CountValuesForKey(root)}!={size}");
#endif
                }
            }








            ecb.Playback(state.EntityManager);

        }

    }
    [BurstCompile]
    [UpdateInGroup(typeof(GhostSimulationSystemGroup), OrderFirst = true)]//I am not sure why I have put it into GhostSimulationSystemGroup, and not some other group, but it works, so... don't touch it?
    [UpdateAfter(typeof(SpawneeCheckingSystem))]
    [UpdateAfter(typeof(SpawneeCheckingSystemServer))]
    [UpdateBefore(typeof(SpawneeCleanupSystem))]//SpawneeCleanupSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct SpawneeSettingSystem : ISystem
    {
        EntityQuery firstBornQuery;
        EntityQuery spawneeQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            spawneeQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<SpawneeSharedRootReference>()
.Build(ref state);
            firstBornQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<SpawneeSharedRootReference,PrefabRemap>()
.Build(ref state);
            state.RequireForUpdate(spawneeQuery);
            state.RequireForUpdate(firstBornQuery);
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //Debug.LogWarning("SpawneeSettingSystem");
            var entityRemapping = state.EntityManager.CreateEntityRemapArray(Allocator.TempJob);
            /*
            for(int i = 0;  i < entityRemapping.Length; i++)
            {
                entityRemapping[i] = new EntityRemapUtility.EntityRemapInfo { SourceVersion = int.MaxValue, Target = Entity.Null };
            }
            */
            //EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> firstBornEntities = firstBornQuery.ToEntityArray(Allocator.Temp);

            for(int i =0; i< firstBornEntities.Length; i++)
            {

                var map = state.EntityManager.GetBuffer<PrefabRemap>(firstBornEntities[i]);

                Entity prefab = state.EntityManager.GetSharedComponent<SpawneeSharedRootReference>(firstBornEntities[i]).Prefab;
                //Debug.LogWarning($"SharedFilter for the remap: prefab{prefab.Index}:{prefab.Version} -> instance{firstBornEntities[i].Index}:{firstBornEntities[i].Version}");
                //EntityRemapUtility.AddEntityRemapping(ref entityRemapping, prefab, firstBornEntities[i]);//

                //Debug.Log($"{Entity.Null.Version == -1}?");////

                //Debug.Log($"Before: but does it add prefab?  prefab{map[0].Prefab.Index}:{map[0].Prefab.Version} -> instance{firstBornEntities[i].Index}:{firstBornEntities[i].Version} target ? = {entityRemapping[firstBornEntities[i].Index].Target}, version? = {entityRemapping[firstBornEntities[i].Index].SourceVersion}");//
                for (int j =0; j< map.Length; j++)
                {
                    //Debug.Log($"mapping, {j}: prefab{map[j].Prefab.Index}:{map[j].Prefab.Version} -> instance{map[j].Instance.Index}:{map[j].Instance.Version}");
                    EntityRemapUtility.AddEntityRemapping(ref entityRemapping, map[j].Prefab, map[j].Instance);
                    /*
                    EntityRemapUtility.AddEntityRemapping(ref entityRemapping, map[j].Instance, map[j].Instance);
                    Debug.Log($"mapping, {j}: prefab{map[j].Prefab.Index}:{map[j].Prefab.Version} -> instance{map[j].Instance.Index}:{map[j].Instance.Version} |||||||||{map[j].Prefab.Version}?={entityRemapping[map[j].Prefab.Index].SourceVersion}? -> instance{entityRemapping[map[j].Prefab.Index].Target.Index}:{entityRemapping[map[j].Prefab.Index].Target.Version}");
                    //entityRemapping[map[j].Prefab.Index] = new EntityRemapUtility.EntityRemapInfo {SourceVersion = map[j].Prefab .Version, Target = map[j].Instance };

                    Debug.LogWarning("test:");//

                    if (state.EntityManager.HasBuffer<LinkedEntityGroup>(map[j].Instance)){
                        var leg = state.EntityManager.GetBuffer<LinkedEntityGroup>(map[j].Instance);

                        var prefabLeg = state.EntityManager.GetBuffer<LinkedEntityGroup>(map[j].Prefab);
                        UnityEngine.Debug.Log($"in some child: entity:{prefabLeg[0].Value.Index}:{prefabLeg[0].Value.Version} -> entity:{leg[0].Value.Index}:{leg[0].Value.Version} ||||||||     {prefabLeg[0].Value.Version}?={entityRemapping[prefabLeg[0].Value.Index].SourceVersion}? -> instance{entityRemapping[prefabLeg[0].Value.Index].Target.Index}:{entityRemapping[prefabLeg[0].Value.Index].Target.Version}                          | ||||||||||||||||{leg[0].Value.Version}?={entityRemapping[leg[0].Value.Index].SourceVersion}? -> instance{entityRemapping[leg[0].Value.Index].Target.Index}:{entityRemapping[leg[0].Value.Index].Target.Version}");

                    }
                    */
                }
                //Debug.LogWarning($"spawneeQuery filter should be: {map.Length} entities");
                map.Clear();
                //Debug.Log($"After: but does it add prefab?  prefab{map[0].Prefab.Index}:{map[0].Prefab.Version} -> instance{firstBornEntities[i].Index}:{firstBornEntities[i].Version} target ? = {entityRemapping[firstBornEntities[i].Index].Target}, version? = {entityRemapping[firstBornEntities[i].Index].SourceVersion}");//
                // ecb.RemoveComponent<PrefabRemap>(firstBornEntities[i]);
                spawneeQuery.SetSharedComponentFilter(new SpawneeSharedRootReference { Instance = firstBornEntities[i], Prefab = prefab });

                //Debug.LogWarning($"spawneeQuery filter: {spawneeQuery.CalculateEntityCount()} entities found");
                var dstChunks = spawneeQuery.ToArchetypeChunkArray(Allocator.TempJob);
                //spawneeQuery.ResetFilter();
                //ecb.RemoveComponent<SpawneeSharedRootReference>(spawneeQuery, EntityQueryCaptureMode.AtPlayback);
                //Debug.Log("Spawnee Remap in progress!");
                EntityComponentAccess.RemapEntitiesForDiffer(state.EntityManager, entityRemapping,dstChunks);
                //state.EntityManager.RemapEntitiesForDiffer(entityRemapping,new NativeArray<ArchetypeChunk>(), dstChunks);
                //Debug.Log("Spawnee Remap end!");
                dstChunks.Dispose();//
            }
            entityRemapping.Dispose();
            //spawneeQuery.ResetFilter();
            //ecb.RemoveComponent<SpawneeSharedRootReference>(spawneeQuery, EntityQueryCaptureMode.AtPlayback);
            //ecb.Playback(state.EntityManager);

        }

    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(GhostSimulationSystemGroup), OrderFirst = true)]//I am not sure why I have put it into GhostSimulationSystemGroup, and not some other group, but it works, so... don't touch it?
    [UpdateAfter(typeof(SpawneeCheckingSystem))]
    [UpdateBefore(typeof(RelinkSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct SpawneeCleanupSystem : ISystem
    {
        EntityQuery firstBornQuery;
        EntityQuery spawneeQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            spawneeQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<SpawneeSharedRootReference>()
.Build(ref state);
            firstBornQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<SpawneeSharedRootReference,PrefabRemap>()
.Build(ref state);
            state.RequireForUpdate(spawneeQuery);
            state.RequireForUpdate(firstBornQuery);
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //Debug.LogWarning("Cleanup!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> firstBornEntities = firstBornQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < firstBornEntities.Length; i++)
            {
                var prefab = state.EntityManager.GetSharedComponent<SpawneeSharedRootReference>(firstBornEntities[i]).Prefab;

                spawneeQuery.SetSharedComponentFilter(new SpawneeSharedRootReference { Instance = firstBornEntities[i] , Prefab = prefab });

                NativeArray<Entity> spawnees = spawneeQuery.ToEntityArray(Allocator.Temp);

                for(int j = 0; j < spawnees.Length; j++)
                {
                    ecb.RemoveComponent<SpawneeSharedRootReference>(spawnees[j]);
                }
                ecb.RemoveComponent<PrefabRemap>(firstBornEntities[i]);
                ecb.RemoveComponent<SpawneeSharedRootReference>(firstBornEntities[i]);
            }
            ecb.Playback(state.EntityManager);

        }

    }
    
    /*
    [BurstCompile]
    [UpdateInGroup(typeof(GhostSimulationSystemGroup), OrderFirst = true)]//I am not sure why I have put it into GhostSimulationSystemGroup, and not some other group, but it works, so... don't touch it?
    [UpdateBefore(typeof(RelinkSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct SpawneeSettingSystem : ISystem
    {
        EntityQuery spawneeQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            spawneeQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<GhostRootLink, SpawneeIndex>()//, PrefabReference>()
.WithNone<FirstBornSpawnee>()
.Build(ref state);
            state.RequireForUpdate(spawneeQuery);
        }

        //no burst because of EntityComponentAccess.FindAndReplaceAllFieldsInEntityComponentsDictionaryWithBlackList
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NativeParallelMultiHashMap<Entity, int> rootAndChildrenMap = new NativeParallelMultiHashMap<Entity, int>(0, Allocator.Temp);
            var entityRemapping = state.EntityManager.CreateEntityRemapArray(Allocator.TempJob);
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> spawneeEntities = spawneeQuery.ToEntityArray(Allocator.Temp);
            //NativeArray<PrefabReference> prefabReferences = spawneeQuery.ToComponentDataArray<PrefabReference>(Allocator.Temp);
            NativeArray<GhostRootLink> ghostRootLinks = spawneeQuery.ToComponentDataArray<GhostRootLink>(Allocator.Temp);
            NativeArray<SpawneeIndex> spawneeIndex = spawneeQuery.ToComponentDataArray<SpawneeIndex>(Allocator.Temp);
            for (int i = 0; i < spawneeEntities.Length; i++)
            {
                if (ghostRootLinks[i].Value != Entity.Null)
                {
                    rootAndChildrenMap.Add(ghostRootLinks[i].Value, i);
                }
            }
            NativeArray<Entity> roots = rootAndChildrenMap.GetKeyArray(Allocator.Temp);
            for (int i = 0; i < roots.Length; i++)
            {
                Entity root = roots[i];
                if (state.EntityManager.HasComponent<Prefab>(root))
                {
                    continue;
                }

                FirstBornSpawnee firstBornSpawnee = state.EntityManager.GetComponentData<FirstBornSpawnee>(root);
                int size = firstBornSpawnee.lastIndex - firstBornSpawnee.firstIndex - 1;
                if (size == 0)
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.LogWarning($"Something broke, some entities references {root} but it should have no children for that!\n Entities in question:");
                    var values = rootAndChildrenMap.GetValuesForKey(root);
                    while (values.MoveNext())
                    {
                        var index = values.Current;
                        Debug.LogWarning($"{spawneeEntities[index]}");
                    }
#endif
                }
                else if (rootAndChildrenMap.CountValuesForKey(root) == size)
                {
                    NestedGhostAdditionalData NGAD = state.EntityManager.GetComponentData<NestedGhostAdditionalData>(root);

                    if (!NGAD.Rereferencing)
                    {
                        if (NGAD.NonGhostRereferencing)
                        {
                            ecb.RemoveComponent<RelinkNonGhostChildrenReference>(root);
                        }
                        ecb.RemoveComponent<SpawneeIndex>(root);
                        ecb.RemoveComponent<SpawneeData>(root);
                        ecb.RemoveComponent<FirstBornSpawnee>(root);
                        var val = rootAndChildrenMap.GetValuesForKey(root);
                        while (val.MoveNext())
                        {
                            Entity instance = spawneeEntities[val.Current];
                            if (state.EntityManager.HasBuffer<RelinkNonGhostChildrenReference>(instance))
                            {
                                ecb.RemoveComponent<RelinkNonGhostChildrenReference>(instance);
                            }
                            ecb.RemoveComponent<SpawneeIndex>(instance);
                            ecb.RemoveComponent<SpawneeData>(instance);
                        }
                        continue;
                    }


                    NativeParallelHashMap<Entity, Entity> prefabToInstanceMap= new NativeParallelHashMap<Entity, Entity>(0, Allocator.Temp);
                    //Dictionary<Entity, Entity> prefabToInstanceMap = new Dictionary<Entity, Entity>();
                    SpawneeIndex firstBornIndex = state.EntityManager.GetComponentData<SpawneeIndex>(root);
                    var spawneeBuffer = state.EntityManager.GetBuffer<Spawnee>(firstBornIndex.spawner);
                    Entity rootPrefab = spawneeBuffer[firstBornSpawnee.firstIndex].Value;
                    prefabToInstanceMap.Add(rootPrefab, root);

                    var instanceReferencesRoot = state.EntityManager.GetBuffer<RelinkNonGhostChildrenReference>(root);
                    var prefabReferencesRoot = state.EntityManager.GetBuffer<RelinkNonGhostChildrenReference>(rootPrefab);
                    for (int j = 0; j < prefabReferencesRoot.Length; j++)
                    {
                        prefabToInstanceMap.Add(prefabReferencesRoot[j].Reference, instanceReferencesRoot[j].Reference);
                    }
                    ecb.RemoveComponent<RelinkNonGhostChildrenReference>(root);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    //                    Debug.Log($"[SpawneeSettingSystem] Added root relink: {rootPrefab} -> {root}");
#endif
                    var values = rootAndChildrenMap.GetValuesForKey(root);
                    while (values.MoveNext())
                    {
                        var index = values.Current;

                        Entity instance = spawneeEntities[index];

                        Entity prefab = spawneeBuffer[spawneeIndex[index].index].Value;

                        prefabToInstanceMap.Add(prefab, instance);

                        if (state.EntityManager.HasComponent<RelinkNonGhostChildrenReference>(instance) && state.EntityManager.HasComponent<RelinkNonGhostChildrenReference>(prefab))
                        {
                            var instanceReferences = state.EntityManager.GetBuffer<RelinkNonGhostChildrenReference>(instance);
                            var prefabReferences = state.EntityManager.GetBuffer<RelinkNonGhostChildrenReference>(prefab);
                            for (int j = 0; j < prefabReferences.Length; j++)
                            {
                                prefabToInstanceMap.Add(prefabReferences[j].Reference, instanceReferences[j].Reference);
                            }
                            ecb.RemoveComponent<RelinkNonGhostChildrenReference>(instance);
                        }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        //                        Debug.Log($"[SpawneeSettingSystem] Added spawnee relink: {prefab} -> {instance}");
#endif
                    }
                    EntityComponentAccess.FindAndReplaceAllEntityReferencesInEntityComponentsUsingHashMap(state.EntityManager, ecb, root, ref prefabToInstanceMap);

                    //EntityComponentAccess.FindAndReplaceAllFieldsInEntityComponentsDictionaryWithBlackList<Entity>(state.EntityManager, ecb, root, prefabToInstanceMap, blackList);
                    ecb.RemoveComponent<SpawneeIndex>(root);
                    ecb.RemoveComponent<SpawneeData>(root);
                    ecb.RemoveComponent<FirstBornSpawnee>(root);
                    values.Reset();
                    while (values.MoveNext())
                    {
                        var index = values.Current;
                        Entity instance = spawneeEntities[index];
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        //                      Debug.Log($"[SpawneeSettingSystem] Replacing entity references in fields in {instance}...");
#endif
                        EntityComponentAccess.FindAndReplaceAllEntityReferencesInEntityComponentsUsingHashMap(state.EntityManager, ecb, instance, ref prefabToInstanceMap);
                        //EntityComponentAccess.FindAndReplaceAllFieldsInEntityComponentsDictionaryWithBlackList<Entity>(state.EntityManager, ecb, instance, prefabToInstanceMap, blackList);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                        //                        Debug.Log($"[SpawneeSettingSystem] Done entity references in fields in {instance}!");
#endif
                        ecb.RemoveComponent<SpawneeIndex>(instance);
                        ecb.RemoveComponent<SpawneeData>(instance);
                    }

                }
                else if (rootAndChildrenMap.CountValuesForKey(root) > size)
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.LogWarning($"[SpawneeSettingSystem] Found too many spawnees for the firstborn:{root.Index}:{root.Version}. This shouldn't happen. {rootAndChildrenMap.CountValuesForKey(root)}>{size}");
#endif
                }
                else
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.Log($"[SpawneeSettingSystem] Couldn't find all spawnees for the firstborn:{root.Index}:{root.Version}, will try next frame. {rootAndChildrenMap.CountValuesForKey(root)}!={size}");
#endif
                }
            }








                ecb.Playback(state.EntityManager);
           
        }

    }
*/
}


/*
        EntityQuery spawneeQuery;
NativeArray<ComponentType> blackList;
// no burst because of typeof
public void OnCreate(ref SystemState state)
{
    spawneeQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<SpawneeIndex, SpawneeData, GhostRootLink>()
.WithNone<FirstBornSpawnee>()
.Build(ref state);
    blackList = new NativeArray<ComponentType>(new ComponentType[] {
typeof(SpawneeData),
typeof(SpawneeIndex),
typeof(Parent),
typeof(LinkedEntityGroup),
typeof(FirstBornSpawnee),
typeof(GhostRootLink),
}, Allocator.Persistent);
    state.RequireForUpdate(spawneeQuery);
}
*/




















/*
           EntityCommandBuffer ecb = new(Allocator.Temp);
           NativeArray<Entity> spawneeEntities = spawneeQuery.ToEntityArray(Allocator.Temp);
           NativeParallelMultiHashMap<Entity, int> rootAndChildrenMap = new NativeParallelMultiHashMap<Entity, int>(0, Allocator.Temp);
           NativeArray<SpawneeIndex> spawneeIndex = spawneeQuery.ToComponentDataArray<SpawneeIndex>(Allocator.Temp);
           NativeArray<GhostRootLink> ghostRootLinks = spawneeQuery.ToComponentDataArray<GhostRootLink>(Allocator.Temp);
           for (int i = 0; i < spawneeEntities.Length; i++)
           {
               rootAndChildrenMap.Add(ghostRootLinks[i].Value, i);
           }
           NativeArray<Entity> roots = rootAndChildrenMap.GetKeyArray(Allocator.Temp);
           for (int i = 0; i < roots.Length; i++)
           {
               Entity root = roots[i];
               if (root == Entity.Null)
               {
                   continue;
               }
               if (state.EntityManager.HasComponent<Prefab>(root))
               {
                   continue;
               }

               FirstBornSpawnee firstBornSpawnee = state.EntityManager.GetComponentData<FirstBornSpawnee>(root);
               int size = firstBornSpawnee.lastIndex - firstBornSpawnee.firstIndex-1;
               if (size == 0)
               {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                   Debug.LogWarning($"Something broke, some entities references {root} but it should have no children for that!\n Entities in question:");
                   var values = rootAndChildrenMap.GetValuesForKey(root);
                   while (values.MoveNext())
                   {
                       var index = values.Current;
                       Debug.LogWarning($"{spawneeEntities[index]}");
                   }
#endif
               }
               else if (rootAndChildrenMap.CountValuesForKey(root) == size)
               {
                   NestedGhostAdditionalData NGAD = state.EntityManager.GetComponentData<NestedGhostAdditionalData>(root);

                   if (!NGAD.Rereferencing)
                   {
                       if (NGAD.NonGhostRereferencing)
                       {
                           ecb.RemoveComponent<RelinkNonGhostChildrenReference>(root);
                       }
                       ecb.RemoveComponent<SpawneeIndex>(root);
                       ecb.RemoveComponent<SpawneeData>(root);
                       ecb.RemoveComponent<FirstBornSpawnee>(root);
                       var val = rootAndChildrenMap.GetValuesForKey(root);
                       while (val.MoveNext())
                       {
                           Entity instance = spawneeEntities[val.Current];
                           if (state.EntityManager.HasBuffer<RelinkNonGhostChildrenReference>(instance))
                           {
                               ecb.RemoveComponent<RelinkNonGhostChildrenReference>(instance);
                           }
                           ecb.RemoveComponent<SpawneeIndex>(instance);
                           ecb.RemoveComponent<SpawneeData>(instance);
                       }
                           continue;
                   }



                   Dictionary<Entity, Entity> spawneeBlacklist = new Dictionary<Entity, Entity>();
                   SpawneeIndex firstBornIndex  = state.EntityManager.GetComponentData<SpawneeIndex>(root);
                   var spawneeBuffer = state.EntityManager.GetBuffer<Spawnee>(firstBornIndex.spawner);
                   Entity rootPrefab = spawneeBuffer[firstBornSpawnee.firstIndex].Value;
                   spawneeBlacklist.Add(rootPrefab, root);

                   var instanceReferencesRoot = state.EntityManager.GetBuffer<RelinkNonGhostChildrenReference>(root);
                   var prefabReferencesRoot = state.EntityManager.GetBuffer<RelinkNonGhostChildrenReference>(rootPrefab);
                   for (int j = 0; j < prefabReferencesRoot.Length; j++)
                   {
                       spawneeBlacklist.Add(prefabReferencesRoot[j].Reference, instanceReferencesRoot[j].Reference);
                   }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
//                    Debug.Log($"[SpawneeSettingSystem] Added root relink: {rootPrefab} -> {root}");
#endif
                   var values = rootAndChildrenMap.GetValuesForKey(root);
                   while (values.MoveNext())
                   {
                       var index = values.Current;

                       Entity instance = spawneeEntities[index];

                       Entity prefab = spawneeBuffer[spawneeIndex[index].index].Value;

                       spawneeBlacklist.Add(prefab, instance);

                       if (state.EntityManager.HasComponent<RelinkNonGhostChildrenReference>(instance) && state.EntityManager.HasComponent<RelinkNonGhostChildrenReference>(prefab))
                       {
                           var instanceReferences = state.EntityManager.GetBuffer<RelinkNonGhostChildrenReference>(instance);
                           var prefabReferences = state.EntityManager.GetBuffer<RelinkNonGhostChildrenReference>(prefab);
                           for(int  j = 0;j< prefabReferences.Length; j++)
                           {
                               spawneeBlacklist.Add(prefabReferences[j].Reference, instanceReferences[j].Reference);
                           }
                           ecb.RemoveComponent<RelinkNonGhostChildrenReference>(instance);
                       }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
//                        Debug.Log($"[SpawneeSettingSystem] Added spawnee relink: {prefab} -> {instance}");
#endif
                   }


                   EntityComponentAccess.FindAndReplaceAllFieldsInEntityComponentsDictionaryWithBlackList<Entity>(state.EntityManager, ecb, root, spawneeBlacklist, blackList);
                   ecb.RemoveComponent<SpawneeIndex>(root);
                   ecb.RemoveComponent<SpawneeData>(root);
                   ecb.RemoveComponent<FirstBornSpawnee>(root);
                   values.Reset();
                   while (values.MoveNext())
                   {
                       var index = values.Current;
                       Entity instance = spawneeEntities[index];
#if DEVELOPMENT_BUILD || UNITY_EDITOR
 //                      Debug.Log($"[SpawneeSettingSystem] Replacing entity references in fields in {instance}...");
#endif
                       EntityComponentAccess.FindAndReplaceAllFieldsInEntityComponentsDictionaryWithBlackList<Entity>(state.EntityManager, ecb, instance, spawneeBlacklist, blackList);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
//                        Debug.Log($"[SpawneeSettingSystem] Done entity references in fields in {instance}!");
#endif
                       ecb.RemoveComponent<SpawneeIndex>(instance);
                       ecb.RemoveComponent<SpawneeData>(instance);
                   }

               }
               else if (rootAndChildrenMap.CountValuesForKey(root) > size)
               {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                   Debug.LogWarning($"[SpawneeSettingSystem] Found too many spawnees for the firstborn:{root.Index}:{root.Version}. This shouldn't happen. {rootAndChildrenMap.CountValuesForKey(root)}>{size}");
#endif
               }
               else
               {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                   Debug.Log($"[SpawneeSettingSystem] Couldn't find all spawnees for the firstborn:{root.Index}:{root.Version}, will try next frame. {rootAndChildrenMap.CountValuesForKey(root)}!={size}");
#endif
               }
           }
           ecb.Playback(state.EntityManager);
           */
