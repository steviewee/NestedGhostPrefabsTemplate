using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Assertions;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System.Linq;
using Zhorman.NestedGhostPrefabs.Runtime.Components;

namespace Zhorman.NestedGhostPrefabs.Runtime.Systems
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(LocalToWorldSystem))]
    public partial struct ParentChildTransformSystem : ISystem
    {
        private struct BuffAndDeltaMatrix
        {
            public DynamicBuffer<ImmitatedChild> children;
            public float4x4 delta;
            public bool newdelta;
        }


        LocalTransform ApplyDeltaMatrix(float4x4 transitionMatrix, LocalTransform childTransform)
        {
            // Convert child's LocalTransform to a matrix
            float4x4 childWorldMatrix = childTransform.ToMatrix();

            // Apply the transition matrix directly
            float4x4 newChildWorldMatrix = math.mul(transitionMatrix, childWorldMatrix);

            // Convert back to LocalTransform
            return LocalTransform.FromMatrix(newChildWorldMatrix);
        }
        float4x4 ComputeTransitionMatrix(float4x4 oldParentMatrix, float4x4 newParentMatrix)
        {
            // M_transition = M_new * inverse(M_old)
            return math.mul(newParentMatrix, math.inverse(oldParentMatrix));
        }







        private EntityQuery parentQuery;
        private EntityQuery childrenQuery;
        private EntityQuery disabledChildrenQuery;
        public void OnCreate(ref SystemState state)
        {
            childrenQuery = new EntityQueryBuilder(Allocator.Temp)
    .WithAll<ImmitatedParentReference, LocalTransform>()
    .Build(ref state);
            disabledChildrenQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<ImmitatedParentReference, LocalTransform, Disabled>().WithOptions(EntityQueryOptions.IncludeDisabledEntities)
.Build(ref state);
            parentQuery = new EntityQueryBuilder(Allocator.Temp)
.WithAll<ImmitatedChild, ImmitatedDeltaMatrix, LocalTransform>()
.Build(ref state);
            state.RequireAnyForUpdate(childrenQuery, disabledChildrenQuery, parentQuery);
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            EntityCommandBuffer ecb = new(Allocator.Temp);
            NativeArray<Entity> disabledChildrenEntities = disabledChildrenQuery.ToEntityArray(Allocator.Temp);
            NativeArray<ImmitatedParentReference> disabledParentReference = disabledChildrenQuery.ToComponentDataArray<ImmitatedParentReference>(Allocator.Temp);
            for (int i = 0; i < disabledChildrenEntities.Length; i++)
            {
                ecb.SetComponent<ImmitatedParentReference>(disabledChildrenEntities[i],new ImmitatedParentReference {
                    Value = disabledParentReference[i].Value,
                    previousValue = Entity.Null,
                    oldlyParented = false
                });
            }

            NativeArray<Entity> parentEntities = parentQuery.ToEntityArray(Allocator.Temp);
            NativeArray<LocalTransform> parentTransforms = parentQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            NativeArray<ImmitatedDeltaMatrix> parentDeltaMatrices = parentQuery.ToComponentDataArray<ImmitatedDeltaMatrix>(Allocator.Temp);
            NativeHashMap<Entity, BuffAndDeltaMatrix> parents = new NativeHashMap<Entity, BuffAndDeltaMatrix>(parentEntities.Length,Allocator.Temp);

            for (int i = 0; i < parentEntities.Length; i++)
            {
                var buff = state.EntityManager.GetBuffer<ImmitatedChild>(parentEntities[i]);
                buff.Clear();
                parents.Add(parentEntities[i], new BuffAndDeltaMatrix
                {
                    children = buff,

                    delta = ComputeTransitionMatrix(parentDeltaMatrices[i].Value.ToMatrix(), parentTransforms[i].ToMatrix())

                    //delta = ComputeDeltaMatrix(parentDeltaMatrices[i].Value, parentTransforms[i]),
                    //oldParentTransform = parentDeltaMatrices[i].Value.ToMatrix(),
                    
                    
                    //newParentTransform = parentTransforms[i] //parentTransform = parentTransforms[i],deltapos=deltapos,deltarot=deltarot, deltascale = 1f };// delta = parentTransforms[i].ToMatrix() - parentDeltaMatrices[i].Value };
                });
            }
            NativeArray<Entity> childEntities = childrenQuery.ToEntityArray(Allocator.Temp);
            NativeArray<LocalTransform> childTransforms = childrenQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            NativeArray<ImmitatedParentReference> parentReference = childrenQuery.ToComponentDataArray<ImmitatedParentReference>(Allocator.Temp);
            for (int i = 0; i < childEntities.Length; i++)
            {
                if (parentReference[i].Value != null)
                {
                    //Debug.Log("a");
                    if(parents.TryGetValue(parentReference[i].Value, out BuffAndDeltaMatrix info))
                    {
                        if (!info.newdelta)
                        {
                            if (parentReference[i].oldlyParented)
                            {
                                //var parentTransform = state.EntityManager.GetComponentData<LocalTransform>(parentReference[i].Value);
                                ecb.SetComponent(childEntities[i], ApplyDeltaMatrix(info.delta, childTransforms[i]));



                                //ecb.SetComponent(childEntities[i], ApplyDeltaMatrix(info.delta, childTransforms[i], info.oldParentTransform));//UpdateChildTransform(childTransforms[i], info.oldParentTransform, info.newParentTransform));// info.deltapos, info.deltarot, info.deltascale));
                            }

                        }

                        ecb.SetComponent<ImmitatedParentReference>(childEntities[i], new ImmitatedParentReference
                        {
                            Value = parentReference[i].Value,
                            previousValue = parentReference[i].Value,
                            oldlyParented = true,
                        });

                        info.children.Add(new ImmitatedChild {Value= childEntities[i]});
                    }
                    else
                    {
                        if (!state.EntityManager.HasComponent<LocalTransform>(parentReference[i].Value))
                        {
                            continue;
                        }
                        LocalTransform parentTransform = state.EntityManager.GetComponentData<LocalTransform>(parentReference[i].Value);
                        ImmitatedDeltaMatrix immitatedDeltaMatrix = new ImmitatedDeltaMatrix { Value = parentTransform };// deltapos= parentTransform .Position, deltaquaternion = parentTransform.Rotation, deltascale = 1f};//Value = parentTransform.ToMatrix() };
                        DynamicBuffer<ImmitatedChild> buff = ecb.AddBuffer<ImmitatedChild>(parentReference[i].Value);
                        buff.Add(new ImmitatedChild { Value = childEntities[i] });
                        parents.Add(parentReference[i].Value, new BuffAndDeltaMatrix
                        {
                            children = buff,
                            delta = parentTransform.ToMatrix(),
                            //newParentTransform = parentTransform, 
                            newdelta = true
                        });//parentTransform = parentTransform, newdelta = true });
                        ecb.SetComponent<ImmitatedParentReference>(childEntities[i], new ImmitatedParentReference
                        {
                            Value = parentReference[i].Value,
                            previousValue = parentReference[i].Value,
                            oldlyParented = true,
                        });
                        ecb.AddComponent(parentReference[i].Value, immitatedDeltaMatrix);

                    }
                }
                else
                {
                    if (parentReference[i].previousValue != null)
                    {
                        if (parents.TryGetValue(parentReference[i].previousValue, out BuffAndDeltaMatrix info))
                        {
                            if (!info.newdelta)
                            {
                                if (parentReference[i].oldlyParented)
                                {

                                    ecb.SetComponent(childEntities[i], ApplyDeltaMatrix(info.delta, childTransforms[i]));
                                    //ecb.SetComponent(childEntities[i], ApplyDeltaMatrix(info.delta, childTransforms[i], info.oldParentTransform));//UpdateChildTransform(childTransforms[i], info.oldParentTransform, info.newParentTransform));
                                                                                                                             //ecb.SetComponent(childEntities[i], ApplyParentTransform(childTransforms[i], info.deltapos, info.deltarot, info.deltascale));
                                                                                                                             //ecb.SetComponent<LocalTransform>(childEntities[i], SetLocalTransformFromMatrix(childTransforms[i].ToMatrix() + info.delta));
                                }

                            }

                            ecb.SetComponent<ImmitatedParentReference>(childEntities[i], new ImmitatedParentReference
                            {
                                Value = parentReference[i].Value,
                                previousValue = parentReference[i].previousValue,
                                oldlyParented = true,
                            });

                            info.children.Add(new ImmitatedChild { Value = childEntities[i] });
                        }
                        else
                        {
                            if (!state.EntityManager.HasComponent<LocalTransform>(parentReference[i].Value))
                            {
                                continue;
                            }
                            LocalTransform parentTransform = state.EntityManager.GetComponentData<LocalTransform>(parentReference[i].previousValue);
                            ImmitatedDeltaMatrix immitatedDeltaMatrix = new ImmitatedDeltaMatrix { Value = parentTransform };// deltapos = parentTransform.Position, deltaquaternion = parentTransform.Rotation, deltascale = 1f };//Value = parentTransform.ToMatrix() };
                            DynamicBuffer<ImmitatedChild> buff = ecb.AddBuffer<ImmitatedChild>(parentReference[i].previousValue);
                            buff.Add(new ImmitatedChild { Value = childEntities[i] });
                            parents.Add(parentReference[i].Value, new BuffAndDeltaMatrix { 
                                children = buff,

                                //newParentTransform = parentTransform, 
                                delta = parentTransform.ToMatrix(),

                                newdelta = true });// parentTransform = parentTransform, newdelta = true });
                            ecb.SetComponent<ImmitatedParentReference>(childEntities[i], new ImmitatedParentReference
                            {
                                Value = parentReference[i].Value,
                                previousValue = parentReference[i].previousValue,
                                oldlyParented = true,
                            });
                            ecb.AddComponent(parentReference[i].previousValue, immitatedDeltaMatrix);
                        }
                    }
                    else
                    {

                    }
                }
            }


            var keys = parents.GetKeyArray(Allocator.Temp);

            for (int i  = 0; i < keys.Length; i++)
            {
                var Value = parents[keys[i]];
                if (Value.children.Length == 0)
                {
                    ecb.RemoveComponent<ImmitatedDeltaMatrix>(keys[i]);
                }
                else if(!Value.newdelta)
                {
                    LocalTransform newParentTransform = state.EntityManager.GetComponentData<LocalTransform>(keys[i]);
                    ecb.SetComponent<ImmitatedDeltaMatrix>(keys[i], new ImmitatedDeltaMatrix { Value = newParentTransform }); //deltapos = newParentTransform.Position, deltaquaternion = newParentTransform.Rotation, deltascale = 1f });//Value = parentTransform.ToMatrix() };
                }
                else
                {
                    //ecb.AddComponent(parentReference[i].previousValue, new ImmitatedDeltaMatrix { Value = Value.delta });
                }
            }


            ecb.Playback(state.EntityManager);

        }
        public static LocalTransform Float4x4ToLocalTransform(float4x4 matrix)
        {
            float3 position = matrix.c3.xyz; // Extract position from the last column
            quaternion rotation = new quaternion(matrix); // Extract rotation
            float scale = math.length(new float3(
                math.length(matrix.c0.xyz),
                math.length(matrix.c1.xyz),
                math.length(matrix.c2.xyz))
            ); // Extract scale by measuring the column vectors' lengths

            return LocalTransform.FromPositionRotationScale(position, rotation, scale);
        }
    }

}