using System.Collections;
using System.Collections.Generic;
using Unity.Transforms;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using NGPTemplate.Components;
namespace NGPTemplate.Authoring
{

    public class PrefabScalerAuthoring : MonoBehaviour
    {
        
        public Vector3 scale = Vector3.one;
        
        public class PrefabScalerBaker : Baker<PrefabScalerAuthoring>
        {
            public override void Bake(PrefabScalerAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                if (authoring.scale != Vector3.one)
                {
                    //Debug.Log($"{authoring.scale} a");
                    Matrix4x4 scaleMatrix = Matrix4x4.Scale(authoring.scale);
                    AddComponent(entity, new PostTransformMatrix
                    {
                        Value = scaleMatrix,
                    });
                }

            }
        }
        public void CopyScale()
        {
            scale = transform.localScale;
        }
        public void PasteScale()
        {
            transform.localScale = scale;
        }
        public void ResetTransformScale()
        {
            transform.localScale = Vector3.one;
        }
        public void ResetCopiedScale()
        {
            scale = Vector3.one;
        }

    }
    /*
    [UpdateInGroup(typeof(PreBakingSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial class PrefabScalerCleanupBaking : SystemBase
    {
        private EntityQuery m_PreviouslyBakedEntities;

        public static ComponentTypeSet PreSpawnedGhostsComponents = new ComponentTypeSet(new ComponentType[]
        {
            typeof(PrefabScalerBakedBefore),
            typeof(PostTransformMatrix)
        });

        protected override void OnCreate()
        {
            base.OnCreate();

            // Query to get all the child entities baked before
            m_PreviouslyBakedEntities = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PrefabScalerBakedBefore>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab
            });
            
        }

        protected void RevertPreviousBakings()
        {
            Debug.Log(m_PreviouslyBakedEntities.CalculateEntityCount());

            EntityManager.RemoveComponent(m_PreviouslyBakedEntities, PreSpawnedGhostsComponents);
        }

        protected override void OnUpdate()
        {

            // Remove the components added by the baker for the entities not contained in hashToEntity
            RevertPreviousBakings();
        }
    }
    */
    /*
    [TemporaryBakingType]
    public struct PrefabScalerComponent : IComponentData
    {
        public float4x4 Value;
    }
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct PrefabScalerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PrefabScalerComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var cmdBuffer = new EntityCommandBuffer(Allocator.Temp);
            //QueryEnumerableWithEntity<RefRO<PrefabScalerComponent>> query = SystemAPI.Query<RefRO<PrefabScalerComponent>>().WithEntityAccess();
            //foreach (var (psc, entity) in query)
            //{
            foreach (var (psc, entity) in SystemAPI.Query<RefRO<PrefabScalerComponent>>().WithEntityAccess())
            {
                cmdBuffer.AddComponent<PostTransformMatrix>(entity, new PostTransformMatrix
                {
                    Value = psc.ValueRO.Value,
                });
                cmdBuffer.RemoveComponent<PrefabScalerComponent>(entity);
            }
            cmdBuffer.Playback(state.EntityManager);
            cmdBuffer.Dispose();
        }
    }
    */
}


                /*
                state.EntityManager.AddComponentData<PostTransformMatrix>(entity, new PostTransformMatrix
                {
                    Value = psc.ValueRO.Value,
                });
                state.EntityManager.RemoveComponent<PrefabScalerComponent>(entity);
                */
            