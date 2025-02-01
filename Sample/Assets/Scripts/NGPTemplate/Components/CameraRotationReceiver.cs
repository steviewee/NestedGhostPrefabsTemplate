using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.NetCode;

namespace NGPTemplate.Components
{
    [GhostComponent]
    public struct CameraRotationReceiver : IComponentData
    {
        [GhostField]
        public bool receiveRot;
        [GhostField]
        public quaternion rotation;
        [GhostField]
        public bool receiveOffset;
        [GhostField]
        public float2 offset;
        [GhostField]
        public Entity pivot;
    }
}