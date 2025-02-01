using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.NetCode;

namespace NGPTemplate.Components
{
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct LastProcessedLookDirection : IInputComponentData
    {
        public bool ready;
        public quaternion Value;
    }
}