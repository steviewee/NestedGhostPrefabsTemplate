using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using NGPTemplate.Components;
using NGPTemplate.Misc;
using Unity.Burst;
using Unity.Collections;
namespace NGPTemplate.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct InputCommandRequestSystem : ISystem
    {
        EntityQuery inputCommandRequestQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            inputCommandRequestQuery = new EntityQueryBuilder(Allocator.Temp)
    .WithAll<InputCommandRequest, OwningPlayer, GhostOwner, Simulate>()
    .Build(ref state);
            state.RequireForUpdate(inputCommandRequestQuery);
            state.RequireForUpdate<InputPrefab>();
        }
        [GenerateTestsForBurstCompatibility]
        public bool CheckIfAlreadyContainsInputInstance(int prefab, DynamicBuffer<InputInstance> inputInstances)
        {
            for (int i = 0; i < inputInstances.Length; i++)
            {
                if (inputInstances[i].Prefab == prefab)
                {
                    return true;
                }
            }
            return false;
        }
        [GenerateTestsForBurstCompatibility]
        public bool CheckIfAlreadyContainsInputInstance(int prefab, DynamicBuffer<InputInstance> inputInstances, int length)
        {
            for(int i = 0;i< length; i++)
            {
                if (inputInstances[i].Prefab == prefab)
                {
                    return true;
                }
            }
            return false;
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NativeParallelMultiHashMap<Entity, InputInstance> newInputs = new NativeParallelMultiHashMap<Entity, InputInstance>(0, Allocator.Temp); 


            DynamicBuffer<InputPrefab> inputPrefabBuffer = SystemAPI.GetSingletonBuffer<InputPrefab>();
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> inputCommandEntities = inputCommandRequestQuery.ToEntityArray(Allocator.Temp);
            NativeArray<GhostOwner> owners = inputCommandRequestQuery.ToComponentDataArray<GhostOwner>(Allocator.Temp);
            NativeArray<OwningPlayer> owningPlayers = inputCommandRequestQuery.ToComponentDataArray<OwningPlayer>(Allocator.Temp);
            for (int i = 0; i < inputCommandEntities.Length; i++)
            {
                Entity owningPlayer = owningPlayers[i].Value;//why not Value???
                if (owningPlayer == Entity.Null)
                {
                    Debug.Log($"owningPlayer == Entity.Null! on client at entity{inputCommandEntities[i].Index}:{inputCommandEntities[i].Index}");
                    continue;
                }
                DynamicBuffer<InputInstance> inputInstances = state.EntityManager.GetBuffer<InputInstance>(owningPlayer);
                //int length = inputInstances.Length;
                DynamicBuffer<InputCommandRequest> inputCommandRequestBuffer = state.EntityManager.GetBuffer<InputCommandRequest>(inputCommandEntities[i]);
                //DynamicBuffer<InputReference> inputReferenceBuffer = state.EntityManager.GetBuffer<InputReference>(inputCommandEntities[i]);

                bool found = false;
                if (newInputs.ContainsKey(owningPlayer))
                {
                    var values = newInputs.GetValuesForKey(owningPlayer);

                    while (values.MoveNext())
                    {
                        var instance = values.Current;
                        for (int j = 0; j < inputCommandRequestBuffer.Length; j++)
                        {
                            if (instance.Prefab == inputCommandRequestBuffer[j].Value)
                            {
                                //inputReferenceBuffer.Add(new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstances[k].Value });
                                ecb.AppendToBuffer<InputReference>(inputCommandEntities[i], new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = instance.Value });
                                found = true;
                                break;
                            }
                        }
                    }
                    if (!found)
                    {
                        for (int j = 0; j < inputCommandRequestBuffer.Length; j++)
                        {

                            for (int k = 0; k < inputInstances.Length; k++)
                            {
                                if (inputInstances[k].Prefab == inputCommandRequestBuffer[j].Value)
                                {
                                    //inputReferenceBuffer.Add(new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstances[k].Value });
                                    ecb.AppendToBuffer<InputReference>(inputCommandEntities[i], new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstances[k].Value });
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                Entity inputInstance = ecb.Instantiate(inputPrefabBuffer[inputCommandRequestBuffer[j].Value].Value);
                                ecb.SetComponent(inputInstance, new GhostOwner { NetworkId = owners[i].NetworkId });
                                ecb.SetComponent(inputInstance, new OwningPlayer { Value = owningPlayer });
                                ecb.AppendToBuffer(owningPlayer, new InputInstance { Value = inputInstance, Prefab = inputCommandRequestBuffer[j].Value });
                                ecb.AppendToBuffer(owningPlayer, new LinkedEntityGroup { Value = inputInstance });
                                ecb.AppendToBuffer<InputReference>(inputCommandEntities[i], new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstance });
                                newInputs.Add(owningPlayer, new InputInstance { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstance });
                            }
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < inputCommandRequestBuffer.Length; j++)
                    {

                        for (int k = 0; k < inputInstances.Length; k++)
                        {
                            if (inputInstances[k].Prefab == inputCommandRequestBuffer[j].Value)
                            {
                                //inputReferenceBuffer.Add(new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstances[k].Value });
                                ecb.AppendToBuffer<InputReference>(inputCommandEntities[i], new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstances[k].Value });
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            Entity inputInstance = ecb.Instantiate(inputPrefabBuffer[inputCommandRequestBuffer[j].Value].Value);
                            ecb.SetComponent(inputInstance, new GhostOwner { NetworkId = owners[i].NetworkId });
                            ecb.SetComponent(inputInstance, new OwningPlayer { Value = owningPlayer });
                            ecb.AppendToBuffer(owningPlayer, new InputInstance { Value = inputInstance, Prefab = inputCommandRequestBuffer[j].Value });
                            ecb.AppendToBuffer(owningPlayer, new LinkedEntityGroup { Value = inputInstance });
                            ecb.AppendToBuffer<InputReference>(inputCommandEntities[i], new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstance });
                            newInputs.Add(owningPlayer, new InputInstance {Prefab= inputCommandRequestBuffer[j].Value, Value = inputInstance });
                        }
                    }

                }

            



                ecb.RemoveComponent<InputCommandRequest>(inputCommandEntities);
            }

            ecb.Playback(state.EntityManager);

        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct InputCommandRequestServerSystem : ISystem
    {
        EntityQuery inputCommandRequestQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            inputCommandRequestQuery = new EntityQueryBuilder(Allocator.Temp)
    .WithAll<InputCommandRequest, OwningPlayer, ConnectionReference, GhostOwner, Simulate>()
    .Build(ref state);
            state.RequireForUpdate(inputCommandRequestQuery);
            state.RequireForUpdate<InputPrefab>();
        }
        [GenerateTestsForBurstCompatibility]
        public bool CheckIfAlreadyContainsInputInstance(int prefab, DynamicBuffer<InputInstance> inputInstances)
        {
            for (int i = 0; i < inputInstances.Length; i++)
            {
                if (inputInstances[i].Prefab == prefab)
                {
                    return true;
                }
            }
            return false;
        }
        [GenerateTestsForBurstCompatibility]
        public bool CheckIfAlreadyContainsInputInstance(int prefab, DynamicBuffer<InputInstance> inputInstances, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (inputInstances[i].Prefab == prefab)
                {
                    return true;
                }
            }
            return false;
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NativeParallelMultiHashMap<Entity, InputInstance> newInputs = new NativeParallelMultiHashMap<Entity, InputInstance>(0, Allocator.Temp);
            DynamicBuffer<InputPrefab> inputPrefabBuffer = SystemAPI.GetSingletonBuffer<InputPrefab>();
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> inputCommandEntities = inputCommandRequestQuery.ToEntityArray(Allocator.Temp);
            NativeArray<GhostOwner> owners = inputCommandRequestQuery.ToComponentDataArray<GhostOwner>(Allocator.Temp);
            NativeArray<OwningPlayer> owningPlayers = inputCommandRequestQuery.ToComponentDataArray<OwningPlayer>(Allocator.Temp);
            NativeArray<ConnectionReference> connectionReferences = inputCommandRequestQuery.ToComponentDataArray<ConnectionReference>(Allocator.Temp);
            for (int i = 0; i < inputCommandEntities.Length; i++)
            {
                Entity owningPlayer = owningPlayers[i].Value;
                if (owningPlayer == Entity.Null)
                {
                    Debug.Log($"owningPlayer == Entity.Null on server at entity{inputCommandEntities[i].Index}:{inputCommandEntities[i].Index}");
                    continue;
                }

                DynamicBuffer<InputInstance> inputInstances = state.EntityManager.GetBuffer<InputInstance>(owningPlayer);
                //int length = inputInstances.Length;
      
                DynamicBuffer<InputCommandRequest> inputCommandRequestBuffer = state.EntityManager.GetBuffer<InputCommandRequest>(inputCommandEntities[i]);
                //DynamicBuffer<InputReference> inputReferenceBuffer = state.EntityManager.GetBuffer<InputReference>(inputCommandEntities[i]);


                bool found = false;
                if (newInputs.ContainsKey(owningPlayer))
                {
                    var values = newInputs.GetValuesForKey(owningPlayer);

                    while (values.MoveNext())
                    {
                        var instance = values.Current;
                        for (int j = 0; j < inputCommandRequestBuffer.Length; j++)
                        {
                            if (instance.Prefab == inputCommandRequestBuffer[j].Value)
                            {
                                //inputReferenceBuffer.Add(new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstances[k].Value });
                                ecb.AppendToBuffer<InputReference>(inputCommandEntities[i], new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = instance.Value });
                                found = true;
                                break;
                            }
                        }
                    }
                    if (!found)
                    {
                        for (int j = 0; j < inputCommandRequestBuffer.Length; j++)
                        {

                            for (int k = 0; k < inputInstances.Length; k++)
                            {
                                if (inputInstances[k].Prefab == inputCommandRequestBuffer[j].Value)
                                {
                                    //inputReferenceBuffer.Add(new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstances[k].Value });
                                    ecb.AppendToBuffer<InputReference>(inputCommandEntities[i], new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstances[k].Value });
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                Entity inputInstance = ecb.Instantiate(inputPrefabBuffer[inputCommandRequestBuffer[j].Value].Value);
                                ecb.SetComponent(inputInstance, new GhostOwner { NetworkId = owners[i].NetworkId });
                                ecb.SetComponent(inputInstance, new OwningPlayer { Value = owningPlayer });
                                ecb.AppendToBuffer(owningPlayer, new InputInstance { Value = inputInstance, Prefab = inputCommandRequestBuffer[j].Value });
                                ecb.AppendToBuffer(owningPlayer, new LinkedEntityGroup { Value = inputInstance });
                                ecb.AppendToBuffer(connectionReferences[i].Value, new LinkedEntityGroup { Value = inputInstance });
                                ecb.AddComponent(inputInstance, connectionReferences[i]);
                                ecb.AppendToBuffer<InputReference>(inputCommandEntities[i], new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstance });
                                newInputs.Add(owningPlayer, new InputInstance { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstance });
                            }
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < inputCommandRequestBuffer.Length; j++)
                    {

                        for (int k = 0; k < inputInstances.Length; k++)
                        {
                            if (inputInstances[k].Prefab == inputCommandRequestBuffer[j].Value)
                            {
                                //inputReferenceBuffer.Add(new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstances[k].Value });
                                ecb.AppendToBuffer<InputReference>(inputCommandEntities[i], new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstances[k].Value });
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            Entity inputInstance = ecb.Instantiate(inputPrefabBuffer[inputCommandRequestBuffer[j].Value].Value);
                            ecb.SetComponent(inputInstance, new GhostOwner { NetworkId = owners[i].NetworkId });
                            ecb.SetComponent(inputInstance, new OwningPlayer { Value = owningPlayer });
                            ecb.AppendToBuffer(owningPlayer, new InputInstance { Value = inputInstance, Prefab = inputCommandRequestBuffer[j].Value });
                            ecb.AppendToBuffer(owningPlayer, new LinkedEntityGroup { Value = inputInstance });
                            ecb.AppendToBuffer(connectionReferences[i].Value, new LinkedEntityGroup { Value = inputInstance });
                            ecb.AddComponent(inputInstance, connectionReferences[i]);
                            ecb.AppendToBuffer<InputReference>(inputCommandEntities[i], new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstance });
                            newInputs.Add(owningPlayer, new InputInstance { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstance });
                        }
                    }

                }
                /*
                bool found = false;
                for (int j = 0; j < inputCommandRequestBuffer.Length; j++)
                {

                    for (int k = 0; k < inputInstances.Length; k++)
                    {
                        if (inputInstances[k].Prefab == inputCommandRequestBuffer[j].Value)
                        {
                            //inputReferenceBuffer.Add(new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstances[k].Value });
                            ecb.AppendToBuffer<InputReference>(inputCommandEntities[i], new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstances[k].Value });
                            found = true;
                            continue;
                        }
                    }
                    if (!found)
                    {
                        Entity inputInstance = ecb.Instantiate(inputPrefabBuffer[inputCommandRequestBuffer[j].Value].Value);
                        ecb.SetComponent(inputInstance, new GhostOwner { NetworkId = owners[i].NetworkId });
                        ecb.SetComponent(inputInstance, new OwningPlayer { Value = owningPlayer });
                        ecb.AppendToBuffer(owningPlayer, new InputInstance { Value = inputInstance, Prefab = inputCommandRequestBuffer[j].Value });
                        ecb.AppendToBuffer(owningPlayer, new LinkedEntityGroup { Value = inputInstance });
                        ecb.AppendToBuffer(connectionReferences[i].Value, new LinkedEntityGroup { Value = inputInstance });
                        ecb.AddComponent(inputInstance, connectionReferences[i]);
                        //inputReferenceBuffer.Add(new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstance });

                        ecb.AppendToBuffer<InputReference>(inputCommandEntities[i], new InputReference { Prefab = inputCommandRequestBuffer[j].Value, Value = inputInstance });

                    }
                    */
                /*
                if (!CheckIfAlreadyContainsInputInstance(inputCommandRequestBuffer[j].Value, inputInstances))//, length))
                {
                    Entity inputInstance = ecb.Instantiate(inputPrefabBuffer[inputCommandRequestBuffer[j].Value].Value);
                    ecb.SetComponent(inputInstance, new GhostOwner { NetworkId = owners[i].NetworkId });
                    ecb.SetComponent(inputInstance, new OwningPlayer { Value = owningPlayer });
                    ecb.AppendToBuffer(owningPlayer, new InputInstance { Value = inputInstance, Prefab = inputCommandRequestBuffer[j].Value });
                    ecb.AppendToBuffer(owningPlayer, new LinkedEntityGroup { Value = inputInstance});
                    ecb.AppendToBuffer(connectionReferences[i].Value, new LinkedEntityGroup { Value = inputInstance });
                    ecb.AddComponent(inputInstance, connectionReferences[i]);
                    //inputInstances.Add(new InputInstance { Value = inputInstance, Prefab = inputCommandRequestBuffer[j].Value });
                }
                */
            
                ecb.RemoveComponent<InputCommandRequest>(inputCommandEntities);
            }

            ecb.Playback(state.EntityManager);

        }
    }



















    //TODO
    /*
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(InputCommandRequestSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct InputCommandRemovalSystem : ISystem
    {
        EntityQuery inputCommandRequestQuery;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            inputCommandRequestQuery = new EntityQueryBuilder(Allocator.Temp)
    .WithAll<InputCommandRequest, OwningPlayer, Simulate>()
    .Build(ref state);
            state.RequireForUpdate(inputCommandRequestQuery);
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> inputCommandEntities = inputCommandRequestQuery.ToEntityArray(Allocator.Temp);
            ecb.Playback(state.EntityManager);

        }
    }
    */
}