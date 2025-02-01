using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using NGPTemplate.Components;

namespace NGPTemplate.Authoring
{
    public class CameraRotationReceiverAuthoring : MonoBehaviour
    {
        public bool receiveRot;
        public bool receiveOffset;
        public GameObject pivot;
        public class Baker : Baker<CameraRotationReceiverAuthoring>
        {
            public override void Bake(CameraRotationReceiverAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                Entity pivot = GetEntity(authoring.pivot, TransformUsageFlags.Dynamic);
                AddComponent(entity, new CameraRotationReceiver
                {
                    receiveRot = authoring.receiveRot,
                    receiveOffset = authoring.receiveOffset,
                    pivot= pivot
                });
            }
        }
    }



}
