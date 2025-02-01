using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using NGPTemplate.Components;

namespace NGPTemplate.Authoring
{
    public class CameraInputAuthoring : MonoBehaviour
    {
        public class Baker : Baker<CameraInputAuthoring>
        {
            public override void Bake(CameraInputAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new LastProcessedLookDirection
                {
                    Value = quaternion.identity
                });
                AddComponent(entity, new LastKnownTargetOrientation
                {

                });
                AddComponent(entity, new LookDirectionInput
                {

                });
            }
        }
    }



}
